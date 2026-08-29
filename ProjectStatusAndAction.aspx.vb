Imports System.Data
Imports System.Drawing
Public Class ProjectStatusAndAction
    Inherits System.Web.UI.Page
    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        If Not IsPostBack Then
            LoadProjects()
        End If

        LoadProjectStatuses()
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

    End Sub
End Class
