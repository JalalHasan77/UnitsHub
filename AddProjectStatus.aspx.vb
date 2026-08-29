Imports System.Data
Imports System.Web.UI.HtmlControls

Partial Class AddProjectStatus
    Inherits System.Web.UI.Page

    ' ---------------------------------------------------------------------------------
    ' Query-string parameters this page is opened with (from ProjectStatusAndAction.aspx's
    ' "Add State ▲/▼" links): ProjectID, StatusID, MODE, Dir.
    ' Backed by ViewState (not re-read from Request.QueryString) so they survive postbacks
    ' (e.g. clicking Save), since the query string isn't guaranteed to still be present on
    ' the posted-back request.
    ' ---------------------------------------------------------------------------------

    ''' <summary>The project this status is being added to.</summary>
    Private Property ProjectIdParam As String
        Get
            Return CStr(If(ViewState("ProjectIdParam"), String.Empty))
        End Get
        Set(ByVal value As String)
            ViewState("ProjectIdParam") = value
        End Set
    End Property

    ''' <summary>
    ''' The STATE_ID of the row "Add State ▲/▼" was clicked from — the reference point to
    ''' insert the new state above/below.
    ''' </summary>
    Private Property StatusIdParam As String
        Get
            Return CStr(If(ViewState("StatusIdParam"), String.Empty))
        End Get
        Set(ByVal value As String)
            ViewState("StatusIdParam") = value
        End Set
    End Property

    ''' <summary>Currently always "NEW"; reserved for a future edit mode.</summary>
    Private Property ModeParam As String
        Get
            Return CStr(If(ViewState("ModeParam"), String.Empty))
        End Get
        Set(ByVal value As String)
            ViewState("ModeParam") = value
        End Set
    End Property

    ''' <summary>
    ''' "Asc" (Add State ▲) or "Desc" (Add State ▼) — which side of StatusIdParam the new
    ''' state should be inserted on.
    ''' </summary>
    Private Property DirParam As String
        Get
            Return CStr(If(ViewState("DirParam"), String.Empty))
        End Get
        Set(ByVal value As String)
            ViewState("DirParam") = value
        End Set
    End Property

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Load
        If Not IsPostBack Then
            ProjectIdParam = Request.QueryString("ProjectID")
            StatusIdParam = Request.QueryString("StatusID")
            ModeParam = Request.QueryString("MODE")
            DirParam = Request.QueryString("Dir")

            lblProjectID.Text = ProjectIdParam
            lblStatusID.Text = StatusIdParam
            lblMode.Text = ModeParam
            lblDir.Text = DirParam

            LoadProjects()

            ' If we were opened for a specific project (the normal popup flow), pre-select
            ' it — but leave the dropdown enabled so it doesn't look frozen/unusable.
            If Not String.IsNullOrEmpty(ProjectIdParam) AndAlso ddlProjectName.Items.FindByValue(ProjectIdParam) IsNot Nothing Then
                ddlProjectName.SelectedValue = ProjectIdParam
            End If
        End If
    End Sub

    ''' <summary>
    ''' Populates ddlProjectName from UNITSHUB_PROJECTS.
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

        ddlProjectName.Items.Insert(0, New ListItem("Select Project", ""))
    End Sub

    Protected Sub btnCancel_Click(ByVal sender As Object, ByVal e As EventArgs)
        Response.Redirect(Request.RawUrl)
    End Sub

    ''' <summary>
    ''' Inserts a new row into UNITSHUB_PROJECTSTATUS from the form values. The new STATE_ID
    ''' is the midpoint between the reference state (StatusIdParam) and its neighbor in the
    ''' direction of DirParam — "Asc" (Add State ▲) looks for the first larger STATE_ID in
    ''' the same project, "Desc" (Add State ▼) looks for the first smaller one.
    ''' </summary>
    ''' <remarks>
    ''' TODO: "ExecuteNonQuery" is a placeholder for whatever write helper your project
    ''' actually exposes (this codebase only gave me GetDataTable(conn, sql) for reads) —
    ''' swap it for your real INSERT/UPDATE/DELETE helper.
    ''' </remarks>
    Protected Sub btnSave_Click(ByVal sender As Object, ByVal e As EventArgs)
        If Not Page.IsValid Then
            Return
        End If

        Dim stateId As String = Nothing

        If Not String.IsNullOrEmpty(StatusIdParam) AndAlso Not String.IsNullOrEmpty(DirParam) Then
            Dim projectId As String = ddlProjectName.SelectedValue
            Dim referenceId As Integer = CInt(StatusIdParam)

            Dim neighborSql As String
            If DirParam = "Asc" Then
                neighborSql = "SELECT Max(TO_NUMBER(STATE_ID)) AS NEIGHBOR_ID FROM UNITSHUB_PROJECTSTATUS WHERE PROJECT_ID = '" &
                    projectId.Replace("'", "''") & "' AND TO_NUMBER(STATE_ID) > " & referenceId
                '"SELECT MIN(TO_NUMBER(STATE_ID)) AS NEIGHBOR_ID FROM UNITSHUB_PROJECTSTATUS " &
                '"WHERE PROJECT_ID = '" & projectId.Replace("'", "''") & "' AND TO_NUMBER(STATE_ID) > " & referenceId

            Else
                neighborSql = "SELECT Min(TO_NUMBER(STATE_ID)) AS NEIGHBOR_ID FROM UNITSHUB_PROJECTSTATUS WHERE PROJECT_ID = '" &
                    projectId.Replace("'", "''") & "' AND TO_NUMBER(STATE_ID) > " & referenceId

                '"SELECT MAX(TO_NUMBER(STATE_ID)) AS NEIGHBOR_ID FROM UNITSHUB_PROJECTSTATUS " &
                '"WHERE PROJECT_ID = '" & projectId.Replace("'", "''") & "' AND TO_NUMBER(STATE_ID) < " & referenceId
            End If

            Dim neighborDT As New Data.DataTable
            neighborDT = GetDataTable(EBDB, neighborSql)

            If neighborDT.Rows(0)("NEIGHBOR_ID") Is DBNull.Value Then
                lblMessage.CssClass = "msg-error"
                lblMessage.Text = "Could not find a neighboring status to insert " &
                    If(DirParam = "Asc", "above", "below") & " STATE_ID " & referenceId.ToString("0000") & "."
                Return
            End If

            Dim neighborId As Integer = CInt(neighborDT.Rows(0)("NEIGHBOR_ID"))

            Dim isBoundaryPair As Boolean =
                (referenceId = 0 AndAlso neighborId = 9216) OrElse
                (referenceId = 9216 AndAlso neighborId = 0)

            Dim newStateIdNumber As Integer
            If isBoundaryPair Then
                newStateIdNumber = 1024
            Else
                newStateIdNumber = CInt((referenceId + neighborId) / 2)
            End If

            stateId = newStateIdNumber.ToString("0000")
        Else
            ' No reference state given (e.g. page opened outside the normal Add State ▲/▼
            ' popup flow) — fall back to a plain next-available id.
            Dim nextIdDT As New Data.DataTable
            nextIdDT = GetDataTable(EBDB, "SELECT LPAD(NVL(MAX(TO_NUMBER(STATE_ID)), 0) + 1, 4, '0') AS NEXT_ID FROM UNITSHUB_PROJECTSTATUS")
            stateId = nextIdDT.Rows(0)("NEXT_ID").ToString()
        End If

        Dim insertSql As String =
            "INSERT INTO UNITSHUB_PROJECTSTATUS (STATE_ID, PROJECT_ID, STATUS, SUBTITLE, " &
            "STATUS_BG_COLOR, STATUS_FG_COLOR, SUBTITLE_BG_COLOR, SUBTITLE_FG_COLOR) VALUES (" &
            "'" & stateId & "', " &
            "'" & ddlProjectName.SelectedValue.Replace("'", "''") & "', " &
            "'" & txtStatusTitle.Text.Trim().Replace("'", "''") & "', " &
            "'" & txtStatusSubtitle.Text.Trim().Replace("'", "''") & "', " &
            "'" & colorStatusBg.Value.Replace("'", "''") & "', " &
            "'" & colorStatusFg.Value.Replace("'", "''") & "', " &
            "'" & colorSubtitleBg.Value.Replace("'", "''") & "', " &
            "'" & colorSubtitleFg.Value.Replace("'", "''") & "')"

        ExecuteNonQuery(EBDB, insertSql)

        lblMessage.CssClass = "msg-success"
        lblMessage.Text = "Status '" & txtStatusTitle.Text.Trim() & "' saved (ID " & stateId & ")."

        ' Reset the entry fields for the next status, but keep the selected project —
        ' statuses are usually added one project at a time.
        Dim currentProject As String = ddlProjectName.SelectedValue
        txtStatusTitle.Text = ""
        txtStatusSubtitle.Text = ""
        ddlProjectName.SelectedValue = currentProject
    End Sub

End Class
