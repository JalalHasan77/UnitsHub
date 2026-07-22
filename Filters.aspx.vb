
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






        If Not Page.IsPostBack Then

            Dim ProjectID As String = Request("ProjectID")
            Label3.Text = ProjectID
            RefreshData()



        End If
    End Sub


    Sub RefreshData()
        Dim MainTable As Data.DataTable = Session("FilterMainTable")

        Dim Filter As String = BuildFilterExpression(GetCheckedCategories())


        'Refine table to show only "Searchable Fields" — only needs to run once per session,
        'not on every postback, since MainTable/ProjectID don't change afterward.
        Dim DT As New DataTable
        DT = GetDataTable(EBDB, "Select NAME_IN_UI from UNITSHUB_ATTRIBUTES_PROPERTIES where PROJECT_ID='" & Label3.Text & "' and SEARCHABEL = 'Y'")
        KeepOnlyColumns(MainTable, DT)

        If Filter.Trim <> "" Then
            Dim DR() As DataRow
            DR = MainTable.Select(Filter.Trim)
            MainTable = DR.CopyToDataTable
        End If

        Call MakeFilterGridView(MainTable:=MainTable)
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

    ''' <summary>
    ''' Walks GridView1 (outer) and each row's nested GridView2 (inner), collecting the
    ''' "Categories" of every checked CheckBox1.
    ''' Key = the outer row's Title (from Label1); Value = the checked categories for that title.
    ''' Rows with no checked boxes are skipped (not added with an empty list).
    ''' </summary>
    Public Function GetCheckedCategories() As Dictionary(Of String, List(Of String))
        Dim Result As Dictionary(Of String, List(Of String))

        If Session("Result") IsNot Nothing Then
            Result = CType(Session("Result"), Dictionary(Of String, List(Of String)))
        Else
            Result = New Dictionary(Of String, List(Of String))
        End If

        For Each OuterRow As GridViewRow In GridView1.Rows
            If OuterRow.RowType <> DataControlRowType.DataRow Then Continue For

            Dim TitleLabel As Label = CType(OuterRow.FindControl("Label1"), Label)
            Dim InnerGrid As GridView = CType(OuterRow.FindControl("GridView2"), GridView)

            If TitleLabel Is Nothing OrElse InnerGrid Is Nothing Then Continue For

            Dim Title As String = TitleLabel.Text
            Dim CheckedCategories As New List(Of String)

            For Each InnerRow As GridViewRow In InnerGrid.Rows
                If InnerRow.RowType <> DataControlRowType.DataRow Then Continue For

                Dim Chk As CheckBox = CType(InnerRow.FindControl("CheckBox1"), CheckBox)
                If Chk Is Nothing OrElse Not Chk.Checked Then Continue For

                Dim CategoriesRaw As String = Chk.Attributes("Categories")
                If Not String.IsNullOrEmpty(CategoriesRaw) Then
                    CheckedCategories.AddRange(CategoriesRaw.Split("|"c))
                End If
            Next

            ' Refresh this title's entry to reflect the current checkbox state:
            ' overwrite if it exists, add if new, remove if nothing is checked anymore.
            If CheckedCategories.Count > 0 Then
                Result(Title) = CheckedCategories
            ElseIf Result.ContainsKey(Title) Then
                Result.Remove(Title)
            End If
        Next

        Session("Result") = Result
        Return Result
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
            Dim ToValue As Double = FromValue + Width '- 1

            Dim Cnt As Integer = 0
            Dim CatList As New List(Of String)

            For Each nv As Double In NumericKeys
                If nv >= FromValue AndAlso nv < ToValue Then
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
        Dim CheckedCategories As List(Of String)
        If Session("CheckedCheckBox") IsNot Nothing Then
            CheckedCategories = CType(Session("CheckedCheckBox"), List(Of String))
        Else
            CheckedCategories = New List(Of String)
        End If

        Dim Chk As CheckBox
        Chk = CType(sender, CheckBox)
        Dim CategoriesRaw As String = Chk.Attributes("Categories")

        If Not String.IsNullOrEmpty(CategoriesRaw) Then
            Dim ItemsToToggle As String() = CategoriesRaw.Split("|"c)

            If Chk.Checked = True Then
                For Each Item As String In ItemsToToggle
                    If Not CheckedCategories.Contains(Item) Then
                        CheckedCategories.Add(Item)
                    End If
                Next
            Else
                For Each Item As String In ItemsToToggle
                    CheckedCategories.Remove(Item)
                Next
            End If
        End If
        Session("CheckedCheckBox") = CheckedCategories

        RefreshData()
    End Sub
    Protected Sub GridView2_RowDataBound(sender As Object, e As GridViewRowEventArgs)
        Dim R As GridViewRow
        R = e.Row
        If R.RowType = DataControlRowType.DataRow Then
            Dim chk As CheckBox
            chk = R.FindControl("CheckBox1")
            chk.Text = R.DataItem("Key").ToString
            chk.Attributes.Add("Categories", R.DataItem("Categories").ToString)
            Dim CheckedCategories As List(Of String)
            If Session("CheckedCheckBox") IsNot Nothing Then
                CheckedCategories = CType(Session("CheckedCheckBox"), List(Of String))

                If (chk.Text.Contains(" to ") Or chk.Text.Contains(" - ")) = False Then
                    If CheckedCategories.Contains(chk.Text) Then
                        chk.Checked = True
                    End If
                Else
                    Dim Parts() As String
                    If chk.Text.Contains(" to ") Then
                        Parts = chk.Text.Split(New String() {" to "}, StringSplitOptions.None)
                    Else
                        Parts = chk.Text.Split(New String() {" - "}, StringSplitOptions.None)
                    End If

                    Dim FromPart As String = Parts(0).Trim()
                    Dim ToPart As String = Parts(1).Trim()

                    Dim IsInRange As Boolean = False

                    If IsNumeric(FromPart) AndAlso IsNumeric(ToPart) Then
                        Dim FromVal As Double = CDbl(FromPart)
                        Dim ToVal As Double = CDbl(ToPart)

                        For Each Item As String In CheckedCategories
                            If IsNumeric(Item) Then
                                Dim ItemVal As Double = CDbl(Item)
                                If ItemVal >= FromVal AndAlso ItemVal <= ToVal Then
                                    IsInRange = True
                                    Exit For
                                End If
                            End If
                        Next
                    Else
                        For Each Item As String In CheckedCategories
                            If String.Compare(Item, FromPart) >= 0 AndAlso String.Compare(Item, ToPart) <= 0 Then
                                IsInRange = True
                                Exit For
                            End If
                        Next
                    End If

                    chk.Checked = IsInRange
                End If

            End If

        End If

    End Sub
    ''' <summary>
    ''' OK: builds the DataTable.Select-style FilterExpression from the currently checked
    ''' boxes and hands it back to the parent window, mirroring how
    ''' AddMultipleItemsFromList.Button1_Click returns its selection via VendorPopupHelper.
    ''' </summary>
    Protected Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Dim MainTable As DataTable = Session("FilterMainTable")
        Dim FilterExpression As String = BuildFilterExpression(GetCheckedCategories(), MainTable)

        hfFilter.Value = FilterExpression

        VendorPopupHelper.RegisterPopupSelectionAndClose(
            page:=Me,
            returnValue:=FilterExpression,
            startupScriptKey:="FilterExpression",
            skipPostBack:=False)
    End Sub

    ''' <summary>
    ''' Cancel: closes the popup without returning anything. Client-side only
    ''' (Button2 has UseSubmitBehavior="false" and its own OnClientClick), but this
    ''' handler stays in place in case the button is ever wired to Handles Button2.Click
    ''' as well - e.g. for non-JS fallback / server-side cleanup.
    ''' </summary>
    Protected Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        Dim script As String = "(function () {" &
               "    if (window.parent && typeof window.parent.closeVendorDialog === 'function') {" &
               "        window.parent.closeVendorDialog();" &
               "    }" &
               "})();"

        If ScriptManager.GetCurrent(Me) IsNot Nothing Then
            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "ClosePopupOnly", script, True)
        Else
            Me.ClientScript.RegisterStartupScript(Me.GetType(), "ClosePopupOnly", script, True)
        End If
    End Sub
End Class


Public Class TableViewModel
    Public Property Title As String
    Public Property Rows As DataTable
End Class
