Imports System
Imports System.Collections.Generic
Imports System.Data
Imports System.Globalization
Imports System.Text
Imports System.Web
Imports System.Web.UI.WebControls

Partial Class AddMultipleItemsFromList
    Inherits System.Web.UI.Page

    Private EncryNDecry As New EncryDecry()
    Private Const SessionDataKey As String = "AddMultipleItemsFromList_Data"
    Private Const ViewStateSqlTextKey As String = "AddMultipleItemsFromList_SqlText"
    Private Const ViewStateHideMaskKey As String = "AddMultipleItemsFromList_HideColumnsMask"
    Private Const ViewStateEditableMaskKey As String = "AddMultipleItemsFromList_EditableColumnsMask"
    Private Const ViewStateColumnsWidthsKey As String = "AddMultipleItemsFromList_ColumnsWidths"
    Private Const ViewStateIsNoSqlModeKey As String = "AddMultipleItemsFromList_IsNoSqlMode"
    Private Const ViewStateWholeListKey As String = "AddMultipleItemsFromList_WholeList"
    Private Const ViewStateCheckedListKey As String = "AddMultipleItemsFromList_CheckedList"

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

    Private Property IsNoSqlMode As Boolean
        Get
            Return Convert.ToBoolean(If(ViewState(ViewStateIsNoSqlModeKey), False))
        End Get
        Set(value As Boolean)
            ViewState(ViewStateIsNoSqlModeKey) = value
        End Set
    End Property

    Private Property WholeListItems As List(Of String)
        Get
            Return DeserializeStringList(Convert.ToString(ViewState(ViewStateWholeListKey)))
        End Get
        Set(value As List(Of String))
            ViewState(ViewStateWholeListKey) = SerializeStringList(value)
        End Set
    End Property

    Private Property CheckedListItems As List(Of String)
        Get
            Return DeserializeStringList(Convert.ToString(ViewState(ViewStateCheckedListKey)))
        End Get
        Set(value As List(Of String))
            ViewState(ViewStateCheckedListKey) = SerializeStringList(value)
        End Set
    End Property

    Protected Sub Page_Load(sender As Object, e As EventArgs) Handles Me.Load
        If Not Page.IsPostBack Then
            InitializeFromParameters()
        End If

        LoadOptions(GetSelectedIdsFromRequest())
    End Sub

    Private Sub InitializeFromParameters()
        Dim paramsKey As String = Request("Parameters")
        Dim NoSqlListParameters As clsListPropertiesNoSQL = Nothing
        Dim ListParameters As clsListProperties = Nothing

        If Not String.IsNullOrEmpty(paramsKey) Then
            Dim sessionValue As Object = Session("PopupParams_" & paramsKey)
            NoSqlListParameters = TryCast(sessionValue, clsListPropertiesNoSQL)

            If NoSqlListParameters Is Nothing Then
                ListParameters = TryCast(sessionValue, clsListProperties)
            End If

            ' Optional: clear it once consumed so it doesn't linger in Session
            Session.Remove("PopupParams_" & paramsKey)
        End If

        If NoSqlListParameters IsNot Nothing Then
            IsNoSqlMode = True
            SqlText = String.Empty
            Label1.Text = If(NoSqlListParameters.FormTitle, String.Empty)

            ' The synthetic single "Value" column carries both the checkbox value and the
            ' visible text, so it must never be hidden or made editable - a hide/edit mask
            ' from the caller was designed for multi-column SQL result sets, not this case.
            HideColumnsMask = String.Empty
            EditableColumnsMask = String.Empty
            ColumnsWidths = Nothing

            WholeListItems = NoSqlListParameters.WholeList
            CheckedListItems = If(NoSqlListParameters.CheckedList, New List(Of String)())
        ElseIf ListParameters IsNot Nothing Then
            IsNoSqlMode = False
            SqlText = If(ListParameters.SQL, String.Empty)
            Label1.Text = If(ListParameters.FormTitle, String.Empty)
            HideColumnsMask = ListParameters.ColumnHideAndShow
            EditableColumnsMask = ListParameters.EditableColumns
            ColumnsWidths = ListParameters.ColumnsWidth
            WholeListItems = New List(Of String)()
            CheckedListItems = New List(Of String)()
        Else
            IsNoSqlMode = False
            SqlText = String.Empty
            Label1.Text = String.Empty
            HideColumnsMask = String.Empty
            EditableColumnsMask = String.Empty
            ColumnsWidths = Nothing
            WholeListItems = New List(Of String)()
            CheckedListItems = New List(Of String)()
        End If
    End Sub

    Private Function TryDecrypt(Of T As Class)(ByVal encryptedParameters As String) As T
        Try
            Return EncryNDecry.DecryptObject(Of T)(encryptedParameters)
        Catch
            Return Nothing
        End Try
    End Function

    Private Sub LoadOptions(ByVal selectedIds As HashSet(Of String))
        Dim dt As DataTable

        If IsNoSqlMode Then
            dt = BuildDataTableFromWholeList(WholeListItems)

            ' Only apply the default CheckedList on the initial (non-postback) load.
            ' On postback, the checkbox states already posted by the browser are authoritative.
            If Not Page.IsPostBack Then
                MergeIntoSelection(selectedIds, CheckedListItems)
            End If
        ElseIf String.IsNullOrWhiteSpace(SqlText) Then
            dt = New DataTable()
        Else
            dt = DB.GetDataTable(DB.EBDB, SqlText)
        End If

        Session(SessionDataKey) = dt
        litMembersTable.Text = BuildMembersTableHtml(dt, selectedIds)
    End Sub

    Private Function BuildDataTableFromWholeList(ByVal items As List(Of String)) As DataTable
        Dim dt As New DataTable()
        dt.Columns.Add("Value")

        If items IsNot Nothing Then
            For Each item As String In items
                dt.Rows.Add(item)
            Next
        End If

        Return dt
    End Function

    Private Sub MergeIntoSelection(ByVal selectedIds As HashSet(Of String), ByVal itemsToAdd As List(Of String))
        If selectedIds Is Nothing OrElse itemsToAdd Is Nothing Then
            Return
        End If

        For Each item As String In itemsToAdd
            If Not String.IsNullOrEmpty(item) Then
                selectedIds.Add(item)
            End If
        Next
    End Sub

    Private Function BuildMembersTableHtml(ByVal dt As DataTable, ByVal selectedIds As HashSet(Of String)) As String
        Dim html As New StringBuilder()

        html.Append("<table id=""membersTable"" class=""members-table"">")
        html.Append(BuildColumnGroupHtml(dt))
        html.Append("<thead><tr>")
        html.Append("<th class=""members-selector""></th>")

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

                html.Append("<tr data-searchtext=""")
                html.Append(HttpUtility.HtmlAttributeEncode(BuildSearchText(dr, rowIndex)))
                html.Append(""">")

                html.Append("<td class=""members-selector-cell""><input type=""checkbox"" name=""selectedItem"" value=""")
                html.Append(HttpUtility.HtmlAttributeEncode(originalIdValue))
                html.Append("""")

                If selectedIds IsNot Nothing AndAlso selectedIds.Contains(originalIdValue) Then
                    html.Append(" checked=""checked""")
                End If

                html.Append(" /></td>")

                For colIndex As Integer = 0 To dt.Columns.Count - 1
                    If IsColumnHidden(colIndex) Then
                        Continue For
                    End If

                    Dim originalCellText As String = Convert.ToString(dr(colIndex))
                    Dim renderedCellText As String = GetRenderedCellValue(rowIndex, colIndex, originalCellText)

                    If IsColumnEditable(colIndex) Then
                        html.Append("<td class=""editable-cell""")
                        html.Append(GetColumnWidthStyleAttribute(dt, colIndex))
                        html.Append("><input type=""text"" name=""")
                        html.Append(HttpUtility.HtmlAttributeEncode(GetCellInputName(rowIndex, colIndex)))
                        html.Append(""" value=""")
                        html.Append(HttpUtility.HtmlAttributeEncode(renderedCellText))
                        html.Append(""" data-search-input=""1"" /></td>")
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

            Dim cellText As String = Convert.ToString(dr(colIndex))
            sb.Append(GetRenderedCellValue(rowIndex, colIndex, cellText))
        Next

        Return sb.ToString()
    End Function

    Private Function BuildColumnGroupHtml(ByVal dt As DataTable) As String
        If dt Is Nothing Then
            Return String.Empty
        End If

        Dim html As New StringBuilder()
        html.Append("<colgroup>")
        html.Append("<col class=""members-selector-column"" />")

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

    Private Function SerializeStringList(ByVal values As List(Of String)) As String
        If values Is Nothing OrElse values.Count = 0 Then
            Return String.Empty
        End If

        Dim parts As New List(Of String)()

        For Each value As String In values
            parts.Add(HttpUtility.UrlEncode(If(value, String.Empty)))
        Next

        Return String.Join("|", parts.ToArray())
    End Function

    Private Function DeserializeStringList(ByVal value As String) As List(Of String)
        Dim result As New List(Of String)()

        If String.IsNullOrEmpty(value) Then
            Return result
        End If

        Dim tokens As String() = value.Split("|"c)

        For Each token As String In tokens
            result.Add(HttpUtility.UrlDecode(token))
        Next

        Return result
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

    Private Function GetRenderedCellValue(ByVal rowIndex As Integer, ByVal colIndex As Integer, ByVal defaultValue As String) As String
        Dim value As Object = GetSelectedRowCellValue(rowIndex, colIndex, defaultValue)
        Return Convert.ToString(value)
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

    Private Function GetSelectedIdsFromRequest() As HashSet(Of String)
        Dim selected As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        Dim values As String() = Request.Form.GetValues("selectedItem")

        If values Is Nothing Then
            Return selected
        End If

        For Each value As String In values
            If Not String.IsNullOrEmpty(value) Then
                selected.Add(value)
            End If
        Next

        Return selected
    End Function

    Private Function BuildSelectedItemsPayload(ByVal dt As DataTable, ByVal selectedIds As HashSet(Of String)) As List(Of Dictionary(Of String, Object))
        Dim selectedRows As New List(Of Dictionary(Of String, Object))()

        If dt Is Nothing OrElse dt.Columns.Count = 0 OrElse selectedIds Is Nothing OrElse selectedIds.Count = 0 Then
            Return selectedRows
        End If

        For rowIndex As Integer = 0 To dt.Rows.Count - 1
            Dim dr As DataRow = dt.Rows(rowIndex)
            Dim originalIdValue As String = Convert.ToString(dr(0))

            If Not selectedIds.Contains(originalIdValue) Then
                Continue For
            End If

            Dim rowValues As New Dictionary(Of String, Object)(StringComparer.OrdinalIgnoreCase)

            For colIndex As Integer = 0 To dt.Columns.Count - 1
                Dim col As DataColumn = dt.Columns(colIndex)
                rowValues(col.ColumnName) = GetSelectedRowCellValue(rowIndex, colIndex, dr(colIndex))
            Next

            selectedRows.Add(rowValues)
        Next

        Return selectedRows
    End Function

    Protected Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Dim selectedIds As HashSet(Of String) = GetSelectedIdsFromRequest()
        Dim dt As DataTable = TryCast(Session(SessionDataKey), DataTable)

        If dt Is Nothing Then
            If IsNoSqlMode Then
                dt = BuildDataTableFromWholeList(WholeListItems)
            ElseIf String.IsNullOrWhiteSpace(SqlText) Then
                dt = New DataTable()
            Else
                dt = DB.GetDataTable(DB.InfoDB, SqlText)
            End If
        End If

        Dim selectedItems As List(Of Dictionary(Of String, Object)) = BuildSelectedItemsPayload(dt, selectedIds)

        VendorPopupHelper.RegisterPopupSelectionAndClose(
            page:=Me,
            returnValue:=selectedItems,
            startupScriptKey:="SelectedItems",
            skipPostBack:=False)
    End Sub

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