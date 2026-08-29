Imports System
Imports System.Collections.Generic
Imports System.Data
Imports System.Globalization
Imports System.Text
Imports System.Web
Imports System.Web.UI
Imports System.Web.UI.WebControls

Partial Class SelectOneItemFromListMultiColumns
    Inherits System.Web.UI.Page

    Private EncryNDecry As New EncryDecry()
    Private Const SessionDataKey As String = "SelectOneItemFromListMultiColumns_Data"
    Private Const ViewStateSqlTextKey As String = "SelectOneItemFromListMultiColumns_SqlText"
    Private Const ViewStateHideMaskKey As String = "SelectOneItemFromListMultiColumns_HideColumnsMask"
    Private Const ViewStateEditableMaskKey As String = "SelectOneItemFromListMultiColumns_EditableColumnsMask"
    Private Const ViewStateColumnsWidthsKey As String = "SelectOneItemFromListMultiColumns_ColumnsWidths"
    Private Const ViewStateHoverableListKey As String = "SelectOneItemFromListMultiColumns_HoverableList"

    Private Property SqlText As String
        Get
            Return Convert.ToString(ViewState(ViewStateSqlTextKey))
        End Get
        Set(value As String)
            ViewState(ViewStateSqlTextKey) = value
        End Set
    End Property

    Private Property HideColumnsMask As String
        Get
            Return Convert.ToString(ViewState(ViewStateHideMaskKey))
        End Get
        Set(value As String)
            ViewState(ViewStateHideMaskKey) = NormalizeMask(value)
        End Set
    End Property

    Private Property EditableColumnsMask As String
        Get
            Return Convert.ToString(ViewState(ViewStateEditableMaskKey))
        End Get
        Set(value As String)
            ViewState(ViewStateEditableMaskKey) = NormalizeMask(value)
        End Set
    End Property

    Private Property ColumnsWidths As Double()
        Get
            Return DeserializeColumnsWidths(Convert.ToString(ViewState(ViewStateColumnsWidthsKey)))
        End Get
        Set(value As Double())
            ViewState(ViewStateColumnsWidthsKey) = SerializeColumnsWidths(value)
        End Set
    End Property

    Protected Property HoverableListMode As String
        Get
            Return Convert.ToString(ViewState(ViewStateHoverableListKey))
        End Get
        Set(value As String)
            ViewState(ViewStateHoverableListKey) = NormalizeYesNo(value, "Y")
        End Set
    End Property

    Protected ReadOnly Property IsHoverableList As Boolean
        Get
            Return String.Equals(HoverableListMode, "Y", StringComparison.OrdinalIgnoreCase)
        End Get
    End Property

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Load
        If Not Page.IsPostBack Then
            InitializeFromParameters()
        End If

        LoadOptions(GetSelectedIdFromRequest())
    End Sub

    Private Sub InitializeFromParameters()
        Dim encryptedParameters As String = Request("Parameters")
        Dim ListParameters As clsListProperties = Nothing

        If Not String.IsNullOrEmpty(encryptedParameters) Then
            ListParameters = EncryNDecry.DecryptObject(Of clsListProperties)(encryptedParameters)
        End If

        If ListParameters IsNot Nothing Then
            SqlText = If(ListParameters.SQL, String.Empty)
            Label1.Text = If(ListParameters.FormTitle, String.Empty)
            HideColumnsMask = ListParameters.ColumnHideAndShow
            EditableColumnsMask = ListParameters.EditableColumns
            ColumnsWidths = ListParameters.ColumnsWidth
            HoverableListMode = ListParameters.HoverableList
        Else
            SqlText = String.Empty
            Label1.Text = String.Empty
            HideColumnsMask = String.Empty
            EditableColumnsMask = String.Empty
            ColumnsWidths = Nothing
            HoverableListMode = "Y"
        End If
    End Sub

    Private Sub LoadOptions(ByVal selectedId As String)
        Dim dt As DataTable

        If String.IsNullOrWhiteSpace(SqlText) Then
            dt = New DataTable()
        Else
            dt = DB.GetDataTable(DB.InfoDB, SqlText)
        End If

        Session(SessionDataKey) = dt
        litItemsTable.Text = BuildItemsTableHtml(dt, selectedId)
    End Sub

    Private Function BuildItemsTableHtml(ByVal dt As DataTable, ByVal selectedId As String) As String
        Dim html As New StringBuilder()

        html.Append("<table id=""itemsTable"" class=""items-table"">")
        html.Append(BuildColumnGroupHtml(dt))
        html.Append("<thead><tr>")
        html.Append("<th class=""items-selector""></th>")

        If dt IsNot Nothing Then
            For colIndex As Integer = 0 To dt.Columns.Count - 1
                If IsColumnHidden(colIndex) Then
                    Continue For
                End If

                Dim col As DataColumn = dt.Columns(colIndex)
                html.Append("<th")
                html.Append(GetColumnWidthStyleAttribute(dt, colIndex))
                html.Append(">")
                html.Append(HttpUtility.HtmlEncode(col.ColumnName))
                html.Append("</th>")
            Next
        End If

        html.Append("</tr></thead>")
        html.Append("<tbody>")

        If dt IsNot Nothing Then
            For rowIndex As Integer = 0 To dt.Rows.Count - 1
                Dim dr As DataRow = dt.Rows(rowIndex)
                Dim originalIdValue As String = String.Empty

                If dt.Columns.Count > 0 Then
                    originalIdValue = Convert.ToString(dr(0))
                End If

                Dim isSelected As Boolean = String.Equals(selectedId, originalIdValue, StringComparison.OrdinalIgnoreCase)

                html.Append("<tr data-searchtext=""")
                html.Append(HttpUtility.HtmlAttributeEncode(BuildSearchText(dr, rowIndex)))
                html.Append("""")

                If isSelected Then
                    html.Append(" class=""selected-row""")
                End If

                html.Append(">")
                html.Append("<td class=""items-selector-cell""><input type=""radio"" name=""selectedItem"" value=""")
                html.Append(HttpUtility.HtmlAttributeEncode(originalIdValue))
                html.Append("""")

                If isSelected Then
                    html.Append(" checked=""checked""")
                End If

                html.Append(" onclick=""event.stopPropagation(); updateSelectedRowStyles();"" /></td>")

                For colIndex As Integer = 0 To dt.Columns.Count - 1
                    If IsColumnHidden(colIndex) Then
                        Continue For
                    End If

                    Dim originalCellValue As Object = dr(colIndex)
                    Dim renderedCellText As String = Convert.ToString(GetSelectedRowCellValue(rowIndex, colIndex, originalCellValue))

                    If IsColumnEditable(colIndex) Then
                        html.Append("<td class=""editable-cell""")
                        html.Append(GetColumnWidthStyleAttribute(dt, colIndex))
                        html.Append("><input type=""text"" name=""")
                        html.Append(HttpUtility.HtmlAttributeEncode(GetCellInputName(rowIndex, colIndex)))
                        html.Append(""" value=""")
                        html.Append(HttpUtility.HtmlAttributeEncode(renderedCellText))
                        html.Append(""" data-search-input=""1"" onclick=""event.stopPropagation();"" /></td>")
                    Else
                        html.Append("<td data-original-text=""")
                        html.Append(HttpUtility.HtmlAttributeEncode(renderedCellText))
                        html.Append("""")
                        html.Append(GetColumnWidthStyleAttribute(dt, colIndex))
                        html.Append(">")
                        html.Append(HttpUtility.HtmlEncode(renderedCellText))
                        html.Append("</td>")
                    End If
                Next

                html.Append("</tr>")
            Next
        End If

        html.Append("</tbody></table>")
        Return html.ToString()
    End Function

    Private Function BuildSearchText(ByVal dr As DataRow, ByVal rowIndex As Integer) As String
        Dim sb As New StringBuilder()

        For colIndex As Integer = 0 To dr.Table.Columns.Count - 1
            If IsColumnHidden(colIndex) Then
                Continue For
            End If

            If sb.Length > 0 Then
                sb.Append(" ")
            End If

            sb.Append(Convert.ToString(GetSelectedRowCellValue(rowIndex, colIndex, dr(colIndex))))
        Next

        Return sb.ToString()
    End Function

    Private Function BuildColumnGroupHtml(ByVal dt As DataTable) As String
        If dt Is Nothing Then
            Return String.Empty
        End If

        Dim html As New StringBuilder()
        html.Append("<colgroup>")
        html.Append("<col class=""items-selector-column"" />")

        For colIndex As Integer = 0 To dt.Columns.Count - 1
            If IsColumnHidden(colIndex) Then
                Continue For
            End If

            html.Append("<col")
            html.Append(GetColumnWidthStyleAttribute(dt, colIndex))
            html.Append(" />")
        Next

        html.Append("</colgroup>")
        Return html.ToString()
    End Function

    Private Function GetColumnWidthStyleAttribute(ByVal dt As DataTable, ByVal columnIndex As Integer) As String
        Dim widthPercent As Double = GetColumnWidthPercent(dt, columnIndex)

        If widthPercent <= 0 Then
            Return String.Empty
        End If

        Return String.Format(CultureInfo.InvariantCulture, " style=""width:{0:0.####}%""", widthPercent)
    End Function

    Private Function GetColumnWidthPercent(ByVal dt As DataTable, ByVal columnIndex As Integer) As Double
        If dt Is Nothing OrElse columnIndex < 0 OrElse columnIndex >= dt.Columns.Count OrElse IsColumnHidden(columnIndex) Then
            Return 0R
        End If

        Dim visibleCount As Integer = GetVisibleColumnCount(dt)
        If visibleCount <= 0 Then
            Return 0R
        End If

        Dim widths As Double() = ColumnsWidths

        If widths Is Nothing OrElse widths.Length < dt.Columns.Count Then
            Return 100.0R / visibleCount
        End If

        Dim total As Double = 0R

        For i As Integer = 0 To dt.Columns.Count - 1
            If IsColumnHidden(i) Then
                Continue For
            End If

            If widths(i) <= 0 Then
                Return 100.0R / visibleCount
            End If

            total += widths(i)
        Next

        If total <= 0 Then
            Return 100.0R / visibleCount
        End If

        Return (widths(columnIndex) / total) * 100.0R
    End Function

    Private Function GetVisibleColumnCount(ByVal dt As DataTable) As Integer
        If dt Is Nothing Then
            Return 0
        End If

        Dim visibleCount As Integer = 0

        For i As Integer = 0 To dt.Columns.Count - 1
            If Not IsColumnHidden(i) Then
                visibleCount += 1
            End If
        Next

        Return visibleCount
    End Function

    Private Function SerializeColumnsWidths(ByVal values As Double()) As String
        If values Is Nothing OrElse values.Length = 0 Then
            Return String.Empty
        End If

        Dim parts As New List(Of String)()

        For Each value As Double In values
            parts.Add(value.ToString("R", CultureInfo.InvariantCulture))
        Next

        Return String.Join("|", parts.ToArray())
    End Function

    Private Function DeserializeColumnsWidths(ByVal value As String) As Double()
        If String.IsNullOrWhiteSpace(value) Then
            Return New Double() {}
        End If

        Dim tokens As String() = value.Split("|"c)
        Dim result As New List(Of Double)()

        For Each token As String In tokens
            Dim parsed As Double
            If Double.TryParse(token, NumberStyles.Float Or NumberStyles.AllowThousands, CultureInfo.InvariantCulture, parsed) Then
                result.Add(parsed)
            End If
        Next

        Return result.ToArray()
    End Function

    Private Function NormalizeMask(ByVal value As String) As String
        If String.IsNullOrEmpty(value) Then
            Return String.Empty
        End If

        Dim sb As New StringBuilder()

        For Each ch As Char In value.Trim().ToUpperInvariant()
            If ch = "Y"c OrElse ch = "N"c Then
                sb.Append(ch)
            End If
        Next

        Return sb.ToString()
    End Function

    Private Function NormalizeYesNo(ByVal value As String, Optional ByVal defaultValue As String = "Y") As String
        If String.IsNullOrWhiteSpace(value) Then
            Return defaultValue
        End If

        If String.Equals(value.Trim(), "Y", StringComparison.OrdinalIgnoreCase) Then
            Return "Y"
        End If

        If String.Equals(value.Trim(), "N", StringComparison.OrdinalIgnoreCase) Then
            Return "N"
        End If

        Return defaultValue
    End Function

    Private Function IsColumnHidden(ByVal columnIndex As Integer) As Boolean
        If String.IsNullOrEmpty(HideColumnsMask) Then
            Return False
        End If

        If columnIndex < 0 OrElse columnIndex >= HideColumnsMask.Length Then
            Return False
        End If

        Return HideColumnsMask(columnIndex) = "Y"c
    End Function

    Private Function IsColumnEditable(ByVal columnIndex As Integer) As Boolean
        If IsColumnHidden(columnIndex) Then
            Return False
        End If

        If String.IsNullOrEmpty(EditableColumnsMask) Then
            Return False
        End If

        If columnIndex < 0 OrElse columnIndex >= EditableColumnsMask.Length Then
            Return False
        End If

        Return EditableColumnsMask(columnIndex) = "Y"c
    End Function

    Private Function GetCellInputName(ByVal rowIndex As Integer, ByVal colIndex As Integer) As String
        Return String.Format("cell_{0}_{1}", rowIndex, colIndex)
    End Function

    Private Function GetSelectedRowCellValue(ByVal rowIndex As Integer, ByVal colIndex As Integer, ByVal defaultValue As Object) As Object
        If IsColumnHidden(colIndex) OrElse Not Page.IsPostBack OrElse Not IsColumnEditable(colIndex) Then
            Return defaultValue
        End If

        Dim postedValue As String = Request.Form(GetCellInputName(rowIndex, colIndex))
        If postedValue Is Nothing Then
            Return defaultValue
        End If

        Return postedValue
    End Function

    Private Function GetSelectedIdFromRequest() As String
        Return Convert.ToString(Request.Form("selectedItem"))
    End Function

    Private Function BuildSelectedItemsPayload(ByVal dt As DataTable, ByVal selectedId As String) As List(Of Dictionary(Of String, Object))
        Dim selectedRows As New List(Of Dictionary(Of String, Object))()

        If dt Is Nothing OrElse dt.Columns.Count = 0 OrElse String.IsNullOrEmpty(selectedId) Then
            Return selectedRows
        End If

        For rowIndex As Integer = 0 To dt.Rows.Count - 1
            Dim dr As DataRow = dt.Rows(rowIndex)
            Dim originalIdValue As String = Convert.ToString(dr(0))

            If Not String.Equals(selectedId, originalIdValue, StringComparison.OrdinalIgnoreCase) Then
                Continue For
            End If

            Dim rowValues As New Dictionary(Of String, Object)(StringComparer.OrdinalIgnoreCase)

            For colIndex As Integer = 0 To dt.Columns.Count - 1
                Dim col As DataColumn = dt.Columns(colIndex)
                rowValues(col.ColumnName) = GetSelectedRowCellValue(rowIndex, colIndex, dr(colIndex))
            Next

            selectedRows.Add(rowValues)
            Exit For
        Next

        Return selectedRows
    End Function

    Protected Sub Button1_Click(ByVal sender As Object, ByVal e As EventArgs) Handles Button1.Click
        Dim selectedId As String = GetSelectedIdFromRequest()
        Dim dt As DataTable = TryCast(Session(SessionDataKey), DataTable)

        If dt Is Nothing Then
            If String.IsNullOrWhiteSpace(SqlText) Then
                dt = New DataTable()
            Else
                dt = DB.GetDataTable(DB.InfoDB, SqlText)
            End If
        End If

        Dim selectedItems As List(Of Dictionary(Of String, Object)) = BuildSelectedItemsPayload(dt, selectedId)

        VendorPopupHelper.RegisterPopupSelectionAndClose(
            page:=Me,
            returnValue:=selectedItems,
            startupScriptKey:="SelectedItems",
            skipPostBack:=False)
    End Sub

    Protected Sub Button2_Click(ByVal sender As Object, ByVal e As EventArgs) Handles Button2.Click
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
