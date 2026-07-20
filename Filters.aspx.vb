
Imports System.Data

Partial Class Filters
    Inherits System.Web.UI.Page

    Private Sub Filters_Load(sender As Object, e As EventArgs) Handles Me.Load

        Dim MainTable As Data.DataTable = Session("MainTable")
        Call MakeFilterGridView(MainTable:=MainTable)

    End Sub

    Private Sub MakeFilterGridView(MainTable As DataTable)
        Dim counts As New Dictionary(Of String, Dictionary(Of String, Integer))
        counts = GetDistinctValueCounts(MainTable)
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

        Dim DS As New DataSet
        DS = ConvertToDataSet(sortedCounts:=SortedCounts)


        Dim Tables As New List(Of TableViewModel)

        For Each dt As DataTable In ds.Tables

            Tables.Add(New TableViewModel With {
        .Title = dt.TableName,
        .Rows = dt
    })

        Next

        rptTables.DataSource = Tables
        rptTables.DataBind()

    End Sub


    Public Function ConvertToDataSet(sortedCounts As Dictionary(Of String, Dictionary(Of String, Integer))) As DataSet
        Dim ds As New DataSet()

        For Each outerKvp In sortedCounts
            ' Sanitize table name in case keys contain invalid characters
            Dim dt As New DataTable(outerKvp.Key)

            dt.Columns.Add("Key", GetType(String))
            dt.Columns.Add("Count", GetType(Integer))

            For Each innerKvp In outerKvp.Value
                dt.Rows.Add(innerKvp.Key, innerKvp.Value)
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

    Protected Sub rptTables_ItemDataBound(sender As Object, e As RepeaterItemEventArgs) Handles rptTables.ItemDataBound
        If e.Item.ItemType = ListItemType.Item OrElse
      e.Item.ItemType = ListItemType.AlternatingItem Then

            Dim tbl = CType(e.Item.DataItem, TableViewModel)

            Dim rpt As Repeater =
                CType(e.Item.FindControl("rptRows"), Repeater)

            rpt.DataSource = tbl.Rows
            rpt.DataBind()

        End If
    End Sub
End Class


Public Class TableViewModel
    Public Property Title As String
    Public Property Rows As DataTable
End Class
