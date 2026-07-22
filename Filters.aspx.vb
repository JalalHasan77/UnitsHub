
Imports System.Data
Imports System.Linq

Public Class RangeGroupInfo
    Public Property Count As Integer
    Public Property Categories As New List(Of String)
End Class

Partial Class Filters
    Inherits System.Web.UI.Page

    ' Transient holder used only while rptTables_ItemDataBound -> rpt.DataBind() -> rptRows_ItemDataBound
    ' is on the call stack, so the inner handler knows which column/table it's binding rows for.
    ' (RepeaterItem.DataItem is only valid during binding, not afterward, so this is captured here
    ' and then stamped onto each checkbox as an attribute for later use.)
    Private _currentColumnName As String

    Private Sub Filters_Load(sender As Object, e As EventArgs) Handles Me.Load

        Dim MainTable As Data.DataTable = Session("MainTable")
        Dim ProjectID As String = Request("ProjectID")
        Dim FilterExpression As String = ""
        If Not Page.IsPostBack Then
            'Refine table to show only "Searchable Fields" — only needs to run once per session,
            'not on every postback, since MainTable/ProjectID don't change afterward.
            Dim DT As New DataTable
            DT = GetDataTable(EBDB, "Select NAME_IN_UI from UNITSHUB_ATTRIBUTES_PROPERTIES where PROJECT_ID='" & ProjectID & "' and SEARCHABEL = 'Y'")
            KeepOnlyColumns(MainTable, DT)
            Session("MainTable") = MainTable

            Call MakeFilterGridView(MainTable:=MainTable)

        End If

        If hfFilter.Value <> "" Then
            Dim DR() As DataRow
            DR = MainTable.Select(hfFilter.Value)
            MainTable = DR.CopyToDataTable
        End If

        ' Runs on every request (initial load AND postback): the Repeater tree must be rebuilt
        ' identically every time for control IDs to line up.


        ' Because chkSelect sits two Repeaters deep and both are fully rebuilt above, ASP.NET's
        ' automatic "reapply the posted checkbox value onto the recreated control" mechanism is
        ' unreliable at this nesting depth (this is what was causing the checked state to revert
        ' immediately after postback). Instead, read each checkbox's actual posted value directly
        ' from Request.Form now that the tree exists, and drive Checked/Session from that.
        SyncCheckboxSelectionsFromForm()

    End Sub

    ''' <summary>
    ''' Converts the SelectedFilters dictionary (ColumnName -> selected raw values) into a
    ''' string usable with DataTable.Select(filterExpression).
    ''' Logic: values within the same column are OR'd together; different columns are AND'd.
    ''' e.g. { "Price" -> {"1000","2000"}, "Status" -> {"Available"} }
    '''   -> "([Price] = '1000' OR [Price] = '2000') AND ([Status] = 'Available')"
    ''' </summary>
    ''' <param name="SelectedFilters">The selection dictionary from GetSelectedFilters().</param>
    ''' <param name="dt">
    ''' Optional: the DataTable being filtered. If supplied, numeric columns are emitted as
    ''' unquoted numeric literals (required by DataTable.Select for numeric column types);
    ''' otherwise every value is treated as a quoted string.
    ''' </param>
    Public Function BuildFilterExpression(SelectedFilters As Dictionary(Of String, List(Of String)),
                                           Optional dt As DataTable = Nothing) As String

        If SelectedFilters Is Nothing OrElse SelectedFilters.Count = 0 Then
            Return String.Empty
        End If

        Dim columnExpressions As New List(Of String)

        For Each kvp In SelectedFilters
            Dim columnName As String = kvp.Key
            Dim values As List(Of String) = kvp.Value

            If values Is Nothing OrElse values.Count = 0 Then Continue For

            Dim isNumeric As Boolean = False
            If dt IsNot Nothing AndAlso dt.Columns.Contains(columnName) Then
                Dim colType As Type = dt.Columns(columnName).DataType
                isNumeric = colType Is GetType(Integer) OrElse
                            colType Is GetType(Double) OrElse
                            colType Is GetType(Decimal) OrElse
                            colType Is GetType(Single) OrElse
                            colType Is GetType(Long) OrElse
                            colType Is GetType(Short)
            End If

            Dim valueExpressions As New List(Of String)
            For Each v As String In values
                ' Escape single quotes for DataTable.Select's expression syntax
                Dim safeValue As String = v.Replace("'", "''")

                If isNumeric Then
                    valueExpressions.Add(String.Format("[{0}] = {1}", columnName, safeValue))
                Else
                    valueExpressions.Add(String.Format("[{0}] = '{1}'", columnName, safeValue))
                End If
            Next

            columnExpressions.Add("(" & String.Join(" OR ", valueExpressions) & ")")
        Next

        Return String.Join(" AND ", columnExpressions)
    End Function

    Public Sub KeepOnlyColumns(dt As DataTable, desiredColumns As List(Of String))
        ' Iterate backwards since we're removing items from the collection while looping
        For i As Integer = dt.Columns.Count - 1 To 0 Step -1
            Dim colName As String = dt.Columns(i).ColumnName

            If Not desiredColumns.Contains(colName, StringComparer.OrdinalIgnoreCase) Then
                dt.Columns.RemoveAt(i)
            End If
        Next
    End Sub

    Public Sub KeepOnlyColumns(dt As DataTable, desiredColumns As DataTable)
        Dim L As New List(Of String)
        For Each DR As DataRow In desiredColumns.Rows
            L.Add(DR(0).ToString)
        Next
        KeepOnlyColumns(dt, L)
    End Sub




    Private Sub MakeFilterGridView(MainTable As DataTable)
        Dim counts As New Dictionary(Of String, Dictionary(Of String, Integer))
        counts = GetDistinctValueCounts(MainTable)
        Dim SortedCounts As New Dictionary(Of String, Dictionary(Of String, RangeGroupInfo))
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

            Dim GroupedDic As Dictionary(Of String, RangeGroupInfo)

            If NewDic.Count > 5 Then
                GroupedDic = GroupDictionaryIntoRanges(NewDic, 5)
            Else
                ' Not grouped: each distinct value is its own bucket of one category
                GroupedDic = New Dictionary(Of String, RangeGroupInfo)
                For Each kvp In NewDic
                    GroupedDic.Add(kvp.Key, New RangeGroupInfo With {
                        .Count = kvp.Value,
                        .Categories = New List(Of String) From {kvp.Key}
                    })
                Next
            End If

            SortedCounts.Add(columnEntry.Key, GroupedDic)
        Next

        Dim DS As New DataSet
        DS = ConvertToDataSet(sortedCounts:=SortedCounts)
        Dim Tables As New List(Of TableViewModel)

        For Each dt As DataTable In DS.Tables

            Tables.Add(New TableViewModel With {
        .Title = dt.TableName,
        .Rows = dt
    })

        Next

        'rptTables.DataSource = Tables
        'rptTables.DataBind()

        GridView1.DataSource = Tables
        GridView1.DataBind()


    End Sub


    Public Function ConvertToDataSet(sortedCounts As Dictionary(Of String, Dictionary(Of String, RangeGroupInfo))) As DataSet
        Dim ds As New DataSet()

        For Each outerKvp In sortedCounts
            ' Sanitize table name in case keys contain invalid characters
            Dim dt As New DataTable(outerKvp.Key)

            dt.Columns.Add("Key", GetType(String))
            dt.Columns.Add("Count", GetType(Integer))
            dt.Columns.Add("Categories", GetType(String))

            For Each innerKvp In outerKvp.Value
                Dim catString As String = String.Join("|", innerKvp.Value.Categories)
                dt.Rows.Add(innerKvp.Key, innerKvp.Value.Count, catString)
            Next

            ds.Tables.Add(dt)
        Next

        Return ds
    End Function

    Private Function BuildDisplayTable(ds As DataSet) As DataTable
        Dim dt As New DataTable("Display")
        dt.Columns.Add("TableName", GetType(String))
        dt.Columns.Add("DisplayText", GetType(String))
        dt.Columns.Add("Selected", GetType(Boolean))
        dt.Columns.Add("Key", GetType(String))   ' kept for reference/lookup if needed

        For Each table As DataTable In ds.Tables
            For Each row As DataRow In table.Rows
                Dim keyVal As String = row("Key").ToString()
                Dim countVal As String = row("Count").ToString()
                dt.Rows.Add(table.TableName, keyVal & " (" & countVal & ")", False, keyVal)
            Next
        Next

        Return dt
    End Function



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


    Public Function GroupDictionaryIntoRanges(Values As Dictionary(Of String, Integer),
                                        NumberOfRanges As Integer) As Dictionary(Of String, RangeGroupInfo)

        Dim Keys As New List(Of String)
        Keys = Values.Keys.ToList

        If Keys.All(AddressOf IsItNumeric) Then
            Try
                Return GroupDictionaryIntoRangesNumaric(Values, NumberOfRanges)
            Catch ex As Exception
                IO.File.WriteAllText("\\eskanbank.com\EBUP\EBU\2271\Desktop\UnitsHub\Values.txt", Join(Values.Keys.ToList.ToArray, vbCrLf))
                MsgBox("done")
                Return New Dictionary(Of String, RangeGroupInfo)
            End Try

        Else
            Return GroupDictionaryIntoRangesString(Values, NumberOfRanges)
        End If
    End Function

    Public Function GroupDictionaryIntoRangesNumaric(
                                                Values As Dictionary(Of String, Integer),
                                                NumberOfRanges As Integer) As Dictionary(Of String, RangeGroupInfo)
        Dim lcDic As New Dictionary(Of String, RangeGroupInfo)

        Dim NumericKeys As New List(Of Double)
        Dim KeyLookup As New Dictionary(Of Double, String)  ' maps parsed number back to its original string form
        For Each k As String In Values.Keys
            Dim d As Double = CDbl(k)
            NumericKeys.Add(d)
            If Not KeyLookup.ContainsKey(d) Then KeyLookup.Add(d, k)
        Next

        Dim MinValue As Double = NumericKeys.Min()
        Dim MaxValue As Double = NumericKeys.Max()

        'Inclusive range width
        Dim Width As Integer = Math.Ceiling((MaxValue - MinValue + 1) / NumberOfRanges)
        Dim xLen As Integer = 10 ^ (CStr(Width).Length - 1)
        Width = Math.Floor(Width / xLen) * xLen

        For i As Integer = 0 To NumberOfRanges - 1
            Dim FromValue As Double = MinValue + (i * Width)
            Dim ToValue As Double = FromValue + Width - 1

            Dim Cnt As Integer = 0
            Dim CatList As New List(Of String)

            For Each nv As Double In NumericKeys
                If nv >= FromValue AndAlso nv <= ToValue Then
                    Cnt += 1
                    CatList.Add(KeyLookup(nv))
                End If
            Next

            lcDic.Add(String.Format("{0:N0} - {1:N0}", FromValue, ToValue),
                      New RangeGroupInfo With {.Count = Cnt, .Categories = CatList})
        Next

        Return lcDic
    End Function


    Public Function GroupDictionaryIntoRangesString(
        Items As Dictionary(Of String, Integer),
        NumberOfGroups As Integer) As Dictionary(Of String, RangeGroupInfo)

        Dim Result As New Dictionary(Of String, RangeGroupInfo)

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
            Dim CatList As New List(Of String)

            For i As Integer = Index To EndIndex

                Dim Value As Integer

                If Integer.TryParse(SortedItems(i).Value, Value) Then
                    Total += Value
                End If

                CatList.Add(SortedItems(i).Key)

            Next

            Dim RangeName As String =
    If(StartKey = EndKey,
       StartKey,
       StartKey & " to " & EndKey)

            Result.Add(RangeName, New RangeGroupInfo With {.Count = Total, .Categories = CatList})

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

    Protected Sub rptTables_ItemDataBound(sender As Object, e As RepeaterItemEventArgs) Handles rptTables.ItemDataBound
        If e.Item.ItemType = ListItemType.Item OrElse
      e.Item.ItemType = ListItemType.AlternatingItem Then

            Dim tbl = CType(e.Item.DataItem, TableViewModel)

            Dim rpt As Repeater =
                CType(e.Item.FindControl("rptRows"), Repeater)

            _currentColumnName = tbl.Title
            rpt.DataSource = tbl.Rows
            rpt.DataBind()

        End If
    End Sub

    Protected Sub rptRows_ItemDataBound(sender As Object, e As RepeaterItemEventArgs)
        If e.Item.ItemType = ListItemType.Item OrElse
           e.Item.ItemType = ListItemType.AlternatingItem Then

            Dim rowView As DataRowView = CType(e.Item.DataItem, DataRowView)

            Dim chk As CheckBox = CType(e.Item.FindControl("chkSelect"), CheckBox)
            If chk Is Nothing Then Exit Sub

            chk.Attributes("Categories") = If(rowView("Categories") IsNot Nothing, rowView("Categories").ToString(), "")
            chk.Attributes("ColumnName") = _currentColumnName
            'chk.Attributes("onclick") = "alert(this.getAttribute('Categories'));"

        End If
    End Sub

    ''' <summary>
    ''' Active filter selections for this user's session.
    ''' Key = column name (e.g. "Price"), Value = list of raw values currently selected for that column.
    ''' Stored server-side in Session so it can't be tampered with client-side and doesn't bloat ViewState.
    ''' </summary>
    Private Function GetSelectedFilters() As Dictionary(Of String, List(Of String))
        If Session("SelectedFilters") Is Nothing Then
            Session("SelectedFilters") = New Dictionary(Of String, List(Of String))
        End If
        Return CType(Session("SelectedFilters"), Dictionary(Of String, List(Of String)))
    End Function

    ''' <summary>
    ''' Walks the already-bound rptTables/rptRows tree and syncs each checkbox's Checked state:
    ''' - On postback: trusts Request.Form(chk.UniqueID) directly (the value the browser just posted),
    '''   since automatic postback-data restoration is unreliable for controls this deep in nested,
    '''   fully-rebuilt Repeaters. Updates Session to match.
    ''' - On initial GET: restores Checked from previously saved Session selections.
    ''' </summary>
    Private Sub SyncCheckboxSelectionsFromForm()
        Dim SelectedFilters As Dictionary(Of String, List(Of String)) = GetSelectedFilters()

        For Each tableItem As RepeaterItem In rptTables.Items
            If tableItem.ItemType <> ListItemType.Item AndAlso tableItem.ItemType <> ListItemType.AlternatingItem Then Continue For

            Dim innerRpt As Repeater = CType(tableItem.FindControl("rptRows"), Repeater)
            If innerRpt Is Nothing Then Continue For

            For Each rowItem As RepeaterItem In innerRpt.Items
                If rowItem.ItemType <> ListItemType.Item AndAlso rowItem.ItemType <> ListItemType.AlternatingItem Then Continue For

                Dim chk As CheckBox = CType(rowItem.FindControl("chkSelect"), CheckBox)
                If chk Is Nothing Then Continue For

                ' Note: RepeaterItem.DataItem is only valid during ItemDataBound — it's Nothing by
                ' the time we get here. columnName/Categories were stamped onto the checkbox's
                ' Attributes while binding was in progress (see rptRows_ItemDataBound), so read
                ' them back from there instead.
                Dim columnName As String = chk.Attributes("ColumnName")
                Dim categoriesStr As String = chk.Attributes("Categories")
                Dim categoryList As String() = If(String.IsNullOrEmpty(categoriesStr), New String() {}, categoriesStr.Split("|"c))

                If String.IsNullOrEmpty(columnName) Then Continue For

                If Page.IsPostBack Then
                    Dim isChecked As Boolean = (Request.Form(chk.UniqueID) IsNot Nothing)
                    chk.Checked = isChecked

                    If isChecked Then
                        If Not SelectedFilters.ContainsKey(columnName) Then SelectedFilters(columnName) = New List(Of String)
                        For Each v As String In categoryList
                            If Not SelectedFilters(columnName).Contains(v) Then SelectedFilters(columnName).Add(v)
                        Next
                    Else
                        If SelectedFilters.ContainsKey(columnName) Then
                            For Each v As String In categoryList
                                SelectedFilters(columnName).Remove(v)
                            Next
                            If SelectedFilters(columnName).Count = 0 Then SelectedFilters.Remove(columnName)
                        End If
                    End If


                Else
                    ' Initial GET: reflect any selections already saved in Session (e.g. user navigated back)
                    If SelectedFilters.ContainsKey(columnName) Then
                        If categoryList.Intersect(SelectedFilters(columnName)).Any() Then
                            chk.Checked = True
                        End If
                    End If
                End If
            Next
        Next
        Session("SelectedFilters") = SelectedFilters

        If SelectedFilters.Count > 0 Then
            hfFilter.Value = BuildFilterExpression(SelectedFilters)
        End If



    End Sub
    Protected Sub GridView1_RowDataBound(sender As Object, e As GridViewRowEventArgs) Handles GridView1.RowDataBound
        Dim R As GridViewRow
        R = e.Row

        If R.RowType = DataControlRowType.DataRow Then
            Dim GV As GridView
            GV = R.FindControl("GridView2")
            Dim table As TableViewModel = CType(e.Row.DataItem, TableViewModel)

            'Access the properties
            Dim title As String = table.Title
            Dim dt As DataTable = table.Rows

            GV.DataSource = dt
            GV.DataBind()

        End If
    End Sub
    Protected Sub CheckBox1_CheckedChanged(sender As Object, e As EventArgs)
        Dim CHK As CheckBox
        CHK = CType(sender, CheckBox)
        MsgBox(CHK.Attributes("Categories"))
    End Sub
    Protected Sub GridView2_RowDataBound(sender As Object, e As GridViewRowEventArgs)
        Dim R As GridViewRow
        R = e.Row
        If R.RowType = DataControlRowType.DataRow Then
            Dim chk As CheckBox
            chk = R.FindControl("CheckBox1")
            chk.Text = R.DataItem("Key").ToString
            chk.Attributes.Add("Categories", R.DataItem("Categories").ToString)

        End If

    End Sub
End Class


Public Class TableViewModel
    Public Property Title As String
    Public Property Rows As DataTable
End Class
