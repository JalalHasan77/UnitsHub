Imports System.Data
Imports System.Collections.Generic

''' <summary>
''' Manage Groups: pick a group (or add a new one), tick the users who belong to it,
''' then choose a place (Project -> Status -> Sub-status) and set each action there to
''' Inherit (whatever the group's roles give), Allow or Deny.
'''
''' Tables:
'''   UNITSHUB_SEC_GROUPS          (GROUP_ID, GROUP_NAME, DESCRIPTION, IS_SYSTEM)
'''   UNITSHUB_SEC_USER_GROUPS     (USER_ID, GROUP_ID)                       - members
'''   UNITSHUB_SEC_GROUPS_ACTIONS  (GROUP_ID, PROJECT_ID, STATE_ID, ACTION_ID, ALLOW_DENY)
'''   UNITSHUB_SEC_GROUPS_ROLES + UNITSHUB_SEC_ROLES_ACTIONS + UNITSHUB_SEC_ROLES (what roles give)
'''   UNITSHUB_USERS (USER_ID, FULL_NAME, EMAIL, IS_ENABLED, ...)
'''   UNITSHUB_PROJECTS, UNITSHUB_PROJECTSTATUS, UNITSHUB_PROJECTSUBSTATUS,
'''   UNITSHUB_ACTION_PLACEMENTS + UNITSHUB_ACTIONS (which actions exist at a place)
'''
''' ALLOW_DENY is saved as 'A' (allow) or 'D' (deny). "Inherit" means no row.
''' The security tables have no sub-status column: the sub-status only narrows which
''' actions are listed; settings are saved per status.
''' Members, Role and Actions are tabs (menuTabs + mvTabs) under the Group card;
''' the Role tab sets the group's one role (UNITSHUB_SEC_GROUPS_ROLES; a role can serve many groups).
''' Optional parameters: ?GroupID=&ProjectID=&StatusID=&SUBSTATEID= pre-select the lists;
''' ?Tab=Roles opens on Roles; ?Tab=Actions (or a ProjectID) opens on Actions.
''' </summary>
Partial Class ManageGroups
    Inherits System.Web.UI.Page

    Protected Sub Page_Load(sender As Object, e As EventArgs) Handles Me.Load
        If Not IsPostBack Then
            LoadGroups(QueryValue("GroupID"))
            LoadProjects(QueryValue("ProjectID"))
            LoadStatuses(QueryValue("StatusID"))
            LoadSubStatuses(QueryValue("SUBSTATEID"))
            RefreshGroup()

            ' Open on Actions when the caller says so (?Tab=Actions) or passes a place
            If QueryValue("Tab").Equals("Roles", StringComparison.OrdinalIgnoreCase) Then
                ShowTab(RolesTabIndex)
            ElseIf QueryValue("Tab").Equals("Actions", StringComparison.OrdinalIgnoreCase) OrElse
                   QueryValue("ProjectID") <> "" Then
                ShowTab(ActionsTabIndex)
            Else
                ShowTab(MembersTabIndex)
            End If
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

    ''' <summary>Re-shows everything that depends on the selected group.</summary>
    Private Sub RefreshGroup()
        BindGroupInfo()
        BindMembers()
        BindRoles()
        BindActions()
    End Sub

    ' ==================================================================
    ' Group: pick one, or add a new one
    ' ==================================================================

    Private ReadOnly Property GroupId As String
        Get
            Return ddlGroup.SelectedValue
        End Get
    End Property

    ''' <summary>All groups (UNITSHUB_SEC_GROUPS), by name; system groups are marked.</summary>
    Private Sub LoadGroups(Preselect As String)
        ddlGroup.Items.Clear()
        ddlGroup.Items.Add(New ListItem("-- Select a group --", ""))
        Dim DT As DataTable = GetDataTable(EBDB,
            "SELECT GROUP_ID, GROUP_NAME, DESCRIPTION, IS_SYSTEM FROM UNITSHUB_SEC_GROUPS ORDER BY GROUP_NAME")
        If DT IsNot Nothing Then
            For Each r As DataRow In DT.Rows
                Dim name As String = Convert.ToString(r("GROUP_NAME")).Trim()
                If IsYes(r("IS_SYSTEM")) Then name &= "  [System]"
                Dim item As New ListItem(name, Convert.ToString(r("GROUP_ID")).Trim())
                Dim description As String = Convert.ToString(r("DESCRIPTION")).Trim()
                If description <> "" Then item.Attributes("title") = description
                ddlGroup.Items.Add(item)
            Next
        End If
        SelectIfPresent(ddlGroup, Preselect)
    End Sub

    ''' <summary>True for Y / YES / 1 / T / TRUE (any case).</summary>
    Private Function IsYes(Value As Object) As Boolean
        Dim s As String = Convert.ToString(Value).Trim().ToUpperInvariant()
        Return s = "Y" OrElse s = "YES" OrElse s = "1" OrElse s = "T" OrElse s = "TRUE"
    End Function

    ''' <summary>One line under the dropdown: description, roles and member count.</summary>
    Private Sub BindGroupInfo()
        lblGroupInfo.Visible = False
        If GroupId = "" Then Exit Sub

        Try
            Dim SQL As String = ""
            SQL = SQL + vbCrLf + " SELECT G.DESCRIPTION, G.IS_SYSTEM, "
            SQL = SQL + vbCrLf + "        (SELECT LISTAGG(R.ROLE_NAME, ', ') WITHIN GROUP (ORDER BY R.ROLE_NAME) "
            SQL = SQL + vbCrLf + "           FROM UNITSHUB_SEC_GROUPS_ROLES GR "
            SQL = SQL + vbCrLf + "           JOIN UNITSHUB_SEC_ROLES R ON R.ROLE_ID = GR.ROLE_ID "
            SQL = SQL + vbCrLf + "          WHERE GR.GROUP_ID = G.GROUP_ID) AS ROLE_NAMES, "
            SQL = SQL + vbCrLf + "        (SELECT COUNT(*) FROM UNITSHUB_SEC_USER_GROUPS UG "
            SQL = SQL + vbCrLf + "          WHERE UG.GROUP_ID = G.GROUP_ID) AS MEMBER_COUNT "
            SQL = SQL + vbCrLf + " FROM   UNITSHUB_SEC_GROUPS G "
            SQL = SQL + vbCrLf + " WHERE  G.GROUP_ID = " & Q(GroupId)

            Dim DT As DataTable = GetDataTable(EBDB, SQL)
            If DT Is Nothing OrElse DT.Rows.Count = 0 Then Exit Sub
            Dim r As DataRow = DT.Rows(0)

            Dim parts As New List(Of String)
            Dim description As String = Convert.ToString(r("DESCRIPTION")).Trim()
            If description <> "" Then parts.Add(Server.HtmlEncode(description))
            If IsYes(r("IS_SYSTEM")) Then parts.Add("<strong>System group</strong>")
            Dim roles As String = Convert.ToString(r("ROLE_NAMES")).Trim()
            parts.Add("Role: <strong>" & If(roles = "", "none", Server.HtmlEncode(roles)) & "</strong>")
            parts.Add("Members: <strong>" & Convert.ToString(r("MEMBER_COUNT")) & "</strong>")

            lblGroupInfo.Text = String.Join(" &middot; ", parts)
            lblGroupInfo.Visible = True
        Catch ex As Exception
            ' The info line is only a convenience; the rest of the form still works
        End Try
    End Sub

    Protected Sub ddlGroup_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ddlGroup.SelectedIndexChanged
        lblGroupNote.Visible = False
        RefreshGroup()
    End Sub

    Protected Sub btnShowAddGroup_Click(sender As Object, e As EventArgs) Handles btnShowAddGroup.Click
        txtNewGroupName.Text = ""
        txtNewGroupDesc.Text = ""
        chkNewGroupSystem.Checked = False
        lblGroupNote.Visible = False
        pnlNewGroup.Visible = True
        btnShowAddGroup.Visible = False
        txtNewGroupName.Focus()
    End Sub

    Protected Sub btnCancelAddGroup_Click(sender As Object, e As EventArgs) Handles btnCancelAddGroup.Click
        pnlNewGroup.Visible = False
        btnShowAddGroup.Visible = True
        lblGroupNote.Visible = False
    End Sub

    ''' <summary>
    ''' Adds the group (next GROUP_ID) to UNITSHUB_SEC_GROUPS and selects it, ready for its
    ''' members and actions. The name is required and must not already be used.
    ''' </summary>
    Protected Sub btnAddGroup_Click(sender As Object, e As EventArgs) Handles btnAddGroup.Click
        Dim groupName As String = txtNewGroupName.Text.Trim()
        Dim description As String = txtNewGroupDesc.Text.Trim()

        If groupName = "" Then
            ShowNote(lblGroupNote, "Enter the group name.", "err")
            Exit Sub
        End If

        Dim dup As DataTable = GetDataTable(EBDB,
            "SELECT GROUP_ID FROM UNITSHUB_SEC_GROUPS WHERE UPPER(TRIM(GROUP_NAME)) = " & Q(groupName.ToUpperInvariant()))
        If dup IsNot Nothing AndAlso dup.Rows.Count > 0 Then
            ShowNote(lblGroupNote, "A group named '" & groupName & "' already exists.", "err")
            Exit Sub
        End If

        Try
            Dim newId As String = GetNextGroupId()
            DB.ExecuteNonQuery(EBDB_CS,
                "INSERT INTO UNITSHUB_SEC_GROUPS (GROUP_ID, GROUP_NAME, DESCRIPTION, IS_SYSTEM) VALUES (" &
                Q(newId) & ", " & Q(groupName) & ", " & If(description = "", "NULL", Q(description)) & ", " &
                If(chkNewGroupSystem.Checked, "'Y'", "'N'") & ")")

            LoadGroups(newId)
            pnlNewGroup.Visible = False
            btnShowAddGroup.Visible = True
            RefreshGroup()
            ShowTab(MembersTabIndex)
            ShowNote(lblGroupNote, "Group '" & groupName & "' added (ID " & newId & "). Now tick its members and set its actions.", "ok")
        Catch ex As Exception
            ShowNote(lblGroupNote, "The group couldn't be added: " & ex.Message, "err")
        End Try
    End Sub

    ''' <summary>
    ''' Next GROUP_ID: the highest numeric GROUP_ID + 1, padded with zeros to the width of
    ''' the existing IDs (e.g. 0007 -> 0008). "1" when there are none.
    ''' </summary>
    Private Function GetNextGroupId() As String
        Dim DT As DataTable = GetDataTable(EBDB,
            "SELECT MAX(TO_NUMBER(GROUP_ID)) AS MAX_ID, MAX(LENGTH(GROUP_ID)) AS ID_LEN " &
            "FROM UNITSHUB_SEC_GROUPS WHERE REGEXP_LIKE(GROUP_ID, '^[0-9]+$')")
        Dim maxId As Long = 0
        Dim width As Integer = 1
        If DT IsNot Nothing AndAlso DT.Rows.Count > 0 Then
            If Not IsDBNull(DT.Rows(0)("MAX_ID")) Then maxId = Convert.ToInt64(DT.Rows(0)("MAX_ID"))
            If Not IsDBNull(DT.Rows(0)("ID_LEN")) Then width = Convert.ToInt32(DT.Rows(0)("ID_LEN"))
        End If
        Return (maxId + 1).ToString().PadLeft(width, "0"c)
    End Function

    ' ==================================================================
    ' Members: the users who belong to the group
    ' ==================================================================

    ''' <summary>
    ''' Lists every user (UNITSHUB_USERS); ticked = in the group (UNITSHUB_SEC_USER_GROUPS).
    ''' Disabled accounts are marked but can still be ticked.
    ''' </summary>
    Private Sub BindMembers()
        cblMembers.Items.Clear()
        lblMembersNote.Visible = False
        SetTabCaption(MembersTabIndex, "Members", -1)

        Dim canEdit As Boolean = (GroupId <> "")
        cblMembers.Enabled = canEdit
        pnlMemberTools.Visible = canEdit
        btnSaveMembers.Enabled = False

        Try
            Dim SQL As String = ""
            SQL = SQL + vbCrLf + " SELECT U.USER_ID, U.FULL_NAME, U.EMAIL, U.IS_ENABLED, "
            If canEdit Then
                SQL = SQL + vbCrLf + "        CASE WHEN EXISTS (SELECT 1 FROM UNITSHUB_SEC_USER_GROUPS UG "
                SQL = SQL + vbCrLf + "                          WHERE UG.USER_ID = U.USER_ID AND UG.GROUP_ID = " & Q(GroupId) & ") "
                SQL = SQL + vbCrLf + "             THEN 'Y' ELSE 'N' END AS IS_MEMBER "
            Else
                SQL = SQL + vbCrLf + "        'N' AS IS_MEMBER "
            End If
            SQL = SQL + vbCrLf + " FROM   UNITSHUB_USERS U "
            SQL = SQL + vbCrLf + " ORDER BY NVL(U.FULL_NAME, U.USER_ID) "

            Dim DT As DataTable = GetDataTable(EBDB, SQL)
            If DT IsNot Nothing Then
                For Each r As DataRow In DT.Rows
                    Dim userId As String = Convert.ToString(r("USER_ID")).Trim()
                    Dim fullName As String = Convert.ToString(r("FULL_NAME")).Trim()
                    Dim enabled As String = Convert.ToString(r("IS_ENABLED")).Trim().ToUpperInvariant()

                    ' CheckBoxList renders item text as HTML, so encode the values
                    Dim html As String = Server.HtmlEncode(If(fullName = "", userId, fullName)) &
                                         " <span class=""u-id"">(" & Server.HtmlEncode(userId) & ")</span>"
                    If enabled.StartsWith("N") OrElse enabled = "0" Then
                        html &= "<span class=""u-tag"">Disabled</span>"
                    End If

                    Dim item As New ListItem(html, userId)
                    item.Selected = (Convert.ToString(r("IS_MEMBER")) = "Y")
                    Dim email As String = Convert.ToString(r("EMAIL")).Trim()
                    If email <> "" Then item.Attributes("title") = email
                    cblMembers.Items.Add(item)
                Next
            End If

            btnSaveMembers.Enabled = canEdit AndAlso cblMembers.Items.Count > 0

            ' Tab caption shows how many users are in the group
            If canEdit Then
                Dim memberCount As Integer = 0
                For Each item As ListItem In cblMembers.Items
                    If item.Selected Then memberCount += 1
                Next
                SetTabCaption(MembersTabIndex, "Members", memberCount)
            End If

            If cblMembers.Items.Count = 0 Then
                ShowNote(lblMembersNote, "There are no users.", "info")
                pnlMemberTools.Visible = False
            ElseIf Not canEdit Then
                ShowNote(lblMembersNote, "Choose a group above to tick its members.", "info")
            End If
        Catch ex As Exception
            cblMembers.Enabled = False
            btnSaveMembers.Enabled = False
            ShowNote(lblMembersNote, "Users can't be shown: " & ex.Message, "err")
        End Try
    End Sub

    ''' <summary>
    ''' Replaces the group's members with the ticked users: its UNITSHUB_SEC_USER_GROUPS
    ''' rows are removed, then one row is added per ticked user.
    ''' </summary>
    Protected Sub btnSaveMembers_Click(sender As Object, e As EventArgs) Handles btnSaveMembers.Click
        If GroupId = "" OrElse cblMembers.Items.Count = 0 Then Exit Sub

        ' Read the ticks before BindMembers rebuilds the list
        Dim ticked As New List(Of String)
        For Each item As ListItem In cblMembers.Items
            If item.Selected Then ticked.Add(item.Value)
        Next

        Try
            DB.ExecuteNonQuery(EBDB_CS,
                "DELETE FROM UNITSHUB_SEC_USER_GROUPS WHERE GROUP_ID = " & Q(GroupId))

            For Each userId As String In ticked
                DB.ExecuteNonQuery(EBDB_CS,
                    "INSERT INTO UNITSHUB_SEC_USER_GROUPS (USER_ID, GROUP_ID) VALUES (" &
                    Q(userId) & ", " & Q(GroupId) & ")")
            Next

            BindGroupInfo()
            BindMembers()
            ShowNote(lblMembersNote, "Saved: " & ticked.Count & If(ticked.Count = 1, " member", " members") &
                     " in this group.", "ok")
        Catch ex As Exception
            ShowNote(lblMembersNote, "The members couldn't be saved: " & ex.Message, "err")
        End Try
    End Sub

    ' ==================================================================
    ' Role: the one role assigned to the group
    ' ==================================================================

    ''' <summary>
    ''' Lists "(No role)" plus every role (UNITSHUB_SEC_ROLES) as radio buttons; the one
    ''' assigned to the group (UNITSHUB_SEC_GROUPS_ROLES) is selected. A group has at most
    ''' one role (a role can serve many groups). If old data gives the group more than one
    ''' role, the first by name is selected and a note says Save will keep only that one.
    ''' </summary>
    Private Sub BindRoles()
        rblRoles.Items.Clear()
        lblRolesNote.Visible = False
        SetTabCaption(RolesTabIndex, "Role", -1)

        Dim canEdit As Boolean = (GroupId <> "")
        rblRoles.Enabled = canEdit
        btnSaveRoles.Enabled = False

        Try
            Dim SQL As String = ""
            SQL = SQL + vbCrLf + " SELECT R.ROLE_ID, R.ROLE_NAME, R.DESCRIPTION, "
            If canEdit Then
                SQL = SQL + vbCrLf + "        CASE WHEN EXISTS (SELECT 1 FROM UNITSHUB_SEC_GROUPS_ROLES GR "
                SQL = SQL + vbCrLf + "                          WHERE GR.ROLE_ID = R.ROLE_ID AND GR.GROUP_ID = " & Q(GroupId) & ") "
                SQL = SQL + vbCrLf + "             THEN 'Y' ELSE 'N' END AS IS_ASSIGNED "
            Else
                SQL = SQL + vbCrLf + "        'N' AS IS_ASSIGNED "
            End If
            SQL = SQL + vbCrLf + " FROM   UNITSHUB_SEC_ROLES R "
            SQL = SQL + vbCrLf + " ORDER BY R.ROLE_NAME "

            ' First choice: no role at all
            rblRoles.Items.Add(New ListItem("(No role)<span class=""r-desc"">Members get only the actions allowed on the Actions tab.</span>", ""))

            Dim assignedNames As New List(Of String)
            Dim selectedValue As String = ""
            Dim DT As DataTable = GetDataTable(EBDB, SQL)
            If DT IsNot Nothing Then
                For Each r As DataRow In DT.Rows
                    Dim roleId As String = Convert.ToString(r("ROLE_ID")).Trim()
                    Dim roleName As String = Convert.ToString(r("ROLE_NAME")).Trim()
                    Dim description As String = Convert.ToString(r("DESCRIPTION")).Trim()

                    ' RadioButtonList renders item text as HTML, so encode the values
                    Dim html As String = Server.HtmlEncode(If(roleName = "", roleId, roleName))
                    If description <> "" Then html &= "<span class=""r-desc"">" & Server.HtmlEncode(description) & "</span>"
                    rblRoles.Items.Add(New ListItem(html, roleId))

                    If Convert.ToString(r("IS_ASSIGNED")) = "Y" Then
                        assignedNames.Add(If(roleName = "", roleId, roleName))
                        If selectedValue = "" Then selectedValue = roleId
                    End If
                Next
            End If

            rblRoles.SelectedValue = selectedValue
            btnSaveRoles.Enabled = canEdit
            If canEdit Then
                menuTabs.Items(RolesTabIndex).Text = "Role (" & If(assignedNames.Count = 0, "none", Server.HtmlEncode(assignedNames(0))) & ")"
            End If

            If rblRoles.Items.Count = 1 Then
                ShowNote(lblRolesNote, "There are no roles yet. Add them in Manage Roles.", "info")
            ElseIf Not canEdit Then
                ShowNote(lblRolesNote, "Choose a group above to assign its role.", "info")
            ElseIf assignedNames.Count > 1 Then
                ShowNote(lblRolesNote, "This group currently has " & assignedNames.Count & " roles (" &
                         String.Join(", ", assignedNames) & "). A group may have only one: Save keeps the selected role and removes the others.", "err")
            End If
        Catch ex As Exception
            rblRoles.Enabled = False
            btnSaveRoles.Enabled = False
            ShowNote(lblRolesNote, "Roles can't be shown: " & ex.Message, "err")
        End Try
    End Sub

    ''' <summary>
    ''' Saves the group's single role: its UNITSHUB_SEC_GROUPS_ROLES rows are removed, then
    ''' one row is added for the selected role ("(No role)" adds none). The info line and
    ''' the Actions tab ("From the group's role") are refreshed to match.
    ''' </summary>
    Protected Sub btnSaveRoles_Click(sender As Object, e As EventArgs) Handles btnSaveRoles.Click
        If GroupId = "" Then Exit Sub

        ' Read the choice before BindRoles rebuilds the list
        Dim roleId As String = rblRoles.SelectedValue
        Dim roleName As String = If(rblRoles.SelectedItem Is Nothing OrElse roleId = "", "",
                                    System.Text.RegularExpressions.Regex.Replace(rblRoles.SelectedItem.Text, "<span.*$", ""))

        Try
            DB.ExecuteNonQuery(EBDB_CS,
                "DELETE FROM UNITSHUB_SEC_GROUPS_ROLES WHERE GROUP_ID = " & Q(GroupId))

            If roleId <> "" Then
                DB.ExecuteNonQuery(EBDB_CS,
                    "INSERT INTO UNITSHUB_SEC_GROUPS_ROLES (GROUP_ID, ROLE_ID) VALUES (" &
                    Q(GroupId) & ", " & Q(roleId) & ")")
            End If

            BindGroupInfo()
            BindRoles()
            BindActions()
            ShowNote(lblRolesNote, If(roleId = "", "Saved: this group has no role.",
                                      "Saved: this group's role is " & Server.HtmlDecode(roleName) & "."), "ok")
        Catch ex As Exception
            ShowNote(lblRolesNote, "The role couldn't be saved: " & ex.Message, "err")
        End Try
    End Sub

    ' ==================================================================
    ' Where: Project -> Status -> Sub-status
    ' ==================================================================

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
        ddlStatus.Enabled = (ProjectId <> "")
        If ProjectId = "" Then Exit Sub

        AddItems(ddlStatus, GetDataTable(EBDB,
            "SELECT STATE_ID, STATUS FROM UNITSHUB_PROJECTSTATUS WHERE PROJECT_ID = " & Q(ProjectId) &
            " ORDER BY TO_NUMBER(STATE_ID)"), "STATE_ID", "STATUS")
        SelectIfPresent(ddlStatus, Preselect)
    End Sub

    ''' <summary>The selected status's sub-statuses; the field is hidden when it has none.</summary>
    Private Sub LoadSubStatuses(Preselect As String)
        ddlSubStatus.Items.Clear()
        ddlSubStatus.Items.Add(New ListItem("-- Whole status --", ""))
        pnlSubStatus.Visible = False
        If ProjectId = "" OrElse ddlStatus.SelectedValue = "" Then Exit Sub

        Dim DT As DataTable = GetDataTable(EBDB,
            "SELECT SUBSTATE_ID, SUBTITLE FROM UNITSHUB_PROJECTSUBSTATUS WHERE PROJECT_ID = " & Q(ProjectId) &
            " AND STATE_ID = " & Q(ddlStatus.SelectedValue) & " ORDER BY SORT_ORDER, SUBSTATE_ID")
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
    ' Actions: Inherit / Allow / Deny for the group at the selected place
    ' ==================================================================

    ''' <summary>
    ''' Lists the actions available at the selected place (same placement rule as
    ''' ManageRoles), with:
    '''   VIA_ROLES     - the group's roles that allow the action at this status
    '''   GROUP_SETTING - the group's own row in UNITSHUB_SEC_GROUPS_ACTIONS: A, D or none
    ''' Each row's switch starts on the saved setting (Inherit when there's no row).
    ''' </summary>
    Private Sub BindActions()
        lblActionsNote.Visible = False
        rptActions.DataSource = Nothing
        rptActions.DataBind()

        litWhere.Text = If(StatusId = "", "<em>choose a project and status</em>",
            Server.HtmlEncode(ddlProject.SelectedItem.Text & " - " & ddlStatus.SelectedItem.Text &
                              If(SubStatusId <> "", " / " & ddlSubStatus.SelectedItem.Text, "")))

        btnSaveActions.Enabled = False
        pnlActions.Visible = False

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
            If GroupId <> "" Then
                SQL = SQL + vbCrLf + "        (SELECT LISTAGG(R.ROLE_NAME, ', ') WITHIN GROUP (ORDER BY R.ROLE_NAME) "
                SQL = SQL + vbCrLf + "           FROM UNITSHUB_SEC_GROUPS_ROLES GR "
                SQL = SQL + vbCrLf + "           JOIN UNITSHUB_SEC_ROLES R ON R.ROLE_ID = GR.ROLE_ID "
                SQL = SQL + vbCrLf + "          WHERE GR.GROUP_ID = " & Q(GroupId)
                SQL = SQL + vbCrLf + "            AND EXISTS (SELECT 1 FROM UNITSHUB_SEC_ROLES_ACTIONS RA "
                SQL = SQL + vbCrLf + "                         WHERE RA.ROLE_ID = GR.ROLE_ID "
                SQL = SQL + vbCrLf + "                           AND RA.PROJECT_ID = A.PROJECT_ID "
                SQL = SQL + vbCrLf + "                           AND RA.STATE_ID = " & Q(StatusId)
                SQL = SQL + vbCrLf + "                           AND RA.ACTION_ID = A.ACTION_ID)) AS VIA_ROLES, "
                SQL = SQL + vbCrLf + "        (SELECT CASE WHEN MAX(CASE WHEN SUBSTR(UPPER(TRIM(GA.ALLOW_DENY)), 1, 1) IN ('D', 'N', '0') "
                SQL = SQL + vbCrLf + "                                   THEN 1 ELSE 0 END) = 1 THEN 'D' "
                SQL = SQL + vbCrLf + "                     WHEN COUNT(*) > 0 THEN 'A' END "
                SQL = SQL + vbCrLf + "           FROM UNITSHUB_SEC_GROUPS_ACTIONS GA "
                SQL = SQL + vbCrLf + "          WHERE GA.GROUP_ID = " & Q(GroupId)
                SQL = SQL + vbCrLf + "            AND GA.PROJECT_ID = A.PROJECT_ID "
                SQL = SQL + vbCrLf + "            AND GA.STATE_ID = " & Q(StatusId)
                SQL = SQL + vbCrLf + "            AND GA.ACTION_ID = A.ACTION_ID) AS GROUP_SETTING "
            Else
                SQL = SQL + vbCrLf + "        CAST(NULL AS VARCHAR2(4000)) AS VIA_ROLES, "
                SQL = SQL + vbCrLf + "        CAST(NULL AS VARCHAR2(1)) AS GROUP_SETTING "
            End If
            SQL = SQL + vbCrLf + " FROM   UNITSHUB_ACTIONS A "
            SQL = SQL + vbCrLf + " WHERE  A.PROJECT_ID = " & Q(ProjectId)
            SQL = SQL + vbCrLf + "   AND  EXISTS (SELECT 1 FROM UNITSHUB_ACTION_PLACEMENTS P "
            SQL = SQL + vbCrLf + "                WHERE P.PROJECT_ID = A.PROJECT_ID AND P.ACTION_ID = A.ACTION_ID "
            SQL = SQL + vbCrLf + "                  AND " & place & ") "
            SQL = SQL + vbCrLf + " ORDER BY A.ACTION_TITLE "

            Dim DT As DataTable = GetDataTable(EBDB, SQL)
            If DT Is Nothing OrElse DT.Rows.Count = 0 Then
                ShowNote(lblActionsNote, "No actions are placed here.", "info")
                Exit Sub
            End If

            ' What the roles give, as HTML for the middle column
            DT.Columns.Add("ROLE_HTML", GetType(String))
            For Each r As DataRow In DT.Rows
                Dim viaRoles As String = Convert.ToString(r("VIA_ROLES")).Trim()
                If viaRoles = "" Then
                    r("ROLE_HTML") = "<span class=""role-no"">Not given</span>"
                Else
                    r("ROLE_HTML") = "<span class=""role-yes"">Allowed by " & Server.HtmlEncode(viaRoles) & "</span>"
                End If
            Next

            rptActions.DataSource = DT
            rptActions.DataBind()
            pnlActions.Visible = True
            btnSaveActions.Enabled = (GroupId <> "")

            If GroupId = "" Then
                ShowNote(lblActionsNote, "Choose a group above to allow or deny actions for it.", "info")
            End If
        Catch ex As Exception
            ShowNote(lblActionsNote, "Actions can't be shown: " & ex.Message, "err")
        End Try
    End Sub

    ''' <summary>Puts each row's switch on its saved setting; locked when no group is chosen.</summary>
    Protected Sub rptActions_ItemDataBound(sender As Object, e As RepeaterItemEventArgs) Handles rptActions.ItemDataBound
        If e.Item.ItemType <> ListItemType.Item AndAlso e.Item.ItemType <> ListItemType.AlternatingItem Then Exit Sub

        Dim row As DataRowView = CType(e.Item.DataItem, DataRowView)
        Dim rbl As RadioButtonList = CType(e.Item.FindControl("rblSetting"), RadioButtonList)
        Dim setting As String = Convert.ToString(row("GROUP_SETTING")).Trim().ToUpperInvariant()

        rbl.SelectedValue = If(setting = "A" OrElse setting = "D", setting, "R")
        rbl.Enabled = (GroupId <> "")
    End Sub

    ''' <summary>
    ''' Saves the switches for the group at the selected project and status: for every
    ''' action listed, its UNITSHUB_SEC_GROUPS_ACTIONS row is removed, then added back with
    ''' ALLOW_DENY 'A' or 'D' unless the switch is on Inherit.
    ''' Actions not listed (e.g. placed only on another sub-status) are left as they are.
    ''' </summary>
    Protected Sub btnSaveActions_Click(sender As Object, e As EventArgs) Handles btnSaveActions.Click
        If GroupId = "" OrElse ProjectId = "" OrElse StatusId = "" Then Exit Sub
        If rptActions.Items.Count = 0 Then Exit Sub

        ' Read the switches before BindActions rebuilds the rows
        Dim listed As New List(Of String)
        Dim settings As New Dictionary(Of String, String)
        For Each item As RepeaterItem In rptActions.Items
            Dim actionId As String = CType(item.FindControl("hfActionId"), HiddenField).Value.Trim()
            Dim choice As String = CType(item.FindControl("rblSetting"), RadioButtonList).SelectedValue
            If actionId = "" OrElse listed.Contains(actionId) Then Continue For
            listed.Add(actionId)
            If choice = "A" OrElse choice = "D" Then settings(actionId) = choice
        Next
        If listed.Count = 0 Then Exit Sub

        Try
            Dim inList As String = String.Join(", ", listed.ConvertAll(Function(x) Q(x)))
            DB.ExecuteNonQuery(EBDB_CS,
                "DELETE FROM UNITSHUB_SEC_GROUPS_ACTIONS WHERE GROUP_ID = " & Q(GroupId) &
                " AND PROJECT_ID = " & Q(ProjectId) & " AND STATE_ID = " & Q(StatusId) &
                " AND ACTION_ID IN (" & inList & ")")

            Dim allowed As Integer = 0
            Dim denied As Integer = 0
            For Each kv As KeyValuePair(Of String, String) In settings
                DB.ExecuteNonQuery(EBDB_CS,
                    "INSERT INTO UNITSHUB_SEC_GROUPS_ACTIONS (GROUP_ID, PROJECT_ID, STATE_ID, ACTION_ID, ALLOW_DENY) VALUES (" &
                    Q(GroupId) & ", " & Q(ProjectId) & ", " & Q(StatusId) & ", " & Q(kv.Key) & ", " & Q(kv.Value) & ")")
                If kv.Value = "A" Then allowed += 1 Else denied += 1
            Next

            BindActions()
            ShowNote(lblActionsNote, "Saved: " & allowed & " allowed, " & denied & " denied, " &
                     (listed.Count - allowed - denied) & " inherited from roles.", "ok")
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

    ' ==================================================================
    ' Tabs: Members | Actions
    ' ==================================================================

    Private Const MembersTabIndex As Integer = 0
    Private Const RolesTabIndex As Integer = 1
    Private Const ActionsTabIndex As Integer = 2

    Protected Sub menuTabs_MenuItemClick(sender As Object, e As MenuEventArgs) Handles menuTabs.MenuItemClick
        ShowTab(menuTabs.Items.IndexOf(e.Item))
    End Sub

    ''' <summary>Shows a tab and highlights its menu item.</summary>
    Private Sub ShowTab(Index As Integer)
        If Index < 0 OrElse Index >= mvTabs.Views.Count Then Exit Sub
        mvTabs.ActiveViewIndex = Index
        menuTabs.Items(Index).Selected = True
    End Sub

    ''' <summary>Tab captions with counts, e.g. "Members (5)".</summary>
    Private Sub SetTabCaption(Index As Integer, Caption As String, Count As Integer)
        menuTabs.Items(Index).Text = If(Count >= 0, Caption & " (" & Count & ")", Caption)
    End Sub

    ''' <summary>Closes the popup.</summary>
    Protected Sub imgClose_Click(sender As Object, e As ImageClickEventArgs) Handles imgClose.Click
        VendorPopupHelper.RegisterPopupSelectionAndClose(Me, False, skipPostBack:=False)
    End Sub
End Class
