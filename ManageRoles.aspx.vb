Imports System.Data
Imports System.Collections.Generic

''' <summary>
''' Manage Roles: pick a role (or add a new one), pick where (Project -> Status ->
''' Sub-status), then tick the actions that role may perform there and Save.
'''
''' Tables:
'''   UNITSHUB_SEC_ROLES          (ROLE_ID, ROLE_NAME, DESCRIPTION)
'''   UNITSHUB_SEC_ROLES_ACTIONS  (ROLE_ID, PROJECT_ID, STATE_ID, ACTION_ID)
'''   UNITSHUB_PROJECTS, UNITSHUB_PROJECTSTATUS, UNITSHUB_PROJECTSUBSTATUS,
'''   UNITSHUB_ACTION_PLACEMENTS + UNITSHUB_ACTIONS (which actions exist at a place)
'''
''' UNITSHUB_SEC_ROLES_ACTIONS has no sub-status column: the sub-status only narrows
''' which actions are listed; ticks are saved per status.
''' Optional parameters: ?RoleID=&ProjectID=&StatusID=&SUBSTATEID= pre-select the lists.
''' </summary>
Partial Class ManageRoles
    Inherits System.Web.UI.Page

    Protected Sub Page_Load(sender As Object, e As EventArgs) Handles Me.Load
        If Not IsPostBack Then
            LoadRoles(QueryValue("RoleID"))
            LoadProjects(QueryValue("ProjectID"))
            LoadStatuses(QueryValue("StatusID"))
            LoadSubStatuses(QueryValue("SUBSTATEID"))
            BindActions()
        End If
    End Sub

    ''' <summary>A request parameter, trimmed; "" when it wasn't sent (never Nothing).</summary>
    Private Function QueryValue(Name As String) As String
        Return If(Request(Name), "").Trim()
    End Function

    ''' <summary>A value as a quoted, apostrophe-escaped SQL literal.</summary>
    Private Function Q(Value As String) As String
        Return "'" & If(Value, "").Replace("'", "''") & "'"
    End Function

    ' ==================================================================
    ' Role: pick one, or add a new one
    ' ==================================================================

    ''' <summary>All roles (UNITSHUB_SEC_ROLES), by name.</summary>
    Private Sub LoadRoles(Preselect As String)
        ddlRole.Items.Clear()
        ddlRole.Items.Add(New ListItem("-- Select a role --", ""))
        Dim DT As DataTable = GetDataTable(EBDB,
            "SELECT ROLE_ID, ROLE_NAME, DESCRIPTION FROM UNITSHUB_SEC_ROLES ORDER BY ROLE_NAME")
        If DT IsNot Nothing Then
            For Each r As DataRow In DT.Rows
                Dim item As New ListItem(Convert.ToString(r("ROLE_NAME")).Trim(), Convert.ToString(r("ROLE_ID")).Trim())
                Dim description As String = Convert.ToString(r("DESCRIPTION")).Trim()
                If description <> "" Then item.Attributes("title") = description
                ddlRole.Items.Add(item)
            Next
        End If
        SelectIfPresent(ddlRole, Preselect)
    End Sub

    Protected Sub ddlRole_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ddlRole.SelectedIndexChanged
        lblRoleNote.Visible = False
        BindActions()
    End Sub

    Protected Sub btnShowAddRole_Click(sender As Object, e As EventArgs) Handles btnShowAddRole.Click
        txtNewRoleName.Text = ""
        txtNewRoleDesc.Text = ""
        lblRoleNote.Visible = False
        pnlNewRole.Visible = True
        btnShowAddRole.Visible = False
        txtNewRoleName.Focus()
    End Sub

    Protected Sub btnCancelAddRole_Click(sender As Object, e As EventArgs) Handles btnCancelAddRole.Click
        pnlNewRole.Visible = False
        btnShowAddRole.Visible = True
        lblRoleNote.Visible = False
    End Sub

    ''' <summary>
    ''' Adds the role (next ROLE_ID) to UNITSHUB_SEC_ROLES and selects it, ready for its
    ''' actions to be ticked. The name is required and must not already be used.
    ''' </summary>
    Protected Sub btnAddRole_Click(sender As Object, e As EventArgs) Handles btnAddRole.Click
        Dim roleName As String = txtNewRoleName.Text.Trim()
        Dim description As String = txtNewRoleDesc.Text.Trim()

        If roleName = "" Then
            ShowNote(lblRoleNote, "Enter the role name.", "err")
            Exit Sub
        End If

        Dim dup As DataTable = GetDataTable(EBDB,
            "SELECT ROLE_ID FROM UNITSHUB_SEC_ROLES WHERE UPPER(TRIM(ROLE_NAME)) = " & Q(roleName.ToUpperInvariant()))
        If dup IsNot Nothing AndAlso dup.Rows.Count > 0 Then
            ShowNote(lblRoleNote, "A role named '" & roleName & "' already exists.", "err")
            Exit Sub
        End If

        Try
            Dim newId As String = GetNextRoleId()
            DB.ExecuteNonQuery(EBDB_CS,
                "INSERT INTO UNITSHUB_SEC_ROLES (ROLE_ID, ROLE_NAME, DESCRIPTION) VALUES (" &
                Q(newId) & ", " & Q(roleName) & ", " & If(description = "", "NULL", Q(description)) & ")")

            LoadRoles(newId)
            pnlNewRole.Visible = False
            btnShowAddRole.Visible = True
            ShowNote(lblRoleNote, "Role '" & roleName & "' added (ID " & newId & "). Now choose where and tick its actions.", "ok")
            BindActions()
        Catch ex As Exception
            ShowNote(lblRoleNote, "The role couldn't be added: " & ex.Message, "err")
        End Try
    End Sub

    ''' <summary>
    ''' Next ROLE_ID: the highest numeric ROLE_ID + 1, padded with zeros to the width of the
    ''' existing IDs (e.g. 0007 -> 0008). "1" when there are none.
    ''' </summary>
    Private Function GetNextRoleId() As String
        Dim DT As DataTable = GetDataTable(EBDB,
            "SELECT MAX(TO_NUMBER(ROLE_ID)) AS MAX_ID, MAX(LENGTH(ROLE_ID)) AS ID_LEN " &
            "FROM UNITSHUB_SEC_ROLES WHERE REGEXP_LIKE(ROLE_ID, '^[0-9]+$')")
        Dim maxId As Long = 0
        Dim width As Integer = 1
        If DT IsNot Nothing AndAlso DT.Rows.Count > 0 Then
            If Not IsDBNull(DT.Rows(0)("MAX_ID")) Then maxId = Convert.ToInt64(DT.Rows(0)("MAX_ID"))
            If Not IsDBNull(DT.Rows(0)("ID_LEN")) Then width = Convert.ToInt32(DT.Rows(0)("ID_LEN"))
        End If
        Return (maxId + 1).ToString().PadLeft(width, "0"c)
    End Function

    ' ==================================================================
    ' Where: Project -> Status -> Sub-status
    ' ==================================================================

    ''' <summary>Projects (UNITSHUB_PROJECTS), by English name.</summary>
    Private Sub LoadProjects(Preselect As String)
        ddlProject.Items.Clear()
        ddlProject.Items.Add(New ListItem("-- Select a project --", ""))
        AddItems(ddlProject, GetDataTable(EBDB,
            "SELECT PROJECT_ID, PROJECT_NAME_EN FROM UNITSHUB_PROJECTS ORDER BY PROJECT_NAME_EN"),
            "PROJECT_ID", "PROJECT_NAME_EN")
        SelectIfPresent(ddlProject, Preselect)
    End Sub

    ''' <summary>The selected project's statuses (UNITSHUB_PROJECTSTATUS), in STATE_ID order.</summary>
    Private Sub LoadStatuses(Preselect As String)
        ddlStatus.Items.Clear()
        ddlStatus.Items.Add(New ListItem("-- Select a status --", ""))
        Dim projectId As String = ddlProject.SelectedValue
        ddlStatus.Enabled = (projectId <> "")
        If projectId = "" Then Exit Sub

        AddItems(ddlStatus, GetDataTable(EBDB,
            "SELECT STATE_ID, STATUS FROM UNITSHUB_PROJECTSTATUS WHERE PROJECT_ID = " & Q(projectId) &
            " ORDER BY TO_NUMBER(STATE_ID)"), "STATE_ID", "STATUS")
        SelectIfPresent(ddlStatus, Preselect)
    End Sub

    ''' <summary>The selected status's sub-statuses; the field is hidden when it has none.</summary>
    Private Sub LoadSubStatuses(Preselect As String)
        ddlSubStatus.Items.Clear()
        ddlSubStatus.Items.Add(New ListItem("-- Whole status --", ""))
        pnlSubStatus.Visible = False
        Dim projectId As String = ddlProject.SelectedValue
        Dim statusId As String = ddlStatus.SelectedValue
        If projectId = "" OrElse statusId = "" Then Exit Sub

        Dim DT As DataTable = GetDataTable(EBDB,
            "SELECT SUBSTATE_ID, SUBTITLE FROM UNITSHUB_PROJECTSUBSTATUS WHERE PROJECT_ID = " & Q(projectId) &
            " AND STATE_ID = " & Q(statusId) & " ORDER BY SORT_ORDER, SUBSTATE_ID")
        If DT Is Nothing OrElse DT.Rows.Count = 0 Then Exit Sub

        AddItems(ddlSubStatus, DT, "SUBSTATE_ID", "SUBTITLE")
        SelectIfPresent(ddlSubStatus, Preselect)
        pnlSubStatus.Visible = True
    End Sub

    Protected Sub ddlProject_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ddlProject.SelectedIndexChanged
        LoadStatuses("")
        LoadSubStatuses("")
        BindActions()
    End Sub

    Protected Sub ddlStatus_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ddlStatus.SelectedIndexChanged
        LoadSubStatuses("")
        BindActions()
    End Sub

    Protected Sub ddlSubStatus_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ddlSubStatus.SelectedIndexChanged
        BindActions()
    End Sub

    ' ==================================================================
    ' Actions: tick the ones the role may perform at the selected place
    ' ==================================================================

    Private ReadOnly Property RoleId As String
        Get
            Return ddlRole.SelectedValue
        End Get
    End Property

    Private ReadOnly Property ProjectId As String
        Get
            Return ddlProject.SelectedValue
        End Get
    End Property

    Private ReadOnly Property StatusId As String
        Get
            Return ddlStatus.SelectedValue
        End Get
    End Property

    Private ReadOnly Property SubStatusId As String
        Get
            Return If(pnlSubStatus.Visible, ddlSubStatus.SelectedValue, "")
        End Get
    End Property

    ''' <summary>
    ''' Lists the actions available at the selected place (same placement rule as
    ''' ProjectStatusAndAction: placed on all statuses, on this status, or on the selected
    ''' sub-status; with no sub-status chosen, on any of this status's sub-statuses too).
    ''' Ticked = the role is allowed it here (UNITSHUB_SEC_ROLES_ACTIONS).
    ''' The ticks can only be changed once a role is chosen.
    ''' </summary>
    Private Sub BindActions()
        cblActions.Items.Clear()
        lblActionsNote.Visible = False

        litWhere.Text = If(StatusId = "", "<em>choose a project and status</em>",
            Server.HtmlEncode(ddlProject.SelectedItem.Text & " - " & ddlStatus.SelectedItem.Text &
                              If(SubStatusId <> "", " / " & ddlSubStatus.SelectedItem.Text, "")))

        Dim canEdit As Boolean = (RoleId <> "" AndAlso ProjectId <> "" AndAlso StatusId <> "")
        cblActions.Enabled = canEdit
        pnlSelectTools.Visible = canEdit
        btnSaveActions.Enabled = False

        If ProjectId = "" OrElse StatusId = "" Then
            ShowNote(lblActionsNote, "Choose a project and status to see their actions.", "info")
            Exit Sub
        End If

        Try
            Dim place As String
            If SubStatusId = "" Then
                place = "(P.STATUS_ID = '*' OR P.STATUS_ID = " & Q(StatusId) & ")"
            Else
                place = "(P.STATUS_ID = '*' OR (P.STATUS_ID = " & Q(StatusId) & " AND P.SUBSTATE_ID IN (" & Q(SubStatusId) & ", '*')))"
            End If

            Dim SQL As String = ""
            SQL = SQL + vbCrLf + " SELECT A.ACTION_ID, A.ACTION_TITLE, "
            If RoleId <> "" Then
                SQL = SQL + vbCrLf + "        CASE WHEN EXISTS (SELECT 1 FROM UNITSHUB_SEC_ROLES_ACTIONS RA "
                SQL = SQL + vbCrLf + "                          WHERE RA.ROLE_ID = " & Q(RoleId)
                SQL = SQL + vbCrLf + "                            AND RA.PROJECT_ID = A.PROJECT_ID "
                SQL = SQL + vbCrLf + "                            AND RA.STATE_ID = " & Q(StatusId)
                SQL = SQL + vbCrLf + "                            AND RA.ACTION_ID = A.ACTION_ID) "
                SQL = SQL + vbCrLf + "             THEN 'Y' ELSE 'N' END AS IS_ALLOWED "
            Else
                SQL = SQL + vbCrLf + "        'N' AS IS_ALLOWED "
            End If
            SQL = SQL + vbCrLf + " FROM   UNITSHUB_ACTIONS A "
            SQL = SQL + vbCrLf + " WHERE  A.PROJECT_ID = " & Q(ProjectId)
            SQL = SQL + vbCrLf + "   AND  EXISTS (SELECT 1 FROM UNITSHUB_ACTION_PLACEMENTS P "
            SQL = SQL + vbCrLf + "                WHERE P.PROJECT_ID = A.PROJECT_ID AND P.ACTION_ID = A.ACTION_ID "
            SQL = SQL + vbCrLf + "                  AND " & place & ") "
            SQL = SQL + vbCrLf + " ORDER BY A.ACTION_TITLE "

            Dim DT As DataTable = GetDataTable(EBDB, SQL)
            If DT IsNot Nothing Then
                For Each r As DataRow In DT.Rows
                    Dim item As New ListItem(Convert.ToString(r("ACTION_TITLE")).Trim(), Convert.ToString(r("ACTION_ID")).Trim())
                    item.Selected = (Convert.ToString(r("IS_ALLOWED")) = "Y")
                    cblActions.Items.Add(item)
                Next
            End If

            btnSaveActions.Enabled = canEdit AndAlso cblActions.Items.Count > 0

            If cblActions.Items.Count = 0 Then
                ShowNote(lblActionsNote, "No actions are placed here.", "info")
                pnlSelectTools.Visible = False
            ElseIf RoleId = "" Then
                ShowNote(lblActionsNote, "Choose a role above to tick the actions it may perform here.", "info")
            End If
        Catch ex As Exception
            cblActions.Enabled = False
            btnSaveActions.Enabled = False
            ShowNote(lblActionsNote, "Actions can't be shown: " & ex.Message, "err")
        End Try
    End Sub

    ''' <summary>
    ''' Saves the ticks for the role at the selected project and status: for every action
    ''' listed, its UNITSHUB_SEC_ROLES_ACTIONS row is removed, then added back when ticked.
    ''' Actions not listed (e.g. placed only on another sub-status) are left as they are.
    ''' </summary>
    Protected Sub btnSaveActions_Click(sender As Object, e As EventArgs) Handles btnSaveActions.Click
        If RoleId = "" OrElse ProjectId = "" OrElse StatusId = "" Then Exit Sub
        If cblActions.Items.Count = 0 Then Exit Sub

        ' Read the ticks before BindActions rebuilds the list
        Dim listed As New List(Of String)
        Dim ticked As New List(Of String)
        For Each item As ListItem In cblActions.Items
            listed.Add(item.Value)
            If item.Selected Then ticked.Add(item.Value)
        Next

        Try
            Dim inList As String = String.Join(", ", listed.ConvertAll(Function(x) Q(x)))
            DB.ExecuteNonQuery(EBDB_CS,
                "DELETE FROM UNITSHUB_SEC_ROLES_ACTIONS WHERE ROLE_ID = " & Q(RoleId) &
                " AND PROJECT_ID = " & Q(ProjectId) & " AND STATE_ID = " & Q(StatusId) &
                " AND ACTION_ID IN (" & inList & ")")

            For Each actionId As String In ticked
                DB.ExecuteNonQuery(EBDB_CS,
                    "INSERT INTO UNITSHUB_SEC_ROLES_ACTIONS (ROLE_ID, PROJECT_ID, STATE_ID, ACTION_ID) VALUES (" &
                    Q(RoleId) & ", " & Q(ProjectId) & ", " & Q(StatusId) & ", " & Q(actionId) & ")")
            Next

            BindActions()
            ShowNote(lblActionsNote, "Saved: " & ddlRole.SelectedItem.Text & " may perform " & ticked.Count &
                     " of " & listed.Count & If(listed.Count = 1, " action", " actions") & " here.", "ok")
        Catch ex As Exception
            ShowNote(lblActionsNote, "The actions couldn't be saved: " & ex.Message, "err")
        End Try
    End Sub

    ' ==================================================================
    ' Helpers
    ' ==================================================================

    ''' <summary>Adds one item per row (value column, text column) after the existing items.</summary>
    Private Sub AddItems(ddl As DropDownList, DT As DataTable, ValueColumn As String, TextColumn As String)
        If DT Is Nothing Then Exit Sub
        For Each r As DataRow In DT.Rows
            Dim value As String = Convert.ToString(r(ValueColumn)).Trim()
            Dim text As String = Convert.ToString(r(TextColumn)).Trim()
            ddl.Items.Add(New ListItem(If(text = "", value, text), value))
        Next
    End Sub

    ''' <summary>Selects Value when the list has it; otherwise leaves the first item selected.</summary>
    Private Sub SelectIfPresent(ddl As DropDownList, Value As String)
        If String.IsNullOrEmpty(Value) Then Exit Sub
        If ddl.Items.FindByValue(Value) IsNot Nothing Then ddl.SelectedValue = Value
    End Sub

    Private Sub ShowNote(Target As Label, Text As String, Kind As String)
        Target.Text = Server.HtmlEncode(Text)
        Target.CssClass = "note " & Kind
        Target.Visible = True
    End Sub

    ''' <summary>Closes the popup.</summary>
    Protected Sub imgClose_Click(sender As Object, e As ImageClickEventArgs) Handles imgClose.Click
        VendorPopupHelper.RegisterPopupSelectionAndClose(Me, False, skipPostBack:=False)
    End Sub
End Class
