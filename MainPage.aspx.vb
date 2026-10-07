Imports System.Data
Imports System.Globalization
Imports System.Collections
Imports System.Collections.Generic
Imports System.Linq
Imports System.Web
Imports System.Web.Caching
Imports System.Web.UI.HtmlControls


Partial Class MainPage
    Inherits System.Web.UI.Page

    Private Shared ReadOnly BadgeColors As String() = {"badge-blue", "badge-green", "badge-orange", "badge-purple", "badge-teal", "badge-pink"}
    Private Const ConfirmationPopupReturnKey As String = "AddAdjustmentAndClose"
    Private Const OpenActionPopupReturnKey As String = "OpenActionAndClose"
    Private encryNdecry As New EncryDecry

    ''' <summary>
    ''' Per-request cache for GetAvailableActions(). The permission set for a user
    ''' only varies by USER_ID and PROJECT_ID (it covers every STATE_ID for that
    ''' project in one shot), so this is keyed by "ProjectID|UserID" and holds the
    ''' full unfiltered DataTable of (PROJECT_ID, STATE_ID, ACTION_ID, ICON,
    ''' PERMISSION_NAME, STATUS_NAME, STATUS_SUBTITLE) rows for that project/user.
    ''' GetAvailableActions() then filters that table in memory by the row's own
    ''' status name. Without this, binding a grid with
    ''' N rows would fire N identical DB round-trips - and RepopulateGridActionsIfNeeded()
    ''' would repeat that on EVERY postback, even ones unrelated to the grid.
    ''' </summary>
    Private ReadOnly _projectActionsCache As New Dictionary(Of String, DataTable)

    ''' <summary>
    ''' Shared, cross-user, cross-request cache for slow-changing lookup/config data
    ''' (column aliases, visible-field lists, unit type, project view names) that is
    ''' identical for every user viewing the same project. loadData() re-runs on every
    ''' Approve/Bounce/DropDownList change, so without this cache those config queries
    ''' would be re-fetched from the DB on every single one of those postbacks even
    ''' though the underlying config almost never changes.
    ''' </summary>
    Private Shared Function GetOrAddToCache(Of T As Class)(cacheKey As String,
                                                            expirationMinutes As Double,
                                                            factory As Func(Of T)) As T
        Dim cached As T = TryCast(HttpRuntime.Cache.Get(cacheKey), T)
        If cached IsNot Nothing Then Return cached

        Dim fresh As T = factory()
        HttpRuntime.Cache.Insert(cacheKey,
                                  fresh,
                                  Nothing,
                                  DateTime.Now.AddMinutes(expirationMinutes),
                                  Cache.NoSlidingExpiration)
        Return fresh
    End Function

    ''' <summary>
    ''' Clears both Session and the ASP.NET Cache when MainPage is freshly loaded (not
    ''' on postback - see Page_Load).
    ''' - Session.Clear() drops THIS visitor's own leftovers - e.g.
    '''   Session("FilterMainTable"), Session("CheckedCheckBox"), Session("Result"),
    '''   Session("PopupParams_...") - from a previous visit.
    ''' - The Cache sweep below drops every entry HttpRuntime.Cache is holding, not
    '''   just this file's own config caches (UnitType_/ProjectView_/ProjectViewName_/
    '''   VisibleFields_/ColumnAliases_ per project, plus the global UnitStatuses
    '''   lookup - see GetOrAddToCache's callers). ASP.NET's Cache is shared
    '''   APPLICATION-WIDE across every user and every page, unlike Session, so this
    '''   also evicts anything any other page in the app may be caching. The very next
    '''   request anywhere - this user or another, this page or another - re-fetches
    '''   from the DB instead of getting a cache hit, which is exactly what
    '''   GetOrAddToCache exists to avoid. Only wire this in where that trade-off is
    '''   genuinely wanted.
    ''' </summary>
    Private Sub ClearSessionAndCache()
        Session.Clear()

        Dim cache As System.Web.Caching.Cache = HttpRuntime.Cache
        Dim keysToRemove As New List(Of String)

        Dim enumerator As IDictionaryEnumerator = cache.GetEnumerator()
        While enumerator.MoveNext()
            keysToRemove.Add(CStr(enumerator.Key))
        End While

        For Each key As String In keysToRemove
            cache.Remove(key)
        Next
    End Sub

    'Dim DT As DataTable
    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        If Not Page.IsPostBack Then
            ClearSessionAndCache()

            PopulateDropDownList(ddl:=DropDownList1,
                                 sql:="Select PROJECT_NAME_EN,PROJECT_ID from UNITSHUB_PROJECTS",
                                 DataConnection:=EBDB_CS,
                                 firstItemIs:="Select A Project")
        End If

        VendorPopupHelper.RegisterVendorPopup(Me,
                                      lnkOpenAction,
                                      "ProjectStatusAndAction.aspx",
                                      1000, 0,
                                      PopupPlacement.Center,
                                      "",
                                      VendorPopupHelper.PopupDisplayMode.FrameOnly,
                                      returnKey:=OpenActionPopupReturnKey)

        'lnkConfirmation

        VendorPopupHelper.RegisterVendorPopup(Me,
                                      lnkConfirmation,
                                      "ReserveUnit.aspx",
                                      850, 650,
                                      PopupPlacement.Center,
                                      "",
                                      VendorPopupHelper.PopupDisplayMode.FrameOnly,
                                      returnKey:=ConfirmationPopupReturnKey)

        VendorPopupHelper.RegisterVendorPopup(Me,
                                      Button1,
                                      "DisplayInvoice.aspx?PDF=27147",
                                      1000, 0,
                                      PopupPlacement.Center,
                                      "",
                                      VendorPopupHelper.PopupDisplayMode.FrameOnly,
                                      returnKey:=OpenActionPopupReturnKey)



        PopulateSideMenu()
        RepopulateGridActionsIfNeeded()
    End Sub

    ''' <summary>
    ''' Builds the right-side slide-in menu from the ISSIDE menu definition table.
    ''' Rows of TYPE "TITLE" render as collapsible group headers; rows of TYPE "LINK"
    ''' render as clickable items nested under the group whose INDX matches their
    ''' PARENT. Only rows whose VISIBLETO list contains the current user's role (or
    ''' "ALL") are included. Groups with no visible children are omitted entirely.
    ''' Rebuilt on every request (like the grid's action placeholders) since the
    ''' PlaceHolder's dynamically-added children aren't preserved by ViewState.
    ''' </summary>
    Private Sub PopulateSideMenu()
        phSideMenu.Controls.Clear()

        Dim dt As New DataTable
        dt.Columns.Add("TITLE")
        dt.Columns.Add("URL")
        dt.Columns.Add("WIDTH", GetType(Integer))
        dt.Columns.Add("HEIGHT", GetType(Integer))
        dt.Columns.Add("ISSIDE")
        dt.Columns.Add("VISIBLETO")
        dt.Columns.Add("TYPE")
        dt.Columns.Add("INDX")
        dt.Columns.Add("PARENT")
        ' Optional, for links that open through VendorPopupHelper instead of openMenuLink:
        '   POPUP = "Y", POPUPTITLE = the popup's title, RETURNKEY = the popup's return key ("" = none)
        dt.Columns.Add("POPUP")
        dt.Columns.Add("POPUPTITLE")
        dt.Columns.Add("RETURNKEY")

        'dt.Rows.Add("EMAIL TODAY", "", 800, 550, "Y", "ADM", "TITLE", "1", "")
        dt.Rows.Add("CUSTOMER", "", 800, 550, "Y", "ADM", "TITLE", "1", "")

        'dt.Rows.Add("Events Control", "", 800, 550, "Y", "ADM", "TITLE", "3", "")
        'dt.Rows.Add("Charts and Statistics", "", 800, 550, "Y", "ADM", "TITLE", "4", "")
        Dim MemberListParameters As New clsListProperties
        With MemberListParameters
            .ItemsSQL = "Select ID, NAME, NATIONALID from UNITSHUB_CONTACTS "
            .CheckedItemsSQL = ""
            .FormTitle = "Select Customer"
            .ColumnHideAndShow = "YNN"
            .EditableColumns = "NNN"
            .ColumnsWidth = New Double() {3, 1}
            .HoverableList = "Y"
        End With
        Dim SelectMembersParameters As String = encryNdecry.EncryptObject(Of clsListProperties)(MemberListParameters)
        ' "Maintain A Customer" = pick an existing customer from the list
        dt.Rows.Add("Maintain A Customer",
                    "SelectOneItemFromListMultiColumns.aspx?Parameters=" & Server.UrlEncode(SelectMembersParameters),
                    400, 500, "Y", "ADM", "LINK", "1.1", "1",
                    "Y", "Select Adj", "SelectedCustomer")
        ' "Add New Customer" = empty ContactMaintenance form for a new customer
        dt.Rows.Add("Add New Customer",
                    "ContactMaintenance.aspx?mode=New&isDialogue=Yes",
                    950, 750, "Y", "ADM", "LINK", "1.2", "1",
                    "Y", "", "")

        dt.Rows.Add("AUTOTRANSFER PLANS", "", 800, 550, "Y", "ADM", "TITLE", "2", "")
        dt.Rows.Add("Autotransfer Plans",
                    "AutoTransferPlanForm.aspx",
                    950, 0, "Y", "ADM", "LINK", "2.1", "2",
                    "Y", "", "")


        'dt.Rows.Add("Bands Control", "frmInsertEditBand.aspx", 800, 550, "Y", "ADM", "LINK", "2.1", "2")
        'dt.Rows.Add("Packages Control", "frmPackages.aspx", 800, 550, "Y", "ADM", "LINK", "2.2", "2")
        'dt.Rows.Add("Uploadees Control", "frmUploadees.aspx", 800, 550, "Y", "ADM", "LINK", "2.3", "2")
        'dt.Rows.Add("User Management", "frmUsers.aspx", 800, 550, "Y", "ADM,ALL", "LINK", "2.4", "2")
        'dt.Rows.Add("Vacations Control", "frmVacations.aspx", 800, 550, "Y", "ADM", "LINK", "2.5", "2")

        'dt.Rows.Add("Requested-By-Me OnGoing", "frmStateActionsUsersPackages.aspx", 800, 550, "Y", "ADM,FCD", "LINK", "3.1", "3")
        'dt.Rows.Add("Requested-From-Me OnGoing", "frmStateActionsUsersPackagesRequestedFromMe.aspx", 800, 550, "Y", "ADM", "LINK", "3.2", "3")
        'dt.Rows.Add("Upcoming Events", "frmUpcomingsAuthorities.aspx", 800, 550, "Y", "ADM", "LINK", "3.3", "3")

        'dt.Rows.Add("Dashboard", "frmDashboard.aspx", 800, 550, "Y", "ADM,FCD,ALL", "LINK", "4.1", "4")

        Dim currentRole As String = GetCurrentUserRole()

        Dim titleRows = dt.AsEnumerable().
            Where(Function(r) String.Equals(r.Field(Of String)("TYPE"), "TITLE", StringComparison.OrdinalIgnoreCase) _
                        AndAlso IsVisibleToRole(r.Field(Of String)("VISIBLETO"), currentRole)).
            OrderBy(Function(r) ParseIndx(r.Field(Of String)("INDX")))

        Dim sb As New System.Text.StringBuilder

        For Each titleRow As DataRow In titleRows
            Dim titleIndx As String = titleRow.Field(Of String)("INDX")

            Dim childRows = dt.AsEnumerable().
                Where(Function(r) String.Equals(r.Field(Of String)("TYPE"), "LINK", StringComparison.OrdinalIgnoreCase) _
                            AndAlso r.Field(Of String)("PARENT") = titleIndx _
                            AndAlso IsVisibleToRole(r.Field(Of String)("VISIBLETO"), currentRole)).
                OrderBy(Function(r) ParseIndx(r.Field(Of String)("INDX"))).ToList()

            If childRows.Count = 0 Then Continue For

            sb.Append("<div class=""sideMenuGroup"">")
            sb.Append("<div class=""sideMenuGroupHeader"" onclick=""toggleSideMenuGroup(this)"">")
            sb.Append("<span>").Append(Server.HtmlEncode(titleRow.Field(Of String)("TITLE"))).Append("</span>")
            sb.Append("<span class=""sideMenuGroupArrow"">&#9662;</span>")
            sb.Append("</div>")
            sb.Append("<div class=""sideMenuGroupItems"">")

            For Each childRow As DataRow In childRows
                Dim url As String = childRow.Field(Of String)("URL")
                Dim width As Integer = childRow.Field(Of Integer)("WIDTH")
                Dim height As Integer = childRow.Field(Of Integer)("HEIGHT")

                If String.Equals(childRow.Field(Of String)("POPUP"), "Y", StringComparison.OrdinalIgnoreCase) Then
                    ' VendorPopupHelper needs a server control: flush the HTML so far, then add
                    ' a LinkButton (same look) and register its popup
                    phSideMenu.Controls.Add(New LiteralControl(sb.ToString()))
                    sb.Clear()
                    AddSideMenuPopupLink(childRow, url, width, height)
                Else
                    sb.Append("<a class=""sideMenuItem"" href=""javascript:void(0);"" onclick=""openMenuLink('") _
                      .Append(Server.HtmlEncode(url)).Append("', ").Append(width).Append(", ").Append(height).Append("); return false;"">") _
                      .Append(Server.HtmlEncode(childRow.Field(Of String)("TITLE"))) _
                      .Append("</a>")
                End If
            Next

            sb.Append("</div>")
            sb.Append("</div>")
        Next

        phSideMenu.Controls.Add(New LiteralControl(sb.ToString()))
    End Sub

    ''' <summary>
    ''' One side-menu link that opens through VendorPopupHelper (POPUP = "Y"). Added on every
    ''' request from Page_Load (PopulateSideMenu), with a fixed ID per INDX, so it exists for
    ''' its popup and for the postback when the popup returns.
    ''' </summary>
    Private Sub AddSideMenuPopupLink(Row As DataRow, Url As String, Width As Integer, Height As Integer)
        Dim lnk As New LinkButton()
        lnk.ID = "lnkSideMenu_" & Convert.ToString(Row("INDX")).Replace(".", "_")
        lnk.CssClass = "sideMenuItem"
        lnk.CausesValidation = False
        lnk.Text = Server.HtmlEncode(Convert.ToString(Row("TITLE")))
        phSideMenu.Controls.Add(lnk)

        ' When the customer list closes with a choice, VendorPopupHelper posts back to this
        ' link: open the chosen customer in ContactMaintenance
        If String.Equals(Convert.ToString(Row("RETURNKEY")), SelectedCustomerReturnKey, StringComparison.OrdinalIgnoreCase) Then
            AddHandler lnk.Click, AddressOf SelectCustomerLink_Click
        End If

        Dim popupTitle As String = Convert.ToString(Row("POPUPTITLE"))
        Dim returnKey As String = Convert.ToString(Row("RETURNKEY"))

        If returnKey <> "" Then
            VendorPopupHelper.RegisterVendorPopup(Me,
                                          lnk,
                                          Url,
                                          Width,
                                          Height,
                                          PopupPlacement.Center,
                                          popupTitle,
                                          VendorPopupHelper.PopupDisplayMode.FrameOnly,
                                          returnKey)
        Else
            VendorPopupHelper.RegisterVendorPopup(Me,
                                          lnk,
                                          Url,
                                          Width,
                                          Height,
                                          PopupPlacement.Center,
                                          popupTitle,
                                          VendorPopupHelper.PopupDisplayMode.FrameOnly)
        End If
    End Sub

    Private Const SelectedCustomerReturnKey As String = "SelectedCustomer"

    ''' <summary>
    ''' "Maintain A Customer": runs when SelectOneItemFromListMultiColumns closes with a
    ''' customer chosen. Reads the chosen row (returned under "SelectedCustomer") and opens
    ''' ContactMaintenance.aspx?mode=Edit&amp;ID=&lt;ID&gt;&amp;isDialogue=Yes for it.
    ''' Nothing happens if the list was closed without a choice.
    ''' </summary>
    Private Sub SelectCustomerLink_Click(sender As Object, e As EventArgs)
        Dim selectedItems As List(Of Dictionary(Of String, Object)) =
            TryCast(VendorPopupHelper.GetPopupReturnValue(Me, SelectedCustomerReturnKey),
                    List(Of Dictionary(Of String, Object)))
        If selectedItems Is Nothing OrElse selectedItems.Count = 0 Then Exit Sub

        ' The list's first column is ID (ItemsSQL: Select ID, NAME, NATIONALID ...)
        Dim item As Dictionary(Of String, Object) = selectedItems(0)
        Dim contactId As String = ""
        If item.ContainsKey("ID") Then
            contactId = Convert.ToString(item("ID")).Trim()
        ElseIf item.Count > 0 Then
            contactId = Convert.ToString(item.Values.First()).Trim()
        End If
        If contactId = "" Then Exit Sub

        OpenContactMaintenance(contactId)
    End Sub

    ''' <summary>
    ''' Opens ContactMaintenance for a customer, in Edit mode, as a popup. VendorPopupHelper
    ''' only opens popups from a click, so a hidden link is registered with the URL and then
    ''' clicked by a startup script (after the helper's own script has wired it up).
    ''' </summary>
    Private Sub OpenContactMaintenance(ContactId As String)
        Dim lnkOpen As New LinkButton()
        lnkOpen.ID = "lnkOpenSelectedCustomer"
        lnkOpen.CausesValidation = False
        lnkOpen.Style("display") = "none"
        phSideMenu.Controls.Add(lnkOpen)

        VendorPopupHelper.RegisterVendorPopup(Me,
                                      lnkOpen,
                                      "ContactMaintenance.aspx?mode=Edit&ID=" & Server.UrlEncode(ContactId) & "&isDialogue=Yes",
                                      950,
                                      750,
                                      PopupPlacement.Center,
                                      "",
                                      VendorPopupHelper.PopupDisplayMode.FrameOnly)

        Dim script As String =
            "setTimeout(function () {" &
            "  var l = document.getElementById('" & lnkOpen.ClientID & "');" &
            "  if (l) { l.click(); }" &
            "}, 100);"
        ClientScript.RegisterStartupScript(Me.GetType(), "OpenSelectedCustomer", script, True)
    End Sub

    ''' <summary>
    ''' Returns True if the comma-separated VISIBLETO list contains the given role,
    ''' or the special value "ALL" (visible to every role). If no role could be
    ''' determined yet (GetCurrentUserRole returned blank), this fails OPEN and shows
    ''' everything rather than silently rendering an empty menu - remove the
    ''' String.IsNullOrEmpty(role) branch once GetCurrentUserRole is wired up for real.
    ''' </summary>
    Private Function IsVisibleToRole(visibleTo As String, role As String) As Boolean
        If String.IsNullOrEmpty(visibleTo) Then Return False
        If String.IsNullOrEmpty(role) Then Return True ' TEMP: no role source wired up yet

        Dim roles = visibleTo.Split(","c).Select(Function(x) x.Trim().ToUpperInvariant())
        Return roles.Contains("ALL") OrElse roles.Contains(role.ToUpperInvariant())
    End Function

    ''' <summary>
    ''' Parses an INDX value ("1", "2.3", ...) into a sortable Decimal.
    ''' </summary>
    Private Function ParseIndx(indx As String) As Decimal
        Dim result As Decimal
        If Decimal.TryParse(indx, Global.System.Globalization.NumberStyles.Any, Global.System.Globalization.CultureInfo.InvariantCulture, result) Then
            Return result
        End If
        Return 0
    End Function

    ''' <summary>
    ''' TODO: Wire this to your actual authentication/session mechanism.
    ''' Currently reads a "UserType" session value (e.g. "ADM", "FCD") set at login.
    ''' </summary>
    Private Function GetCurrentUserRole() As String
        Return Convert.ToString(Session("UserType"))
    End Function

    ''' <summary>
    ''' TODO: Wire this to your actual authentication/session mechanism, same as
    ''' GetCurrentUserRole(). Currently reads a "UserID" session value set at login,
    ''' falling back to the previously hardcoded test user ('2271') so behavior is
    ''' unchanged until real session wiring is in place - remove the fallback once
    ''' Session("UserID") is reliably populated at login.
    ''' </summary>
    Private Function GetCurrentUserID() As String
        Dim sessionUserID As String = Convert.ToString(Session("UserID"))
        If String.IsNullOrEmpty(sessionUserID) Then
            Return "2271" ' TEMP: fallback while real auth/session wiring is pending
        End If
        Return sessionUserID
    End Function

    ''' <summary>
    ''' GridView4 (the Actions menu inside each row) is bound dynamically in
    ''' RowDataBound, which only fires when GridView1.DataBind() runs (e.g. from
    ''' loadData()). Postbacks that don't rebind the grid - like the Columns
    ''' dialog's btnColumns_Click, which only toggles cell Visible flags - still
    ''' reconstruct the grid's declared row/cell/control structure from ViewState,
    ''' but a nested GridView's own rows aren't preserved that way since they came
    ''' from a DataBind, not markup. Left alone, GridView4 ends up empty on any
    ''' such postback. Re-binding it here, on every postback before
    ''' RaisePostBackEvent, keeps its rows present (and able to raise their own
    ''' postback events, e.g. clicking an action) regardless of which control
    ''' triggered the postback. DataKeyNames is used instead of DataItem, which is
    ''' only populated during an actual DataBind.
    ''' </summary>
    Private Sub RepopulateGridActionsIfNeeded()
        If GridView1.Rows Is Nothing OrElse GridView1.Rows.Count = 0 Then Exit Sub

        For Each row As GridViewRow In GridView1.Rows
            If row.RowType <> DataControlRowType.DataRow Then Continue For

            Dim GV As GridView = TryCast(row.FindControl("GridView4"), GridView)
            If GV Is Nothing OrElse GV.Rows.Count > 0 Then Continue For

            Dim RequestID As String = Convert.ToString(GridView1.DataKeys(row.RowIndex)("Reference"))
            Dim State As String = Convert.ToString(GridView1.DataKeys(row.RowIndex)("STATUS"))
            Dim NodeId As String = Convert.ToString(GridView1.DataKeys(row.RowIndex)("NodeId"))
            Dim StateId As String = Convert.ToString(GridView1.DataKeys(row.RowIndex)("StateId"))
            Dim SubStateId As String = Convert.ToString(GridView1.DataKeys(row.RowIndex)("SubStateId"))

            Dim Actions As List(Of WorkflowAction) = GetAvailableActions(RequestID, StateId, SubStateId)

            GV.DataSource = Actions
            GV.DataBind()
        Next
    End Sub

    Private Sub PopulateDropDownList(ByRef ddl As DropDownList,
                                     sql As Object,
                                     DataConnection As String,
                                     Optional firstItemIs As String = "",
                                     Optional SelectedItemIs As String = "")

        PF.PopulateDropDownList(ddl, sql, DataConnection, firstItemIs, SelectedItemIs)
    End Sub

    Public Function AnalyzeTable(dt As DataTable) As List(Of ColumnStatistics)

        Dim Results As New List(Of ColumnStatistics)

        Dim Distinct As New Dictionary(Of String, HashSet(Of String))
        Dim Frequency As New Dictionary(Of String, Dictionary(Of String, Integer))
        Dim LengthTotals As New Dictionary(Of String, Integer)

        'Initialize
        For Each c As DataColumn In dt.Columns

            Dim S As New ColumnStatistics

            S.ColumnName = c.ColumnName
            S.DataType = c.DataType
            S.TotalRows = dt.Rows.Count

            Results.Add(S)

            Distinct(c.ColumnName) = New HashSet(Of String)
            Frequency(c.ColumnName) = New Dictionary(Of String, Integer)
            LengthTotals(c.ColumnName) = 0

        Next

        'Scan the table ONCE
        For Each r As DataRow In dt.Rows

            For Each S In Results

                Dim Value = r(S.ColumnName)

                If Value Is DBNull.Value Then

                    S.ContainsNulls = True
                    Continue For

                End If

                S.NonNullRows += 1

                Dim txt As String = Value.ToString.Trim

                Distinct(S.ColumnName).Add(txt)

                Dim L As Integer = txt.Length

                LengthTotals(S.ColumnName) += L

                If L > S.MaxLength Then S.MaxLength = L
                If L < S.MinLength Then S.MinLength = L

                'Frequency
                If Frequency(S.ColumnName).ContainsKey(txt) Then
                    Frequency(S.ColumnName)(txt) += 1
                Else
                    Frequency(S.ColumnName).Add(txt, 1)
                End If

                'Numeric
                Dim N As Double
                If Double.TryParse(txt, NumberStyles.Any, CultureInfo.InvariantCulture, N) Then

                    S.ContainsNumbers = True
                    S.NumericCount += 1

                    If Not S.MinNumber.HasValue OrElse N < S.MinNumber Then
                        S.MinNumber = N
                    End If

                    If Not S.MaxNumber.HasValue OrElse N > S.MaxNumber Then
                        S.MaxNumber = N
                    End If

                End If

                'Date
                Dim D As Date

                If Date.TryParse(txt, D) Then

                    S.ContainsDates = True
                    S.DateCount += 1

                    If Not S.MinDate.HasValue OrElse D < S.MinDate Then
                        S.MinDate = D
                    End If

                    If Not S.MaxDate.HasValue OrElse D > S.MaxDate Then
                        S.MaxDate = D
                    End If

                End If

                'Boolean

                Select Case txt.ToUpper

                    Case "TRUE", "FALSE", "YES", "NO", "Y", "N", "1", "0"

                        S.ContainsBoolean = True
                        S.BooleanCount += 1

                End Select

            Next

        Next

        'Finalize
        For Each S In Results

            S.NullRows = S.TotalRows - S.NonNullRows

            If S.TotalRows > 0 Then
                S.NullPercent = S.NullRows / S.TotalRows
            End If

            S.DistinctValues = Distinct(S.ColumnName).Count

            If S.TotalRows > 0 Then
                S.DistinctRatio = S.DistinctValues / S.TotalRows
            End If

            If S.NonNullRows > 0 Then
                S.AverageLength = LengthTotals(S.ColumnName) / S.NonNullRows
            End If

            If S.MinLength = Integer.MaxValue Then
                S.MinLength = 0
            End If

            If Frequency(S.ColumnName).Count > 0 Then

                Dim Winner = Frequency(S.ColumnName).OrderByDescending(Function(x) x.Value).First()

                S.MostCommonValue = Winner.Key
                S.MostCommonCount = Winner.Value

            End If

            S.IsUnique = (S.DistinctValues = S.NonNullRows)

            S.IsLikelyPrimaryKey =
            S.IsUnique AndAlso
            S.ContainsNumbers AndAlso
            S.NullRows = 0

            S.SuggestedFilter = ChooseFilter(S)

        Next

        Return Results

    End Function

    Private Function ChooseFilter(S As ColumnStatistics) As FilterType

        If S.IsLikelyPrimaryKey Then
            Return FilterType.None
        End If

        If S.ContainsBoolean AndAlso S.DistinctValues <= 2 Then
            Return FilterType.CheckBox
        End If

        If S.ContainsDates Then
            Return FilterType.DateRange
        End If

        If S.ContainsNumbers Then

            If S.DistinctValues <= 20 Then
                Return FilterType.DropDownList
            End If

            Return FilterType.NumberRange

        End If

        If S.DistinctValues <= 5 Then
            Return FilterType.RadioButtonList
        End If

        If S.DistinctValues <= 30 Then
            Return FilterType.DropDownList
        End If

        If S.DistinctValues <= 300 Then
            Return FilterType.AutoComplete
        End If

        Return FilterType.TextBox

    End Function


    ''' <summary>
    ''' Field names that are already shown by a declared TemplateField/BoundField in
    ''' GridView1's markup, so the matching AutoGeneratedField (built from the DataSource
    ''' by AutoGenerateColumns="True") must be stripped out to avoid showing the value twice.
    ''' "Status" covers both "STATUS" and "Status" casings coming from different query paths
    ''' (comparison below is case-insensitive anyway, but keep the DataField name here).
    ''' "StateId"/"NodeId" aren't shown at all - they only exist to key the status
    ''' color/action lookups and the History action's Node_ID display by the real
    ''' STATE_ID/NODE_ID codes rather than display text.
    ''' </summary>
    Private Shared ReadOnly DuplicatedAutoGeneratedFields As String() = {"STATUS", "Status_Subtitle", "StateId", "SubStateId", "NodeId"}

    ''' <summary>
    ''' Runs after every single GridView1.DataBind() call, from wherever it's triggered
    ''' (loadData, ApplyFilterToGrid, or any future call site) - so removal of the
    ''' duplicate auto-generated columns can never be missed by forgetting to call it
    ''' manually after a particular DataBind(). AutoGeneratedField objects are rebuilt
    ''' on every DataBind() (they aren't persisted in ViewState like declared columns
    ''' are), which is exactly why this needs to run on the DataBound event itself
    ''' rather than just once at startup.
    ''' </summary>
    Private Sub GridView1_DataBound(sender As Object, e As EventArgs) Handles GridView1.DataBound
        RemoveDuplicateAutoGeneratedColumns(GridView1, DuplicatedAutoGeneratedFields)
    End Sub

    ''' <summary>
    ''' Removes auto-generated columns (from GridView1's AutoGenerateColumns="True" default)
    ''' whose DataField matches a name already shown by a declared TemplateField, so the
    ''' value doesn't appear twice.
    ''' </summary>
    Private Sub RemoveDuplicateAutoGeneratedColumns(gv As GridView, fieldNamesToRemove As IEnumerable(Of String))
        For i As Integer = gv.Columns.Count - 1 To 0 Step -1
            Dim bf As BoundField = TryCast(gv.Columns(i), BoundField)
            If bf Is Nothing Then Continue For

            For Each fieldName As String In fieldNamesToRemove
                If String.Equals(fieldName, bf.DataField, StringComparison.OrdinalIgnoreCase) Then
                    gv.Columns.RemoveAt(i)
                    Exit For
                End If
            Next
        Next
    End Sub

    Protected Sub DropDownList1_SelectedIndexChanged(sender As Object, e As EventArgs) Handles DropDownList1.SelectedIndexChanged
        Session("Result") = Nothing
        Session("CheckedCheckBox") = Nothing
        loadData()

    End Sub

    ''' <summary>
    ''' Returns the DISPLAY_ORDER configured in UNITSHUB_ATTRIBUTES for the "Status"
    ''' attribute. This is a single small admin-configured value that's the same for
    ''' every project/user and essentially never changes, so - like GetColumnAliases/
    ''' BuildPivotSql's aliases/VisibleFields lookups below - it's cached via
    ''' GetOrAddToCache rather than hit on every loadData() call (project switch,
    ''' Approve/Bounce, etc.), which would otherwise add a DB round-trip per postback
    ''' for a value that's effectively static.
    ''' </summary>
    Private Function GetStatusAttributeDisplayOrder(ProjectID As String, Node_Type As String) As String
        Dim sql1 As String = "SELECT DISPLAY_ORDER FROM UNITSHUB_ATTRIBUTES WHERE ATTRIBUTE_NAME = 'Status' " &
                " AND PROJECT_ID='" & ProjectID & "' and NODE_TYPE_ID='" & Node_Type & "'"
        Dim ID As String = DB.RetreiveScalarSTRING(EBDB, sql1)
        GetStatusAttributeDisplayOrder = ID

        'Return GetOrAddToCache(Of String)(
        '    "DisplayOrder_Status_Attribute", 30,
        '    Function()
        '        Dim sql As String = "SELECT DISPLAY_ORDER FROM UNITSHUB_ATTRIBUTES WHERE ATTRIBUTE_NAME = 'Status' " &
        '        " AND PROJECT_ID='" & ProjectID & "' and NODE_TYPE_ID='" & Node_Type & "'"
        '        Return Convert.ToString(DB.RetreiveScalarSTRING(EBDB, sql))
        '    End Function)
    End Function

    Sub loadData(Optional Filter As String = "")
        Dim SQL As String = ""
        Dim DT As New DataTable
        Dim lcProjectID As String = DropDownList1.SelectedItem.Value

        If lcProjectID = "##" Then
            GridView1.DataSource = Nothing
            GridView1.DataBind()

            GridView2.DataSource = Nothing
            GridView2.DataBind()

            DataList2.DataSource = Nothing
            DataList2.DataBind()

            Label2.Text = ""

            Exit Sub
        End If



        'Build the SQL to get units

        'Get "Units" of the Selected project: it is the lowest 
        SQL = SQL + vbCrLf + "SELECT NODE_TYPE_ID "
        SQL = SQL + vbCrLf + "FROM UNITSHUB_PROJECTSHIERARCHY "
        SQL = SQL + vbCrLf + "WHERE PROJECT_ID ='" & lcProjectID & "' "
        SQL = SQL + vbCrLf + "  AND TREELEVEL = ( "
        SQL = SQL + vbCrLf + "        SELECT MAX(TREELEVEL) "
        SQL = SQL + vbCrLf + "        FROM UNITSHUB_PROJECTSHIERARCHY "
        SQL = SQL + vbCrLf + "        WHERE PROJECT_ID ='" & lcProjectID & "' "
        SQL = SQL + vbCrLf + "      ) "
        Dim UnitTypeSql As String = SQL
        Dim UnitType As String = GetOrAddToCache(Of String)(
            "UnitType_" & lcProjectID, 10,
            Function() DB.RetreiveScalarSTRING(EBDB, UnitTypeSql))


        Dim aliases As New List(Of Tuple(Of Integer, String))
        aliases = GetColumnAliases(projectId:=lcProjectID, nodeTypeId:=UnitType)
        SQL = ""
        SQL = BuildPivotSql(aliases:=aliases, projectId:=lcProjectID, Node_Type_ID:=UnitType)

        '=============================================================
        ' Cached lookup (see GetStatusAttributeDisplayOrder) - runs once per app-cache
        ' window, not once per node/row, so it adds no measurable delay here even
        ' though loadData() itself re-runs on every project switch/Approve/Bounce.
        lblDisplayOrder.Text = GetStatusAttributeDisplayOrder(ProjectID:=DropDownList1.SelectedItem.Value,
                                                              Node_Type:=UnitType)


        Dim DT0 As New DataTable
        DT0 = DB.GetDataTable(EBDB, SQL)

        'add the table to session so i can use it in other forms
        '=========================================================
        Call AddTBLtoSession(DT0)


        '==================================================================================================
        'Dim selectedItems As List(Of Dictionary(Of String, Object)) = BuildSelectedItemsPayload(DT, selectedIds)

        'VendorPopupHelper.RegisterPopupSelectionAndClose(
        '    page:=Me,
        '    returnValue:=selectedItems,
        '    startupScriptKey:="SelectedItems",
        '    skipPostBack:=False)
        '==============================================================================================



        'Filter ther result 
        If Filter <> "" Then
            Dim DR() As DataRow = DT0.Select(Filter)
            DT0 = If(DR.Length > 0, DR.CopyToDataTable(), DT0.Clone())
        End If
        'Datatable to GridView =============================
        GridView1.DataSource = DT0
        GridView1.DataBind()
        ' Duplicate STATUS/Status_Subtitle auto-generated columns are stripped
        ' automatically by GridView1_DataBound (Handles GridView1.DataBound).

        'Build the SQL Again to get Project Info
        SQL = ""
        SQL = SQL + vbCrLf + " SELECT PROJECT_ID, "
        SQL = SQL + vbCrLf + "        TYPE_NAME, "
        SQL = SQL + vbCrLf + "        TREELEVEL, "
        SQL = SQL + vbCrLf + "        VIEWNAME "
        SQL = SQL + vbCrLf + " FROM ( "
        SQL = SQL + vbCrLf + "     SELECT uv.*, "
        SQL = SQL + vbCrLf + "            ROW_NUMBER() OVER ( "
        SQL = SQL + vbCrLf + "                PARTITION BY PROJECT_ID "
        SQL = SQL + vbCrLf + "                ORDER BY TREELEVEL DESC "
        SQL = SQL + vbCrLf + "            ) AS RN "
        SQL = SQL + vbCrLf + "     FROM UNITSHUB_VIEWS uv "
        SQL = SQL + vbCrLf + " ) "
        SQL = SQL + vbCrLf + " WHERE RN = 1 "
        SQL = SQL + vbCrLf + " and PROJECT_ID='@PRJID@' "
        SQL = SQL + vbCrLf + " ORDER BY PROJECT_ID "
        SQL = SQL.Replace("@PRJID@", lcProjectID)
        'Get the View
        Dim ViewSql As String = SQL
        Dim View As DataTable = GetOrAddToCache(Of DataTable)(
            "ProjectView_" & lcProjectID, 10,
            Function() DB.GetDataTable(EBDB, ViewSql))


        'SQL = ""
        'SQL = SQL + vbCrLf + " SELECT  "
        'SQL = SQL + vbCrLf + "    s.STATUS AS UnitStatus,  "
        'SQL = SQL + vbCrLf + "    COUNT(a.STATUS) AS UnitCount  "
        'SQL = SQL + vbCrLf + " FROM  "
        'SQL = SQL + vbCrLf + "    ( "
        'SQL = SQL + vbCrLf + "        SELECT STATE_ID, STATUS  "
        'SQL = SQL + vbCrLf + "        FROM UNITSHUB_PROJECTSTATUS  "
        'SQL = SQL + vbCrLf + "        WHERE PROJECT_ID = '" & lcProjectID & "' "
        'SQL = SQL + vbCrLf + "    ) s  "
        'SQL = SQL + vbCrLf + "    LEFT JOIN UNITSHUB_PRJ001_APARTMMENTS a  "
        'SQL = SQL + vbCrLf + "        ON UPPER(a.STATUS) = UPPER(s.STATE_ID)  "
        'SQL = SQL + vbCrLf + " GROUP BY  "
        'SQL = SQL + vbCrLf + "    s.STATE_ID, s.STATUS "
        'SQL = SQL + vbCrLf + " ORDER BY  "
        'SQL = SQL + vbCrLf + "    s.STATE_ID; "

        Dim statistics As DataTable = DT.Clone()

        statistics.Columns.Clear()
        statistics.Columns.Add("Text", GetType(String))
        statistics.Columns.Add("Count", GetType(Integer))

        Dim grouped = DT0.AsEnumerable() _
    .GroupBy(Function(r)
                 Dim status As String = r.Field(Of String)("Status")
                 Dim subtitle As String = r.Field(Of String)("Status_Subtitle")

                 If String.IsNullOrWhiteSpace(subtitle) Then
                     Return status
                 Else
                     Return status & ":" & subtitle
                 End If
             End Function)

        For Each g In grouped
            statistics.Rows.Add(g.Key, g.Count())
        Next

        GridView2.DataSource = statistics ' DB.GetDataTable(EBDB, SQL)
        GridView2.DataBind()

        'Hide Status and Status_subtitle from GridView ===========================================
        Dim result As New DataTable()
        result.Columns.Add("value", GetType(String))
        result.Rows.Add("Status")
        result.Rows.Add("Status_subtitle")
        PF.ApplyColumnInvisibility(GridView1, result)
        'End of: Hide Status and Status_subtitle from GridView ===================================


        'Dim ProjectTable As String = GetOrAddToCache(Of String)(
        '    "ProjectViewName_" & lcProjectID, 10,
        '    Function() DB.RetreiveScalarSTRING(EBDB, "Select VIEWNAME from UNITSHUB_VIEWS where PROJECT_ID ='" & lcProjectID & "' and TREELEVEL =1"))
        'DT = GetDataTable(EBDB, "Select * from " & ProjectTable)

        'Dim dtFields As New DataTable()

        'dtFields.Columns.Add("FIELD_NAME")
        'dtFields.Columns.Add("FIELD_VALUE")

        'For Each col As DataColumn In DT.Rows(0).Table.Columns
        '    Dim r As DataRow = dtFields.NewRow()

        '    r("FIELD_NAME") = col.ColumnName
        '    r("FIELD_VALUE") = DT.Rows(0)(col).ToString()

        '    dtFields.Rows.Add(r)
        'Next

        'Dim rowsPerColumn As Integer = 7

        'DataList2.RepeatColumns = Math.Ceiling(dtFields.Rows.Count / rowsPerColumn)

        'DataList2.DataSource = dtFields
        'DataList2.DataBind()

        Label2.Text = DropDownList1.SelectedItem.Text

        '===============================================================================
        '===============================================================================

        AddDialogueToColumns() 'add dialogue to Columns Button
        RegisterFilterPopup()
    End Sub

    ''' <summary>
    ''' Wires the Filter popup to btnFilter. Needs to run after any postback that
    ''' rebinds the grid (both a full loadData() and a filter-only ApplyFilterToGrid()),
    ''' since - like GridView4's actions above - this dynamic wiring isn't preserved by
    ''' ViewState on its own.
    ''' </summary>
    Private Sub RegisterFilterPopup()
        VendorPopupHelper.RegisterVendorPopup(Me,
                      btnFilter,
                      "Filters.aspx?Parameters=FilterMainTable&ProjectID=" & DropDownList1.SelectedItem.Value & "",
                      600,
                      700,
                      PopupPlacement.Center,
                      "Select Adj",
                      VendorPopupHelper.PopupDisplayMode.FrameOnly)
    End Sub

    Private Function GetColumnAliases(projectId As String,
                                      nodeTypeId As String) As List(Of Tuple(Of Integer, String))
        ' Column alias config for a project/node type is admin-configured and rarely
        ' changes, but loadData() re-runs this on every Approve/Bounce/project switch -
        ' cache it instead of hitting the DB every time.
        Return GetOrAddToCache(Of List(Of Tuple(Of Integer, String)))(
            "ColumnAliases_" & projectId & "_" & nodeTypeId, 10,
            Function()
                Dim aliases As New List(Of Tuple(Of Integer, String))

                Dim sql As String = "SELECT DISPLAY_ORDER, NAME_IN_UI " &
                                     "FROM UNITSHUB_ATTRIBUTES_PROPERTIES " &
                                     "WHERE PROJECT_ID = '" & projectId & "' AND NODE_TYPE_ID = '" & nodeTypeId & "' " &
                                     "ORDER BY DISPLAY_ORDER"

                Dim DT As New DataTable
                DT = GetDataTable(EBDB, sql)

                For Each DR As DataRow In DT.Rows
                    aliases.Add(Tuple.Create(Convert.ToInt32(DR("DISPLAY_ORDER")), DR("NAME_IN_UI").ToString()))
                Next

                Return aliases
            End Function)
    End Function

    Private Function BuildPivotSql(aliases As List(Of Tuple(Of Integer, String)),
                               projectId As String,
                               Node_Type_ID As String) As String

        Dim safeProjectId As String = projectId.Replace("'", "''")
        Dim safeNodeType As String = Node_Type_ID.Replace("'", "''")

        ' Same reasoning as GetColumnAliases: admin-configured, rarely changes, but was
        ' being re-fetched on every loadData() call for the same project.
        Dim VisibleFields As List(Of String) = GetOrAddToCache(Of List(Of String))(
            "VisibleFields_" & projectId, 10,
            Function() DB.BringDataInDataList(EBDB, "SELECT NAME_IN_UI FROM UNITSHUB_ATTRIBUTES_PROPERTIES WHERE PROJECT_ID = '" & safeProjectId & "' AND SHOW_IN_UI = 'Y'"))

        ' DISPLAY_ORDER of the unit's "SubStatus" attribute ("" = this project / node
        ' type has no sub-statuses) - see GetSubStatusAttributeDisplayOrder
        Dim SubStatusOrder As String = GetSubStatusAttributeDisplayOrder(projectId, Node_Type_ID)
        Dim HasSubStatus As Boolean = SubStatusOrder <> ""

        Dim InnerFields As New List(Of String)
        Dim OuterFields As New List(Of String)

        ' n.NODE_ID is the row's actual physical key (not an admin-configurable UI
        ' attribute like the aliased columns below), so it's added directly rather
        ' than through the aliases loop, and always included regardless of config.
        InnerFields.Add("    n.NODE_ID")
        OuterFields.Add("Q.""NODE_ID"" AS ""NodeId""")

        ' GridView1's DataKeyNames requires these columns to exist in the bound
        ' DataTable no matter what - a field marked SHOW_IN_UI='N' would otherwise be
        ' silently dropped from the outer SELECT below, which throws "DataBinding: ...
        ' does not contain a property with the name 'Reference'" on the first real row.
        Dim RequiredKeyFields As New List(Of String) From {"Reference", "Status"}

        For Each item In aliases

            Dim displayOrder As Integer = item.Item1
            Dim uiName As String = item.Item2.Replace("""", "")

            ' SubStatus is always handled below, whether or not it's set up as a UI column
            If uiName.Equals("SubStatus", StringComparison.OrdinalIgnoreCase) Then Continue For

            Dim isVisible As Boolean = VisibleFields.Contains(uiName, StringComparer.OrdinalIgnoreCase)
            Dim isRequiredKey As Boolean = RequiredKeyFields.Contains(uiName, StringComparer.OrdinalIgnoreCase)

            If isVisible OrElse isRequiredKey Then

                ' Inner (pivot) field
                InnerFields.Add(
                "    MAX(CASE WHEN v.DISPLAY_ORDER = " & displayOrder &
                " THEN v.VALUE_TEXT END) AS """ & uiName & """")

                ' Outer select field
                If uiName.Equals("Status", StringComparison.OrdinalIgnoreCase) Then
                    OuterFields.Add("PS.STATUS AS ""Status""")
                    ' Subtitle: the unit's sub-status if it has one (Sold -> "Waiting 2nd
                    ' Payment"), otherwise the status's own subtitle (Vacant -> "Ready To Reserve")
                    If HasSubStatus Then
                        OuterFields.Add("NVL(SS.SUBTITLE, PS.SUBTITLE) AS ""Status_Subtitle""")
                    Else
                        OuterFields.Add("PS.SUBTITLE AS ""Status_Subtitle""")
                    End If
                    ' Raw STATE_ID - the row's real status key (two statuses can share a
                    ' display name, so colours/actions are matched on this, not the name)
                    OuterFields.Add("Q.""Status"" AS ""StateId""")
                ElseIf uiName.Replace(" ", "").Replace("_", "").Equals("PAYMENTPLAN", StringComparison.OrdinalIgnoreCase) Then
                    ' Payment plan: its name (UNITSHUB_PAYMENTPLAN.NAME) instead of the PLAN_ID,
                    ' matched as text or as the same number written differently ("1" = "00001");
                    ' the stored value is kept when no plan matches. Column name unchanged.
                    Dim planCol As String = "TRIM(Q.""" & uiName & """)"
                    OuterFields.Add(
                        "NVL((SELECT MAX(PP.NAME) FROM UNITSHUB_PAYMENTPLAN PP " &
                        "WHERE PP.PLAN_ID = " & planCol & " " &
                        "OR CASE WHEN REGEXP_LIKE(" & planCol & ", '^[0-9]+$') THEN TO_NUMBER(" & planCol & ") END " &
                        "= CASE WHEN REGEXP_LIKE(PP.PLAN_ID, '^[0-9]+$') THEN TO_NUMBER(PP.PLAN_ID) END), " &
                        planCol & ") AS """ & uiName & """")
                    ' ...and the stored PLAN_ID itself, hidden: what's written to the unit's
                    ' history and passed to actions (DataKeys "PaymentPlanCode")
                    OuterFields.Add("Q.""" & uiName & """ AS ""PaymentPlanCode""")
                Else
                    OuterFields.Add("Q.""" & uiName & """")
                End If

            End If

        Next

        ' GridView1's DataKeyNames needs "PaymentPlanCode" in every result: empty when this
        ' project has no payment plan column
        If Not OuterFields.Any(Function(f) f.EndsWith("""PaymentPlanCode""")) Then
            OuterFields.Add("CAST(NULL AS VARCHAR2(10)) AS ""PaymentPlanCode""")
        End If

        ' Raw SUBSTATE_ID of the unit - always present (empty when the unit's status has
        ' no sub-statuses), so actions and card colours can be picked by it
        If HasSubStatus Then
            InnerFields.Add(
                "    MAX(CASE WHEN v.DISPLAY_ORDER = " & CInt(SubStatusOrder) &
                " THEN v.VALUE_TEXT END) AS ""SubStatus""")
            OuterFields.Add("Q.""SubStatus"" AS ""SubStateId""")
        Else
            OuterFields.Add("CAST(NULL AS VARCHAR2(4)) AS ""SubStateId""")
        End If

        Dim sb As New System.Text.StringBuilder()

        sb.AppendLine("SELECT")
        sb.AppendLine(String.Join("," & vbCrLf, OuterFields))
        sb.AppendLine("FROM")
        sb.AppendLine("(")

        sb.AppendLine("SELECT")
        sb.AppendLine(String.Join("," & vbCrLf, InnerFields))
        sb.AppendLine("FROM UNITSHUB_NODES n")
        sb.AppendLine("INNER JOIN UNITSHUB_NODE_ATTRIBUTE_VALUE v")
        sb.AppendLine("    ON n.NODE_ID = v.NODE_ID")
        sb.AppendLine("WHERE n.NODE_TYPE_ID = '" & safeNodeType & "'")
        sb.AppendLine("  AND n.PROJECT_ID = '" & safeProjectId & "'")
        sb.AppendLine("GROUP BY n.NODE_ID, n.PARENT_NODE_ID, n.PROJECT_ID")

        sb.AppendLine(") Q")

        sb.AppendLine("LEFT JOIN UNITSHUB_PROJECTSTATUS PS")
        sb.AppendLine("    ON PS.STATE_ID = Q.""Status""")
        sb.AppendLine("    AND PS.PROJECT_ID = '" & safeProjectId & "'")

        ' The unit's sub-status, within its status
        If HasSubStatus Then
            sb.AppendLine("LEFT JOIN UNITSHUB_PROJECTSUBSTATUS SS")
            sb.AppendLine("    ON SS.PROJECT_ID = '" & safeProjectId & "'")
            sb.AppendLine("    AND SS.STATE_ID = Q.""Status""")
            sb.AppendLine("    AND SS.SUBSTATE_ID = Q.""SubStatus""")
        End If

        Return sb.ToString()

    End Function

    ''' <summary>
    ''' DISPLAY_ORDER of the "SubStatus" attribute in UNITSHUB_ATTRIBUTES for a project
    ''' and node type, or "" if that project/node type has no sub-statuses. Read from
    ''' UNITSHUB_ATTRIBUTES directly (not UNITSHUB_ATTRIBUTES_PROPERTIES) because it
    ''' doesn't need to be set up as a visible UI column. Cached like the other
    ''' admin-configured lookups.
    ''' </summary>
    Private Function GetSubStatusAttributeDisplayOrder(ProjectID As String, Node_Type As String) As String
        Return GetOrAddToCache(Of String)(
            "SubStatusOrder_" & ProjectID & "_" & Node_Type, 10,
            Function() Convert.ToString(DB.RetreiveScalarSTRING(EBDB,
                "SELECT DISPLAY_ORDER FROM UNITSHUB_ATTRIBUTES " &
                "WHERE PROJECT_ID = '" & If(ProjectID, "").Replace("'", "''") & "' " &
                "AND NODE_TYPE_ID = '" & If(Node_Type, "").Replace("'", "''") & "' " &
                "AND UPPER(ATTRIBUTE_NAME) = 'SUBSTATUS'")).Trim())
    End Function



    Private Sub AddTBLtoSession(DT0 As DataTable)

        Session("FilterMainTable") = DT0
        'VendorPopupHelper.RegisterVendorPopup(Me,
        '                              btnFilter,
        '                              "Filters.aspx?Parameters=MainTable&ProjectID=" & DropDownList1.SelectedItem.Value,
        '                              600,
        '                              700,
        '                              PopupPlacement.Center,
        '                              "Select Adj",
        '                              VendorPopupHelper.PopupDisplayMode.FrameOnly)

    End Sub




    Private Function MakeTwoColumnGrid(source As DataTable) As DataTable

        Dim result As New DataTable()

        result.Columns.Add("FIELD_LEFT")
        result.Columns.Add("VALUE_LEFT")
        result.Columns.Add("FIELD_RIGHT")
        result.Columns.Add("VALUE_RIGHT")

        Dim half As Integer = Math.Ceiling(source.Rows.Count / 2)

        For i As Integer = 0 To half - 1

            Dim r As DataRow = result.NewRow()

            r("FIELD_LEFT") = source.Rows(i)("FIELD_NAME")
            r("VALUE_LEFT") = source.Rows(i)("FIELD_VALUE")

            If i + half < source.Rows.Count Then
                r("FIELD_RIGHT") = source.Rows(i + half)("FIELD_NAME")
                r("VALUE_RIGHT") = source.Rows(i + half)("FIELD_VALUE")
            End If

            result.Rows.Add(r)

        Next

        Return result

    End Function


    Private Sub DataList1_ItemDataBound(sender As Object, e As DataListItemEventArgs) Handles DataList1.ItemDataBound

        If e.Item.ItemType <> ListItemType.Item AndAlso e.Item.ItemType <> ListItemType.AlternatingItem Then Exit Sub

        Dim drv As DataRowView = TryCast(e.Item.DataItem, DataRowView)
        If drv Is Nothing Then Exit Sub

        Dim litFields As Literal = TryCast(e.Item.FindControl("litFields"), Literal)
        If litFields Is Nothing Then Exit Sub

        Dim excludedColumns As New List(Of String) From {"UnitName"}

        Dim sb As New System.Text.StringBuilder()
        Dim colorIndex As Integer = 0

        For Each col As DataColumn In drv.Row.Table.Columns
            If excludedColumns.Contains(col.ColumnName) Then Continue For

            Dim cssClass As String = BadgeColors(colorIndex Mod BadgeColors.Length)
            'colorIndex += 1

            sb.Append("<span class=""field-badge " & cssClass & """>")
            sb.Append(Server.HtmlEncode(col.ColumnName))
            sb.Append(": ")
            sb.Append(Server.HtmlEncode(drv(col.ColumnName).ToString()))
            sb.Append("</span>")
        Next

        litFields.Text = sb.ToString()
    End Sub

    Protected Sub btnColumns_Click(sender As Object, e As EventArgs) Handles btnColumns.Click
        Dim selectedItems As List(Of Dictionary(Of String, Object)) =
                           TryCast(VendorPopupHelper.GetPopupReturnValue(Me, "SelectedItems"),
                            List(Of Dictionary(Of String, Object)))
        Dim DT As New DataTable
        DT = PF.ConvertSelectedItemsToDataTable(selectedItems)
        PF.ApplyColumnVisibility(GridView1, DT)
        '===============================================================================
        '===============================================================================
        AddDialogueToColumns()
    End Sub

    Private Sub AddDialogueToColumns()
        Dim ColumnListParameters As New clsListPropertiesNoSQL
        With ColumnListParameters
            .WholeList = PF.GetAllColumnNames(GridView1)
            .CheckedList = PF.GetVisibleColumnNames(GridView1)
            .FormTitle = "Select Columns"
            .ColumnHideAndShow = "YN"
            .EditableColumns = "NN"
            .ColumnsWidth = New Double() {1, 3}
            .HoverableList = "Y"
        End With
        Dim ParamsKey As String = Guid.NewGuid().ToString("N")
        Session("PopupParams_" & ParamsKey) = ColumnListParameters
        VendorPopupHelper.RegisterVendorPopup(Me,
                                      btnColumns,
                                      "AddMultipleItemsFromList.aspx?Parameters=" & ParamsKey,
                                      400,
                                      600,
                                      PopupPlacement.Center,
                                      "Select Adj",
                                      VendorPopupHelper.PopupDisplayMode.FrameOnly)
    End Sub

    Protected Sub btnFilter_Click(sender As Object, e As EventArgs) Handles btnFilter.Click

        Dim FilterExpression As String =
                           TryCast(VendorPopupHelper.GetPopupReturnValue(Me, "FilterExpression"), String)

        ApplyFilterToGrid(FilterExpression)
    End Sub

    ''' <summary>
    ''' Filtering doesn't need to touch the database at all: loadData() already caches
    ''' the full unfiltered project table in Session("FilterMainTable"). Re-filtering
    ''' that in memory instead of calling loadData() again avoids re-running the entire
    ''' chain of unrelated queries (unit type lookup, column aliases, main pivot query,
    ''' project view lookup, status counts, project field list) that don't change just
    ''' because the filter changed.
    ''' </summary>
    Private Sub ApplyFilterToGrid(Filter As String)
        Dim DT0 As DataTable = TryCast(Session("FilterMainTable"), DataTable)

        If DT0 Is Nothing Then
            ' Nothing cached yet (e.g. session expired, or filter used before any
            ' project was loaded) - fall back to a full reload.
            loadData(Filter)
            Exit Sub
        End If

        Dim FilteredTable As DataTable = DT0
        If Filter <> "" Then
            Dim DR() As DataRow = DT0.Select(Filter)
            FilteredTable = If(DR.Length > 0, DR.CopyToDataTable(), DT0.Clone())
        End If

        GridView1.DataSource = FilteredTable
        GridView1.DataBind()
        ' Duplicate STATUS/Status_Subtitle auto-generated columns are stripped
        ' automatically by GridView1_DataBound (Handles GridView1.DataBound).

        AddDialogueToColumns() 'keep the Columns dialog's checked/unchecked state wiring current
        RegisterFilterPopup()
    End Sub

    ''' <summary>
    ''' Runs on every request, after any rebind or Columns-dialog change, so the Reference
    ''' links always have their text and popup, and the duplicate auto-generated
    ''' "Reference" column always stays hidden.
    ''' </summary>
    Protected Sub Page_PreRender(sender As Object, e As EventArgs) Handles Me.PreRender
        WireReferenceLinks()
    End Sub

    ''' <summary>
    ''' The Reference column (declared TemplateField right after Status in MainPage.aspx):
    ''' each row's lnkReference shows the unit's Reference and opens Unit.aspx for that
    ''' unit's NodeID and the selected project. Done here rather than in RowDataBound
    ''' because the popup wiring isn't kept by ViewState on postbacks that don't rebind
    ''' the grid (same issue RepopulateGridActionsIfNeeded handles for GridView4).
    ''' </summary>
    Private Sub WireReferenceLinks()
        If GridView1.HeaderRow IsNot Nothing Then HideAutoGeneratedReferenceCell(GridView1.HeaderRow)
        If GridView1.Rows Is Nothing OrElse GridView1.Rows.Count = 0 Then Exit Sub

        Dim ProjectId As String = If(DropDownList1.SelectedItem Is Nothing, "", Convert.ToString(DropDownList1.SelectedItem.Value))

        For Each row As GridViewRow In GridView1.Rows
            HideAutoGeneratedReferenceCell(row)
            If row.RowType <> DataControlRowType.DataRow Then Continue For

            Dim lnk As LinkButton = TryCast(row.FindControl("lnkReference"), LinkButton)
            If lnk Is Nothing Then Continue For

            Dim Reference As String = Convert.ToString(GridView1.DataKeys(row.RowIndex)("Reference"))
            Dim NodeId As String = Convert.ToString(GridView1.DataKeys(row.RowIndex)("NodeId"))

            lnk.Text = Server.HtmlEncode(Reference)
            lnk.ToolTip = "Open unit " & Reference

            VendorPopupHelper.RegisterVendorPopup(Me,
                                          lnk,
                                          "Unit.aspx?NodeID=" & Server.UrlEncode(NodeId) & "&ProjectId=" & Server.UrlEncode(ProjectId),
                                          1000, 0,
                                          PopupPlacement.RightSide,
                                          "",
                                          VendorPopupHelper.PopupDisplayMode.FrameOnly,
                                          returnKey:=OpenActionPopupReturnKey)
        Next
    End Sub

    ''' <summary>
    ''' Hides the auto-generated "Reference" cell of a GridView1 row (header or data), since
    ''' the declared Reference TemplateField already shows it as a link. Matched on the
    ''' cell's AutoGeneratedField.DataField, so the declared column (same header text) is
    ''' never touched.
    ''' </summary>
    Private Sub HideAutoGeneratedReferenceCell(row As GridViewRow)
        For Each cell As TableCell In row.Cells
            Dim fieldCell As DataControlFieldCell = TryCast(cell, DataControlFieldCell)
            If fieldCell Is Nothing Then Continue For
            Dim autoField As AutoGeneratedField = TryCast(fieldCell.ContainingField, AutoGeneratedField)
            If autoField IsNot Nothing AndAlso
               HiddenAutoGeneratedFields.Contains(autoField.DataField, StringComparer.OrdinalIgnoreCase) Then
                cell.Visible = False
            End If
        Next
    End Sub

    ''' <summary>
    ''' Auto-generated columns never shown: Reference (shown by its own link column) and the
    ''' key-only columns NodeId / StateId / SubStateId, which only feed DataKeys and the
    ''' status / action lookups. They're hidden cell by cell (from WireReferenceLinks, every
    ''' request): auto-generated columns aren't in GridView1.Columns, so
    ''' RemoveDuplicateAutoGeneratedColumns can't remove them.
    ''' </summary>
    Private Shared ReadOnly HiddenAutoGeneratedFields As String() = {"Reference", "NodeId", "StateId", "SubStateId", "PaymentPlanCode"}

    Private Sub GridView1_RowDataBound(sender As Object, e As GridViewRowEventArgs) Handles GridView1.RowDataBound
        If e.Row.RowType <> DataControlRowType.DataRow Then Exit Sub
        'Dim WorkflowEngine As New WorkflowAction


        Dim RequestID As String =
            CStr(DataBinder.Eval(e.Row.DataItem, "Reference"))

        Dim State As String =
            DataBinder.Eval(e.Row.DataItem, "STATUS").ToString()

        Dim StateId As String =
            Convert.ToString(DataBinder.Eval(e.Row.DataItem, "StateId"))

        Dim SubStateId As String =
            Convert.ToString(DataBinder.Eval(e.Row.DataItem, "SubStateId"))

        Dim SubtitleText As String =
            Convert.ToString(DataBinder.Eval(e.Row.DataItem, "Status_Subtitle"))

        '===============================================
        '===============================================

        'Dim returnValue As New DataTable("returnValue")

        '' Define columns
        'returnValue.Columns.Add("ID", GetType(Integer))
        'returnValue.Columns.Add("Name", GetType(String))
        'returnValue.Columns.Add("Value", GetType(String))

        '' Add sample rows
        'returnValue.Rows.Add(1, "S. Jalal Hasan", "Husband")
        'returnValue.Rows.Add(2, "Elmeera Yousif", "Wife")

        Dim GV As GridView
        GV = e.Row.FindControl("GridView4")



        '===============================================
        '===============================================




        ' Match actions on the row's STATE_ID, not its status name: several states can
        ' share a name (e.g. 0768 and 1024 are both "Sold", with different subtitles)
        Dim Actions As List(Of WorkflowAction) =
            GetAvailableActions(RequestID, StateId, SubStateId)

        GV.DataSource = Actions
        GV.DataBind()

        ApplyStatusCardColors(e.Row, StateId, SubtitleText, SubStateId)

    End Sub

    ''' <summary>
    ''' Sets the inline colors of the statusCard/statusTitle/statusSubtitle server
    ''' controls (declared runat="server" in MainPage.aspx's Status TemplateField) from
    ''' UNITSHUB_UNITSSTATUS's *_BG_COLOR/*_FG_COLOR columns, looked up by the row's real
    ''' STATE_ID via GetUnitStatusColors() - STATE_ID is the table's actual key, unlike
    ''' the display name, which could in principle be reused across different STATE_IDs
    ''' and pick the wrong row's colors. Runs once per row from RowDataBound (i.e. only
    ''' on an actual DataBind), same as the actions menu - since these are declared
    ''' controls (not dynamically added ones), the colors/visibility set here persist
    ''' across postbacks via ViewState like any other server control property, so nothing
    ''' needs to reapply them on postbacks that don't rebind the grid.
    ''' If a color column is blank/DBNull for a status, that inline style is left unset
    ''' and the element falls back to its .statusCard/.statusTitle/.statusSubtitle CSS
    ''' class color declared in MainPage.aspx's <style> block. The subtitle div is
    ''' hidden entirely (rather than left empty) whenever the row has no subtitle text.
    ''' </summary>
    Private Sub ApplyStatusCardColors(row As GridViewRow, StateId As String, SubtitleText As String,
                                      Optional SubStateId As String = "")
        Dim cardDiv As HtmlGenericControl = TryCast(row.FindControl("statusCard"), HtmlGenericControl)
        Dim titleDiv As HtmlGenericControl = TryCast(row.FindControl("statusTitle"), HtmlGenericControl)
        Dim subtitleDiv As HtmlGenericControl = TryCast(row.FindControl("statusSubtitle"), HtmlGenericControl)

        If subtitleDiv IsNot Nothing Then
            subtitleDiv.Visible = Not String.IsNullOrWhiteSpace(SubtitleText)
        End If

        Dim colors As DataRow = GetUnitStatusColors(StateId)
        Dim subColors As DataRow = GetUnitSubStatusColors(StateId, SubStateId)
        If colors Is Nothing AndAlso subColors Is Nothing Then Exit Sub

        ' Each colour: the sub-status's own if it has one set, else the status's
        SetColorStyleIfPresent(cardDiv, "background-color", PickColor(subColors, colors, "STATUS_BG_COLOR"))
        SetColorStyleIfPresent(titleDiv, "color", PickColor(subColors, colors, "STATUS_FG_COLOR"))
        SetColorStyleIfPresent(subtitleDiv, "background-color", PickColor(subColors, colors, "SUBTITLE_BG_COLOR"))
        SetColorStyleIfPresent(subtitleDiv, "color", PickColor(subColors, colors, "SUBTITLE_FG_COLOR"))
    End Sub

    ''' <summary>First non-empty value of Column in Preferred, then Fallback ("" if neither).</summary>
    Private Function PickColor(Preferred As DataRow, Fallback As DataRow, Column As String) As String
        For Each r As DataRow In {Preferred, Fallback}
            If r IsNot Nothing AndAlso r.Table.Columns.Contains(Column) Then
                Dim v As String = Convert.ToString(r(Column)).Trim()
                If v <> "" Then Return v
            End If
        Next
        Return ""
    End Function

    ''' <summary>
    ''' Cross-user, cross-request cache of UNITSHUB_PROJECTSUBSTATUS (the sub-statuses of
    ''' every status, with their card / subtitle colours) - same idea as GetUnitStatusTable.
    ''' </summary>
    Private Function GetUnitSubStatusTable() As DataTable
        Return GetOrAddToCache(Of DataTable)(
            "UnitSubStatuses", 30,
            Function() GetDataTable(EBDB, "SELECT PROJECT_ID, STATE_ID, SUBSTATE_ID, SUBTITLE, STATUS_BG_COLOR, STATUS_FG_COLOR, SUBTITLE_BG_COLOR, SUBTITLE_FG_COLOR FROM UNITSHUB_PROJECTSUBSTATUS"))
    End Function

    ''' <summary>
    ''' The UNITSHUB_PROJECTSUBSTATUS row for the current project, a status and one of
    ''' its sub-statuses, or Nothing (no sub-status given / not found). IDs compared with
    ''' NormalizeStateId, like GetUnitStatusColors.
    ''' </summary>
    Private Function GetUnitSubStatusColors(stateId As String, subStateId As String) As DataRow
        If String.IsNullOrWhiteSpace(subStateId) Then Return Nothing

        Dim projectId As String = DropDownList1.SelectedItem.Value
        Dim targetState As String = NormalizeStateId(stateId)
        Dim targetSub As String = NormalizeStateId(subStateId)

        Return GetUnitSubStatusTable().AsEnumerable().
            FirstOrDefault(Function(r) Convert.ToString(r("PROJECT_ID")) = projectId AndAlso
                                       String.Equals(NormalizeStateId(Convert.ToString(r("STATE_ID"))), targetState, StringComparison.OrdinalIgnoreCase) AndAlso
                                       String.Equals(NormalizeStateId(Convert.ToString(r("SUBSTATE_ID"))), targetSub, StringComparison.OrdinalIgnoreCase))
    End Function

    ''' <summary>
    ''' UNITSHUB_UNITSSTATUS's *_BG_COLOR/*_FG_COLOR columns store hex colors WITHOUT
    ''' the leading '#' (e.g. "F8E08A"), so it's added back here before assigning the
    ''' CSS style - a bare "F8E08A" is not valid CSS and would be silently ignored by
    ''' the browser.
    ''' </summary>
    Private Sub SetColorStyleIfPresent(control As HtmlGenericControl, styleName As String, value As Object)
        If control Is Nothing Then Exit Sub
        Dim text As String = Convert.ToString(value).Trim()
        If String.IsNullOrWhiteSpace(text) Then Exit Sub ' leave the CSS class's default color in place
        If Not text.StartsWith("#") Then text = "#" & text
        control.Style(styleName) = text
    End Sub

    ''' <summary>
    ''' Cross-user, cross-request cache of the full UNITSHUB_UNITSSTATUS table (STATUS,
    ''' STATE_ID, SUBTITLE, STATUS_BG_COLOR, STATUS_FG_COLOR, SUBTITLE_BG_COLOR,
    ''' SUBTITLE_FG_COLOR) - it's the same slow-changing admin config for every user/
    ''' project, so it's fetched once and reused rather than re-queried per row/request.
    ''' </summary>
    Private Function GetUnitStatusTable() As DataTable
        Return GetOrAddToCache(Of DataTable)(
            "UnitStatuses", 30,
            Function() GetDataTable(EBDB, "SELECT STATUS, STATE_ID, SUBTITLE, STATUS_BG_COLOR, STATUS_FG_COLOR, SUBTITLE_BG_COLOR, SUBTITLE_FG_COLOR FROM UNITSHUB_PROJECTSTATUS"))
    End Function

    ''' <summary>
    ''' Looks up the UNITSHUB_UNITSSTATUS row for a given STATE_ID (the table's real key -
    ''' see BuildPivotSql's "Q.""Status"" AS ""StateId""" for where this comes from on the
    ''' grid). Returns Nothing if there's no matching row (e.g. blank/unrecognized state).
    ''' Compares via NormalizeStateId rather than a raw trimmed match, since the pivoted
    ''' status code coming through the grid and UNITSHUB_UNITSSTATUS.STATE_ID may not be
    ''' zero-padded the same way (e.g. "1" vs "001") depending on the underlying column
    ''' types - a strict match would then silently fail for every row, not just some.
    ''' </summary>
    Private Function GetUnitStatusColors(stateId As String) As DataRow
        Dim target As String = NormalizeStateId(stateId)
        Return GetUnitStatusTable().AsEnumerable().
            FirstOrDefault(Function(r) String.Equals(NormalizeStateId(Convert.ToString(r("STATE_ID"))),
                                                       target,
                                                       StringComparison.OrdinalIgnoreCase))
    End Function

    ''' <summary>
    ''' Trims and strips leading zeros (e.g. "001" -> "1", "000" -> "0") so codes that
    ''' differ only in zero-padding still compare equal. Left alone for non-numeric/
    ''' alphanumeric codes (e.g. "A01" is untouched, since it doesn't start with '0').
    ''' </summary>
    Private Function NormalizeStateId(value As String) As String
        Dim s As String = If(value, "").Trim()
        Dim stripped As String = s.TrimStart("0"c)
        Return If(stripped = "", "0", stripped)
    End Function

    ''' <summary>
    ''' Fires when any LinkButton inside a GridView1 row raises a command - including
    ''' every action LinkButton inside a row's nested GridView4, whose Command
    ''' events bubble up automatically because they live inside a GridView row.
    ''' All action LinkButtons share the one CommandName ("ExecuteAction"), so what
    ''' actually runs is decided by the ACTION_ID packed into CommandArgument, not
    ''' by CommandName/PERMISSION_NAME text.
    ''' </summary>
    Protected Sub GridView1_RowCommand(sender As Object, e As GridViewCommandEventArgs) Handles GridView1.RowCommand

        'If e.CommandName <> "ExecuteAction" Then Exit Sub

        'Dim parts As String() = Convert.ToString(e.CommandArgument).Split(New String() {ActionCommandArgSeparator}, 4, StringSplitOptions.None)
        'If parts.Length <> 4 Then Exit Sub

        'Dim RequestID As String = parts(0)
        'Dim ActionID As String = parts(1)
        'Dim NodeId As String = parts(2)
        'Dim StateID As String = parts(3)

        ''MsgBox(RequestID & vbCrLf & ActionID & vbCrLf & NodeId & vbCrLf & StateID)

        'ExecuteWorkflowAction(RequestID, ActionID, NodeId, StateID)

    End Sub

    ''' <summary>
    ''' Runs the given ACTION_ID against the given unit/request, then rebinds the grid
    ''' so its status (color/subtitle) and each row's available actions reflect the
    ''' new state - the whole point of the action menu being driven by
    ''' STATE_ID/ACTION_ID in the first place is that once the underlying state
    ''' changes, GetAvailableActions() + ApplyStatusCardColors() naturally produce a
    ''' different menu/card on the next bind without any extra plumbing here.
    ''' "Show History" is handled as a special case: it doesn't change any state, so
    ''' it just pops up the row's Node_ID and returns without hitting the DB or
    ''' rebinding - adjust HistoryPermissionName below if your configured
    ''' PERMISSION_NAME text for it isn't exactly "History".
    ''' </summary>
    Private Const HistoryPermissionName As String = "History"


    ''' <summary>
    ''' Looks up the PERMISSION_NAME for a given ACTION_ID from the same per-project/
    ''' user permissions table GetAvailableActions() already fetches/caches, so
    ''' ExecuteWorkflowAction can recognize "the History action" without needing to
    ''' route on PERMISSION_NAME text directly (see GridView1_RowCommand's comment on
    ''' why ACTION_ID, not text, is what's used for dispatch).
    ''' </summary>
    Private Sub ExecuteWorkflowAction(RequestID As String, ActionID As String, NodeId As String, StateID As String)
        Dim Action_Title As String = GetActionAndStateForAction(StateID, ActionID)

        If String.Equals(Action_Title, HistoryPermissionName, StringComparison.OrdinalIgnoreCase) Then
            ShowNodeIdMessageBox(NodeId)
            Exit Sub
        End If

        ' The Project 001 / Action 00001 / State 0000 entry opens the ConfirmBox
        ' popup on click (see GridView4_RowDataBound) instead
        ' of running straight through. By the time THIS postback runs - the one
        ' __doPostBack fires from inside ConfirmBox's btnOK_Click - the popup's
        ' selection is already sitting in Session under ConfirmationPopupReturnKey,
        ' exactly the way lnkConfirmation_Click reads its own. Read it once here
        ' (GetPopupReturnValue clears it after reading, same as everywhere else)
        ' and branch out before falling into the generic ACTION_TYPE logic below,
        ' since that logic doesn't know anything about the popup's data.
        If String.Equals(If(ActionID, "").Trim(), ConfirmationPopupActionId, StringComparison.OrdinalIgnoreCase) AndAlso
           String.Equals(If(StateID, "").Trim(), ConfirmationPopupStateId, StringComparison.OrdinalIgnoreCase) AndAlso
           String.Equals(DropDownList1.SelectedItem.Value, ConfirmationPopupProjectId, StringComparison.OrdinalIgnoreCase) Then

            Dim confirmationData As DataTable =
                TryCast(VendorPopupHelper.GetPopupReturnValue(Me, ConfirmationPopupReturnKey), DataTable)

            If confirmationData Is Nothing Then
                ' Cancel was clicked in the popup (or it never ran) - nothing was
                ' saved to Session, so there's nothing to act on.
                Exit Sub
            End If

            ' TODO: replace with whatever this action actually needs to do with
            ' the rows ConfirmBox_aspx.vb's btnOK_Click built, e.g.:
            For Each row As DataRow In confirmationData.Rows
                Dim id As Integer = Convert.ToInt32(row("ID"))
                Dim name As String = Convert.ToString(row("Name"))
                Dim value As String = Convert.ToString(row("Value"))
                ' ... persist / apply these against RequestID/NodeId as needed ...
            Next

            loadData()
            Exit Sub
        End If

        ' TODO: your action-execution logic here - can now use StateID directly
        MsgBox("RequestID" & RequestID & vbCrLf &
            "ActionID: " & ActionID & vbCrLf &
            "NodeId: " & NodeId & vbCrLf &
            "StateID: " & StateID)

        Dim DT As New DataTable
        ' The action is identified by its ID alone (it may be placed in many statuses);
        ' where it leads comes from the placement that applies to this unit
        DT = GetDataTable(EBDB_CS, "Select * from UNITSHUB_ACTIONS where PROJECT_ID= '" & DropDownList1.SelectedItem.Value.Replace("'", "''") & "' and ACTION_ID = '" & If(ActionID, "").Replace("'", "''") & "'")

        If DT.Rows.Count = 0 Then Exit Sub

        Dim Placement As WorkflowAction = FindActionPlacement(StateID, GetNodeSubStatus(NodeId), ActionID)
        If Placement Is Nothing Then Exit Sub        ' not available here / not for this user

        Dim ToStatusId As String = Placement.ToStatusId
        Dim ToSubStateIdTarget As String = Placement.ToSubStateId
        Dim PreExecution As String = Convert.ToString(DT.Rows(0)("PRE_EXECUTION"))

        ' If the action is configured with PRE_EXECUTION = "Confirmation", the status
        ' change must NOT happen on this postback. Instead we stash what we'd need to
        ' apply it (NodeId/ToStatusId) in Session and pop a client-side confirm() -
        ' WebForms has no synchronous server-side way to "wait" for the user's answer,
        ' so if they click OK the confirm script triggers a second, hidden postback
        ' (btnConfirmAction) that actually performs the change. See
        ' ShowConfirmationThenApply/btnConfirmAction_Click below.
        Select Case DT.Rows(0)("ACTION_TYPE").ToString
            Case "CHANGE", ""

                If String.Equals(PreExecution, "Confirmation", StringComparison.OrdinalIgnoreCase) Then
                    Dim ConfirmationText As String = Convert.ToString(DT.Rows(0)("CONFIRMATION_TEXT"))
                    ShowConfirmationThenApply(NodeId, ToStatusId, ConfirmationText, ToSubStateIdTarget)
                    Exit Sub
                ElseIf DT.Rows(0)("NEED_DIALOGUE") = "1" Then

                End If

                'ApplyNodeStatusChange(NodeId, ToStatusId)

            Case ""

        End Select

        loadData()
    End Sub

    ''' <summary>
    ''' Holds just enough state to finish a confirmed action on the follow-up
    ''' postback fired by btnConfirmAction (see ShowConfirmationThenApply). Kept as
    ''' its own small class, rather than several loose Session keys, so there's a
    ''' single thing to null-check/clear in btnConfirmAction_Click.
    ''' </summary>
    Private Class PendingConfirmAction
        Public Property NodeId As String
        Public Property ToStatusId As String
        Public Property ToSubStateId As String
    End Class

    Private Const PendingConfirmActionSessionKey As String = "PendingConfirmAction"

    ''' <summary>
    ''' Stashes the pending Node/ToStatus change in Session, then registers a
    ''' client-side confirm(ConfirmationText). If the user clicks OK, the script
    ''' calls __doPostBack for the hidden btnConfirmAction button, which re-enters the
    ''' page (running Page_Load/RepopulateGridActionsIfNeeded as usual) and then fires
    ''' btnConfirmAction_Click, which is the only place that actually applies the
    ''' change. If the user clicks Cancel, nothing further happens and the stashed
    ''' Session value is simply overwritten/ignored next time.
    ''' </summary>
    Private Sub ShowConfirmationThenApply(NodeId As String, ToStatusId As String, ConfirmationText As String,
                                          Optional ToSubStateId As String = "")
        Session(PendingConfirmActionSessionKey) = New PendingConfirmAction With {
            .NodeId = NodeId,
            .ToStatusId = ToStatusId,
            .ToSubStateId = ToSubStateId
        }

        Dim safeMessage As String = HttpUtility.JavaScriptStringEncode(If(ConfirmationText, "Are you sure?"))
        Dim script As String =
            "if (confirm('" & safeMessage & "')) { " &
            "__doPostBack(" & HttpUtility.JavaScriptStringEncode(btnConfirmAction.UniqueID, True) & ", ''); " &
            "}"

        ClientScript.RegisterStartupScript(Me.GetType(), "ConfirmAction_" & Guid.NewGuid().ToString("N"), script, True)
    End Sub

    ''' <summary>
    ''' Fires on the hidden postback triggered from the confirm() script in
    ''' ShowConfirmationThenApply once the user clicks OK. btnConfirmAction is a
    ''' plain, always-hidden server Button added to MainPage.aspx purely as a
    ''' postback target - it has no visible UI of its own.
    ''' </summary>
    Protected Sub btnConfirmAction_Click(sender As Object, e As EventArgs) Handles btnConfirmAction.Click
        Dim pending As PendingConfirmAction = TryCast(Session(PendingConfirmActionSessionKey), PendingConfirmAction)
        Session.Remove(PendingConfirmActionSessionKey)

        If pending Is Nothing Then Exit Sub

        ApplyNodeStatusChange(pending.NodeId, pending.ToStatusId, pending.ToSubStateId)
        loadData()
    End Sub

    ''' <summary>
    ''' Applies the workflow transition itself: sets the unit's Status attribute to
    ''' ToStatusId and, when the project uses sub-statuses, its SubStatus attribute to
    ''' ToSubStateId (the action's TO_SUBSTATE_ID; empty clears it, e.g. when moving to
    ''' a status without sub-statuses). UNITSHUB_NODE_ATTRIBUTE_VALUE stores one row per
    ''' (NODE_ID, DISPLAY_ORDER) - lblDisplayOrder.Text is the Status attribute's
    ''' DISPLAY_ORDER for the current project/unit type (set by loadData).
    ''' </summary>
    Private Sub ApplyNodeStatusChange(NodeId As String, ToStatusId As String, Optional ToSubStateId As String = "")

        Dim safeNodeId As String = If(NodeId, "").Replace("'", "''")
        Dim safeToStatusId As String = If(ToStatusId, "").Replace("'", "''")
        Dim safeDisplayOrder As String = If(lblDisplayOrder.Text, "").Replace("'", "''")

        Dim SQL As String = " UPDATE UNITSHUB_NODE_ATTRIBUTE_VALUE " &
                            " SET VALUE_TEXT = '" & safeToStatusId & "' " &
                            " WHERE NODE_ID = '" & safeNodeId & "' " &
                            " AND DISPLAY_ORDER = '" & CInt(safeDisplayOrder).ToString("000") & "'"

        DB.ExecuteNonQuery(EBDB_CS, SQL)

        SetNodeSubStatus(NodeId, ToSubStateId)
    End Sub

    ''' <summary>
    ''' Writes the unit's SubStatus attribute (UNITSHUB_NODE_ATTRIBUTE_VALUE at the
    ''' SubStatus DISPLAY_ORDER of the unit's own project / node type). Updates the value
    ''' if the unit already has one, otherwise inserts it. Does nothing if the project
    ''' has no SubStatus attribute.
    ''' </summary>
    Private Sub SetNodeSubStatus(NodeId As String, SubStateId As String)
        Dim safeNodeId As String = If(NodeId, "").Trim().Replace("'", "''")
        If safeNodeId = "" Then Exit Sub

        Dim NodeDT As DataTable = GetDataTable(EBDB,
            "SELECT PROJECT_ID, NODE_TYPE_ID FROM UNITSHUB_NODES WHERE NODE_ID = '" & safeNodeId & "'")
        If NodeDT Is Nothing OrElse NodeDT.Rows.Count = 0 Then Exit Sub

        Dim SubOrder As String = GetSubStatusAttributeDisplayOrder(Convert.ToString(NodeDT.Rows(0)("PROJECT_ID")),
                                                                   Convert.ToString(NodeDT.Rows(0)("NODE_TYPE_ID")))
        If SubOrder = "" Then Exit Sub

        Dim DisplayOrder As String = CInt(SubOrder).ToString("000")
        Dim safeValue As String = If(SubStateId, "").Trim().Replace("'", "''")

        Dim SQL As String =
            "MERGE INTO UNITSHUB_NODE_ATTRIBUTE_VALUE v " &
            "USING (SELECT '" & safeNodeId & "' AS NODE_ID, '" & DisplayOrder & "' AS DISPLAY_ORDER FROM DUAL) s " &
            "ON (v.NODE_ID = s.NODE_ID AND TO_NUMBER(v.DISPLAY_ORDER) = TO_NUMBER(s.DISPLAY_ORDER)) " &
            "WHEN MATCHED THEN UPDATE SET v.VALUE_TEXT = '" & safeValue & "' " &
            "WHEN NOT MATCHED THEN INSERT (NODE_ID, DISPLAY_ORDER, VALUE_TEXT) " &
            "VALUES (s.NODE_ID, s.DISPLAY_ORDER, '" & safeValue & "')"

        DB.ExecuteNonQuery(EBDB_CS, SQL)
    End Sub


    Private Function GetActionAndStateForAction(StateID As String, ActionID As String) As String
        Dim lcProjectID As String = DropDownList1.SelectedItem.Value
        Dim UserID As String = GetCurrentUserID()
        Dim cacheKey As String = lcProjectID & "|" & UserID

        Dim actionsTable As DataTable = Nothing
        If Not _projectActionsCache.TryGetValue(cacheKey, actionsTable) Then
            actionsTable = LoadAvailableActionsForProject(lcProjectID, UserID)
            _projectActionsCache(cacheKey) = actionsTable
        End If

        Dim match As DataRow = actionsTable.AsEnumerable().
        FirstOrDefault(Function(r) String.Equals(Convert.ToString(r("ACTION_ID")).Trim(),
                                                   If(ActionID, "").Trim(),
                                                   StringComparison.OrdinalIgnoreCase))   ' title is the same in every placement

        If match Is Nothing Then Return ""
        Return Convert.ToString(match("ACTION_TITLE"))
    End Function


    ''' <summary>
    ''' Shows the row's Node_ID in a client-side (browser) message box. A server-side
    ''' VB MsgBox() would pop up on the WEB SERVER's console, not the user's browser -
    ''' completely wrong for a web app and would block that worker thread - so this
    ''' registers a JavaScript alert() to run on the page instead, which is the actual
    ''' web equivalent of a message box.
    ''' </summary>
    Private Sub ShowNodeIdMessageBox(NodeId As String)
        Dim safeNodeId As String = HttpUtility.JavaScriptStringEncode(If(NodeId, ""))
        Dim script As String = "alert('Node ID: " & safeNodeId & "');"
        ClientScript.RegisterStartupScript(Me.GetType(), "ShowNodeId_" & Guid.NewGuid().ToString("N"), script, True)
    End Sub

    ''' <summary>
    ''' Returns the actions available to the current user, for the currently selected
    ''' project (DropDownList1), filtered down to the given row's STATE_ID and
    ''' SUBSTATE_ID. Each GridView row calls this with its own RequestID/StateId/SubStateId, so the
    ''' same permission set (fetched/cached once per project+user) ends up producing
    ''' a different menu per row whenever that row's status differs.
    ''' </summary>
    Public Function GetAvailableActions(ByVal RequestID As String,
                                        ByVal StateId As String,
                                        Optional ByVal SubStateId As String = "") As List(Of WorkflowAction)

        Dim lcProjectID As String = DropDownList1.SelectedItem.Value
        Dim UserID As String = GetCurrentUserID()

        Dim cacheKey As String = lcProjectID & "|" & UserID
        Dim actionsTable As DataTable = Nothing

        If Not _projectActionsCache.TryGetValue(cacheKey, actionsTable) Then
            ' Not fetched yet this request - load every placement this user may use
            ' in this project in one round-trip, then filter in memory per row below.
            actionsTable = LoadAvailableActionsForProject(lcProjectID, UserID)
            _projectActionsCache(cacheKey) = actionsTable
        End If

        Dim Actions As New List(Of WorkflowAction)

        ' A placement fits the row when:
        '   - its STATUS_ID is '*' or the row's STATE_ID, and
        '   - its SUBSTATE_ID is '*' or the row's SUBSTATE_ID (a row without a
        '     sub-status only gets '*' placements).
        ' If an action fits in several ways (e.g. "all of Sold" and "Sold 2048"), the
        ' most specific placement wins - its target is the one that applies here:
        '   exact sub-status (2)  >  whole status (1)  >  all statuses (0)
        Dim targetStateId As String = NormalizeStateId(StateId)
        Dim unitHasSubState As Boolean = (If(SubStateId, "").Trim() <> "")
        Dim targetSubStateId As String = NormalizeStateId(If(SubStateId, "").Trim())

        Dim Specificity = Function(r As DataRow) As Integer
                              If Convert.ToString(r("STATUS_ID")).Trim() = "*" Then Return 0
                              If Convert.ToString(r("SUBSTATE_ID")).Trim() = "*" Then Return 1
                              Return 2
                          End Function

        Dim fitting = actionsTable.AsEnumerable().
            Where(Function(r)
                      Dim pStatus As String = Convert.ToString(r("STATUS_ID")).Trim()
                      If pStatus <> "*" AndAlso
                         Not String.Equals(NormalizeStateId(pStatus), targetStateId, StringComparison.OrdinalIgnoreCase) Then Return False

                      Dim pSub As String = Convert.ToString(r("SUBSTATE_ID")).Trim()
                      If pSub = "" OrElse pSub = "*" Then Return True
                      ' NormalizeStateId("") is "0" - a row without a sub-status must not
                      ' match a sub-status "0000" placement
                      If Not unitHasSubState Then Return False
                      Return String.Equals(NormalizeStateId(pSub), targetSubStateId, StringComparison.OrdinalIgnoreCase)
                  End Function)

        ' One placement per action (the most specific), then menu order:
        ' whole-status actions, sub-status actions, all-status actions (e.g. Show History)
        Dim chosen = fitting.
            GroupBy(Function(r) Convert.ToString(r("ACTION_ID")).Trim()).
            Select(Function(g) g.OrderByDescending(Function(r) Specificity(r)).First()).
            OrderBy(Function(r)
                        Select Case Specificity(r)
                            Case 1 : Return 0
                            Case 2 : Return 1
                            Case Else : Return 2
                        End Select
                    End Function).
            ThenBy(Function(r) Convert.ToInt32(r("PL_SORT"))).
            ThenBy(Function(r) Convert.ToString(r("ACTION_ID")))

        ' StateId     = the row's real status (never '*')
        ' SubStateId / PlacementStatusId / PlacementSubStateId = the placement's ('*' = all)
        ' ToStatusId / ToSubStateId = the target from THIS placement
        For Each DR As DataRow In chosen
            Actions.Add(New WorkflowAction With {
                .Text = DR("ACTION_TITLE").ToString(),
                .CommandName = DR("ACTION_TITLE").ToString(),
                .CommandArgument = RequestID,
                .ProjectId = DR("PROJECT_ID").ToString(),
                .ActionId = DR("ACTION_ID").ToString(),
                .StateId = StateId,
                .SubStateId = DR("SUBSTATE_ID").ToString(),
                .PlacementStatusId = DR("STATUS_ID").ToString(),
                .PlacementSubStateId = DR("SUBSTATE_ID").ToString(),
                .Icon = DR("ICON").ToString(),
                .StatusSubtitle = DR("STATUS_SUBTITLE").ToString(),
                .ToStatusId = DR("TO_STATUS_ID").ToString(),
                .ToSubStateId = DR("TO_SUBSTATE_ID").ToString(),
                .PreExecution = DR("PRE_EXECUTION").ToString(),
                .ActionType = DR("ACTION_TYPE").ToString(),
                .ConfirmationText = DR("CONFIRMATION_TEXT").ToString(),
                .IsActive = DR("IS_ACTIVE").ToString(),
                .ImplementerTitle = DR("IMPLEMENTER_TITLE").ToString(),
                .ShowInDefault = DR("SHOW_IN_DEFAULT").ToString(),
                .ShowInPreview = DR("SHOW_IN_PREVIEW").ToString(),
                .ReceiveParametersEnabled = DR("RECEIVE_PARAMETERS_ENABLED").ToString(),
                .ReceiveParametersMode = DR("RECEIVE_PARAMETERS_MODE").ToString(),
                .NeedPayment = DR("NEED_PAYMENT").ToString(),
                .PaymentPlanId = DR("PAYMENT_PLAN_ID").ToString(),
                .ScriptText = DR("SCRIPT_TEXT").ToString(),
                .ParameterType = DR("PARAMETER_TYPE").ToString(),
                .FormTitle = DR("FORM_TITLE").ToString(),
                .SelectSql = DR("SELECT_SQL").ToString(),
                .CreatedDate = DR("CREATED_DATE").ToString(),
                .NeedAutotransfer = DR("NEED_AUTOTRANSFER").ToString(),
                .AutotransferPlanId = DR("AUTOTRANSFER_PLAN_ID").ToString(),
                .NeedDialogue = DR("NEED_DIALOGUE").ToString(),
                .DialogueText = DR("DIALOGUE_TEXT").ToString()
            })
        Next

        Return Actions
    End Function

    ''' <summary>
    ''' The placement of ActionId that applies to a unit in StateId / SubStateId for the
    ''' current user (same rules as GetAvailableActions), or Nothing if the action isn't
    ''' available there for this user. Its ToStatusId / ToSubStateId are the target to use.
    ''' </summary>
    Private Function FindActionPlacement(StateId As String, SubStateId As String, ActionId As String) As WorkflowAction
        Return GetAvailableActions("", StateId, SubStateId).
            FirstOrDefault(Function(a) String.Equals(If(a.ActionId, "").Trim(), If(ActionId, "").Trim(),
                                                     StringComparison.OrdinalIgnoreCase))
    End Function

    ''' <summary>The unit's current SubStatus value ("" if none / no SubStatus attribute).</summary>
    Private Function GetNodeSubStatus(NodeId As String) As String
        Dim safeNodeId As String = If(NodeId, "").Trim().Replace("'", "''")
        If safeNodeId = "" Then Return ""

        Dim NodeDT As DataTable = GetDataTable(EBDB,
            "SELECT PROJECT_ID, NODE_TYPE_ID FROM UNITSHUB_NODES WHERE NODE_ID = '" & safeNodeId & "'")
        If NodeDT Is Nothing OrElse NodeDT.Rows.Count = 0 Then Return ""

        Dim SubOrder As String = GetSubStatusAttributeDisplayOrder(Convert.ToString(NodeDT.Rows(0)("PROJECT_ID")),
                                                                   Convert.ToString(NodeDT.Rows(0)("NODE_TYPE_ID")))
        If SubOrder = "" Then Return ""

        Return Convert.ToString(DB.RetreiveScalarSTRING(EBDB,
            "SELECT VALUE_TEXT FROM UNITSHUB_NODE_ATTRIBUTE_VALUE WHERE NODE_ID = '" & safeNodeId & "' " &
            "AND TO_NUMBER(DISPLAY_ORDER) = " & CInt(SubOrder))).Trim()
    End Function

    ''' <summary>
    ''' Every action PLACEMENT this user may use in this project, with the action's own
    ''' settings - one round-trip, cached per request in _projectActionsCache and then
    ''' filtered per grid row by GetAvailableActions.
    '''   UNITSHUB_ACTION_PLACEMENTS  where an action is valid: STATUS_ID / SUBSTATE_ID
    '''                               ('*' = all), and the target from there
    '''                               (TO_STATUS_ID / TO_SUBSTATE_ID)
    '''   UNITSHUB_ACTIONS            what the action is (title, type, dialogue, icon ...)
    '''   UNITSHUB_PRJ_STS_ACTN_USRS  who may use it in that place: a right for the same
    '''                               status / sub-status, or for '*'
    ''' Column names are kept as before (STATUS_ID, SUBSTATE_ID, TO_STATUS_ID ...) but now
    ''' describe the placement, not the action row.
    ''' </summary>
    Private Function LoadAvailableActionsForProject(ByVal ProjectID As String, ByVal UserID As String) As DataTable
        Dim safeProjectID As String = If(ProjectID, "").Replace("'", "''")
        Dim safeUserID As String = If(UserID, "").Replace("'", "''")

        Dim SQL As String = ""
        SQL = SQL + vbCrLf + "Select "
        SQL = SQL + vbCrLf + "         P.PROJECT_ID, "
        SQL = SQL + vbCrLf + "         P.STATUS_ID, "
        SQL = SQL + vbCrLf + "         NVL(P.SUBSTATE_ID, '*') as SUBSTATE_ID, "
        SQL = SQL + vbCrLf + "         P.TO_STATUS_ID, "
        SQL = SQL + vbCrLf + "         P.TO_SUBSTATE_ID, "
        SQL = SQL + vbCrLf + "         NVL(P.SORT_ORDER, 0) as PL_SORT, "
        SQL = SQL + vbCrLf + "         A.ACTION_ID, "
        SQL = SQL + vbCrLf + "         PS.STATUS as STATUS_NAME, "
        SQL = SQL + vbCrLf + "         NVL(SS.SUBTITLE, PS.SUBTITLE) as STATUS_SUBTITLE, "
        SQL = SQL + vbCrLf + "         A.IS_ACTIVE, "
        SQL = SQL + vbCrLf + "         A.ACTION_TITLE, "
        SQL = SQL + vbCrLf + "         A.ACTION_TYPE, "
        SQL = SQL + vbCrLf + "         A.IMPLEMENTER_TITLE, "
        SQL = SQL + vbCrLf + "         A.SHOW_IN_DEFAULT, "
        SQL = SQL + vbCrLf + "         A.SHOW_IN_PREVIEW, "
        SQL = SQL + vbCrLf + "         A.RECEIVE_PARAMETERS_ENABLED, "
        SQL = SQL + vbCrLf + "         A.RECEIVE_PARAMETERS_MODE, "
        SQL = SQL + vbCrLf + "         A.NEED_PAYMENT, "
        SQL = SQL + vbCrLf + "         A.PAYMENT_PLAN_ID, "
        SQL = SQL + vbCrLf + "         A.SCRIPT_TEXT, "
        SQL = SQL + vbCrLf + "         A.PRE_EXECUTION, "
        SQL = SQL + vbCrLf + "         A.CONFIRMATION_TEXT, "
        SQL = SQL + vbCrLf + "         A.PARAMETER_TYPE, "
        SQL = SQL + vbCrLf + "         A.FORM_TITLE, "
        SQL = SQL + vbCrLf + "         A.SELECT_SQL, "
        SQL = SQL + vbCrLf + "         A.CREATED_DATE, "
        SQL = SQL + vbCrLf + "         A.ICON, "
        SQL = SQL + vbCrLf + "         A.NEED_AUTOTRANSFER, "
        SQL = SQL + vbCrLf + "         A.AUTOTRANSFER_PLAN_ID, "
        SQL = SQL + vbCrLf + "         A.NEED_DIALOGUE, "
        SQL = SQL + vbCrLf + "         A.DIALOGUE_TEXT "
        SQL = SQL + vbCrLf + "from "
        SQL = SQL + vbCrLf + "         UNITSHUB_ACTION_PLACEMENTS P "
        SQL = SQL + vbCrLf + "inner join "
        SQL = SQL + vbCrLf + "         UNITSHUB_ACTIONS A "
        SQL = SQL + vbCrLf + "on "
        SQL = SQL + vbCrLf + "         A.PROJECT_ID = P.PROJECT_ID "
        SQL = SQL + vbCrLf + "         and A.ACTION_ID = P.ACTION_ID "
        SQL = SQL + vbCrLf + "left join "
        SQL = SQL + vbCrLf + "         UNITSHUB_PROJECTSTATUS PS "
        SQL = SQL + vbCrLf + "on "
        SQL = SQL + vbCrLf + "         PS.PROJECT_ID = P.PROJECT_ID "
        SQL = SQL + vbCrLf + "         and PS.STATE_ID = P.STATUS_ID "
        SQL = SQL + vbCrLf + "left join "
        SQL = SQL + vbCrLf + "         UNITSHUB_PROJECTSUBSTATUS SS "
        SQL = SQL + vbCrLf + "on "
        SQL = SQL + vbCrLf + "         SS.PROJECT_ID = P.PROJECT_ID "
        SQL = SQL + vbCrLf + "         and SS.STATE_ID = P.STATUS_ID "
        SQL = SQL + vbCrLf + "         and SS.SUBSTATE_ID = P.SUBSTATE_ID "
        SQL = SQL + vbCrLf + "where "
        SQL = SQL + vbCrLf + "         P.PROJECT_ID = '" & safeProjectID & "' "
        SQL = SQL + vbCrLf + "         and NVL(P.IS_ACTIVE, 'Y') = 'Y' "
        ' The user needs a right for this placement - or for every placement ('*')
        SQL = SQL + vbCrLf + "         and exists ( "
        SQL = SQL + vbCrLf + "             select 1 from UNITSHUB_PRJ_STS_ACTN_USRS U "
        SQL = SQL + vbCrLf + "             where U.PROJECT_ID = P.PROJECT_ID "
        SQL = SQL + vbCrLf + "               and U.ACTION_ID  = P.ACTION_ID "
        SQL = SQL + vbCrLf + "               and U.USER_ID    = '" & safeUserID & "' "
        SQL = SQL + vbCrLf + "               and U.STATUS_ID in (P.STATUS_ID, '*') "
        SQL = SQL + vbCrLf + "               and NVL(U.SUBSTATE_ID, '*') in (NVL(P.SUBSTATE_ID, '*'), '*') "
        SQL = SQL + vbCrLf + "         ) "
        Return GetDataTable(EBDB, SQL)
    End Function

    ''' <summary>
    ''' Separator packed between RequestID, ActionId and NodeId inside each
    ''' LinkButton's CommandArgument.
    ''' </summary>
    Private Const ActionCommandArgSeparator As String = "|"

    ''' <summary>
    ''' Identifies the one specific (Project, Action, State) menu entry that
    ''' should open the ConfirmBox popup instead of executing its workflow
    ''' action directly.
    ''' </summary>
    Private Const ConfirmationPopupProjectId As String = "001"
    Private Const ConfirmationPopupActionId As String = "00001"
    Private Const ConfirmationPopupStateId As String = "0000"

    ''' <summary>
    ''' True when act is the (Project, Action, State) entry that should trigger
    ''' the ConfirmBox popup on click.
    ''' </summary>
    Private Function IsConfirmationPopupAction(act As WorkflowAction) As Boolean
        Return String.Equals(If(act.ProjectId, "").Trim(), ConfirmationPopupProjectId, StringComparison.OrdinalIgnoreCase) AndAlso
               String.Equals(If(act.ActionId, "").Trim(), ConfirmationPopupActionId, StringComparison.OrdinalIgnoreCase) AndAlso
               String.Equals(If(act.StateId, "").Trim(), ConfirmationPopupStateId, StringComparison.OrdinalIgnoreCase)
    End Function

    Protected Sub lnkOpenAction_Click(sender As Object, e As EventArgs) Handles lnkOpenAction.Click
        Dim returnValue As Object = VendorPopupHelper.GetPopupReturnValue(Me, ConfirmationPopupReturnKey)
        If returnValue Is Nothing Then Exit Sub
        ' Executed when a standard action button is clicked
        MsgBox("Here")
        loadData()
    End Sub

    Protected Sub lnkConfirmation_Click(sender As Object, e As EventArgs) Handles lnkConfirmation.Click
        ' Executed when ConfirmBox.aspx completes and returns data
        Dim returnValue As Object = VendorPopupHelper.GetPopupReturnValue(Me, ConfirmationPopupReturnKey)
        If returnValue Is Nothing Then Exit Sub

        MsgBox("Confirmed")

        Dim confirmationData As DataTable = TryCast(returnValue, DataTable)
        If confirmationData IsNot Nothing Then
            For Each row As DataRow In confirmationData.Rows
                Dim id As Integer = Convert.ToInt32(row("ID"))
                Dim name As String = Convert.ToString(row("Name"))
                Dim value As String = Convert.ToString(row("Value"))
                ' Process confirmed items...
            Next
        End If

        loadData()
    End Sub



    ''' <summary>
    ''' Binds each GridView4 row's LinkButton3 to its WorkflowAction, and packs the
    ''' same RequestID|ActionID|NodeId|StateID payload used elsewhere (see
    ''' GridView1_RowCommand) into CommandArgument. GridView4's own
    ''' row/WorkflowAction has no NodeId of its own, so it's read off the *outer*
    ''' GridView1 row this GridView4 lives inside (via NamingContainer + DataKeys),
    ''' the same way RepopulateGridActionsIfNeeded() reads it.
    ''' </summary>
    Protected Sub GridView4_RowDataBound(sender As Object, e As GridViewRowEventArgs)
        If e.Row.RowType = DataControlRowType.DataRow Then
            Dim l As LinkButton = TryCast(e.Row.FindControl("LinkButton3"), LinkButton)
            If l Is Nothing Then Exit Sub

            Dim innerGrid As GridView = DirectCast(sender, GridView)
            Dim outerRow As GridViewRow = DirectCast(innerGrid.NamingContainer, GridViewRow)
            Dim NodeId As String = Convert.ToString(GridView1.DataKeys(outerRow.RowIndex)("NodeId"))
            ' The plan's ID (PaymentPlanCode), not its name: the "PaymentPlan" column shows the name
            Dim PaymentPlan As String = Convert.ToString(GridView1.DataKeys(outerRow.RowIndex)("PaymentPlanCode"))
            Dim UnitSubStateId As String = Convert.ToString(GridView1.DataKeys(outerRow.RowIndex)("SubStateId"))


            Dim oneAction As WorkflowAction = DirectCast(e.Row.DataItem, WorkflowAction)
            l.Text = oneAction.Icon & " " & oneAction.CommandName
            l.Attributes.Add("ProjectId", oneAction.ProjectId)
            l.Attributes.Add("ActionId", oneAction.ActionId)
            l.Attributes.Add("StateId", oneAction.StateId)
            l.Attributes.Add("NodeID", NodeId)
            l.Attributes.Add("PaymentPlan", PaymentPlan)
            l.Attributes.Add("SubStateId", UnitSubStateId)
            ' Placement this link comes from, and where it leads from here
            l.Attributes.Add("PlacementStatusId", oneAction.PlacementStatusId)
            l.Attributes.Add("PlacementSubStateId", oneAction.PlacementSubStateId)
            l.Attributes.Add("ToStatusId", oneAction.ToStatusId)
            l.Attributes.Add("ToSubStateId", oneAction.ToSubStateId)

            If oneAction.ConfirmationText.ToString <> "" Then

                VendorPopupHelper.RegisterVendorPopup(Me,
                                          l,
                                          "ConfirmBox.aspx?Message=" & encryNdecry.Encrypt(oneAction.ConfirmationText),
                                          600, 250,
                                          PopupPlacement.Center,
                                          "",
                                          VendorPopupHelper.PopupDisplayMode.FrameOnly,
                                          returnKey:=ConfirmationPopupReturnKey)
            ElseIf oneAction.NeedDialogue = "1" And oneAction.DialogueText <> "" Then
                VendorPopupHelper.RegisterVendorPopup(Me,
                                          l,
                                          oneAction.DialogueText & "?NeedsPayment=" & oneAction.NeedPayment & "&ProjectId=" & oneAction.ProjectId & "&ActionId=" & oneAction.ActionId & "&STATEID=" & oneAction.StateId & "&SUBSTATEID=" & UnitSubStateId & "&NodeID=" & NodeId,
                                          1000, 800,
                                          PopupPlacement.Center,
                                          "",
                                          VendorPopupHelper.PopupDisplayMode.FrameOnly,
                                          returnKey:=ConfirmationPopupReturnKey)

            End If
        End If

    End Sub

    ''' <summary>
    ''' Handles LinkButton3's click (wired via OnClick in markup, plain Click
    ''' event - not Command). Since Click's EventArgs carries no
    ''' CommandName/CommandArgument, both are read directly off the LinkButton
    ''' itself (sender), which GridView4_RowDataBound already populated. Mirrors
    ''' GridView1_RowCommand: same "ExecuteAction" CommandName, same
    ''' RequestID|ActionID|NodeId|StateID CommandArgument layout, so it just
    ''' unpacks and runs it the same way.
    ''' </summary>
    Protected Sub LinkButton3_Command(sender As Object, e As EventArgs)

        Dim returnValue As Object = VendorPopupHelper.GetPopupReturnValue(Me, ConfirmationPopupReturnKey)
        If returnValue Is Nothing Then
            loadData()
            Exit Sub
        End If

        ' TryCast: Cancel/Close return False instead of a list, and CType would throw on that
        Dim Row As List(Of Dictionary(Of String, Object)) = TryCast(returnValue, List(Of Dictionary(Of String, Object)))

        Dim L As LinkButton
        L = CType(sender, LinkButton)

        Dim ProjectId As String = L.Attributes("ProjectId")
        Dim ActionId As String = L.Attributes("ActionId")
        Dim StateId As String = L.Attributes("StateId")
        Dim NodeId As String = L.Attributes("NodeId")
        Dim PaymentPlan As String = L.Attributes("PaymentPlan")

        Dim DT As New DataTable
        ' The action is identified by its ID alone (it may be placed in many statuses)
        DT = GetDataTable(EBDB_CS, "Select * from UNITSHUB_ACTIONS where PROJECT_ID= '" & If(ProjectId, "").Replace("'", "''") & "' and ACTION_ID = '" & If(ActionId, "").Replace("'", "''") & "'")

        If DT.Rows.Count = 0 Then Exit Sub
        Dim ContactId As String = ""
        Dim Comments As String = ""
        Dim ACCOUNT As String = ""
        Dim SubStateId As String = L.Attributes("SubStateId")                          ' unit's current sub-status

        ' Where the unit goes comes from the placement that applies to it (checked again
        ' here, so a stale page can't use an action the user no longer has there)
        Dim Placement As WorkflowAction = FindActionPlacement(StateId, SubStateId, ActionId)
        If Placement Is Nothing Then
            loadData()
            Exit Sub
        End If
        Dim ToStatusId As String = Placement.ToStatusId
        Dim ToSubStateId As String = Placement.ToSubStateId                             ' "" = none
        Dim PreExecution As String = Convert.ToString(DT.Rows(0)("PRE_EXECUTION"))



        Select Case DT.Rows(0)("ACTION_TYPE").ToString.ToUpper
            Case "CHANGE"
                ' ChangeStatus only comes back when the popup was ReserveUnit: "True" only when every
                ' required payment is linked. For any other popup (ConfirmBox, other dialogue pages)
                ' there is no ChangeStatus to check, so the status change is not gated.
                Dim DialoguePage As String = Convert.ToString(DT.Rows(0)("DIALOGUE_TEXT"))
                Dim FromReserveUnit As Boolean = (Convert.ToString(DT.Rows(0)("NEED_DIALOGUE")) = "1" AndAlso
                                                  DialoguePage.IndexOf("ReserveUnit", StringComparison.OrdinalIgnoreCase) >= 0)

                Dim ChangeStatus As Boolean = True
                If FromReserveUnit Then
                    ChangeStatus = False
                    If Row IsNot Nothing AndAlso Row.Count > 0 AndAlso Row(0).ContainsKey("ChangeStatus") Then
                        ChangeStatus = (Convert.ToString(Row(0)("ChangeStatus")) = "True")
                    End If
                End If

                If ChangeStatus Then ApplyNodeStatusChange(NodeId, ToStatusId, ToSubStateId)
                Try
                    ContactId = Row(0)("CONTACT_ID").ToString 'GetReturnValueField(returnValue, "CONTACT_ID")
                    Comments = Row(0)("COMMENTS").ToString 'GetReturnValueField(returnValue, "COMMENTS")
                    ACCOUNT = Row(0)("ACCOUNT").ToString
                Catch ex As Exception

                End Try
                UpsertCustomerProperty(NodeId, ContactId, ToStatusId, Comments, ACCOUNT)

                ' InsertHistory comes back from ReserveUnit: "True" only when a customer or a
                ' payment was assigned in the form. If the status did not change,
                ' FROM_STATUS and TO_STATUS are both the current status.
                Dim InsertHistory As Boolean = False
                If Row IsNot Nothing AndAlso Row.Count > 0 AndAlso Row(0).ContainsKey("InsertHistory") Then
                    InsertHistory = (Convert.ToString(Row(0)("InsertHistory")) = "True")
                End If

                If InsertHistory Then
                    ' Summary list sent back by ReserveUnit, saved as one text in SUMMARY
                    Dim Summary As String = ""
                    If Row IsNot Nothing AndAlso Row.Count > 0 AndAlso Row(0).ContainsKey("Summary") Then
                        Summary = SummaryToText(Row(0)("Summary"))
                    End If

                    Dim HistoryToStatus As String = If(ChangeStatus, ToStatusId, StateId)

                    ' Sub-status before / after, saved in FROM_SUBSTATUS / TO_SUBSTATUS
                    Dim HistoryToSub As String = If(ChangeStatus, ToSubStateId, SubStateId)

                    InsertUnitsHistory(ProjectId, NodeId, ContactId, StateId, HistoryToStatus, PaymentPlan, ActionId, Summary,
                                       FromSubStatus:=SubStateId, ToSubStatus:=HistoryToSub)
                End If
            Case ""


        End Select

        loadData()

    End Sub

    ''' <summary>
    ''' Inserts or updates the UNITSHUB_CUSTOMERPROPERTIES row for (NODE_ID, CONTACT_ID).
    ''' If a row with that NODE_ID + CONTACT_ID already exists it is updated with the
    ''' new values; otherwise a new row is inserted.
    '''   STATUS       = ToStatusId
    '''   START_DATE   = today's date (yyyy-MM-dd)
    '''   END_DATE     = empty
    '''   UNIT_ACCOUNT = empty
    '''   CREATED_AT   = current date/time as a Unix timestamp (seconds, UTC)
    ''' </summary>
    Private Sub UpsertCustomerProperty(NodeId As String, ContactId As String, ToStatusId As String, Comments As String, ByVal ACCOUNT As String)

        If String.IsNullOrWhiteSpace(NodeId) OrElse String.IsNullOrWhiteSpace(ContactId) Then Exit Sub

        Dim safeNodeId As String = NodeId.Trim().Replace("'", "''")
        Dim safeContactId As String = ContactId.Trim().Replace("'", "''")
        Dim safeStatus As String = If(ToStatusId, "").Replace("'", "''")
        Dim safeComments As String = If(Comments, "").Replace("'", "''")

        Dim startDate As String = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
        Dim createdAt As String = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)

        Dim whereClause As String = " WHERE NODE_ID = '" & safeNodeId & "' AND CONTACT_ID = '" & safeContactId & "'"

        Dim existingCount As Integer = 0
        Integer.TryParse(DB.RetreiveScalarSTRING(EBDB, "SELECT COUNT(*) FROM UNITSHUB_CUSTOMERPROPERTIES" & whereClause), existingCount)

        Dim SQL As String
        If existingCount > 0 Then
            SQL = "UPDATE UNITSHUB_CUSTOMERPROPERTIES SET " &
                  "STATUS = '" & safeStatus & "', " &
                  "START_DATE = '" & startDate & "', " &
                  "END_DATE = '', " &
                  "UNIT_ACCOUNT = '" & ACCOUNT & "', " &
                  "CREATED_AT = '" & createdAt & "', " &
                  "COMMENTS = '" & safeComments & "'" &
                  whereClause
        Else
            SQL = "INSERT INTO UNITSHUB_CUSTOMERPROPERTIES " &
                  "(NODE_ID, CONTACT_ID, STATUS, START_DATE, END_DATE, UNIT_ACCOUNT, CREATED_AT, COMMENTS) VALUES (" &
                  "'" & safeNodeId & "', " &
                  "'" & safeContactId & "', " &
                  "'" & safeStatus & "', " &
                  "'" & startDate & "', " &
                  "'', " &
                  "'" & ACCOUNT & "', " &
                  "'" & createdAt & "', " &
                  "'" & safeComments & "')"
        End If

        DB.ExecuteNonQuery(EBDB_CS, SQL)
    End Sub

    ''' <summary>
    ''' Reads one field (e.g. "CONTACT_ID", "COMMENTS") out of the popup's returnValue.
    ''' Supports both DataTable layouts:
    '''   1) a column named after the field (value taken from the first row), or
    '''   2) Name/Value rows, where Name = the field name.
    ''' Returns "" if the field isn't found.
    ''' </summary>
    Private Function GetReturnValueField(returnValue As Object, FieldName As String) As String
        Dim dt As DataTable = TryCast(returnValue, DataTable)
        If dt Is Nothing OrElse dt.Rows.Count = 0 Then Return ""

        If dt.Columns.Contains(FieldName) Then
            Return Convert.ToString(dt.Rows(0)(FieldName))
        End If

        If dt.Columns.Contains("Name") AndAlso dt.Columns.Contains("Value") Then
            For Each row As DataRow In dt.Rows
                If String.Equals(Convert.ToString(row("Name")).Trim(), FieldName, StringComparison.OrdinalIgnoreCase) Then
                    Return Convert.ToString(row("Value"))
                End If
            Next
        End If

        Return ""
    End Function

    ''' <summary>
    ''' Turns the Summary value returned by ReserveUnit into one line of text,
    ''' items separated by "; ". Accepts a List(Of String), any other list/array
    ''' (in case the popup helper serializes it), or a plain string.
    ''' </summary>
    Private Function SummaryToText(Value As Object) As String
        If Value Is Nothing OrElse Value Is DBNull.Value Then Return ""
        If TypeOf Value Is String Then Return CStr(Value)

        Dim Items As IEnumerable = TryCast(Value, IEnumerable)
        If Items Is Nothing Then Return Convert.ToString(Value)

        Dim Parts As New List(Of String)
        For Each Item As Object In Items
            Dim Text As String = Convert.ToString(Item)
            If Not String.IsNullOrWhiteSpace(Text) Then Parts.Add(Text.Trim())
        Next
        Return String.Join("; ", Parts)
    End Function

    ''' <summary>
    ''' Inserts a NEW UNITSHUB_UNITSHISTORY row every time it is called (full history
    ''' trail - existing rows are never updated).
    '''   FROM_STATUS      = node status before the action (the StateId the action was clicked from)
    '''   TO_STATUS        = node status after the action (same as FROM_STATUS when the status didn't change)
    '''   FROM_SUBSTATUS   = node sub-status before the action ("" = none)
    '''   TO_SUBSTATUS     = node sub-status after the action (same as FROM_SUBSTATUS when nothing changed)
    '''   PAYMENT_PLAN     = the unit's payment plan
    '''   PAYMENTS         = comma-separated TRANSACTIONID values from UNITSHUB_PAYMENTS for NODE_ID + CONTACT_ID
    '''   PAYMENTSROWS     = comma-separated PAYMENT_ID values from UNITSHUB_PAYMENTS for NODE_ID + CONTACT_ID
    '''   AUTOTRANSFER     = empty for now
    '''   AUTOTRANSFERROWS = empty for now
    '''   IMPLEMENTEDBY    = current user ID
    '''   WORKSTATION      = workstation (currently fixed to "S069" - see TODO)
    '''   DATENTIME        = current date/time as a Unix timestamp (seconds, UTC)
    '''   SUMMARY          = what was assigned in ReserveUnit, e.g.
    '''                      "Customer 123 is assigned; Payment No. 0000012"
    ''' </summary>
    Private Sub InsertUnitsHistory(ProjectId As String, NodeId As String, ContactId As String,
                                   FromStatus As String, ToStatus As String,
                                   PaymentPlan As String, ActionId As String,
                                   Optional Summary As String = "",
                                   Optional FromSubStatus As String = "",
                                   Optional ToSubStatus As String = "")


        If String.IsNullOrWhiteSpace(NodeId) Then Exit Sub

        Dim safeProjectId As String = If(ProjectId, "").Trim().Replace("'", "''")
        Dim safeNodeId As String = NodeId.Trim().Replace("'", "''")
        Dim safeContactId As String = If(ContactId, "").Trim().Replace("'", "''")
        Dim safeFromStatus As String = If(FromStatus, "").Replace("'", "''")
        Dim safeToStatus As String = If(ToStatus, "").Replace("'", "''")
        Dim safeFromSubStatus As String = If(FromSubStatus, "").Trim().Replace("'", "''")   ' "" = no sub-status
        Dim safeToSubStatus As String = If(ToSubStatus, "").Trim().Replace("'", "''")
        Dim safePaymentPlan As String = If(PaymentPlan, "").Replace("'", "''")
        Dim safeActionId As String = If(ActionId, "").Trim().Replace("'", "''")
        Dim safeUserId As String = If(GetCurrentUserID(), "").Replace("'", "''")
        'TODO change Workstation to real station
        Dim safeWorkstation As String = "S069"

        ' Payments linked to this node + contact
        Dim payments As String = ""
        Dim paymentRows As String = ""
        GetNodeContactPayments(safeNodeId, safeContactId, payments, paymentRows)
        Dim safePayments As String = payments.Replace("'", "''")
        Dim safePaymentRows As String = paymentRows.Replace("'", "''")

        Dim DateNTime As String = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)

        Dim SQL As String =
            "INSERT INTO UNITSHUB_UNITSHISTORY " &
            "(PROJECT_ID, NODE_ID, CONTACT_ID, FROM_STATUS, TO_STATUS, FROM_SUBSTATUS, TO_SUBSTATUS, PAYMENT_PLAN, ACTION_ID, " &
            "PAYMENTS, PAYMENTSROWS, AUTOTRANSFER, AUTOTRANSFERROWS, IMPLEMENTEDBY, WORKSTATION, DATENTIME, SUMMARY) VALUES (" &
            "'" & safeProjectId & "', " &
            "'" & safeNodeId & "', " &
            "'" & safeContactId & "', " &
            "'" & safeFromStatus & "', " &
            "'" & safeToStatus & "', " &
            "'" & safeFromSubStatus & "', " &
            "'" & safeToSubStatus & "', " &
            "'" & safePaymentPlan & "', " &
            "'" & safeActionId & "', " &
            "'" & safePayments & "', " &
            "'" & safePaymentRows & "', " &
            "'', " &
            "'', " &
            "'" & safeUserId & "', " &
            "'" & safeWorkstation & "', " &
            DateNTime & ", " &
            "'" & If(Summary, "").Replace("'", "''") & "')"

        DB.ExecuteNonQuery(EBDB_CS, SQL)
    End Sub

    ''' <summary>
    ''' Reads UNITSHUB_PAYMENT for the given NODE_ID + CONTACT_ID and returns the
    ''' TRANSACTIONID values (payments) and PAYMENT_ID values (paymentRows) as
    ''' comma-separated lists, in the same order. Both come back "" if there are none.
    ''' Expects already-escaped (quote-doubled) NodeId/ContactId.
    ''' </summary>
    Private Sub GetNodeContactPayments(safeNodeId As String, safeContactId As String,
                                       ByRef payments As String, ByRef paymentRows As String)
        payments = ""
        paymentRows = ""
        If String.IsNullOrEmpty(safeContactId) Then Exit Sub

        Dim DT As DataTable = GetDataTable(EBDB,
            "SELECT PAYMENT_ID, TRANSACTIONID FROM UNITSHUB_PAYMENTs " &
            " WHERE NODE_ID = '" & safeNodeId & "' AND CONTACT_ID = '" & safeContactId & "'" &
            " ORDER BY PAYMENT_ID")
        If DT Is Nothing OrElse DT.Rows.Count = 0 Then Exit Sub

        Dim txIds As New List(Of String)
        Dim payIds As New List(Of String)
        For Each row As DataRow In DT.Rows
            txIds.Add(Convert.ToString(row("TRANSACTIONID")))
            payIds.Add(Convert.ToString(row("PAYMENT_ID")))
        Next

        payments = String.Join(",", txIds)
        paymentRows = String.Join(",", payIds)
    End Sub

    ''' <summary>
    ''' Best available identification of the user's workstation in a web app.
    ''' The browser never sends its machine name, so this uses the client IP
    ''' (honouring X-Forwarded-For if the site sits behind a proxy/load balancer)
    ''' and tries a reverse-DNS lookup to turn it into a host name. On an internal
    ''' network with working reverse DNS that gives the PC name; otherwise the IP.
    ''' </summary>
    Private Function GetClientWorkstation() As String
        Dim ip As String = Convert.ToString(Request.ServerVariables("HTTP_X_FORWARDED_FOR"))
        If Not String.IsNullOrWhiteSpace(ip) Then
            ip = ip.Split(","c)(0).Trim()
        Else
            ip = Convert.ToString(Request.UserHostAddress)
        End If
        If String.IsNullOrWhiteSpace(ip) Then Return ""

        Try
            Dim hostName As String = System.Net.Dns.GetHostEntry(ip).HostName
            If Not String.IsNullOrWhiteSpace(hostName) AndAlso hostName <> ip Then
                Return hostName
            End If
        Catch
            ' Reverse DNS not available - fall back to the IP address.
        End Try

        Return ip
    End Function
End Class

Public Class WorkflowAction
    Public Property Text As String
    Public Property CommandName As String
    Public Property CommandArgument As String
    Public Property ProjectId As String
    Public Property ActionId As String
    Public Property StateId As String
    Public Property Icon As String
    Public Property CssClass As String = "menuItem"
    Public Property StatusSubtitle As String
    Public Property ToStatusId As String
    Public Property SubStateId As String        ' placement's sub-status ('*' = every sub-status)
    Public Property ToSubStateId As String      ' sub-status the unit moves to (from this placement)
    Public Property PlacementStatusId As String     ' placement's status ('*' = every status)
    Public Property PlacementSubStateId As String   ' placement's sub-status ('*' = every sub-status)
    Public Property PreExecution As String
    Public Property ActionType As String
    Public Property ConfirmationText As String
    Public Property IsActive As String
    Public Property ImplementerTitle As String
    Public Property ShowInDefault As String
    Public Property ShowInPreview As String
    Public Property ReceiveParametersEnabled As String
    Public Property ReceiveParametersMode As String
    Public Property NeedPayment As String
    Public Property PaymentPlanId As String
    Public Property ScriptText As String
    Public Property ParameterType As String
    Public Property FormTitle As String
    Public Property SelectSql As String
    Public Property CreatedDate As String
    Public Property NeedAutotransfer As String
    Public Property AutotransferPlanId As String
    Public Property NeedDialogue As String
    Public Property DialogueText As String
End Class