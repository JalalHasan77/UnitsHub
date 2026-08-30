Imports System.Data
Imports System.Drawing
Imports System.Collections.Generic
Public Class ProjectStatusAndAction
    Inherits System.Web.UI.Page

    Dim encryNdecry As New EncryDecry

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        If Not IsPostBack Then
            LoadProjects()
        End If

        LoadProjectStatuses()
    End Sub

    ''' <summary>
    ''' Closes this popup and reloads its parent, the same way Save/Cancel already do on
    ''' DesignAction.aspx and AddMultipleItemsFromList.aspx — skipPostBack:=False triggers
    ''' window.parent.__doPostBack(vendorPopupContext.postBackId, '') after closing.
    ''' </summary>
    Protected Sub btnClose_Click(ByVal sender As Object, ByVal e As EventArgs)
        VendorPopupHelper.RegisterPopupSelectionAndClose(Me, False, skipPostBack:=False)
    End Sub

    ''' <summary>
    ''' Populates ddlProjectName from the projects table. Only called on the initial load
    ''' (not on postback) so the selection persists via ViewState instead of being re-bound.
    ''' </summary>
    Private Sub LoadProjects()
        Dim DT As New Data.DataTable
        Dim SQL As String = "Select PROJECT_ID, PROJECT_NAME_EN from UNITSHUB_PROJECTS"
        DT = GetDataTable(EBDB, SQL)

        ddlProjectName.Items.Clear()
        ddlProjectName.DataSource = DT
        ddlProjectName.DataTextField = "PROJECT_NAME_EN"
        ddlProjectName.DataValueField = "PROJECT_ID"
        ddlProjectName.DataBind()
    End Sub

    ''' <summary>
    ''' Fires when a different project is picked; reloads the status grid for it.
    ''' </summary>
    Protected Sub ddlProjectName_SelectedIndexChanged(ByVal sender As Object, ByVal e As EventArgs)
        LoadProjectStatuses()
    End Sub

    ''' <summary>
    ''' Loads UNITSHUB_PROJECTSTATUS rows for the currently selected project into gvProjectStatus.
    ''' </summary>
    Private Sub LoadProjectStatuses()
        Dim projectId As String = ddlProjectName.SelectedValue

        If String.IsNullOrEmpty(projectId) Then
            gvProjectStatus.DataSource = Nothing
            gvProjectStatus.DataBind()
            Return
        End If

        Dim DT As New Data.DataTable
        Dim SQL As String = "Select STATE_ID, STATUS, SUBTITLE, STATUS_BG_COLOR, STATUS_FG_COLOR, " &
                             "SUBTITLE_BG_COLOR, SUBTITLE_FG_COLOR from UNITSHUB_PROJECTSTATUS " &
                             "where PROJECT_ID = '" & projectId & "' order by STATE_ID"
        DT = GetDataTable(EBDB, SQL)

        gvProjectStatus.DataSource = DT
        gvProjectStatus.DataBind()
    End Sub

    ''' <summary>
    ''' Converts a hex color string (with or without a leading '#') into a Color.
    ''' STATUS_BG_COLOR / STATUS_FG_COLOR / SUBTITLE_BG_COLOR / SUBTITLE_FG_COLOR come from the
    ''' database as plain hex text (e.g. "3366FF"), and ColorTranslator.FromHtml requires the '#'.
    ''' </summary>
    Private Function GetColorFromHex(ByVal hexValue As String) As Color
        If String.IsNullOrWhiteSpace(hexValue) Then
            Return Color.Empty
        End If

        Dim hex As String = hexValue.Trim()
        If Not hex.StartsWith("#") Then
            hex = "#" & hex
        End If

        Return ColorTranslator.FromHtml(hex)
    End Function

    ''' <summary>
    ''' Colors each status row from its STATUS_BG_COLOR/STATUS_FG_COLOR values, and shows/colors
    ''' the small subtitle chip only when SUBTITLE has a value.
    ''' </summary>
    Protected Sub gvProjectStatus_RowDataBound(ByVal sender As Object, ByVal e As GridViewRowEventArgs)
        If e.Row.RowType <> DataControlRowType.DataRow Then
            Return
        End If

        Dim rowData As Data.DataRowView = CType(e.Row.DataItem, Data.DataRowView)

        Dim pnlStatusBox As Panel = CType(e.Row.FindControl("pnlStatusBox"), Panel)
        pnlStatusBox.BackColor = GetColorFromHex(rowData("STATUS_BG_COLOR").ToString())
        pnlStatusBox.ForeColor = GetColorFromHex(rowData("STATUS_FG_COLOR").ToString())

        Dim pnlSubtitle As Panel = CType(e.Row.FindControl("pnlSubtitle"), Panel)
        Dim subtitleText As String = rowData("SUBTITLE").ToString()

        If String.IsNullOrEmpty(subtitleText) Then
            pnlSubtitle.Visible = False
        Else
            pnlSubtitle.Visible = True
            pnlSubtitle.BackColor = GetColorFromHex(rowData("SUBTITLE_BG_COLOR").ToString())
            pnlSubtitle.ForeColor = GetColorFromHex(rowData("SUBTITLE_FG_COLOR").ToString())
        End If

        ' 0000 and 9216 are the boundary states — there's nothing above the first or below
        ' the last to bisect against, so hide the corresponding "Add State" link. Otherwise,
        ' wire each one to open AddProjectStatus.aspx in a popup for this project/status.
        Dim projectId As String = ddlProjectName.SelectedValue
        Dim statusId As String = rowData("STATE_ID").ToString().Trim()

        Dim lblStatus As LinkButton = CType(e.Row.FindControl("lblStatus"), LinkButton)
        If lblStatus IsNot Nothing Then
            Dim popupUrlEdit As String = "AddProjectStatus.aspx?ProjectID=" & Server.UrlEncode(projectId) &
                                          "&StatusID=" & Server.UrlEncode(statusId) &
                                          "&MODE=EDIT"

            VendorPopupHelper.RegisterVendorPopup(Me,
                                                  lblStatus,
                                                  popupUrlEdit,
                                                  1000, 0,
                                                  PopupPlacement.Center,
                                                  "",
                                                  VendorPopupHelper.PopupDisplayMode.Standard)
        End If

        Dim lnkAddStateUp As LinkButton = CType(e.Row.FindControl("lnkAddStateUp"), LinkButton)
        If lnkAddStateUp IsNot Nothing Then
            lnkAddStateUp.Visible = (statusId <> "0000")

            If lnkAddStateUp.Visible Then
                Dim popupUrlUp As String = "AddProjectStatus.aspx?ProjectID=" & Server.UrlEncode(projectId) &
                                            "&StatusID=" & Server.UrlEncode(statusId) &
                                            "&MODE=NEW" &
                                            "&Dir=Asc"

                VendorPopupHelper.RegisterVendorPopup(Me,
                                                      lnkAddStateUp,
                                                      popupUrlUp,
                                                      1000, 0,
                                                      PopupPlacement.Center,
                                                      "",
                                                      VendorPopupHelper.PopupDisplayMode.Standard)
            End If
        End If

        Dim lnkAddStateDown As LinkButton = CType(e.Row.FindControl("lnkAddStateDown"), LinkButton)
        If lnkAddStateDown IsNot Nothing Then
            lnkAddStateDown.Visible = (statusId <> "9216")

            If lnkAddStateDown.Visible Then
                Dim popupUrlDown As String = "AddProjectStatus.aspx?ProjectID=" & Server.UrlEncode(projectId) &
                                              "&StatusID=" & Server.UrlEncode(statusId) &
                                              "&MODE=NEW" &
                                              "&Dir=Desc"

                VendorPopupHelper.RegisterVendorPopup(Me,
                                                      lnkAddStateDown,
                                                      popupUrlDown,
                                                      1000, 0,
                                                      PopupPlacement.Center,
                                                      "",
                                                      VendorPopupHelper.PopupDisplayMode.Standard)
            End If
        End If

        Dim lnkAddNewAction As LinkButton = CType(e.Row.FindControl("lnkAddNewAction"), LinkButton)
        If lnkAddNewAction IsNot Nothing Then
            Dim L As LinkButton = lnkAddNewAction

            Dim popupUrl As String = "DesignAction.aspx?ProjectID=" & Server.UrlEncode(projectId) &
                                      "&StatusID=" & Server.UrlEncode(statusId) &
                                      "&Mode=New"
            VendorPopupHelper.RegisterVendorPopup(Me,
                                                  L,
                                                  popupUrl,
                                                  1000, 0,
                                                  PopupPlacement.Center,
                                                  "",
                                                  VendorPopupHelper.PopupDisplayMode.Standard)
        End If


        Dim GV As GridView
        GV = e.Row.FindControl("GridView1")

        Dim actionsDT As New Data.DataTable
        Dim actionsSql As String =
            "Select ACTION_ID, ACTION_TITLE, PROJECT_ID, STATUS_ID from UNITSHUB_ACTIONS where PROJECT_ID = '" &
            ddlProjectName.SelectedValue.Replace("'", "''") & "' and STATUS_ID = '" &
            rowData("STATE_ID").ToString().Replace("'", "''") & "'"
        actionsDT = GetDataTable(EBDB, actionsSql)

        GV.DataSource = actionsDT
        GV.DataBind()

    End Sub

    Protected Sub GridView1_RowDataBound(sender As Object, e As GridViewRowEventArgs)

        If e.Row.RowType <> DataControlRowType.DataRow Then
            Return
        End If

        Dim rowView As Data.DataRowView = CType(e.Row.DataItem, Data.DataRowView)

        Dim L As LinkButton
        L = e.Row.FindControl("lnkActionTitle")
        L.Text = rowView("ACTION_TITLE").ToString()

        Dim projectId As String = rowView("PROJECT_ID").ToString()
        Dim statusId As String = rowView("STATUS_ID").ToString()
        Dim actionId As String = rowView("ACTION_ID").ToString()

        Dim popupUrl As String = "DesignAction.aspx?ProjectID=" & Server.UrlEncode(projectId) &
                                  "&StatusID=" & Server.UrlEncode(statusId) &
                                  "&ActionID=" & Server.UrlEncode(actionId) &
                                  "&Mode=Edit"

        VendorPopupHelper.RegisterVendorPopup(Me,
                                              L,
                                              popupUrl,
                                              1000, 0,
                                              PopupPlacement.Center,
                                              "",
                                              VendorPopupHelper.PopupDisplayMode.Standard)

        Dim btnShowUsers As Button = CType(e.Row.FindControl("btnShowUsers"), Button)
        If btnShowUsers IsNot Nothing Then
            Dim MemberListParameters As New clsListProperties
            With MemberListParameters
                .ItemsSQL = "Select USER_ID as ID, FULL_NAME as Name from UNITSHUB_USERS order by Name"
                .CheckedItemsSQL = "Select USER_ID as ID from UNITSHUB_PRJ_STS_ACTN_USRS where PROJECT_ID ='" & projectId & "' and STATUS_ID = '" & statusId & "' and ACTION_ID='" & actionId & "'"
                .FormTitle = "Select Users"
                .ColumnHideAndShow = "YN"
                .EditableColumns = "NN"
                .ColumnsWidth = New Double() {1, 3}
                .HoverableList = "Y"
            End With
            Dim SelectMembersParameters As String = encryNdecry.EncryptObject(Of clsListProperties)(MemberListParameters)

            VendorPopupHelper.RegisterVendorPopup(Me,
                                                  btnShowUsers,
                                                  "AddMultipleItemsFromList.aspx?Parameters=" & Server.UrlEncode(SelectMembersParameters),
                                                  400,
                                                  600,
                                                  PopupPlacement.Center,
                                                  "Select Adj",
                                                  VendorPopupHelper.PopupDisplayMode.FrameOnly)
        End If

    End Sub

    ''' <summary>
    ''' Fires when AddMultipleItemsFromList.aspx closes after Save (via
    ''' RegisterPopupSelectionAndClose's skipPostBack:=False, which re-triggers a postback
    ''' on the same btnShowUsers instance that opened it — this does NOT fire on the normal
    ''' click that opens the popup, since RegisterVendorPopup's own script prevents that
    ''' click's default postback).
    ''' </summary>
    ''' <remarks>
    ''' TODO: "ExecuteNonQuery" is a placeholder for whatever write helper your project
    ''' actually exposes (this codebase only gave me GetDataTable(conn, sql) for reads) —
    ''' swap it for your real INSERT/UPDATE/DELETE helper.
    ''' TODO: UNITSHUB_PRJ_STS_ACTN_USRS's exact schema wasn't given to me — I assumed an
    ''' ID/PROJECT_ID/STATUS_ID/ACTION_ID/USER_ID shape (VARCHAR2(5)-style zero-padded ID),
    ''' matching the convention used elsewhere in this app. Adjust column names/types if
    ''' the real table differs.
    ''' </remarks>
    Protected Sub btnShowUsers_Click(sender As Object, e As EventArgs)
        Dim selectedItems As List(Of Dictionary(Of String, Object)) =
            TryCast(VendorPopupHelper.GetPopupReturnValue(Me, "SelectedItems"),
                List(Of Dictionary(Of String, Object)))

        If selectedItems Is Nothing Then
            Return
        End If

        Dim btn As Button = CType(sender, Button)
        Dim row As GridViewRow = CType(btn.NamingContainer, GridViewRow)
        Dim gv As GridView = CType(row.NamingContainer, GridView)

        Dim projectId As String = gv.DataKeys(row.RowIndex)("PROJECT_ID").ToString()
        Dim statusId As String = gv.DataKeys(row.RowIndex)("STATUS_ID").ToString()
        Dim actionId As String = gv.DataKeys(row.RowIndex)("ACTION_ID").ToString()

        ' Replace this action's existing user list entirely — simpler and safer than
        ' trying to diff which specific users were added/removed.
        Dim deleteSql As String =
            "DELETE FROM UNITSHUB_PRJ_STS_ACTN_USRS WHERE PROJECT_ID = '" & projectId.Replace("'", "''") & "' " &
            "AND STATUS_ID = '" & statusId.Replace("'", "''") & "' AND ACTION_ID = '" & actionId.Replace("'", "''") & "'"
        ExecuteNonQuery(EBDB, deleteSql)

        If selectedItems.Count > 0 Then
            For Each item As Dictionary(Of String, Object) In selectedItems
                Dim userId As String = If(item.ContainsKey("ID"), Convert.ToString(item("ID")), Nothing)

                If String.IsNullOrEmpty(userId) Then
                    Continue For
                End If

                Dim insertSql As String =
                    "INSERT INTO UNITSHUB_PRJ_STS_ACTN_USRS (PROJECT_ID, STATUS_ID, ACTION_ID, USER_ID) VALUES (" &
                    "'" & projectId.Replace("'", "''") & "', " &
                    "'" & statusId.Replace("'", "''") & "', " &
                    "'" & actionId.Replace("'", "''") & "', " &
                    "'" & userId.Replace("'", "''") & "')"

                ExecuteNonQuery(EBDB, insertSql)
            Next
        End If
    End Sub
End Class
