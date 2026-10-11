Imports System.Data
Imports System.Collections.Generic

''' <summary>
''' Manage Users: pick a user, then
'''   Groups tab  - tick the groups the user belongs to (UNITSHUB_SEC_USER_GROUPS)
'''   Actions tab - choose a place (Project -> Status -> Sub-status) and set each action
'''                 there to Inherit (whatever the user's groups give), Allow or Deny
'''                 (UNITSHUB_SEC_USER_ACTIONS, ALLOW_DENY 'A' / 'D'; Inherit = no row).
'''
''' Rules (same as Actions_Roles_Groups_Users):
'''   A group gives an action when it has no Deny row for it and either has an Allow row
'''   or its role (UNITSHUB_SEC_GROUPS_ROLES -> UNITSHUB_SEC_ROLES_ACTIONS) allows it.
'''   The user gets it when any of their groups gives it, unless the user's own row
'''   says Deny; the user's own Allow row gives it regardless of groups.
'''
''' The security tables have no sub-status column: the sub-status only narrows which
''' actions are listed; settings are saved per status.
''' Optional parameters: ?UserID=&ProjectID=&StatusID=&SUBSTATEID= pre-select the lists;
''' ?Tab=Actions (or a ProjectID) opens on the Actions tab.
''' </summary>
Partial Class ManageUsers
    Inherits System.Web.UI.Page

    Private Const GroupsTabIndex As Integer = 0
    Private Const ActionsTabIndex As Integer = 1

    Protected Sub Page_Load(sender As Object, e As EventArgs) Handles Me.Load
        If Not IsPostBack Then
            LoadUsers(QueryValue("UserID"))
            LoadProjects(QueryValue("ProjectID"))
            LoadStatuses(QueryValue("StatusID"))
            LoadSubStatuses(QueryValue("SUBSTATEID"))
            RefreshUser()

            If QueryValue("Tab").Equals("Actions", StringComparison.OrdinalIgnoreCase) OrElse
               QueryValue("ProjectID") <> "" Then
                ShowTab(ActionsTabIndex)
            Else
                ShowTab(GroupsTabIndex)
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

    ''' <summary>Re-shows everything that depends on the selected user.</summary>
    Private Sub RefreshUser()
        BindUserInfo()
        BindGroups()
        BindActions()
    End Sub

    ' ==================================================================
    ' User
    ' ==================================================================

    Private ReadOnly Property UserId As String
        Get
            Return ddlUser.SelectedValue
        End Get
    End Property

    ''' <summary>True when IS_ENABLED says the account is off (N / NO / 0 / F).</summary>
    Private Function IsOff(Value As Object) As Boolean
        Dim s As String = Convert.ToString(Value).Trim().ToUpperInvariant()
        Return s.StartsWith("N") OrElse s = "0" OrElse s = "F" OrElse s = "FALSE"
    End Function

    ''' <summary>All users (UNITSHUB_USERS) as "Full name (USER_ID)"; disabled ones are marked.</summary>
    Private Sub LoadUsers(Preselect As String)
        ddlUser.Items.Clear()
        ddlUser.Items.Add(New ListItem("-- Select a user --", ""))
        Dim DT As DataTable = GetDataTable(EBDB,
            "SELECT USER_ID, FULL_NAME, EMAIL, IS_ENABLED FROM UNITSHUB_USERS ORDER BY NVL(FULL_NAME, USER_ID)")
        If DT IsNot Nothing Then
            For Each r As DataRow In DT.Rows
                Dim id As String = Convert.ToString(r("USER_ID")).Trim()
                Dim fullName As String = Convert.ToString(r("FULL_NAME")).Trim()
                Dim text As String = If(fullName = "", id, fullName & " (" & id & ")")
                If IsOff(r("IS_ENABLED")) Then text &= "  [Disabled]"
                Dim item As New ListItem(text, id)
                Dim email As String = Convert.ToString(r("EMAIL")).Trim()
                If email <> "" Then item.Attributes("title") = email
                ddlUser.Items.Add(item)
            Next
        End If
        SelectIfPresent(ddlUser, Preselect)
    End Sub

    ''' <summary>One line under the dropdown: email, account state, last login, groups.</summary>
    Private Sub BindUserInfo()
        lblUserInfo.Visible = False
        If UserId = "" Then Exit Sub

        Try
            Dim SQL As String = ""
            SQL = SQL + vbCrLf + " SELECT U.EMAIL, U.IS_ENABLED, U.CREATED_ON, U.LAST_LOGIN, "
            SQL = SQL + vbCrLf + "        (SELECT LISTAGG(G.GROUP_NAME, ', ') WITHIN GROUP (ORDER BY G.GROUP_NAME) "
            SQL = SQL + vbCrLf + "           FROM UNITSHUB_SEC_USER_GROUPS UG "
            SQL = SQL + vbCrLf + "           JOIN UNITSHUB_SEC_GROUPS G ON G.GROUP_ID = UG.GROUP_ID "
            SQL = SQL + vbCrLf + "          WHERE UG.USER_ID = U.USER_ID) AS GROUP_NAMES "
            SQL = SQL + vbCrLf + " FROM   UNITSHUB_USERS U "
            SQL = SQL + vbCrLf + " WHERE  U.USER_ID = " & Q(UserId)

            Dim DT As DataTable = GetDataTable(EBDB, SQL)
            If DT Is Nothing OrElse DT.Rows.Count = 0 Then Exit Sub
            Dim r As DataRow = DT.Rows(0)

            Dim parts As New List(Of String)
            Dim email As String = Convert.ToString(r("EMAIL")).Trim()
            If email <> "" Then parts.Add(Server.HtmlEncode(email))
            parts.Add(If(IsOff(r("IS_ENABLED")), "<span class=""off"">Account disabled</span>", "Account enabled"))
            parts.Add("Last login: <strong>" & FormatDate(r("LAST_LOGIN"), "never") & "</strong>")
            Dim groups As String = Convert.ToString(r("GROUP_NAMES")).Trim()
            parts.Add("Groups: <strong>" & If(groups = "", "none", Server.HtmlEncode(groups)) & "</strong>")

            lblUserInfo.Text = String.Join(" &middot; ", parts)
            lblUserInfo.Visible = True
        Catch ex As Exception
            ' The info line is only a convenience; the rest of the form still works
        End Try
    End Sub

    ''' <summary>A date column as dd-MMM-yyyy HH:mm, or Fallback when empty.</summary>
    Private Function FormatDate(Value As Object, Fallback As String) As String
        If Value Is Nothing OrElse IsDBNull(Value) Then Return Fallback
        If TypeOf Value Is DateTime Then Return CType(Value, DateTime).ToString("dd-MMM-yyyy HH:mm")
        Dim s As String = Convert.ToString(Value).Trim()
        Return If(s = "", Fallback, Server.HtmlEncode(s))
    End Function

    Protected Sub ddlUser_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ddlUser.SelectedIndexChanged
        lblUserNote.Visible = False
        RefreshUser()
    End Sub

    ' ==================================================================
    ' Groups: the groups the user belongs to
    ' ==================================================================

    ''' <summary>
    ''' Lists every group (UNITSHUB_SEC_GROUPS) with its role; ticked = the user is a
    ''' member (UNITSHUB_SEC_USER_GROUPS).
    ''' </summary>
    Private Sub BindGroups()
        cblGroups.Items.Clear()
        lblGroupsNote.Visible = False
        SetTabCaption(GroupsTabIndex, "Groups", -1)

        Dim canEdit As Boolean = (UserId <> "")
        cblGroups.Enabled = canEdit
        pnlGroupTools.Visible = canEdit
        btnSaveGroups.Enabled = False

        Try
            Dim SQL As String = ""
            SQL = SQL + vbCrLf + " SELECT G.GROUP_ID, G.GROUP_NAME, G.DESCRIPTION, G.IS_SYSTEM, "
            SQL = SQL + vbCrLf + "        (SELECT LISTAGG(R.ROLE_NAME, ', ') WITHIN GROUP (ORDER BY R.ROLE_NAME) "
            SQL = SQL + vbCrLf + "           FROM UNITSHUB_SEC_GROUPS_ROLES GR "
            SQL = SQL + vbCrLf + "           JOIN UNITSHUB_SEC_ROLES R ON R.ROLE_ID = GR.ROLE_ID "
            SQL = SQL + vbCrLf + "          WHERE GR.GROUP_ID = G.GROUP_ID) AS ROLE_NAMES, "
            If canEdit Then
                SQL = SQL + vbCrLf + "        CASE WHEN EXISTS (SELECT 1 FROM UNITSHUB_SEC_USER_GROUPS UG "
                SQL = SQL + vbCrLf + "                          WHERE UG.GROUP_ID = G.GROUP_ID AND UG.USER_ID = " & Q(UserId) & ") "
                SQL = SQL + vbCrLf + "             THEN 'Y' ELSE 'N' END AS IS_MEMBER "
            Else
                SQL = SQL + vbCrLf + "        'N' AS IS_MEMBER "
            End If
            SQL = SQL + vbCrLf + " FROM   UNITSHUB_SEC_GROUPS G "
            SQL = SQL + vbCrLf + " ORDER BY G.GROUP_NAME "

            Dim memberOf As Integer = 0
            Dim DT As DataTable = GetDataTable(EBDB, SQL)
            If DT IsNot Nothing Then
                For Each r As DataRow In DT.Rows
                    Dim groupId As String = Convert.ToString(r("GROUP_ID")).Trim()
                    Dim groupName As String = Convert.ToString(r("GROUP_NAME")).Trim()
                    Dim roles As String = Convert.ToString(r("ROLE_NAMES")).Trim()
                    Dim description As String = Convert.ToString(r("DESCRIPTION")).Trim()
                    Dim systemFlag As String = Convert.ToString(r("IS_SYSTEM")).Trim().ToUpperInvariant()

                    ' CheckBoxList renders item text as HTML, so encode the values
                    Dim html As String = Server.HtmlEncode(If(groupName = "", groupId, groupName))
                    If systemFlag = "Y" OrElse systemFlag = "1" OrElse systemFlag = "T" OrElse systemFlag = "TRUE" OrElse systemFlag = "YES" Then
                        html &= "<span class=""g-tag"">System</span>"
                    End If
                    Dim subLine As String = "Role: " & If(roles = "", "none", roles) & If(description = "", "", " - " & description)
                    html &= "<span class=""g-desc"">" & Server.HtmlEncode(subLine) & "</span>"

                    Dim item As New ListItem(html, groupId)
                    item.Selected = (Convert.ToString(r("IS_MEMBER")) = "Y")
                    If item.Selected Then memberOf += 1
                    cblGroups.Items.Add(item)
                Next
            End If

            btnSaveGroups.Enabled = canEdit AndAlso cblGroups.Items.Count > 0
            If canEdit Then SetTabCaption(GroupsTabIndex, "Groups", memberOf)

            If cblGroups.Items.Count = 0 Then
                ShowNote(lblGroupsNote, "There are no groups yet. Add them in Manage Groups.", "info")
                pnlGroupTools.Visible = False
            ElseIf Not canEdit Then
                ShowNote(lblGroupsNote, "Choose a user above to tick their groups.", "info")
            End If
        Catch ex As Exception
            cblGroups.Enabled = False
            btnSaveGroups.Enabled = False
            ShowNote(lblGroupsNote, "Groups can't be shown: " & ex.Message, "err")
        End Try
    End Sub

    ''' <summary>
    ''' Replaces the user's groups with the ticked ones: their UNITSHUB_SEC_USER_GROUPS rows
    ''' are removed, then one row is added per ticked group. The info line and the
    ''' Actions tab are refreshed to match.
    ''' </summary>
    Protected Sub btnSaveGroups_Click(sender As Object, e As EventArgs) Handles btnSaveGroups.Click
        If UserId = "" OrElse cblGroups.Items.Count = 0 Then Exit Sub

        ' Read the ticks before BindGroups rebuilds the list
        Dim ticked As New List(Of String)
        For Each item As ListItem In cblGroups.Items
            If item.Selected Then ticked.Add(item.Value)
        Next

        Try
            DB.ExecuteNonQuery(EBDB_CS,
                "DELETE FROM UNITSHUB_SEC_USER_GROUPS WHERE USER_ID = " & Q(UserId))

            For Each groupId As String In ticked
                DB.ExecuteNonQuery(EBDB_CS,
                    "INSERT INTO UNITSHUB_SEC_USER_GROUPS (USER_ID, GROUP_ID) VALUES (" &
                    Q(UserId) & ", " & Q(groupId) & ")")
            Next

            BindUserInfo()
            BindGroups()
            BindActions()
            ShowNote(lblGroupsNote, "Saved: the user is in " & ticked.Count & If(ticked.Count = 1, " group.", " groups."), "ok")
        Catch ex As Exception
            ShowNote(lblGroupsNote, "The groups couldn't be saved: " & ex.Message, "err")
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
    ' Actions: Inherit / Allow / Deny for the user at the selected place
    ' ==================================================================

    ''' <summary>
    ''' SQL condition: group GroupAlias has a row in UNITSHUB_SEC_GROUPS_ACTIONS for action A
    ''' at the selected status; DenyOnly limits it to Deny rows.
    ''' </summary>
    Private Function GroupRowSql(GroupAlias As String, RowAlias As String, DenyOnly As Boolean) As String
        Return "EXISTS (SELECT 1 FROM UNITSHUB_SEC_GROUPS_ACTIONS " & RowAlias &
               " WHERE " & RowAlias & ".GROUP_ID = " & GroupAlias & ".GROUP_ID" &
               " AND " & RowAlias & ".PROJECT_ID = A.PROJECT_ID" &
               " AND " & RowAlias & ".STATE_ID = " & Q(StatusId) &
               " AND " & RowAlias & ".ACTION_ID = A.ACTION_ID" &
               If(DenyOnly, " AND SUBSTR(UPPER(TRIM(" & RowAlias & ".ALLOW_DENY)), 1, 1) IN ('D', 'N', '0')", "") & ")"
    End Function

    ''' <summary>
    ''' Lists the actions available at the selected place (same placement rule as
    ''' ManageRoles / ManageGroups), with:
    '''   VIA_GROUPS   - the user's groups that give the action (no Deny row; Allow row or role)
    '''   DENIED_BY    - the user's groups that deny it
    '''   USER_SETTING - the user's own row in UNITSHUB_SEC_USER_ACTIONS: A, D or none
    ''' The Result column shows the saved outcome.
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
            If UserId <> "" Then
                ' Groups of the user that give the action
                SQL = SQL + vbCrLf + "        (SELECT LISTAGG(G.GROUP_NAME, ', ') WITHIN GROUP (ORDER BY G.GROUP_NAME) "
                SQL = SQL + vbCrLf + "           FROM UNITSHUB_SEC_USER_GROUPS UG "
                SQL = SQL + vbCrLf + "           JOIN UNITSHUB_SEC_GROUPS G ON G.GROUP_ID = UG.GROUP_ID "
                SQL = SQL + vbCrLf + "          WHERE UG.USER_ID = " & Q(UserId)
                SQL = SQL + vbCrLf + "            AND NOT " & GroupRowSql("G", "GXD", True)
                SQL = SQL + vbCrLf + "            AND (" & GroupRowSql("G", "GXA", False)
                SQL = SQL + vbCrLf + "                 OR EXISTS (SELECT 1 FROM UNITSHUB_SEC_GROUPS_ROLES GXR "
                SQL = SQL + vbCrLf + "                            JOIN UNITSHUB_SEC_ROLES_ACTIONS RXA ON RXA.ROLE_ID = GXR.ROLE_ID "
                SQL = SQL + vbCrLf + "                             AND RXA.PROJECT_ID = A.PROJECT_ID "
                SQL = SQL + vbCrLf + "                             AND RXA.STATE_ID = " & Q(StatusId)
                SQL = SQL + vbCrLf + "                             AND RXA.ACTION_ID = A.ACTION_ID "
                SQL = SQL + vbCrLf + "                           WHERE GXR.GROUP_ID = G.GROUP_ID))) AS VIA_GROUPS, "
                ' Groups of the user that deny it
                SQL = SQL + vbCrLf + "        (SELECT LISTAGG(G.GROUP_NAME, ', ') WITHIN GROUP (ORDER BY G.GROUP_NAME) "
                SQL = SQL + vbCrLf + "           FROM UNITSHUB_SEC_USER_GROUPS UG "
                SQL = SQL + vbCrLf + "           JOIN UNITSHUB_SEC_GROUPS G ON G.GROUP_ID = UG.GROUP_ID "
                SQL = SQL + vbCrLf + "          WHERE UG.USER_ID = " & Q(UserId)
                SQL = SQL + vbCrLf + "            AND " & GroupRowSql("G", "GDD", True) & ") AS DENIED_BY, "
                ' The user's own setting
                SQL = SQL + vbCrLf + "        (SELECT CASE WHEN MAX(CASE WHEN SUBSTR(UPPER(TRIM(UA.ALLOW_DENY)), 1, 1) IN ('D', 'N', '0') "
                SQL = SQL + vbCrLf + "                                   THEN 1 ELSE 0 END) = 1 THEN 'D' "
                SQL = SQL + vbCrLf + "                     WHEN COUNT(*) > 0 THEN 'A' END "
                SQL = SQL + vbCrLf + "           FROM UNITSHUB_SEC_USER_ACTIONS UA "
                SQL = SQL + vbCrLf + "          WHERE UA.USER_ID = " & Q(UserId)
                SQL = SQL + vbCrLf + "            AND UA.PROJECT_ID = A.PROJECT_ID "
                SQL = SQL + vbCrLf + "            AND UA.STATE_ID = " & Q(StatusId)
                SQL = SQL + vbCrLf + "            AND UA.ACTION_ID = A.ACTION_ID) AS USER_SETTING "
            Else
                SQL = SQL + vbCrLf + "        CAST(NULL AS VARCHAR2(4000)) AS VIA_GROUPS, "
                SQL = SQL + vbCrLf + "        CAST(NULL AS VARCHAR2(4000)) AS DENIED_BY, "
                SQL = SQL + vbCrLf + "        CAST(NULL AS VARCHAR2(1)) AS USER_SETTING "
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

            DT.Columns.Add("GROUPS_HTML", GetType(String))
            DT.Columns.Add("RESULT_HTML", GetType(String))
            For Each r As DataRow In DT.Rows
                Dim viaGroups As String = Convert.ToString(r("VIA_GROUPS")).Trim()
                Dim deniedBy As String = Convert.ToString(r("DENIED_BY")).Trim()
                Dim setting As String = Convert.ToString(r("USER_SETTING")).Trim().ToUpperInvariant()

                ' What the groups give
                Dim html As String = ""
                If viaGroups <> "" Then html &= "<span class=""via-yes"">Allowed by " & Server.HtmlEncode(viaGroups) & "</span>"
                If deniedBy <> "" Then html &= "<span class=""via-deny"">Denied by " & Server.HtmlEncode(deniedBy) & "</span>"
                If html = "" Then html = "<span class=""via-no"">Not given</span>"
                r("GROUPS_HTML") = html

                ' Saved outcome: own Deny wins, own Allow gives it, otherwise any giving group
                Dim allowed As Boolean
                Dim why As String
                If setting = "D" Then
                    allowed = False : why = "user Deny"
                ElseIf setting = "A" Then
                    allowed = True : why = "user Allow"
                Else
                    allowed = (viaGroups <> "")
                    why = If(allowed, "via groups", "no group gives it")
                End If
                r("RESULT_HTML") = "<span class=""result " & If(allowed, "yes", "no") & """ title=""" & why & """>" &
                                   If(allowed, "Allowed", "Not allowed") & "</span>"
            Next

            rptActions.DataSource = DT
            rptActions.DataBind()
            pnlActions.Visible = True
            btnSaveActions.Enabled = (UserId <> "")

            If UserId = "" Then
                ShowNote(lblActionsNote, "Choose a user above to allow or deny actions for them.", "info")
            End If
        Catch ex As Exception
            ShowNote(lblActionsNote, "Actions can't be shown: " & ex.Message, "err")
        End Try
    End Sub

    ''' <summary>Puts each row's switch on its saved setting; locked when no user is chosen.</summary>
    Protected Sub rptActions_ItemDataBound(sender As Object, e As RepeaterItemEventArgs) Handles rptActions.ItemDataBound
        If e.Item.ItemType <> ListItemType.Item AndAlso e.Item.ItemType <> ListItemType.AlternatingItem Then Exit Sub

        Dim row As DataRowView = CType(e.Item.DataItem, DataRowView)
        Dim rbl As RadioButtonList = CType(e.Item.FindControl("rblSetting"), RadioButtonList)
        Dim setting As String = Convert.ToString(row("USER_SETTING")).Trim().ToUpperInvariant()

        rbl.SelectedValue = If(setting = "A" OrElse setting = "D", setting, "R")
        rbl.Enabled = (UserId <> "")
    End Sub

    ''' <summary>
    ''' Saves the switches for the user at the selected project and status: for every
    ''' action listed, its UNITSHUB_SEC_USER_ACTIONS row is removed, then added back with
    ''' ALLOW_DENY 'A' or 'D' unless the switch is on Inherit.
    ''' Actions not listed (e.g. placed only on another sub-status) are left as they are.
    ''' </summary>
    Protected Sub btnSaveActions_Click(sender As Object, e As EventArgs) Handles btnSaveActions.Click
        If UserId = "" OrElse ProjectId = "" OrElse StatusId = "" Then Exit Sub
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
                "DELETE FROM UNITSHUB_SEC_USER_ACTIONS WHERE USER_ID = " & Q(UserId) &
                " AND PROJECT_ID = " & Q(ProjectId) & " AND STATE_ID = " & Q(StatusId) &
                " AND ACTION_ID IN (" & inList & ")")

            Dim allowed As Integer = 0
            Dim denied As Integer = 0
            For Each kv As KeyValuePair(Of String, String) In settings
                DB.ExecuteNonQuery(EBDB_CS,
                    "INSERT INTO UNITSHUB_SEC_USER_ACTIONS (USER_ID, PROJECT_ID, STATE_ID, ACTION_ID, ALLOW_DENY) VALUES (" &
                    Q(UserId) & ", " & Q(ProjectId) & ", " & Q(StatusId) & ", " & Q(kv.Key) & ", " & Q(kv.Value) & ")")
                If kv.Value = "A" Then allowed += 1 Else denied += 1
            Next

            BindActions()
            ShowNote(lblActionsNote, "Saved: " & allowed & " allowed, " & denied & " denied, " &
                     (listed.Count - allowed - denied) & " inherited from groups.", "ok")
        Catch ex As Exception
            ShowNote(lblActionsNote, "The actions couldn't be saved: " & ex.Message, "err")
        End Try
    End Sub

    ' ==================================================================
    ' Tabs: Groups | Actions
    ' ==================================================================

    Protected Sub menuTabs_MenuItemClick(sender As Object, e As MenuEventArgs) Handles menuTabs.MenuItemClick
        ShowTab(menuTabs.Items.IndexOf(e.Item))
    End Sub

    ''' <summary>Shows a tab and highlights its menu item.</summary>
    Private Sub ShowTab(Index As Integer)
        If Index < 0 OrElse Index >= mvTabs.Views.Count Then Exit Sub
        mvTabs.ActiveViewIndex = Index
        menuTabs.Items(Index).Selected = True
    End Sub

    ''' <summary>Tab captions with counts, e.g. "Groups (3)".</summary>
    Private Sub SetTabCaption(Index As Integer, Caption As String, Count As Integer)
        menuTabs.Items(Index).Text = If(Count >= 0, Caption & " (" & Count & ")", Caption)
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
