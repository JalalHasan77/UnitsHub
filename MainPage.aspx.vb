Imports System.Data

Partial Class MainPage
    Inherits System.Web.UI.Page

    Private Shared ReadOnly BadgeColors As String() = {"badge-blue", "badge-green", "badge-orange", "badge-purple", "badge-teal", "badge-pink"}
    Private encryNdecry As New EncryDecry

    'Dim DT As DataTable
    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        If Not Page.IsPostBack Then
            Dim DT As New DataTable
            DT = GetDataTable(EBDB_CS, "Select * from UNITSHUB_PROJECTS")

            For Each DR As DataRow In DT.Rows
                Dim I As New ListItem
                I.Text = DR("PROJECT_NAME_EN").ToString
                I.Value = DR("PROJECT_ID").ToString
                DropDownList1.Items.Add(I)
            Next



        End If

        VendorPopupHelper.RegisterVendorPopup(Me,
           btnColumns,
           "ColumnsList.aspx",
           400,
           600,
           PopupPlacement.Center,
           "Select Adj",
   VendorPopupHelper.PopupDisplayMode.FrameOnly)

    End Sub

    Protected Sub DropDownList1_SelectedIndexChanged(sender As Object, e As EventArgs) Handles DropDownList1.SelectedIndexChanged
        Dim SQL As String = ""
        Dim DT As New DataTable

        Dim ProjectID As String = DropDownList1.SelectedItem.Value

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


        GridView1.DataSource = DB.GetDataTable(EBDB, "Select * from " & View.Rows(0)("VIEWNAME").ToString)
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
        SQL = ""
        SQL = SQL + vbCrLf + " SELECT a.DISPLAY_ORDER, a.ATTRIBUTE_NAME "
        SQL = SQL + vbCrLf + " FROM UNITSHUB_ATTRIBUTES a "
        SQL = SQL + vbCrLf + " JOIN UNITSHUB_PROJECTSHIERARCHY h "
        SQL = SQL + vbCrLf + "    ON a.PROJECT_ID = h.PROJECT_ID "
        SQL = SQL + vbCrLf + "   AND a.NODE_TYPE_ID = h.NODE_TYPE_ID "
        SQL = SQL + vbCrLf + " WHERE h.PROJECT_ID = '001' "
        SQL = SQL + vbCrLf + "   AND h.TREELEVEL = ( "
        SQL = SQL + vbCrLf + "         SELECT MAX(TREELEVEL) "
        SQL = SQL + vbCrLf + "         FROM UNITSHUB_PROJECTSHIERARCHY "
        SQL = SQL + vbCrLf + "         WHERE PROJECT_ID = '001' "
        SQL = SQL + vbCrLf + "       ) "
        SQL = SQL + vbCrLf + " ORDER BY a.DISPLAY_ORDER; "


        Dim ColumnListParameters As New clsListProperties
        With ColumnListParameters
            .SQL = SQL '"Select MemberID as ID, MemberName as [Name] from Members order by CInt(NoOfMovement) desc"
            .FormTitle = "Select Columns"
            .ColumnHideAndShow = "YN"
            .EditableColumns = "NN"
            .ColumnsWidth = New Double() {1, 3}
            .HoverableList = "Y"
        End With
        Dim SelectMembersParameters As String = encryNdecry.EncryptObject(Of clsListProperties)(ColumnListParameters)

        VendorPopupHelper.RegisterVendorPopup(Me,
                                      btnColumns,
                                      "AddMultipleItemsFromList.aspx?Parameters=" & SelectMembersParameters,
                                      400,
                                      600,
                                      PopupPlacement.Center,
                                      "Select Adj",
                                      VendorPopupHelper.PopupDisplayMode.FrameOnly)



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

End Class






