Imports System.Data
Imports Microsoft.VisualBasic
Imports System.Collections.Generic
Imports System.Web.UI.WebControls


Public Module PF
    Public Function ConvertSelectedItemsToDataTable(
        ByVal items As List(Of Dictionary(Of String, Object))
    ) As DataTable

        Dim dt As New DataTable("SelectedItems")

        If items Is Nothing OrElse items.Count = 0 Then
            Return dt
        End If

        ' Create columns from all keys found in all rows
        For Each item As Dictionary(Of String, Object) In items
            If item Is Nothing Then Continue For

            For Each key As String In item.Keys
                If Not dt.Columns.Contains(key) Then
                    dt.Columns.Add(key, GetType(Object))
                End If
            Next
        Next

        ' Fill rows
        For Each item As Dictionary(Of String, Object) In items
            Dim dr As DataRow = dt.NewRow()

            For Each col As DataColumn In dt.Columns
                If item IsNot Nothing AndAlso item.ContainsKey(col.ColumnName) Then
                    If item(col.ColumnName) Is Nothing Then
                        dr(col.ColumnName) = DBNull.Value
                    Else
                        dr(col.ColumnName) = item(col.ColumnName)
                    End If
                Else
                    dr(col.ColumnName) = DBNull.Value
                End If
            Next

            dt.Rows.Add(dr)
        Next

        Return dt
    End Function


    ''' <summary>
    ''' Returns every column's header text for the GridView, regardless of whether
    ''' it is currently hidden or shown. Works for both explicitly declared columns
    ''' (AutoGenerateColumns="False") and auto-generated columns (AutoGenerateColumns="True",
    ''' which is how GridView1 in MainPage.aspx is configured).
    ''' </summary>
    Public Function GetAllColumnNames(ByVal gv As GridView) As List(Of String)
        Dim columnNames As New List(Of String)()

        If gv Is Nothing Then
            Return columnNames
        End If

        If Not gv.AutoGenerateColumns Then
            ' Explicitly declared columns only (AutoGenerateColumns="False"): DataControlFieldCollection
            ' always contains every field, hidden or not.
            For Each field As DataControlField In gv.Columns
                columnNames.Add(field.HeaderText)
            Next

            Return columnNames
        End If

        ' AutoGenerateColumns="True" (GridView1's case), possibly mixed with one or more explicitly
        ' declared columns (e.g. the "Actions" TemplateField): HeaderRow.Cells contains one cell per
        ' rendered column - declared columns first, then the auto-generated data columns. Declared
        ' columns are UI-only (not real data columns) and are always kept visible by
        ' ApplyColumnVisibility, so they're skipped here too - the dialog should only offer the
        ' actual auto-generated data columns as choices.
        If gv.HeaderRow IsNot Nothing Then
            For colIndex As Integer = gv.Columns.Count To gv.HeaderRow.Cells.Count - 1
                columnNames.Add(gv.HeaderRow.Cells(colIndex).Text)
            Next
        End If

        Return columnNames
    End Function

    ''' <summary>
    ''' Returns the header text of only the columns that are currently Visible.
    ''' </summary>
    Public Function GetVisibleColumnNames(ByVal gv As GridView) As List(Of String)
        Dim VisibleColumnNames As New List(Of String)()

        If gv Is Nothing Then
            Return VisibleColumnNames
        End If

        If Not gv.AutoGenerateColumns Then
            For Each field As DataControlField In gv.Columns
                If field.Visible Then
                    VisibleColumnNames.Add(field.HeaderText)
                End If
            Next

            Return VisibleColumnNames
        End If

        ' AutoGenerateColumns="True": a column is hidden by setting its header cell's
        ' Visible property to False (e.g. in RowCreated/RowDataBound). The cell still
        ' exists in the Cells collection - it just doesn't render. Declared columns
        ' (e.g. "Actions") are skipped since they're never offered as a choice - see
        ' GetAllColumnNames.
        If gv.HeaderRow IsNot Nothing Then
            For colIndex As Integer = gv.Columns.Count To gv.HeaderRow.Cells.Count - 1
                Dim cell As TableCell = gv.HeaderRow.Cells(colIndex)
                If cell.Visible Then
                    VisibleColumnNames.Add(cell.Text)
                End If
            Next
        End If

        Return VisibleColumnNames
    End Function

    ' ... GetAllColumnNames / GetHiddenColumnNames go here ...

    ''' <summary>
    ''' Makes visible only the GridView columns whose header text appears in the
    ''' single-column "Value" DataTable; any column not listed is hidden.
    ''' Works for both explicitly declared columns (AutoGenerateColumns="False")
    ''' and auto-generated columns (AutoGenerateColumns="True", e.g. GridView1),
    ''' including when the two are mixed together.
    ''' </summary>
    Public Sub ApplyColumnVisibility(ByVal gv As GridView, ByVal visibleColumns As DataTable)
        If gv Is Nothing Then
            Return
        End If

        Dim visibleNames As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)

        If visibleColumns IsNot Nothing AndAlso visibleColumns.Columns.Contains("Value") Then
            For Each dr As DataRow In visibleColumns.Rows
                Dim columnName As String = Convert.ToString(dr("Value"))
                If Not String.IsNullOrEmpty(columnName) Then
                    visibleNames.Add(columnName)
                End If
            Next
        End If

        If Not gv.AutoGenerateColumns Then
            ' Explicitly declared columns
            For Each field As DataControlField In gv.Columns
                field.Visible = visibleNames.Contains(field.HeaderText)
            Next

            Return
        End If

        ' AutoGenerateColumns="True" (GridView1's case): toggle each header cell,
        ' then mirror the same Visible flag onto that column's cell in every data row -
        ' each TableCell.Visible is independent, so the header alone isn't enough.
        ' This also correctly covers explicitly declared columns mixed in (e.g. "Actions"),
        ' since they still get a cell in HeaderRow.Cells at the same index as their data cells.
        If gv.HeaderRow Is Nothing Then
            Return
        End If

        For colIndex As Integer = 0 To gv.HeaderRow.Cells.Count - 1
            Dim headerCell As TableCell = gv.HeaderRow.Cells(colIndex)

            ' Declared columns (e.g. the "Actions" TemplateField) occupy the first
            ' gv.Columns.Count header cells and are never offered as choices in the
            ' Select Columns dialog, so they must never be hidden by it.
            Dim isDeclaredColumn As Boolean = colIndex < gv.Columns.Count
            Dim isVisible As Boolean = isDeclaredColumn OrElse visibleNames.Contains(headerCell.Text)

            headerCell.Visible = isVisible

            For Each row As GridViewRow In gv.Rows
                If colIndex < row.Cells.Count Then
                    row.Cells(colIndex).Visible = isVisible
                End If
            Next
        Next
    End Sub


End Module
Public Enum FilterType
    None
    TextBox
    AutoComplete
    DropDownList
    RadioButtonList
    CheckBox
    CheckBoxList
    NumberRange
    DateRange
End Enum