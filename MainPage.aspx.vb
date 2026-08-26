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
                                      "DesignAction.aspx",
                                      1000, 0,
                                      PopupPlacement.Center,
                                      "Select Adj",
                                      VendorPopupHelper.PopupDisplayMode.FrameOnly)


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

        dt.Rows.Add("EMAIL TODAY", "", 800, 550, "Y", "ADM", "TITLE", "1", "")
        dt.Rows.Add("ADMIN", "", 800, 550, "Y", "ADM", "TITLE", "2", "")
        dt.Rows.Add("Events Control", "", 800, 550, "Y", "ADM", "TITLE", "3", "")
        dt.Rows.Add("Charts and Statistics", "", 800, 550, "Y", "ADM", "TITLE", "4", "")
        dt.Rows.Add("Dashboard", "frmDashboard.aspx", 800, 550, "Y", "ADM,FCD,ALL", "LINK", "4.1", "4")
        dt.Rows.Add("Reminder Today", "frmEmailToday.aspx", 800, 550, "Y", "ADM", "LINK", "1.1", "1")
        dt.Rows.Add("Bands Control", "frmInsertEditBand.aspx", 800, 550, "Y", "ADM", "LINK", "2.1", "2")
        dt.Rows.Add("Packages Control", "frmPackages.aspx", 800, 550, "Y", "ADM", "LINK", "2.2", "2")
        dt.Rows.Add("Uploadees Control", "frmUploadees.aspx", 800, 550, "Y", "ADM", "LINK", "2.3", "2")
        dt.Rows.Add("Start Today Event", "frmTodaysEvent.aspx", 800, 550, "Y", "ADM", "LINK", "1.2", "1")
        dt.Rows.Add("Requested-By-Me OnGoing", "frmStateActionsUsersPackages.aspx", 800, 550, "Y", "ADM,FCD", "LINK", "3.1", "3")
        dt.Rows.Add("Requested-From-Me OnGoing", "frmStateActionsUsersPackagesRequestedFromMe.aspx", 800, 550, "Y", "ADM", "LINK", "3.2", "3")
        dt.Rows.Add("Upcoming Events", "frmUpcomingsAuthorities.aspx", 800, 550, "Y", "ADM", "LINK", "3.3", "3")
        dt.Rows.Add("User Management", "frmUsers.aspx", 800, 550, "Y", "ADM,ALL", "LINK", "2.4", "2")
        dt.Rows.Add("Vacations Control", "frmVacations.aspx", 800, 550, "Y", "ADM", "LINK", "2.5", "2")

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

                sb.Append("<a class=""sideMenuItem"" href=""javascript:void(0);"" onclick=""openMenuLink('") _
                  .Append(Server.HtmlEncode(url)).Append("', ").Append(width).Append(", ").Append(height).Append("); return false;"">") _
                  .Append(Server.HtmlEncode(childRow.Field(Of String)("TITLE"))) _
                  .Append("</a>")
            Next

            sb.Append("</div>")
            sb.Append("</div>")
        Next

        phSideMenu.Controls.Add(New LiteralControl(sb.ToString()))
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
    ''' The Actions LinkButtons inside each row's phActions PlaceHolder are added
    ''' dynamically in RowDataBound, which only fires when GridView1.DataBind() runs
    ''' (e.g. from loadData()). Postbacks that don't rebind the grid - like the Columns
    ''' dialog's btnColumns_Click, which only toggles cell Visible flags - still reconstruct
    ''' the grid's declared row/cell/placeholder structure from ViewState, but NOT those
    ''' dynamically-added child controls, since ViewState only persists their state, not
    ''' their existence. Left alone, phActions ends up empty on any such postback. Re-adding
    ''' them here, on every postback before RaisePostBackEvent, keeps them present (and able
    ''' to raise their own postback events, e.g. clicking Approve) regardless of which
    ''' control triggered the postback. DataKeyNames is used instead of DataItem, which is
    ''' only populated during an actual DataBind.
    ''' </summary>
    Private Sub RepopulateGridActionsIfNeeded()
        If GridView1.Rows Is Nothing OrElse GridView1.Rows.Count = 0 Then Exit Sub

        For Each row As GridViewRow In GridView1.Rows
            If row.RowType <> DataControlRowType.DataRow Then Continue For

            Dim ph As PlaceHolder = TryCast(row.FindControl("phActions"), PlaceHolder)
            If ph Is Nothing OrElse ph.Controls.Count > 0 Then Continue For

            Dim RequestID As String = Convert.ToString(GridView1.DataKeys(row.RowIndex)("Reference"))
            Dim State As String = Convert.ToString(GridView1.DataKeys(row.RowIndex)("STATUS"))
            Dim NodeId As String = Convert.ToString(GridView1.DataKeys(row.RowIndex)("NodeId"))

            Dim Actions As List(Of WorkflowAction) = GetAvailableActions(RequestID, State)
            PopulateActions(ph, Actions, NodeId)
        Next
    End Sub

    Private Sub PopulateDropDownList(ByRef ddl As DropDownList,
                                     sql As Object,
                                     DataConnection As String,
                                     Optional firstItemIs As String = "",
                                     Optional SelectedItemIs As String = "")

        Dim DT As New Data.DataTable
        DT = GetDataTable(DataConnection, sql)

        For Each DR As DataRow In DT.Rows
            Dim I As New ListItem
            I.Text = DR(0).ToString
            I.Value = DR(1).ToString
            ddl.Items.Add(I)
        Next

        If firstItemIs <> "" Then
            Dim I As New ListItem
            I.Text = firstItemIs
            I.Value = "##"
            ddl.Items.Insert(0, I)
        End If
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
    Private Shared ReadOnly DuplicatedAutoGeneratedFields As String() = {"STATUS", "Status_Subtitle", "StateId", "NodeId"}

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


        SQL = ""
        SQL = SQL + vbCrLf + " SELECT "
        SQL = SQL + vbCrLf + "     s.UnitStatus, "
        SQL = SQL + vbCrLf + "     COUNT(a.STATUS) AS UnitCount "
        SQL = SQL + vbCrLf + " FROM "
        SQL = SQL + vbCrLf + "     ( "
        SQL = SQL + vbCrLf + " Select STATUS as UnitStatus from UNITSHUB_UNITSSTATUS "
        SQL = SQL + vbCrLf + "     ) s "
        SQL = SQL + vbCrLf + "     LEFT JOIN " & View.Rows(0)("VIEWNAME").ToString & " a "
        SQL = SQL + vbCrLf + "         ON upper(a.STATUS) = upper(s.UnitStatus) "
        SQL = SQL + vbCrLf + " GROUP BY "
        SQL = SQL + vbCrLf + "     s.UnitStatus "
        SQL = SQL + vbCrLf + " ORDER BY "
        SQL = SQL + vbCrLf + "     s.UnitStatus; "

        GridView2.DataSource = DB.GetDataTable(EBDB, SQL)
        GridView2.DataBind()

        'Hide Status and Status_subtitle from GridView ===========================================
        Dim result As New DataTable()
        result.Columns.Add("value", GetType(String))
        result.Rows.Add("Status")
        result.Rows.Add("Status_subtitle")
        PF.ApplyColumnInvisibility(GridView1, result)
        'End of: Hide Status and Status_subtitle from GridView ===================================


        Dim ProjectTable As String = GetOrAddToCache(Of String)(
            "ProjectViewName_" & lcProjectID, 10,
            Function() DB.RetreiveScalarSTRING(EBDB, "Select VIEWNAME from UNITSHUB_VIEWS where PROJECT_ID ='" & lcProjectID & "' and TREELEVEL =1"))
        DT = GetDataTable(EBDB, "Select * from " & ProjectTable)

        Dim dtFields As New DataTable()

        dtFields.Columns.Add("FIELD_NAME")
        dtFields.Columns.Add("FIELD_VALUE")

        For Each col As DataColumn In DT.Rows(0).Table.Columns
            Dim r As DataRow = dtFields.NewRow()

            r("FIELD_NAME") = col.ColumnName
            r("FIELD_VALUE") = DT.Rows(0)(col).ToString()

            dtFields.Rows.Add(r)
        Next

        Dim rowsPerColumn As Integer = 7

        DataList2.RepeatColumns = Math.Ceiling(dtFields.Rows.Count / rowsPerColumn)

        DataList2.DataSource = dtFields
        DataList2.DataBind()

        Label2.Text = DropDownList1.SelectedItem.Text

        '===============================================================================
        '===============================================================================

        AddDialogueToColumns() 'add dialogue to Columns Button
        RegisterFilterPopup()
    End Sub

    ''' <summary>
    ''' Wires the Filter popup to btnFilter. Needs to run after any postback that
    ''' rebinds the grid (both a full loadData() and a filter-only ApplyFilterToGrid()),
    ''' since - like phActions above - this dynamic wiring isn't preserved by ViewState
    ''' on its own.
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

        ' Same reasoning as GetColumnAliases: admin-configured, rarely changes, but was
        ' being re-fetched on every loadData() call for the same project.
        Dim VisibleFields As List(Of String) = GetOrAddToCache(Of List(Of String))(
            "VisibleFields_" & projectId, 10,
            Function() DB.BringDataInDataList(EBDB, "SELECT NAME_IN_UI FROM UNITSHUB_ATTRIBUTES_PROPERTIES WHERE PROJECT_ID = '" & projectId.Replace("'", "''") & "' AND SHOW_IN_UI = 'Y'"))

        Dim InnerFields As New List(Of String)
        Dim OuterFields As New List(Of String)

        ' n.NODE_ID is the row's actual physical key (not an admin-configurable UI
        ' attribute like the aliased columns below), so it's added directly rather
        ' than through the aliases loop, and always included regardless of config.
        InnerFields.Add("    n.NODE_ID")
        OuterFields.Add("Q.""NODE_ID"" AS ""NodeId""")

        ' GridView1's DataKeyNames="Reference,STATUS" requires these columns to exist
        ' in the bound DataTable no matter what - a field marked SHOW_IN_UI='N' would
        ' otherwise be silently dropped from the outer SELECT below, which throws
        ' "DataBinding: ... does not contain a property with the name 'Reference'" as
        ' soon as GridView1 tries to bind a real row.
        Dim RequiredKeyFields As New List(Of String) From {"Reference", "Status"}

        For Each item In aliases

            Dim displayOrder As Integer = item.Item1
            Dim uiName As String = item.Item2.Replace("""", "")

            Dim isVisible As Boolean = VisibleFields.Contains(uiName, StringComparer.OrdinalIgnoreCase)
            Dim isRequiredKey As Boolean = RequiredKeyFields.Contains(uiName, StringComparer.OrdinalIgnoreCase)

            If isVisible OrElse isRequiredKey Then

                ' Inner (pivot) field
                InnerFields.Add(
                "    MAX(CASE WHEN v.DISPLAY_ORDER = " & displayOrder &
                " THEN v.VALUE_TEXT END) AS """ & uiName & """")

                ' Outer select field
                If uiName.Equals("Status", StringComparison.OrdinalIgnoreCase) Then
                    OuterFields.Add("US.STATUS AS ""Status""")
                    OuterFields.Add("US.SUBTITLE AS ""Status_Subtitle""")   'Optional
                    ' Q."Status" here is still the raw STATE_ID code (before the LEFT JOIN
                    ' below overwrites the outer "Status" alias with the display name) -
                    ' carry it through under its own name so it survives as the row's real,
                    ' unique key. Matching status colors by display NAME is unsafe if two
                    ' different STATE_IDs ever share the same name.
                    OuterFields.Add("Q.""Status"" AS ""StateId""")
                Else
                    OuterFields.Add("Q.""" & uiName & """")
                End If

            End If

        Next

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
        sb.AppendLine("WHERE n.NODE_TYPE_ID = '" & Node_Type_ID.Replace("'", "''") & "'")
        sb.AppendLine("  AND n.PROJECT_ID = '" & projectId.Replace("'", "''") & "'")
        sb.AppendLine("GROUP BY n.NODE_ID, n.PARENT_NODE_ID, n.PROJECT_ID")

        sb.AppendLine(") Q")

        sb.AppendLine("LEFT JOIN UNITSHUB_UNITSSTATUS US")
        sb.AppendLine("    ON US.STATE_ID = Q.""Status""")

        Return sb.ToString()

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

    Private Sub GridView1_RowDataBound(sender As Object, e As GridViewRowEventArgs) Handles GridView1.RowDataBound
        If e.Row.RowType <> DataControlRowType.DataRow Then Exit Sub
        'Dim WorkflowEngine As New WorkflowAction


        Dim ph As PlaceHolder =
    CType(e.Row.FindControl("phActions"), PlaceHolder)

        Dim RequestID As String =
            CStr(DataBinder.Eval(e.Row.DataItem, "Reference"))

        Dim State As String =
            DataBinder.Eval(e.Row.DataItem, "STATUS").ToString()

        Dim StateId As String =
            Convert.ToString(DataBinder.Eval(e.Row.DataItem, "StateId"))

        Dim NodeId As String =
            Convert.ToString(DataBinder.Eval(e.Row.DataItem, "NodeId"))

        Dim SubtitleText As String =
            Convert.ToString(DataBinder.Eval(e.Row.DataItem, "Status_Subtitle"))

        Dim Actions As List(Of WorkflowAction) =
            GetAvailableActions(RequestID, State)

        PopulateActions(ph, Actions, NodeId)

        ApplyStatusCardColors(e.Row, StateId, SubtitleText)

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
    Private Sub ApplyStatusCardColors(row As GridViewRow, StateId As String, SubtitleText As String)
        Dim cardDiv As HtmlGenericControl = TryCast(row.FindControl("statusCard"), HtmlGenericControl)
        Dim titleDiv As HtmlGenericControl = TryCast(row.FindControl("statusTitle"), HtmlGenericControl)
        Dim subtitleDiv As HtmlGenericControl = TryCast(row.FindControl("statusSubtitle"), HtmlGenericControl)

        If subtitleDiv IsNot Nothing Then
            subtitleDiv.Visible = Not String.IsNullOrWhiteSpace(SubtitleText)
        End If

        Dim colors As DataRow = GetUnitStatusColors(StateId)
        If colors Is Nothing Then Exit Sub

        SetColorStyleIfPresent(cardDiv, "background-color", colors("STATUS_BG_COLOR"))
        SetColorStyleIfPresent(titleDiv, "color", colors("STATUS_FG_COLOR"))
        SetColorStyleIfPresent(subtitleDiv, "background-color", colors("SUBTITLE_BG_COLOR"))
        SetColorStyleIfPresent(subtitleDiv, "color", colors("SUBTITLE_FG_COLOR"))
    End Sub

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
            Function() GetDataTable(EBDB, "SELECT STATUS, STATE_ID, SUBTITLE, STATUS_BG_COLOR, STATUS_FG_COLOR, SUBTITLE_BG_COLOR, SUBTITLE_FG_COLOR FROM UNITSHUB_UNITSSTATUS"))
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
    ''' every action LinkButton added dynamically in PopulateActions, whose Command
    ''' events bubble up automatically because they live inside a GridView row.
    ''' All action LinkButtons share the one CommandName ("ExecuteAction" - see
    ''' PopulateActions), so what actually runs is decided by the ACTION_ID packed
    ''' into CommandArgument, not by CommandName/PERMISSION_NAME text.
    ''' </summary>
    Protected Sub GridView1_RowCommand(sender As Object, e As GridViewCommandEventArgs) Handles GridView1.RowCommand

        If e.CommandName <> "ExecuteAction" Then Exit Sub

        Dim parts As String() = Convert.ToString(e.CommandArgument).Split(New String() {ActionCommandArgSeparator}, 3, StringSplitOptions.None)
        If parts.Length <> 3 Then Exit Sub

        Dim RequestID As String = parts(0)
        Dim ActionID As String = parts(1)
        Dim NodeId As String = parts(2)

        'MsgBox(RequestID & vbCrLf & ActionID & vbCrLf & NodeId)

        ExecuteWorkflowAction(RequestID, ActionID, NodeId)

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
    '''
    ''' TODO: replace the placeholder below with your real transition logic for every
    ''' other action, e.g.:
    '''   DB.ExecuteNonQuery(EBDB,
    '''       "EXEC sp_ExecuteUnitAction @RequestID = '" & RequestID.Replace("'", "''") & "', " &
    '''       "@ActionID = '" & ActionID.Replace("'", "''") & "', " &
    '''       "@UserID = '" & GetCurrentUserID().Replace("'", "''") & "'")
    ''' (or however this system records that ACTION_ID was performed on RequestID and
    ''' advances its STATE_ID - adjust to whatever stored proc/table your workflow
    ''' engine actually uses; DB.ExecuteNonQuery is a guess at the write-side
    ''' counterpart to the DB.GetDataTable/DB.RetreiveScalarSTRING helpers already
    ''' used elsewhere in this file - swap in whatever your DB helper class calls it).
    ''' </summary>
    Private Const HistoryPermissionName As String = "History"

    Private Sub ExecuteWorkflowAction(RequestID As String, ActionID As String, NodeId As String)
        Dim permissionName As String = GetPermissionNameForAction(ActionID)

        If String.Equals(permissionName, HistoryPermissionName, StringComparison.OrdinalIgnoreCase) Then
            ShowNodeIdMessageBox(NodeId)
            Exit Sub ' view-only action - nothing changed, no need to rebind
        End If

        ' TODO: your action-execution logic here - update RequestID's STATE_ID
        ' according to ActionID, log who did it (GetCurrentUserID()) and when, etc.

        loadData() ' rebind so the grid (status card + actions menu) reflects the new state
    End Sub

    ''' <summary>
    ''' Looks up the PERMISSION_NAME for a given ACTION_ID from the same per-project/
    ''' user permissions table GetAvailableActions() already fetches/caches, so
    ''' ExecuteWorkflowAction can recognize "the History action" without needing to
    ''' route on PERMISSION_NAME text directly (see GridView1_RowCommand's comment on
    ''' why ACTION_ID, not text, is what's used for dispatch).
    ''' </summary>
    Private Function GetPermissionNameForAction(ActionID As String) As String
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
                                                       StringComparison.OrdinalIgnoreCase))

        If match Is Nothing Then Return ""
        Return Convert.ToString(match("PERMISSION_NAME"))
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

    'Private Sub AddAction(ph As PlaceHolder,
    '                  Text As String,
    '                  Command As String)

    '    Dim li As New LiteralControl("<li>")

    '    ph.Controls.Add(li)

    '    Dim btn As New LinkButton()

    '    btn.Text = Text
    '    btn.CommandName = Command
    '    btn.CssClass = "dropdown-item"

    '    ph.Controls.Add(btn)

    '    ph.Controls.Add(New LiteralControl("</li>"))

    'End Sub


    ''' <summary>
    ''' Returns the actions available to the current user, for the currently selected
    ''' project (DropDownList1), filtered down to the given row's STATE_ID (its
    ''' STATUS). Each GridView row calls this with its own RequestID/State, so the
    ''' same permission set (fetched/cached once per project+user) ends up producing
    ''' a different menu per row whenever that row's status differs.
    ''' </summary>
    Public Function GetAvailableActions(ByVal RequestID As String,
                                    ByVal State As String) As List(Of WorkflowAction)

        Dim lcProjectID As String = DropDownList1.SelectedItem.Value
        Dim UserID As String = GetCurrentUserID()

        Dim cacheKey As String = lcProjectID & "|" & UserID
        Dim actionsTable As DataTable = Nothing

        If Not _projectActionsCache.TryGetValue(cacheKey, actionsTable) Then
            ' Not fetched yet this request - load every (STATE_ID, ACTION_ID) this
            ' user is permitted for this project in one round-trip, then filter
            ' in memory per row/state below instead of re-querying per row.
            actionsTable = LoadAvailableActionsForProject(lcProjectID, UserID)
            _projectActionsCache(cacheKey) = actionsTable
        End If

        Dim Actions As New List(Of WorkflowAction)

        ' GridView1's STATUS column holds the human-readable status NAME
        ' (UNITSHUB_UNITSSTATUS.STATUS - see BuildPivotSql's "US.STATUS AS ""Status""").
        ' LoadAvailableActionsForProject now joins UNITSHUB_UNITSSTATUS itself and
        ' returns that same STATUS name (aliased STATUS_NAME) alongside STATE_ID, so
        ' match directly on the name - no separate code<->name lookup needed.
        Dim matchingRows = actionsTable.AsEnumerable().
            Where(Function(r) String.Equals(Convert.ToString(r("STATUS_NAME")).Trim(),
                                             If(State, "").Trim(),
                                             StringComparison.OrdinalIgnoreCase))

        For Each DR As DataRow In matchingRows
            Actions.Add(New WorkflowAction With {
                .Text = DR("PERMISSION_NAME").ToString(),
                .CommandName = DR("PERMISSION_NAME").ToString(),
                .CommandArgument = RequestID,
                .ActionId = DR("ACTION_ID").ToString(),
                .Icon = DR("ICON").ToString(),
                .StatusSubtitle = DR("STATUS_SUBTITLE").ToString()
            })
        Next

        Return Actions
    End Function

    ''' <summary>
    ''' Loads every action the given user is allowed to perform on the given project,
    ''' across all STATE_IDs, mirroring the role-based + user-override permission
    ''' model: role-derived permissions (via group -> role -> role permissions) are
    ''' returned unless a matching user-level override for that exact PROJECT_ID/
    ''' STATE_ID/ACTION_ID already grants it explicitly (to avoid duplicating it, since
    ''' the second half of the UNION adds it back in) or denies it, and any user-level
    ''' ALLOW override is added on top even if no role would otherwise grant it.
    ''' Joins UNITSHUB_UNITSSTATUS so each row carries its STATUS name/SUBTITLE
    ''' alongside the STATE_ID code, since that's what the grid's rows key off of.
    ''' Caller (GetAvailableActions) filters the returned table down to one status
    ''' per grid row.
    ''' </summary>
    Private Function LoadAvailableActionsForProject(ByVal ProjectID As String, ByVal UserID As String) As DataTable
        Dim safeProjectID As String = If(ProjectID, "").Replace("'", "''")
        Dim safeUserID As String = If(UserID, "").Replace("'", "''")

        Dim SQL As String = ""
        SQL = SQL + vbCrLf + " SELECT RP.PROJECT_ID, RP.STATE_ID, RP.ACTION_ID, P.ICON, P.PERMISSION_NAME, "
        SQL = SQL + vbCrLf + "        US.STATUS AS STATUS_NAME, US.SUBTITLE AS STATUS_SUBTITLE "
        SQL = SQL + vbCrLf + " FROM UnitsHub_Users U "
        SQL = SQL + vbCrLf + " INNER JOIN UNITSHUB_USERGROUPS UG ON U.USER_ID = UG.USER_ID "
        SQL = SQL + vbCrLf + " INNER JOIN UNITSHUB_GROUPS G ON UG.GROUP_ID = G.GROUP_ID "
        SQL = SQL + vbCrLf + " INNER JOIN UNITSHUB_GROUPROLES GR ON G.GROUP_ID = GR.GROUP_ID "
        SQL = SQL + vbCrLf + " INNER JOIN UNITSHUB_ROLES R ON GR.ROLE_ID = R.ROLE_ID "
        SQL = SQL + vbCrLf + " INNER JOIN UNITSHUB_ROLEPERMISSIONS RP ON R.ROLE_ID = RP.ROLE_ID "
        SQL = SQL + vbCrLf + " INNER JOIN UNITSHUB_PERMISSIONS P ON RP.ACTION_ID = P.ACTION_ID "
        SQL = SQL + vbCrLf + " INNER JOIN UNITSHUB_UNITSSTATUS US ON US.STATE_ID = RP.STATE_ID "
        SQL = SQL + vbCrLf + " WHERE U.USER_ID = '" & safeUserID & "' "
        SQL = SQL + vbCrLf + "   AND RP.PROJECT_ID = '" & safeProjectID & "' "
        SQL = SQL + vbCrLf + "   AND NOT EXISTS "
        SQL = SQL + vbCrLf + "   ( "
        SQL = SQL + vbCrLf + "       SELECT 1 "
        SQL = SQL + vbCrLf + "       FROM UnitsHub_UserPermissions UP "
        SQL = SQL + vbCrLf + "       WHERE UP.USER_ID = U.USER_ID "
        SQL = SQL + vbCrLf + "         AND UP.PROJECT_ID = RP.PROJECT_ID "
        SQL = SQL + vbCrLf + "         AND UP.STATE_ID = RP.STATE_ID "
        SQL = SQL + vbCrLf + "         AND UP.ACTION_ID = RP.ACTION_ID "
        SQL = SQL + vbCrLf + "         AND UP.ALLOW_DENY = 'A' "
        SQL = SQL + vbCrLf + "   ) "
        SQL = SQL + vbCrLf + " UNION "
        SQL = SQL + vbCrLf + " SELECT UP.PROJECT_ID, UP.STATE_ID, UP.ACTION_ID, P.ICON, P.PERMISSION_NAME, "
        SQL = SQL + vbCrLf + "        US.STATUS AS STATUS_NAME, US.SUBTITLE AS STATUS_SUBTITLE "
        SQL = SQL + vbCrLf + " FROM UnitsHub_UserPermissions UP "
        SQL = SQL + vbCrLf + " JOIN UnitsHub_Permissions P ON P.ACTION_ID = UP.ACTION_ID "
        SQL = SQL + vbCrLf + " JOIN UNITSHUB_UNITSSTATUS US ON US.STATE_ID = UP.STATE_ID "
        SQL = SQL + vbCrLf + " WHERE UP.USER_ID = '" & safeUserID & "' "
        SQL = SQL + vbCrLf + "   AND UP.PROJECT_ID = '" & safeProjectID & "' "
        SQL = SQL + vbCrLf + "   AND UP.ALLOW_DENY = 'A'; "

        Return GetDataTable(EBDB, SQL)
    End Function

    ''' <summary>
    ''' Separator packed between RequestID, ActionId and NodeId inside each
    ''' LinkButton's CommandArgument (GridViewCommandEventArgs only gives you
    ''' CommandName + CommandArgument, so all three values ride in the one string) -
    ''' unpacked again in GridView1_RowCommand.
    ''' </summary>
    Private Const ActionCommandArgSeparator As String = "|"

    ''' <summary>
    ''' Builds a stable, ASP.NET-ID-safe control ID for an action button, derived
    ''' from the row's NodeId and the action's ActionId (both always present -
    ''' NodeId identifies the row, ActionId the specific action within it, so the
    ''' pair is unique per button). MUST be deterministic: PopulateActions is called
    ''' fresh every DataBind AND every RepopulateGridActionsIfNeeded() postback, and
    ''' the resulting LinkButton's UniqueID has to match, byte-for-byte, the ID the
    ''' browser already has (from whatever response last rendered that row) for
    ''' __EVENTTARGET to resolve back to this control - a random ID (e.g.
    ''' Guid.NewGuid()) would mint a brand-new ID on every postback, so the ID the
    ''' browser posts back would never match anything in the freshly-rebuilt control
    ''' tree, ASP.NET would silently fail to find the control, and the postback
    ''' event (and GridView1_RowCommand) would never fire at all - which is exactly
    ''' the "clicking an action does nothing" symptom this fixes. Only letters,
    ''' digits and underscore are valid in a control ID, so anything else in
    ''' NodeId/ActionId is replaced with "_".
    ''' </summary>
    Private Function BuildActionButtonId(NodeId As String, ActionId As String) As String
        Dim raw As String = "cmd_" & If(NodeId, "") & "_" & If(ActionId, "")
        Dim sb As New System.Text.StringBuilder(raw.Length)
        For Each c As Char In raw
            sb.Append(If(Char.IsLetterOrDigit(c), c, "_"c))
        Next
        Return sb.ToString()
    End Function

    Private Sub PopulateActions(ph As PlaceHolder,
                            Actions As List(Of WorkflowAction),
                            NodeId As String)

        ph.Controls.Clear()

        For Each act As WorkflowAction In Actions

            ph.Controls.Add(New LiteralControl("<div class='menuRow'>"))

            Dim btn As New LinkButton()

            btn.ID = BuildActionButtonId(NodeId, act.ActionId)

            btn.Text = act.Icon & " " & act.Text

            ' Every action LinkButton raises the SAME CommandName ("ExecuteAction") -
            ' the actual action to run is carried in CommandArgument as
            ' "RequestID|ActionId|NodeId" and dispatched generically in
            ' GridView1_RowCommand. Routing on PERMISSION_NAME (act.CommandName)
            ' would break the moment someone renames a permission in the DB or adds
            ' a new one; ACTION_ID is the table's real, stable key.
            btn.CommandName = "ExecuteAction"

            btn.CommandArgument = act.CommandArgument & ActionCommandArgSeparator & act.ActionId & ActionCommandArgSeparator & NodeId

            btn.CssClass = "menuItem"

            btn.CausesValidation = False

            ph.Controls.Add(btn)

            ph.Controls.Add(New LiteralControl("</div>"))

        Next

    End Sub

End Class

Public Class WorkflowAction

    Public Property Text As String
    Public Property CommandName As String
    Public Property CommandArgument As String
    Public Property ActionId As String
    Public Property Icon As String
    Public Property CssClass As String = "menuItem"
    Public Property StatusSubtitle As String

End Class