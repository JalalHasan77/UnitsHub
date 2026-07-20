
Imports System.Data
Imports System.Globalization


Partial Class MainPage
    Inherits System.Web.UI.Page

    Private Shared ReadOnly BadgeColors As String() = {"badge-blue", "badge-green", "badge-orange", "badge-purple", "badge-teal", "badge-pink"}
    Private encryNdecry As New EncryDecry

    'Dim DT As DataTable
    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        If Not Page.IsPostBack Then
            PopulateDropDownList(ddl:=DropDownList1,
                                 sql:="Select PROJECT_NAME_EN,PROJECT_ID from UNITSHUB_PROJECTS",
                                 DataConnection:=EBDB_CS,
                                 firstItemIs:="Select A Project")
        End If
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


    Protected Sub DropDownList1_SelectedIndexChanged(sender As Object, e As EventArgs) Handles DropDownList1.SelectedIndexChanged
        Dim SQL As String = ""
        Dim DT As New DataTable

        Dim ProjectID As String = DropDownList1.SelectedItem.Value

        If ProjectID = "##" Then
            GridView1.DataSource = Nothing
            GridView1.DataBind()

            GridView2.DataSource = Nothing
            GridView2.DataBind()

            DataList2.DataSource = Nothing
            DataList2.DataBind()

            Label2.Text = ""

            Exit Sub
        End If

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

        SQL = SQL.Replace("@PRJID@", DropDownList1.SelectedItem.Value)
        Dim View As New DataTable
        View = DB.GetDataTable(EBDB, SQL)

        Dim DT0 As New DataTable
        DT0 = DB.GetDataTable(EBDB, "Select * from " & View.Rows(0)("VIEWNAME").ToString)

        'add the table to session so i can use it in other forms
        '=========================================================
        Call AddTBLtoSession(DT0)

        Dim counts As New Dictionary(Of String, Dictionary(Of String, Integer))
        counts = GetDistinctValueCounts(DT0)
        Dim SortedCounts As New Dictionary(Of String, Dictionary(Of String, Integer))
        Dim TEXT As String


        For Each columnEntry In counts
            TEXT = "Column: " & columnEntry.Key & vbTab & vbTab & vbTab & CType(columnEntry.Value, Dictionary(Of String, Integer)).Count

            Dim DicToSort As Dictionary(Of String, Integer) = CType(columnEntry.Value, Dictionary(Of String, Integer))

            Dim L As New List(Of String)
            L = DicToSort.Keys.ToList
            L.Sort()

            Dim NewDic As New Dictionary(Of String, Integer)
            For Each item In L
                NewDic.Add(item, DicToSort.Item(item))
            Next

            NewDic = GroupDictionaryIntoRanges(NewDic, 5)
            SortedCounts.Add(columnEntry.Key, NewDic)
        Next

        GridView1.DataSource = DT0
        GridView1.DataBind()

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



        Dim ProjectTable As String = DB.RetreiveScalarSTRING(EBDB, "Select VIEWNAME from UNITSHUB_VIEWS where PROJECT_ID ='" & ProjectID & "' and TREELEVEL =1")
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
    End Sub

    Private Sub AddTBLtoSession(DT0 As DataTable)

        Session("MainTable") = DT0
        VendorPopupHelper.RegisterVendorPopup(Me,
                                      btnFilter,
                                      "Filters.aspx?Parameters=MainTable",
                                      400,
                                      600,
                                      PopupPlacement.Center,
                                      "Select Adj",
                                      VendorPopupHelper.PopupDisplayMode.FrameOnly)

    End Sub

    Public Function GroupDictionaryIntoRanges(Values As Dictionary(Of String, Integer),
                                        NumberOfRanges As Integer) As Dictionary(Of String, Integer)

        Dim lcDic As New Dictionary(Of String, Integer)

        Dim Keys As New List(Of String)
        Keys = Values.Keys.ToList

        If Keys.All(AddressOf IsItNumeric) Then
            Return GroupDictionaryIntoRangesNumaric(Values, NumberOfRanges)
        Else
            Return GroupDictionaryIntoRangesString(Values, NumberOfRanges)
        End If
    End Function

    Public Function GroupDictionaryIntoRangesNumaric(
                                                    Values As Dictionary(Of String, Integer),
                                                    NumberOfRanges As Integer) As Dictionary(Of String, Integer)

        Dim lcDic As New Dictionary(Of String, Integer)

        Dim Keys As New List(Of String)
        Keys = Values.Keys.ToList

        Dim MinValue As Double = Keys.Min()
        Dim MaxValue As Double = Keys.Max()

        'Inclusive range width
        Dim Width As Integer = Math.Ceiling((MaxValue - MinValue + 1) / NumberOfRanges)
        Dim xLen As Integer = 10 ^ (CStr(Width).Length - 1)

        Width = Math.Floor(Width / xLen) * xLen

        For i As Integer = 0 To NumberOfRanges - 1

            Dim FromValue As Double = MinValue + (i * Width)

            Dim ToValue As Double
            ToValue = FromValue + Width - 1

            Dim Cnt As Integer = 0

            For Each v As String In Keys
                If v >= CStr(FromValue) AndAlso v <= CStr(ToValue) Then
                    Cnt += 1
                End If
            Next

            lcDic.Add(String.Format("{0:N0} - {1:N0}", FromValue, ToValue), Cnt)

        Next

        Return lcDic
    End Function


    Public Function GroupDictionaryIntoRangesString(
        Items As Dictionary(Of String, Integer),
        NumberOfGroups As Integer) As Dictionary(Of String, Integer)

        Dim Result As New Dictionary(Of String, Integer)

        Dim SortedItems = Items.OrderBy(
        Function(x) x.Key
    ).ToList()

        Dim TotalItems As Integer = SortedItems.Count

        If NumberOfGroups <= 0 OrElse TotalItems = 0 Then
            Return Result
        End If

        'Number of items per group
        Dim GroupSize As Integer = Math.Ceiling(TotalItems / NumberOfGroups)

        Dim Index As Integer = 0

        While Index < TotalItems

            Dim StartKey As String = SortedItems(Index).Key

            Dim EndIndex As Integer = Math.Min(Index + GroupSize - 1, TotalItems - 1)

            Dim EndKey As String = SortedItems(EndIndex).Key

            Dim Total As Integer = 0

            For i As Integer = Index To EndIndex

                Dim Value As Integer

                If Integer.TryParse(SortedItems(i).Value, Value) Then
                    Total += Value
                End If

            Next

            Result.Add(StartKey & " to " & EndKey, Total)

            Index += GroupSize

        End While

        Return Result

    End Function




    Function IsItNumeric(ByVal Item As Object) As Boolean
        If IsNumeric(Item) Then
            Return True
        Else
            Return False
        End If
    End Function



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

    '=====================================================================================
    '=====================================================================================
    '=====================================================================================
    '=====================================================================================

    ''' <summary>
    ''' Iterates through every column of a DataTable and counts how many times
    ''' each distinct value occurs within that column.
    ''' Returns: ColumnName -> (ValueAsString -> OccurrenceCount)
    ''' </summary>
    Public Function GetDistinctValueCounts(ByVal dt As DataTable) As Dictionary(Of String, Dictionary(Of String, Integer))
        Dim result As New Dictionary(Of String, Dictionary(Of String, Integer))()

        If dt Is Nothing Then
            Return result
        End If

        For Each col As DataColumn In dt.Columns
            Dim valueCounts As New Dictionary(Of String, Integer)()

            For Each dr As DataRow In dt.Rows
                Dim rawValue As Object = dr(col)
                Dim key As String = If(IsDBNull(rawValue) OrElse rawValue Is Nothing, "(null)", Convert.ToString(rawValue))

                If valueCounts.ContainsKey(key) Then
                    valueCounts(key) += 1
                Else
                    valueCounts(key) = 1
                End If
            Next

            result(col.ColumnName) = valueCounts
        Next

        Return result
    End Function
    Protected Sub btnFilter_Click(sender As Object, e As EventArgs) Handles btnFilter.Click
        VendorPopupHelper.RegisterVendorPopup(Me,
                              btnFilter,
                              "Filters.aspx?Parameters=MainTable",
                              400,
                              600,
                              PopupPlacement.Center,
                              "Select Adj",
                              VendorPopupHelper.PopupDisplayMode.FrameOnly)
    End Sub
End Class