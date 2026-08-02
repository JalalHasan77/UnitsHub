
Imports System.Data
Imports System.Globalization
Imports System.Collections.Generic
Imports System.Linq
Imports System.Web
Imports System.Web.Caching


Partial Class MainPage
    Inherits System.Web.UI.Page

    Private Shared ReadOnly BadgeColors As String() = {"badge-blue", "badge-green", "badge-orange", "badge-purple", "badge-teal", "badge-pink"}
    Private encryNdecry As New EncryDecry

    ''' <summary>
    ''' Per-request cache for GetAvailableActions(). The underlying query currently
    ''' returns the same result set for every row (its WHERE clause doesn't actually
    ''' branch on RequestID/State), so without this, binding a grid with N rows fires
    ''' N identical DB round-trips - and RepopulateGridActionsIfNeeded() repeats that
    ''' on EVERY postback, even ones unrelated to the grid. Keyed by state so that if
    ''' the query is later fixed to genuinely vary by state, it still only runs once
    ''' per distinct state per request rather than once per row.
    ''' </summary>
    Private ReadOnly _actionsCache As New Dictionary(Of String, List(Of WorkflowAction))

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

    'Dim DT As DataTable
    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        If Not Page.IsPostBack Then
            PopulateDropDownList(ddl:=DropDownList1,
                                 sql:="Select PROJECT_NAME_EN,PROJECT_ID from UNITSHUB_PROJECTS",
                                 DataConnection:=EBDB_CS,
                                 firstItemIs:="Select A Project")
        End If

        RepopulateGridActionsIfNeeded()
    End Sub

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

            Dim Actions As List(Of WorkflowAction) = GetAvailableActions(RequestID, State)
            PopulateActions(ph, Actions)
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
    ''' </summary>
    Private Shared ReadOnly DuplicatedAutoGeneratedFields As String() = {"STATUS", "Status_Subtitle"}

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

        Dim Actions As List(Of WorkflowAction) =
            GetAvailableActions(RequestID, State)

        PopulateActions(ph, Actions)



    End Sub

    ''' <summary>
    ''' Fires when any LinkButton inside a GridView1 row raises a command - including the
    ''' Approve/Bounce/Edit/History LinkButtons added dynamically in PopulateActions, whose
    ''' Command events bubble up automatically because they live inside a GridView row.
    ''' e.CommandName matches the CommandName set on each WorkflowAction (e.g. "Approve"),
    ''' and e.CommandArgument carries the RequestID passed as CommandArgument.
    ''' </summary>
    Protected Sub GridView1_RowCommand(sender As Object, e As GridViewCommandEventArgs) Handles GridView1.RowCommand

        Dim RequestID As String = Convert.ToString(e.CommandArgument)

        Select Case e.CommandName
            Case "Approve"
                ApproveRequest(RequestID)

            Case "Bounce"
                BounceRequest(RequestID)

            Case "Edit"
                EditRequest(RequestID)

            Case "History"
                ShowRequestHistory(RequestID)
        End Select

    End Sub

    Private Sub ApproveRequest(RequestID As String)
        ' TODO: your approve logic here (update status, save, etc.)

        loadData() ' rebind so the grid (and each row's available actions) reflects the new state
    End Sub

    Private Sub BounceRequest(RequestID As String)
        ' TODO: your bounce logic here

        loadData()
    End Sub

    Private Sub EditRequest(RequestID As String)
        ' TODO: your edit logic here - e.g. open an edit popup for RequestID
    End Sub

    Private Sub ShowRequestHistory(RequestID As String)
        ' TODO: your history logic here - e.g. open a history popup for RequestID
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


    Public Function GetAvailableActions(ByVal RequestID As String,
                                    ByVal CurrentUserID As String) As List(Of WorkflowAction)

        Dim cacheKey As String = If(CurrentUserID, "")
        Dim cachedActions As List(Of WorkflowAction) = Nothing
        If _actionsCache.TryGetValue(cacheKey, cachedActions) Then
            ' Same action list already fetched earlier in this request/postback for
            ' this state - reuse it instead of hitting the DB again for this row.
            Return CloneActionsFor(cachedActions, RequestID)
        End If

        Dim Actions As New List(Of WorkflowAction)


        Dim DT As New DataTable
        Dim SQL As String = ""
        SQL = SQL + " SELECT * FROM UNITSHUB_PRJ_STT_ACT_USR SAU"
        SQL = SQL + "   join "
        SQL = SQL + "   UNITSHUB_ACTIONS AC"
        SQL = SQL + "   on SAU.ACTION_ID = AC.ACTION_ID"
        SQL = SQL + "   inner join UNITSHUB_UNITSSTATUS S"
        SQL = SQL + "   ON SAU.STATE_ID = S.STATE_ID"
        SQL = SQL + "   where SAU.STATE_ID='000' and SAU.USERS like '%2271%'"


        DT = GetDataTable(EBDB, SQL)

        For Each DR As DataRow In DT.Rows
            Actions.Add(New WorkflowAction With {
            .Text = DR("Text"),
            .CommandName = DR("Text"),
                .CommandArgument = RequestID,
                .Icon = DR("ICON").ToString
            })
        Next

        _actionsCache(cacheKey) = Actions
        Return CloneActionsFor(Actions, RequestID)
    End Function

    ''' <summary>
    ''' The cached action list is shared across rows, but CommandArgument must carry
    ''' each row's own RequestID (it's what GridView1_RowCommand uses to know which
    ''' record to act on), so return per-row copies with CommandArgument rebound
    ''' rather than mutating - or returning - the single shared cached list.
    ''' </summary>
    Private Function CloneActionsFor(source As List(Of WorkflowAction), RequestID As String) As List(Of WorkflowAction)
        Return source.Select(Function(a) New WorkflowAction With {
            .Text = a.Text,
            .CommandName = a.CommandName,
            .CommandArgument = RequestID,
            .Icon = a.Icon,
            .CssClass = a.CssClass
        }).ToList()
    End Function

    Private Sub PopulateActions(ph As PlaceHolder,
                            Actions As List(Of WorkflowAction))

        ph.Controls.Clear()

        For Each act As WorkflowAction In Actions

            ph.Controls.Add(New LiteralControl("<div class='menuRow'>"))

            Dim btn As New LinkButton()

            btn.ID = "cmd" & Guid.NewGuid().ToString("N")

            btn.Text = act.Icon & " " & act.Text

            btn.CommandName = act.CommandName

            btn.CommandArgument = act.CommandArgument

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
    Public Property Icon As String
    Public Property CssClass As String = "menuItem"

End Class

