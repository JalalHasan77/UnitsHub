Imports System.Data
Imports System.Drawing
Public Class TR_StatusAndAction
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


        Dim GV As GridView
        GV = e.Row.FindControl("GridView1")
        Dim dtActions As New DataTable("Actions")

        dtActions.Columns.Add("ID", GetType(Integer))
        dtActions.Columns.Add("Action", GetType(String))

        For i As Integer = 0 To 4
            dtActions.Rows.Add(i + 1, {"Sell", "Reserve", "HandOver", "Keys", "Release"}(i))
        Next

        GV.DataSource = dtActions
        GV.DataBind()

    End Sub

    ''' <summary>
    ''' Fires when "Add State &#9650;" is clicked for a given row. CommandArgument carries that
    ''' row's STATE_ID.
    ''' TODO: implement — e.g. insert a new state positioned above this one (lower sort order).
    ''' </summary>
    Protected Sub lnkAddStateUp_Click(ByVal sender As Object, ByVal e As EventArgs)
        Dim stateId As String = CType(sender, LinkButton).CommandArgument
    End Sub

    ''' <summary>
    ''' Fires when "Add State &#9660;" is clicked for a given row. CommandArgument carries that
    ''' row's STATE_ID.
    ''' TODO: implement — e.g. insert a new state positioned below this one (higher sort order).
    ''' </summary>
    Protected Sub lnkAddStateDown_Click(ByVal sender As Object, ByVal e As EventArgs)
        Dim stateId As String = CType(sender, LinkButton).CommandArgument
    End Sub

    ''' <summary>
    ''' Fires when "Add New Action" is clicked for a given row. CommandArgument carries that
    ''' row's STATE_ID.
    ''' TODO: implement — e.g. redirect/open DesignAction.aspx pre-populated for this state.
    ''' </summary>
    Protected Sub lnkAddNewAction_Click(ByVal sender As Object, ByVal e As EventArgs)
        Dim stateId As String = CType(sender, LinkButton).CommandArgument
    End Sub

    Protected Sub GridView1_RowDataBound(sender As Object, e As GridViewRowEventArgs)

        If e.Row.RowType <> DataControlRowType.DataRow Then
            Return
        End If

        Dim L As LinkButton
        L = e.Row.FindControl("lnkActionTitle")
        L.Text = e.Row.DataItem("Action").ToString

    End Sub
End Class
