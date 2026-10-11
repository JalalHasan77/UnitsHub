Imports System.Data
Imports System.Collections.Generic

''' <summary>
''' Security setup page. Above the tabs: Project -> Status -> Sub-status -> Action
''' dropdowns, each filled from the one before it. Below: one tab control with three
''' tabs, Roles | Groups | Users - each lists everything with what is allowed the selected action ticked.
'''
''' Opened with ?ProjectID=&StatusID=&SUBSTATEID=&ActionID=, those values are kept in
''' lblProjectID / lblStatusID / lblSUBSTATEID / lblActionID and pre-selected in the
''' dropdowns. Afterwards the labels always hold what is selected in the dropdowns.
''' </summary>
Partial Class Actions_Roles_Groups_Users
    Inherits System.Web.UI.Page

    Private Const RolesTabIndex As Integer = 0
    Private Const GroupsTabIndex As Integer = 1
    Private Const UsersTabIndex As Integer = 2

    ''' <summary>
    ''' First load: keeps the parameters the page was opened with in their labels, then
    ''' fills the four dropdowns, pre-selecting those values where they exist. A dropdown
    ''' whose value came with the request is read-only (see RememberFixedValues).
    ''' </summary>
    Protected Sub Page_Load(sender As Object, e As EventArgs) Handles Me.Load
        If Not IsPostBack Then
            lblProjectID.Text = QueryValue("ProjectID")
            lblStatusID.Text = QueryValue("StatusID")
            lblSUBSTATEID.Text = QueryValue("SUBSTATEID")
            lblActionID.Text = QueryValue("ActionID")

            LoadProjects(lblProjectID.Text)
            LoadStatuses(lblStatusID.Text)
            LoadSubStatuses(lblSUBSTATEID.Text)
            LoadActions(lblActionID.Text)
            RememberFixedValues()   ' values that came with the request make their dropdowns read-only
            SyncLabels()
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
    ' Project -> Status -> Sub-status -> Action
    ' ==================================================================

    ''' <summary>Projects (UNITSHUB_PROJECTS), by English name.</summary>
    Private Sub LoadProjects(Preselect As String)
        ddlProject.Items.Clear()
        ddlProject.Items.Add(New ListItem("-- Select a project --", ""))
        Dim DT As DataTable = GetDataTable(EBDB,
            "SELECT PROJECT_ID, PROJECT_NAME_EN FROM UNITSHUB_PROJECTS ORDER BY PROJECT_NAME_EN")
        AddItems(ddlProject, DT, "PROJECT_ID", "PROJECT_NAME_EN")
        SelectIfPresent(ddlProject, FixedOr("Project", Preselect))
    End Sub

    ''' <summary>The selected project's statuses (UNITSHUB_PROJECTSTATUS), in STATE_ID order.</summary>
    Private Sub LoadStatuses(Preselect As String)
        ddlStatus.Items.Clear()
        ddlStatus.Items.Add(New ListItem("-- Select a status --", ""))
        Dim projectId As String = ddlProject.SelectedValue
        ddlStatus.Enabled = (projectId <> "")
        If projectId = "" Then Exit Sub

        Dim DT As DataTable = GetDataTable(EBDB,
            "SELECT STATE_ID, STATUS FROM UNITSHUB_PROJECTSTATUS WHERE PROJECT_ID = " & Q(projectId) &
            " ORDER BY TO_NUMBER(STATE_ID)")
        AddItems(ddlStatus, DT, "STATE_ID", "STATUS")
        SelectIfPresent(ddlStatus, FixedOr("Status", Preselect))
    End Sub

    ''' <summary>
    ''' The selected status's sub-statuses (UNITSHUB_PROJECTSUBSTATUS), in SORT_ORDER.
    ''' The whole field is hidden when the status has none.
    ''' </summary>
    Private Sub LoadSubStatuses(Preselect As String)
        ddlSubStatus.Items.Clear()
        ddlSubStatus.Items.Add(New ListItem("-- Whole status --", ""))
        Dim projectId As String = ddlProject.SelectedValue
        Dim statusId As String = ddlStatus.SelectedValue
        pnlSubStatus.Visible = False
        If projectId = "" OrElse statusId = "" Then Exit Sub

        Dim DT As DataTable = GetDataTable(EBDB,
            "SELECT SUBSTATE_ID, SUBTITLE FROM UNITSHUB_PROJECTSUBSTATUS WHERE PROJECT_ID = " & Q(projectId) &
            " AND STATE_ID = " & Q(statusId) & " ORDER BY SORT_ORDER, SUBSTATE_ID")
        If DT Is Nothing OrElse DT.Rows.Count = 0 Then Exit Sub   ' no sub-statuses: field stays hidden

        AddItems(ddlSubStatus, DT, "SUBSTATE_ID", "SUBTITLE")
        SelectIfPresent(ddlSubStatus, FixedOr("SubStatus", Preselect))
        pnlSubStatus.Visible = True
    End Sub

    ''' <summary>
    ''' Actions available at the selected place (UNITSHUB_ACTION_PLACEMENTS joined to
    ''' UNITSHUB_ACTIONS), the same rule ProjectStatusAndAction uses:
    '''   placed on all statuses (STATUS_ID '*'), or
    '''   placed on this status as a whole (SUBSTATE_ID '*'), or
    '''   placed on the selected sub-status;
    ''' with no sub-status selected, actions placed on any of this status's sub-statuses too.
    ''' </summary>
    Private Sub LoadActions(Preselect As String)
        ddlAction.Items.Clear()
        ddlAction.Items.Add(New ListItem("-- Select an action --", ""))
        lblActionHint.Visible = False
        Dim projectId As String = ddlProject.SelectedValue
        Dim statusId As String = ddlStatus.SelectedValue
        Dim subId As String = If(pnlSubStatus.Visible, ddlSubStatus.SelectedValue, "")
        ddlAction.Enabled = (projectId <> "" AndAlso statusId <> "")
        If Not ddlAction.Enabled Then Exit Sub

        Dim place As String
        If subId = "" Then
            place = "(P.STATUS_ID = '*' OR P.STATUS_ID = " & Q(statusId) & ")"
        Else
            place = "(P.STATUS_ID = '*' OR (P.STATUS_ID = " & Q(statusId) & " AND P.SUBSTATE_ID IN (" & Q(subId) & ", '*')))"
        End If

        Dim DT As DataTable = GetDataTable(EBDB,
            "SELECT DISTINCT A.ACTION_ID, A.ACTION_TITLE " &
            "FROM UNITSHUB_ACTION_PLACEMENTS P " &
            "JOIN UNITSHUB_ACTIONS A ON A.PROJECT_ID = P.PROJECT_ID AND A.ACTION_ID = P.ACTION_ID " &
            "WHERE P.PROJECT_ID = " & Q(projectId) & " AND " & place & " " &
            "ORDER BY A.ACTION_TITLE")
        AddItems(ddlAction, DT, "ACTION_ID", "ACTION_TITLE")
        SelectIfPresent(ddlAction, FixedOr("Action", Preselect))

        If DT Is Nothing OrElse DT.Rows.Count = 0 Then
            lblActionHint.Text = "No actions are placed here."
            lblActionHint.Visible = True
        End If
    End Sub

    ' ==================================================================
    ' Values that came with the request are fixed
    '   A dropdown whose value was passed in (?ProjectID= / StatusID= / SUBSTATEID= /
    '   ActionID=) and found in its list is read-only; the others stay changeable.
    '   The fixed values are kept in ViewState, so they survive postbacks and are
    '   re-selected whenever their list is reloaded.
    ' ==================================================================

    ''' <summary>The value fixed by the request for a dropdown ("Project", "Status", "SubStatus", "Action"); "" if not fixed.</summary>
    Private Function FixedValue(Name As String) As String
        Return Convert.ToString(ViewState("Fixed_" & Name))
    End Function

    ''' <summary>The fixed value when there is one, otherwise Preselect.</summary>
    Private Function FixedOr(Name As String, Preselect As String) As String
        Dim v As String = FixedValue(Name)
        Return If(v <> "", v, Preselect)
    End Function

    ''' <summary>
    ''' First load only: a dropdown is fixed when its parameter was sent and that value
    ''' could be selected (a value not in the list leaves the dropdown changeable).
    ''' </summary>
    Private Sub RememberFixedValues()
        FixIfSelected("Project", "ProjectID", ddlProject)
        FixIfSelected("Status", "StatusID", ddlStatus)
        If pnlSubStatus.Visible Then FixIfSelected("SubStatus", "SUBSTATEID", ddlSubStatus)
        FixIfSelected("Action", "ActionID", ddlAction)
    End Sub

    Private Sub FixIfSelected(Name As String, Parameter As String, ddl As DropDownList)
        Dim v As String = QueryValue(Parameter)
        If v <> "" AndAlso ddl.SelectedValue = v Then ViewState("Fixed_" & Name) = v
    End Sub

    ''' <summary>
    ''' Every request, after any list was reloaded: fixed dropdowns are locked (read-only),
    ''' the rest keep the Enabled state their Load* routine gave them.
    ''' </summary>
    Protected Sub Page_PreRender(sender As Object, e As EventArgs) Handles Me.PreRender
        LockIfFixed("Project", ddlProject)
        LockIfFixed("Status", ddlStatus)
        LockIfFixed("SubStatus", ddlSubStatus)
        LockIfFixed("Action", ddlAction)
    End Sub

    Private Sub LockIfFixed(Name As String, ddl As DropDownList)
        If FixedValue(Name) = "" Then Exit Sub
        ddl.Enabled = False
        ddl.ToolTip = "Set by the page that opened this form"
    End Sub

    Protected Sub ddlProject_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ddlProject.SelectedIndexChanged
        LoadStatuses("")
        LoadSubStatuses("")
        LoadActions("")
        SyncLabels()
    End Sub

    Protected Sub ddlStatus_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ddlStatus.SelectedIndexChanged
        LoadSubStatuses("")
        LoadActions("")
        SyncLabels()
    End Sub

    Protected Sub ddlSubStatus_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ddlSubStatus.SelectedIndexChanged
        ' Keep the chosen action if it is still available at the new place
        LoadActions(ddlAction.SelectedValue)
        SyncLabels()
    End Sub

    Protected Sub ddlAction_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ddlAction.SelectedIndexChanged
        SyncLabels()
    End Sub

    ''' <summary>
    ''' The four labels follow the dropdowns ("" for nothing selected), and the Roles,
    ''' Groups and Users tabs are refreshed for the new selection.
    ''' </summary>
    Private Sub SyncLabels()
        lblProjectID.Text = ddlProject.SelectedValue
        lblStatusID.Text = ddlStatus.SelectedValue
        lblSUBSTATEID.Text = If(pnlSubStatus.Visible, ddlSubStatus.SelectedValue, "")
        lblActionID.Text = ddlAction.SelectedValue
        BindRoles()
        BindGroups()
        BindUsers()
    End Sub

    ' ==================================================================
    ' Roles tab
    '   Every role of UNITSHUB_SEC_ROLES is listed; a role is ticked when
    '   UNITSHUB_SEC_ROLES_ACTIONS links it to the selected project, status and action.
    '   (That table has no sub-status column, so the link is per status.)
    ' ==================================================================

    ''' <summary>True when project, status and action are all selected.</summary>
    Private ReadOnly Property HasActionSelected As Boolean
        Get
            Return lblProjectID.Text <> "" AndAlso lblStatusID.Text <> "" AndAlso lblActionID.Text <> ""
        End Get
    End Property

    ''' <summary>"WHERE" part for the selected project / status / action.</summary>
    Private Function SelectedActionFilter(Alias_ As String) As String
        Return Alias_ & ".PROJECT_ID = " & Q(lblProjectID.Text) &
               " AND " & Alias_ & ".STATE_ID = " & Q(lblStatusID.Text) &
               " AND " & Alias_ & ".ACTION_ID = " & Q(lblActionID.Text)
    End Function

    ''' <summary>
    ''' Lists all roles (name; description as the tooltip) and ticks the ones linked to the
    ''' selected action. Until an action is selected, the list is shown but locked.
    ''' </summary>
    Private Sub BindRoles()
        cblRoles.Items.Clear()
        lblRolesNote.Visible = False
        litRolesAction.Text = If(HasActionSelected,
            Server.HtmlEncode(ddlAction.SelectedItem.Text & " - " & ddlStatus.SelectedItem.Text &
                              If(lblSUBSTATEID.Text <> "", " / " & ddlSubStatus.SelectedItem.Text, "")),
            "<em>no action selected</em>")

        Try
            Dim DT As DataTable = GetDataTable(EBDB,
                "SELECT R.ROLE_ID, R.ROLE_NAME, R.DESCRIPTION, " &
                If(HasActionSelected,
                   "CASE WHEN EXISTS (SELECT 1 FROM UNITSHUB_SEC_ROLES_ACTIONS RA WHERE RA.ROLE_ID = R.ROLE_ID AND " &
                   SelectedActionFilter("RA") & ") THEN 'Y' ELSE 'N' END",
                   "'N'") & " AS IS_LINKED " &
                "FROM UNITSHUB_SEC_ROLES R ORDER BY R.ROLE_NAME")

            If DT IsNot Nothing Then
                For Each r As DataRow In DT.Rows
                    Dim item As New ListItem(Convert.ToString(r("ROLE_NAME")).Trim(), Convert.ToString(r("ROLE_ID")).Trim())
                    item.Selected = (Convert.ToString(r("IS_LINKED")) = "Y")
                    Dim description As String = Convert.ToString(r("DESCRIPTION")).Trim()
                    If description <> "" Then item.Attributes("title") = description
                    cblRoles.Items.Add(item)
                Next
            End If

            cblRoles.Enabled = HasActionSelected
            btnSaveRoles.Enabled = HasActionSelected AndAlso cblRoles.Items.Count > 0

            If cblRoles.Items.Count = 0 Then
                ShowRolesNote("No roles are defined yet.", "info")
            ElseIf Not HasActionSelected Then
                ShowRolesNote("Select a project, status and action above to see and change which roles may perform it.", "info")
            End If
        Catch ex As Exception
            cblRoles.Enabled = False
            btnSaveRoles.Enabled = False
            ShowRolesNote("Roles can't be shown: " & ex.Message, "err")
        End Try
    End Sub

    ''' <summary>
    ''' Saves the ticks: the selected action's links in UNITSHUB_SEC_ROLES_ACTIONS are
    ''' replaced by one row per ticked role.
    ''' </summary>
    Protected Sub btnSaveRoles_Click(sender As Object, e As EventArgs) Handles btnSaveRoles.Click
        If Not HasActionSelected Then Exit Sub

        ' Read the ticks before BindRoles rebuilds the list
        Dim ticked As New List(Of String)
        For Each item As ListItem In cblRoles.Items
            If item.Selected Then ticked.Add(item.Value)
        Next

        Try
            DB.ExecuteNonQuery(EBDB_CS,
                "DELETE FROM UNITSHUB_SEC_ROLES_ACTIONS RA WHERE " & SelectedActionFilter("RA"))

            For Each roleId As String In ticked
                DB.ExecuteNonQuery(EBDB_CS,
                    "INSERT INTO UNITSHUB_SEC_ROLES_ACTIONS (ROLE_ID, PROJECT_ID, STATE_ID, ACTION_ID) VALUES (" &
                    Q(roleId) & ", " & Q(lblProjectID.Text) & ", " & Q(lblStatusID.Text) & ", " & Q(lblActionID.Text) & ")")
            Next

            BindRoles()
            BindGroups()   ' groups included via a role change with the roles
            BindUsers()    ' ...and so do the users included via those groups
            ShowRolesNote(ticked.Count & If(ticked.Count = 1, " role", " roles") & " allowed for this action.", "ok")
        Catch ex As Exception
            ShowRolesNote("The roles couldn't be saved: " & ex.Message, "err")
        End Try

        ShowTab(RolesTabIndex)
    End Sub

    Private Sub ShowRolesNote(Text As String, Kind As String)
        lblRolesNote.Text = Server.HtmlEncode(Text)
        lblRolesNote.CssClass = "roles-note " & Kind
        lblRolesNote.Visible = True
    End Sub

    ' ==================================================================
    ' Groups tab
    '   Every group of UNITSHUB_SEC_GROUPS is listed. For the selected project, status
    '   and action a group is:
    '     EXCLUDED  when UNITSHUB_SEC_GROUPS_ACTIONS denies it for this group (a DENY
    '               wins, even over a role);
    '     INCLUDED  when UNITSHUB_SEC_GROUPS_ACTIONS allows it for this group, or when
    '               one of the group's roles (UNITSHUB_SEC_GROUPS_ROLES) is allowed it
    '               in UNITSHUB_SEC_ROLES_ACTIONS;
    '     not included otherwise.
    '   Ticked = included. The reason is written under each group.
    '   (UNITSHUB_SEC_GROUPS_ACTIONS has no sub-status column, so it is per status.)
    ' ==================================================================

    ''' <summary>True for an ALLOW_DENY value meaning DENY ("DENY", "D", "N", "0"); anything else is ALLOW.</summary>
    Private Function IsDeny(Value As String) As Boolean
        Dim v As String = If(Value, "").Trim().ToUpperInvariant()
        Return v.StartsWith("D") OrElse v = "N" OrElse v = "0"
    End Function

    ''' <summary>
    ''' Lists all groups (name, a System tag, description as the tooltip) and ticks the
    ''' included ones, with the reason under each. Locked until an action is selected.
    ''' </summary>
    Private Sub BindGroups()
        cblGroups.Items.Clear()
        lblGroupsNote.Visible = False
        litGroupsAction.Text = If(HasActionSelected,
            Server.HtmlEncode(ddlAction.SelectedItem.Text & " - " & ddlStatus.SelectedItem.Text &
                              If(lblSUBSTATEID.Text <> "", " / " & ddlSubStatus.SelectedItem.Text, "")),
            "<em>no action selected</em>")

        Try
            Dim SQL As String = ""
            SQL = SQL + vbCrLf + " SELECT G.GROUP_ID, G.GROUP_NAME, G.DESCRIPTION, G.IS_SYSTEM "
            If HasActionSelected Then
                ' Roles of this group that are allowed the action, as one text
                SQL = SQL + vbCrLf + "      , (SELECT LISTAGG(R.ROLE_NAME, ', ') WITHIN GROUP (ORDER BY R.ROLE_NAME) "
                SQL = SQL + vbCrLf + "         FROM   UNITSHUB_SEC_GROUPS_ROLES GR "
                SQL = SQL + vbCrLf + "         JOIN   UNITSHUB_SEC_ROLES R ON R.ROLE_ID = GR.ROLE_ID "
                SQL = SQL + vbCrLf + "         WHERE  GR.GROUP_ID = G.GROUP_ID "
                SQL = SQL + vbCrLf + "           AND  EXISTS (SELECT 1 FROM UNITSHUB_SEC_ROLES_ACTIONS RA "
                SQL = SQL + vbCrLf + "                        WHERE RA.ROLE_ID = GR.ROLE_ID AND " & SelectedActionFilter("RA") & ") "
                SQL = SQL + vbCrLf + "        ) AS VIA_ROLES "
                ' The group's own ALLOW / DENY for the action (a DENY wins if both exist)
                SQL = SQL + vbCrLf + "      , (SELECT CASE WHEN MAX(CASE WHEN SUBSTR(UPPER(TRIM(GA.ALLOW_DENY)), 1, 1) IN ('D', 'N', '0') THEN 1 ELSE 0 END) = 1 "
                SQL = SQL + vbCrLf + "                     THEN 'DENY' WHEN COUNT(*) > 0 THEN 'ALLOW' END "
                SQL = SQL + vbCrLf + "         FROM   UNITSHUB_SEC_GROUPS_ACTIONS GA "
                SQL = SQL + vbCrLf + "         WHERE  GA.GROUP_ID = G.GROUP_ID AND " & SelectedActionFilter("GA")
                SQL = SQL + vbCrLf + "        ) AS GROUP_SETTING "
            Else
                SQL = SQL + vbCrLf + "      , CAST(NULL AS VARCHAR2(4000)) AS VIA_ROLES, CAST(NULL AS VARCHAR2(5)) AS GROUP_SETTING "
            End If
            SQL = SQL + vbCrLf + " FROM   UNITSHUB_SEC_GROUPS G "
            SQL = SQL + vbCrLf + " ORDER BY G.GROUP_NAME "

            Dim DT As DataTable = GetDataTable(EBDB, SQL)
            If DT IsNot Nothing Then
                For Each r As DataRow In DT.Rows
                    Dim viaRoles As String = Convert.ToString(r("VIA_ROLES")).Trim()
                    Dim setting As String = Convert.ToString(r("GROUP_SETTING")).Trim().ToUpperInvariant()
                    Dim isSystem As Boolean = (Convert.ToString(r("IS_SYSTEM")).Trim().ToUpperInvariant() = "Y" OrElse
                                               Convert.ToString(r("IS_SYSTEM")).Trim() = "1")

                    Dim included As Boolean
                    Dim why As String
                    Dim whyClass As String

                    If Not HasActionSelected Then
                        included = False
                        why = ""
                        whyClass = "none"
                    ElseIf setting = "DENY" Then
                        included = False
                        why = "Excluded - denied for this group" &
                              If(viaRoles <> "", " (overrides role: " & viaRoles & ")", "")
                        whyClass = "exc"
                    ElseIf setting = "ALLOW" Then
                        included = True
                        why = "Included - allowed for this group" &
                              If(viaRoles <> "", " (also via role: " & viaRoles & ")", "")
                        whyClass = "inc"
                    ElseIf viaRoles <> "" Then
                        included = True
                        why = "Included - via role: " & viaRoles
                        whyClass = "inc"
                    Else
                        included = False
                        why = "Not included - no role or group setting allows it"
                        whyClass = "none"
                    End If

                    ' CheckBoxList writes item text as HTML, so every value is encoded here
                    Dim text As String = "<span class=""g-name"">" & Server.HtmlEncode(Convert.ToString(r("GROUP_NAME")).Trim()) & "</span>" &
                                         If(isSystem, "<span class=""g-tag"">System</span>", "") &
                                         If(why <> "", "<span class=""g-why " & whyClass & """>" & Server.HtmlEncode(why) & "</span>", "")

                    Dim item As New ListItem(text, Convert.ToString(r("GROUP_ID")).Trim())
                    item.Selected = included
                    Dim description As String = Convert.ToString(r("DESCRIPTION")).Trim()
                    If description <> "" Then item.Attributes("title") = description
                    cblGroups.Items.Add(item)
                Next
            End If

            cblGroups.Enabled = HasActionSelected
            btnSaveGroups.Enabled = HasActionSelected AndAlso cblGroups.Items.Count > 0

            If cblGroups.Items.Count = 0 Then
                ShowGroupsNote("No groups are defined yet.", "info")
            ElseIf Not HasActionSelected Then
                ShowGroupsNote("Select a project, status and action above to see which groups may perform it.", "info")
            End If
        Catch ex As Exception
            cblGroups.Enabled = False
            btnSaveGroups.Enabled = False
            ShowGroupsNote("Groups can't be shown: " & ex.Message, "err")
        End Try
    End Sub

    ''' <summary>
    ''' Group IDs included through a role for the selected action (one of the group's roles
    ''' is allowed it in UNITSHUB_SEC_ROLES_ACTIONS).
    ''' </summary>
    Private Function GroupsIncludedViaRole() As HashSet(Of String)
        Dim result As New HashSet(Of String)
        Dim DT As DataTable = GetDataTable(EBDB,
            "SELECT DISTINCT GR.GROUP_ID FROM UNITSHUB_SEC_GROUPS_ROLES GR " &
            "WHERE EXISTS (SELECT 1 FROM UNITSHUB_SEC_ROLES_ACTIONS RA " &
            "WHERE RA.ROLE_ID = GR.ROLE_ID AND " & SelectedActionFilter("RA") & ")")
        If DT IsNot Nothing Then
            For Each r As DataRow In DT.Rows
                result.Add(Convert.ToString(r("GROUP_ID")).Trim())
            Next
        End If
        Return result
    End Function

    ''' <summary>
    ''' Saves the ticks as group settings in UNITSHUB_SEC_GROUPS_ACTIONS, only where they
    ''' differ from what the group's roles give:
    '''   ticked,   not via a role -> ALLOW_DENY = 'A' (allow) for this group
    '''   unticked, via a role     -> ALLOW_DENY = 'D' (deny) for this group
    '''   same as the roles give   -> no group setting (removed if there was one)
    ''' </summary>
    Protected Sub btnSaveGroups_Click(sender As Object, e As EventArgs) Handles btnSaveGroups.Click
        If Not HasActionSelected Then Exit Sub

        ' Read the ticks before BindGroups rebuilds the list
        Dim ticks As New Dictionary(Of String, Boolean)
        For Each item As ListItem In cblGroups.Items
            ticks(item.Value) = item.Selected
        Next

        Dim allows As Integer = 0, denies As Integer = 0
        Try
            Dim viaRole As HashSet(Of String) = GroupsIncludedViaRole()

            DB.ExecuteNonQuery(EBDB_CS,
                "DELETE FROM UNITSHUB_SEC_GROUPS_ACTIONS GA WHERE " & SelectedActionFilter("GA"))

            For Each kv As KeyValuePair(Of String, Boolean) In ticks
                Dim fromRole As Boolean = viaRole.Contains(kv.Key)
                If kv.Value = fromRole Then Continue For   ' the roles already give this

                Dim setting As String = If(kv.Value, "A", "D")   ' ALLOW_DENY: A = allow, D = deny
                If kv.Value Then allows += 1 Else denies += 1

                DB.ExecuteNonQuery(EBDB_CS,
                    "INSERT INTO UNITSHUB_SEC_GROUPS_ACTIONS (GROUP_ID, PROJECT_ID, STATE_ID, ACTION_ID, ALLOW_DENY) VALUES (" &
                    Q(kv.Key) & ", " & Q(lblProjectID.Text) & ", " & Q(lblStatusID.Text) & ", " & Q(lblActionID.Text) & ", " & Q(setting) & ")")
            Next

            BindGroups()
            BindUsers()    ' users included via a group change with the groups
            ShowGroupsNote("Saved: " & allows & " group " & If(allows = 1, "ALLOW", "ALLOWs") & ", " &
                           denies & " group " & If(denies = 1, "DENY", "DENYs") & " (the rest follow their roles).", "ok")
        Catch ex As Exception
            ShowGroupsNote("The groups couldn't be saved: " & ex.Message, "err")
        End Try

        ShowTab(GroupsTabIndex)
    End Sub

    Private Sub ShowGroupsNote(Text As String, Kind As String)
        lblGroupsNote.Text = Server.HtmlEncode(Text)
        lblGroupsNote.CssClass = "roles-note " & Kind
        lblGroupsNote.Visible = True
    End Sub

    ' ==================================================================
    ' Users tab
    '   Every user of UNITSHUB_USERS is listed. For the selected project, status and
    '   action a user is:
    '     EXCLUDED  when UNITSHUB_SEC_USER_ACTIONS denies it for this user ('D'; a DENY
    '               wins, even over the user's groups);
    '     INCLUDED  when UNITSHUB_SEC_USER_ACTIONS allows it for this user ('A'), or when
    '               one of the user's groups (UNITSHUB_SEC_USER_GROUPS) is included -
    '               by the same rule as the Groups tab (group ALLOW, or one of its
    '               roles, unless the group is denied);
    '     not included otherwise.
    '   Ticked = included. The reason is written under each user.
    '   (UNITSHUB_SEC_USER_ACTIONS has no sub-status column, so it is per status.)
    ' ==================================================================

    ''' <summary>
    ''' SQL condition: the group GroupIdExpr is included for the selected action - it is not
    ''' denied in UNITSHUB_SEC_GROUPS_ACTIONS, and it is either allowed there or has a role
    ''' that is allowed the action in UNITSHUB_SEC_ROLES_ACTIONS.
    ''' </summary>
    Private Function GroupIncludedSql(GroupIdExpr As String) As String
        Return "NOT EXISTS (SELECT 1 FROM UNITSHUB_SEC_GROUPS_ACTIONS GXD WHERE GXD.GROUP_ID = " & GroupIdExpr &
               " AND " & SelectedActionFilter("GXD") &
               " AND SUBSTR(UPPER(TRIM(GXD.ALLOW_DENY)), 1, 1) IN ('D', 'N', '0')) " &
               "AND (EXISTS (SELECT 1 FROM UNITSHUB_SEC_GROUPS_ACTIONS GXA WHERE GXA.GROUP_ID = " & GroupIdExpr &
               " AND " & SelectedActionFilter("GXA") & ") " &
               "OR EXISTS (SELECT 1 FROM UNITSHUB_SEC_GROUPS_ROLES GXR " &
               "JOIN UNITSHUB_SEC_ROLES_ACTIONS RXA ON RXA.ROLE_ID = GXR.ROLE_ID AND " & SelectedActionFilter("RXA") &
               " WHERE GXR.GROUP_ID = " & GroupIdExpr & "))"
    End Function

    ''' <summary>
    ''' Lists all users (name and ID, tags for disabled accounts, e-mail as the tooltip)
    ''' and ticks the included ones, with the reason under each. Locked until an action
    ''' is selected.
    ''' </summary>
    Private Sub BindUsers()
        cblUsers.Items.Clear()
        lblUsersNote.Visible = False
        litUsersAction.Text = If(HasActionSelected,
            Server.HtmlEncode(ddlAction.SelectedItem.Text & " - " & ddlStatus.SelectedItem.Text &
                              If(lblSUBSTATEID.Text <> "", " / " & ddlSubStatus.SelectedItem.Text, "")),
            "<em>no action selected</em>")

        Try
            Dim SQL As String = ""
            SQL = SQL + vbCrLf + " SELECT U.USER_ID, U.FULL_NAME, U.EMAIL, U.IS_ENABLED "
            If HasActionSelected Then
                ' The user's groups that are included for the action, as one text
                SQL = SQL + vbCrLf + "      , (SELECT LISTAGG(G.GROUP_NAME, ', ') WITHIN GROUP (ORDER BY G.GROUP_NAME) "
                SQL = SQL + vbCrLf + "         FROM   UNITSHUB_SEC_USER_GROUPS UG "
                SQL = SQL + vbCrLf + "         JOIN   UNITSHUB_SEC_GROUPS G ON G.GROUP_ID = UG.GROUP_ID "
                SQL = SQL + vbCrLf + "         WHERE  UG.USER_ID = U.USER_ID AND " & GroupIncludedSql("UG.GROUP_ID")
                SQL = SQL + vbCrLf + "        ) AS VIA_GROUPS "
                ' The user's own ALLOW / DENY for the action (a DENY wins if both exist)
                SQL = SQL + vbCrLf + "      , (SELECT CASE WHEN MAX(CASE WHEN SUBSTR(UPPER(TRIM(UA.ALLOW_DENY)), 1, 1) IN ('D', 'N', '0') THEN 1 ELSE 0 END) = 1 "
                SQL = SQL + vbCrLf + "                     THEN 'DENY' WHEN COUNT(*) > 0 THEN 'ALLOW' END "
                SQL = SQL + vbCrLf + "         FROM   UNITSHUB_SEC_USER_ACTIONS UA "
                SQL = SQL + vbCrLf + "         WHERE  UA.USER_ID = U.USER_ID AND " & SelectedActionFilter("UA")
                SQL = SQL + vbCrLf + "        ) AS USER_SETTING "
            Else
                SQL = SQL + vbCrLf + "      , CAST(NULL AS VARCHAR2(4000)) AS VIA_GROUPS, CAST(NULL AS VARCHAR2(5)) AS USER_SETTING "
            End If
            SQL = SQL + vbCrLf + " FROM   UNITSHUB_USERS U "
            SQL = SQL + vbCrLf + " ORDER BY U.FULL_NAME, U.USER_ID "

            Dim DT As DataTable = GetDataTable(EBDB, SQL)
            If DT IsNot Nothing Then
                For Each r As DataRow In DT.Rows
                    Dim userId As String = Convert.ToString(r("USER_ID")).Trim()
                    Dim fullName As String = Convert.ToString(r("FULL_NAME")).Trim()
                    Dim viaGroups As String = Convert.ToString(r("VIA_GROUPS")).Trim()
                    Dim setting As String = Convert.ToString(r("USER_SETTING")).Trim().ToUpperInvariant()
                    Dim enabledValue As String = Convert.ToString(r("IS_ENABLED")).Trim().ToUpperInvariant()
                    Dim isDisabled As Boolean = (enabledValue = "N" OrElse enabledValue = "0")

                    Dim included As Boolean
                    Dim why As String
                    Dim whyClass As String

                    If Not HasActionSelected Then
                        included = False
                        why = ""
                        whyClass = "none"
                    ElseIf setting = "DENY" Then
                        included = False
                        why = "Excluded - denied for this user" &
                              If(viaGroups <> "", " (overrides group: " & viaGroups & ")", "")
                        whyClass = "exc"
                    ElseIf setting = "ALLOW" Then
                        included = True
                        why = "Included - allowed for this user" &
                              If(viaGroups <> "", " (also via group: " & viaGroups & ")", "")
                        whyClass = "inc"
                    ElseIf viaGroups <> "" Then
                        included = True
                        why = "Included - via group: " & viaGroups
                        whyClass = "inc"
                    Else
                        included = False
                        why = "Not included - no group or user setting allows it"
                        whyClass = "none"
                    End If

                    ' CheckBoxList writes item text as HTML, so every value is encoded here
                    Dim text As String = "<span class=""g-name"">" & Server.HtmlEncode(If(fullName = "", userId, fullName)) & "</span>" &
                                         " <span class=""g-id"">(" & Server.HtmlEncode(userId) & ")</span>" &
                                         If(isDisabled, "<span class=""g-tag off"">Disabled</span>", "") &
                                         If(why <> "", "<span class=""g-why " & whyClass & """>" & Server.HtmlEncode(why) & "</span>", "")

                    Dim item As New ListItem(text, userId)
                    item.Selected = included
                    Dim email As String = Convert.ToString(r("EMAIL")).Trim()
                    If email <> "" Then item.Attributes("title") = email
                    cblUsers.Items.Add(item)
                Next
            End If

            cblUsers.Enabled = HasActionSelected
            btnSaveUsers.Enabled = HasActionSelected AndAlso cblUsers.Items.Count > 0

            If cblUsers.Items.Count = 0 Then
                ShowUsersNote("No users are defined yet.", "info")
            ElseIf Not HasActionSelected Then
                ShowUsersNote("Select a project, status and action above to see which users may perform it.", "info")
            End If
        Catch ex As Exception
            cblUsers.Enabled = False
            btnSaveUsers.Enabled = False
            ShowUsersNote("Users can't be shown: " & ex.Message, "err")
        End Try
    End Sub

    ''' <summary>User IDs included through one of their groups for the selected action.</summary>
    Private Function UsersIncludedViaGroup() As HashSet(Of String)
        Dim result As New HashSet(Of String)
        Dim DT As DataTable = GetDataTable(EBDB,
            "SELECT DISTINCT UG.USER_ID FROM UNITSHUB_SEC_USER_GROUPS UG WHERE " & GroupIncludedSql("UG.GROUP_ID"))
        If DT IsNot Nothing Then
            For Each r As DataRow In DT.Rows
                result.Add(Convert.ToString(r("USER_ID")).Trim())
            Next
        End If
        Return result
    End Function

    ''' <summary>
    ''' Saves the ticks as user settings in UNITSHUB_SEC_USER_ACTIONS, only where they
    ''' differ from what the user's groups give:
    '''   ticked,   not via a group -> ALLOW_DENY = 'A' (allow) for this user
    '''   unticked, via a group     -> ALLOW_DENY = 'D' (deny) for this user
    '''   same as the groups give   -> no user setting (removed if there was one)
    ''' </summary>
    Protected Sub btnSaveUsers_Click(sender As Object, e As EventArgs) Handles btnSaveUsers.Click
        If Not HasActionSelected Then Exit Sub

        ' Read the ticks before BindUsers rebuilds the list
        Dim ticks As New Dictionary(Of String, Boolean)
        For Each item As ListItem In cblUsers.Items
            ticks(item.Value) = item.Selected
        Next

        Dim allows As Integer = 0, denies As Integer = 0
        Try
            Dim viaGroup As HashSet(Of String) = UsersIncludedViaGroup()

            DB.ExecuteNonQuery(EBDB_CS,
                "DELETE FROM UNITSHUB_SEC_USER_ACTIONS UA WHERE " & SelectedActionFilter("UA"))

            For Each kv As KeyValuePair(Of String, Boolean) In ticks
                Dim fromGroup As Boolean = viaGroup.Contains(kv.Key)
                If kv.Value = fromGroup Then Continue For   ' the groups already give this

                Dim setting As String = If(kv.Value, "A", "D")   ' ALLOW_DENY: A = allow, D = deny
                If kv.Value Then allows += 1 Else denies += 1

                DB.ExecuteNonQuery(EBDB_CS,
                    "INSERT INTO UNITSHUB_SEC_USER_ACTIONS (USER_ID, PROJECT_ID, STATE_ID, ACTION_ID, ALLOW_DENY) VALUES (" &
                    Q(kv.Key) & ", " & Q(lblProjectID.Text) & ", " & Q(lblStatusID.Text) & ", " & Q(lblActionID.Text) & ", " & Q(setting) & ")")
            Next

            BindUsers()
            ShowUsersNote("Saved: " & allows & " user " & If(allows = 1, "ALLOW", "ALLOWs") & ", " &
                          denies & " user " & If(denies = 1, "DENY", "DENYs") & " (the rest follow their groups).", "ok")
        Catch ex As Exception
            ShowUsersNote("The users couldn't be saved: " & ex.Message, "err")
        End Try

        ShowTab(UsersTabIndex)
    End Sub

    Private Sub ShowUsersNote(Text As String, Kind As String)
        lblUsersNote.Text = Server.HtmlEncode(Text)
        lblUsersNote.CssClass = "roles-note " & Kind
        lblUsersNote.Visible = True
    End Sub

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

    ' ==================================================================
    ' Tabs
    ' ==================================================================

    ''' <summary>Clicking a tab shows its view.</summary>
    Protected Sub Menu3_MenuItemClick(sender As Object, e As MenuEventArgs) Handles Menu3.MenuItemClick
        ShowTab(Menu3.Items.IndexOf(Menu3.SelectedItem))
    End Sub

    ''' <summary>Shows a tab and highlights its menu item.</summary>
    Private Sub ShowTab(Index As Integer)
        If Index < 0 OrElse Index >= MultiView3.Views.Count Then Exit Sub
        MultiView3.ActiveViewIndex = Index
        Menu3.Items(Index).Selected = True
    End Sub

    ''' <summary>Closes the popup.</summary>
    Protected Sub imgClose_Click(sender As Object, e As ImageClickEventArgs) Handles imgClose.Click
        VendorPopupHelper.RegisterPopupSelectionAndClose(Me, False, skipPostBack:=False)
    End Sub
End Class
