Imports System.Data
Imports System.Globalization
Imports System.Collections.Generic

''' <summary>
''' One screen to set up a project's statuses, sub-statuses, where each action is valid
''' (placements) and which groups / users may use it there.
'''   Left : status tree - "All statuses", each status, and its sub-statuses.
'''   Right: the selected node - its card colours, and the actions available there with
'''          where each comes from (this sub-status / all of the status / all statuses)
'''          and the groups and users allowed it.
''' Tables:
'''   UNITSHUB_PROJECTSTATUS      statuses
'''   UNITSHUB_PROJECTSUBSTATUS   sub-statuses (SORT_ORDER = position under the status)
'''   UNITSHUB_ACTIONS            what an action is
'''   UNITSHUB_ACTION_PLACEMENTS  where it's valid ('*' = all) and its target from there
'''   Rights (who may use an action, per status) - see the "Rights" section:
'''   UNITSHUB_SEC_ROLES, UNITSHUB_SEC_ROLES_ACTIONS, UNITSHUB_SEC_GROUPS_ROLES,
'''   UNITSHUB_SEC_GROUPS_ACTIONS, UNITSHUB_SEC_USER_GROUPS, UNITSHUB_SEC_USER_ACTIONS
'''   (UNITSHUB_PRJ_STS_ACTN_USRS is no longer used.)
''' </summary>
Partial Class ProjectStatusAndAction
    Inherits System.Web.UI.Page

    Dim encryNdecry As New EncryDecry

    ' ------------------------------------------------------------------
    ' Selection
    ' ------------------------------------------------------------------

    ''' <summary>
    ''' Selected tree node: "ALL", "S|&lt;STATE_ID&gt;" or "SS|&lt;STATE_ID&gt;|&lt;SUBSTATE_ID&gt;".
    ''' </summary>
    Private Property SelKey As String
        Get
            Dim v As String = Convert.ToString(ViewState("SelKey"))
            Return If(v = "", "ALL", v)
        End Get
        Set(value As String)
            ViewState("SelKey") = value
        End Set
    End Property

    Private ReadOnly Property ProjectId As String
        Get
            Return Convert.ToString(ddlProjectName.SelectedValue)
        End Get
    End Property

    Private ReadOnly Property SelKind As String
        Get
            Return SelKey.Split("|"c)(0)
        End Get
    End Property

    ''' <summary>Status of the selected node ("*" for All statuses).</summary>
    Private ReadOnly Property SelStatus As String
        Get
            Dim parts() As String = SelKey.Split("|"c)
            Return If(parts.Length > 1, parts(1), "*")
        End Get
    End Property

    ''' <summary>Sub-status of the selected node ("*" when a whole status or All is selected).</summary>
    Private ReadOnly Property SelSub As String
        Get
            Dim parts() As String = SelKey.Split("|"c)
            Return If(parts.Length > 2, parts(2), "*")
        End Get
    End Property

    ' ------------------------------------------------------------------
    ' Page
    ' ------------------------------------------------------------------

    Protected Sub Page_Load(sender As Object, e As EventArgs) Handles Me.Load
        If Not IsPostBack Then
            LoadProjects()
            Dim requested As String = Convert.ToString(Request("ProjectID"))
            If requested <> "" AndAlso ddlProjectName.Items.FindByValue(requested) IsNot Nothing Then
                ddlProjectName.SelectedValue = requested
            End If
        End If
    End Sub

    ''' <summary>
    ''' Everything is (re)drawn here, after the click handlers have run, so the tree, the
    ''' detail pane, the actions list and the popup links always match the latest data.
    ''' </summary>
    Protected Sub Page_PreRender(sender As Object, e As EventArgs) Handles Me.PreRender
        If ProjectId = "" Then Exit Sub
        EnsureSelectionExists()
        BindTree()
        BindDetail()
        BindActions()
        RegisterPopups()
    End Sub

    ''' <summary>Closes this popup and reloads its parent (as before).</summary>
    Protected Sub btnClose_Click(ByVal sender As Object, ByVal e As EventArgs)
        VendorPopupHelper.RegisterPopupSelectionAndClose(Me, False, skipPostBack:=False)
    End Sub

    Private Sub LoadProjects()
        Dim DT As DataTable = GetDataTable(EBDB, "SELECT PROJECT_ID, PROJECT_NAME_EN FROM UNITSHUB_PROJECTS ORDER BY PROJECT_NAME_EN")
        ddlProjectName.DataSource = DT
        ddlProjectName.DataTextField = "PROJECT_NAME_EN"
        ddlProjectName.DataValueField = "PROJECT_ID"
        ddlProjectName.DataBind()
    End Sub

    Protected Sub ddlProjectName_SelectedIndexChanged(sender As Object, e As EventArgs)
        SelKey = "ALL"
        HidePanels()
    End Sub

    Private Sub HidePanels()
        pnlEdit.Visible = False
        pnlAddSub.Visible = False
        pnlPlace.Visible = False
    End Sub

    Private Sub ShowMessage(Text As String, IsOk As Boolean)
        lblMsg.Text = Server.HtmlEncode(Text)
        lblMsg.CssClass = "msg " & If(IsOk, "ok", "err")
        lblMsg.Visible = True
    End Sub

    ' ------------------------------------------------------------------
    ' Data helpers
    ' ------------------------------------------------------------------

    Private Function Q(Value As String) As String
        Return "'" & If(Value, "").Replace("'", "''") & "'"
    End Function

    Private Function QOrNull(Value As String) As String
        Return If(String.IsNullOrEmpty(Value), "NULL", Q(Value))
    End Function

    Private Sub Exec(SQL As String)
        DB.ExecuteNonQuery(EBDB_CS, SQL)
    End Sub

    Private Function Statuses() As DataTable
        Return GetDataTable(EBDB,
            "SELECT STATE_ID, STATUS, SUBTITLE, STATUS_BG_COLOR, STATUS_FG_COLOR, SUBTITLE_BG_COLOR, SUBTITLE_FG_COLOR " &
            "FROM UNITSHUB_PROJECTSTATUS WHERE PROJECT_ID = " & Q(ProjectId) & " ORDER BY TO_NUMBER(STATE_ID)")
    End Function

    Private Function SubStatuses(Optional StateId As String = Nothing) As DataTable
        Return GetDataTable(EBDB,
            "SELECT STATE_ID, SUBSTATE_ID, SUBTITLE, SORT_ORDER, STATUS_BG_COLOR, STATUS_FG_COLOR, SUBTITLE_BG_COLOR, SUBTITLE_FG_COLOR " &
            "FROM UNITSHUB_PROJECTSUBSTATUS WHERE PROJECT_ID = " & Q(ProjectId) &
            If(StateId Is Nothing, "", " AND STATE_ID = " & Q(StateId)) &
            " ORDER BY STATE_ID, SORT_ORDER")
    End Function

    Private Function StatusRow(StateId As String) As DataRow
        Dim DT As DataTable = GetDataTable(EBDB,
            "SELECT * FROM UNITSHUB_PROJECTSTATUS WHERE PROJECT_ID = " & Q(ProjectId) & " AND STATE_ID = " & Q(StateId))
        Return If(DT IsNot Nothing AndAlso DT.Rows.Count > 0, DT.Rows(0), Nothing)
    End Function

    Private Function SubRow(StateId As String, SubId As String) As DataRow
        Dim DT As DataTable = GetDataTable(EBDB,
            "SELECT * FROM UNITSHUB_PROJECTSUBSTATUS WHERE PROJECT_ID = " & Q(ProjectId) &
            " AND STATE_ID = " & Q(StateId) & " AND SUBSTATE_ID = " & Q(SubId))
        Return If(DT IsNot Nothing AndAlso DT.Rows.Count > 0, DT.Rows(0), Nothing)
    End Function

    Private Function Col(Row As DataRow, Name As String) As String
        If Row Is Nothing OrElse Not Row.Table.Columns.Contains(Name) Then Return ""
        Return Convert.ToString(Row(Name)).Trim()
    End Function

    ''' <summary>"#rrggbb" for a colour input ("" stays as the given fallback).</summary>
    Private Function ToColor(Value As String, Fallback As String) As String
        Dim v As String = If(Value, "").Trim()
        If v = "" Then Return Fallback
        If Not v.StartsWith("#") Then v = "#" & v
        Return v.ToLowerInvariant()
    End Function

    ''' <summary>Readable name of a place: "All statuses", "All of Sold", "Sold - Waiting 2nd payment".</summary>
    Private Function PlaceName(StateId As String, SubId As String) As String
        If StateId = "*" OrElse StateId = "" Then Return "All statuses"
        Dim st As DataRow = StatusRow(StateId)
        Dim statusName As String = If(st Is Nothing, StateId, Col(st, "STATUS"))
        If SubId = "*" OrElse SubId = "" Then Return "All of " & statusName
        Dim sb As DataRow = SubRow(StateId, SubId)
        Return statusName & " - " & If(sb Is Nothing, SubId, Col(sb, "SUBTITLE"))
    End Function

    ' ------------------------------------------------------------------
    ' Tree
    ' ------------------------------------------------------------------

    Private Sub EnsureSelectionExists()
        If SelKind = "S" AndAlso StatusRow(SelStatus) Is Nothing Then SelKey = "ALL"
        If SelKind = "SS" AndAlso SubRow(SelStatus, SelSub) Is Nothing Then SelKey = "ALL"
    End Sub

    Private Sub BindTree()
        Dim T As New DataTable
        T.Columns.Add("NodeKey") : T.Columns.Add("NodeText") : T.Columns.Add("NodeLevel", GetType(Integer))
        T.Columns.Add("Marker") : T.Columns.Add("IsSelected", GetType(Boolean))

        T.Rows.Add("ALL", "All statuses", 0, "<span class=""dot"" style=""background:#94a3b8;border-radius:50%""></span>", SelKey = "ALL")

        Dim subs As DataTable = SubStatuses()
        For Each st As DataRow In Statuses().Rows
            Dim sid As String = Col(st, "STATE_ID")
            Dim color As String = ToColor(Col(st, "STATUS_BG_COLOR"), "#cbd5e1")
            Dim key As String = "S|" & sid
            T.Rows.Add(key, Col(st, "STATUS"), 1,
                       "<span class=""dot"" style=""background:" & Server.HtmlEncode(color) & """></span>", SelKey = key)

            For Each sb As DataRow In subs.Rows
                If Col(sb, "STATE_ID") <> sid Then Continue For
                Dim subKey As String = "SS|" & sid & "|" & Col(sb, "SUBSTATE_ID")
                T.Rows.Add(subKey, Col(sb, "SUBTITLE"), 2, "", SelKey = subKey)
            Next
        Next

        rptTree.DataSource = T
        rptTree.DataBind()
    End Sub

    Protected Sub rptTree_ItemCommand(source As Object, e As RepeaterCommandEventArgs)
        If e.CommandName = "Select" Then
            SelKey = Convert.ToString(e.CommandArgument)
            HidePanels()
        End If
    End Sub

    ' ------------------------------------------------------------------
    ' Detail: header, colours, edit, reorder, add sub-status
    ' ------------------------------------------------------------------

    Private Sub BindDetail()
        Dim isAll As Boolean = (SelKind = "ALL")
        Dim isSub As Boolean = (SelKind = "SS")

        Dim isStatus As Boolean = (SelKind = "S")
        btnUp.Visible = isSub
        btnDown.Visible = isSub
        btnEdit.Visible = isSub                ' sub-status: edited here
        btnEditStatus.Visible = isStatus       ' status: AddProjectStatus popup, as before
        btnAddStateUp.Visible = isStatus AndAlso SelStatus <> "0000"
        btnAddStateDown.Visible = isStatus AndAlso SelStatus <> "9216"
        pnlPreview.Visible = Not isAll
        btnShowAddSub.Enabled = Not isAll

        If isAll Then
            litCrumb.Text = Server.HtmlEncode(ddlProjectName.SelectedItem.Text)
            litTitle.Text = "All statuses"
            Exit Sub
        End If

        Dim st As DataRow = StatusRow(SelStatus)
        Dim sb As DataRow = If(isSub, SubRow(SelStatus, SelSub), Nothing)

        If isSub Then
            litCrumb.Text = Server.HtmlEncode(Col(st, "STATUS") & " · sub-status " & SelSub)
            litTitle.Text = Server.HtmlEncode(Col(sb, "SUBTITLE"))
        Else
            litCrumb.Text = Server.HtmlEncode("Status " & SelStatus)
            litTitle.Text = Server.HtmlEncode(Col(st, "STATUS"))
        End If

        ' Preview: sub-status colours where set, otherwise the status's
        Dim pick = Function(name As String, fallback As String) As String
                       Dim v As String = Col(sb, name)
                       If v = "" Then v = Col(st, name)
                       Return ToColor(v, fallback)
                   End Function

        divPreviewStatus.InnerText = Col(st, "STATUS")
        divPreviewStatus.Style("background-color") = pick("STATUS_BG_COLOR", "#e2e8f0")
        divPreviewStatus.Style("color") = pick("STATUS_FG_COLOR", "#1e293b")

        Dim subtitle As String = If(isSub, Col(sb, "SUBTITLE"), Col(st, "SUBTITLE"))
        divPreviewSub.Visible = (subtitle <> "")
        divPreviewSub.InnerText = subtitle
        divPreviewSub.Style("background-color") = pick("SUBTITLE_BG_COLOR", "#ffffff")
        divPreviewSub.Style("color") = pick("SUBTITLE_FG_COLOR", "#1e293b")
    End Sub

    ''' <summary>Opens the inline editor for the selected sub-status (subtitle and colours).</summary>
    Protected Sub btnEdit_Click(sender As Object, e As EventArgs)
        HidePanels()
        If SelKind <> "SS" Then Exit Sub
        Dim st As DataRow = StatusRow(SelStatus)
        Dim sb As DataRow = SubRow(SelStatus, SelSub)
        If sb Is Nothing Then Exit Sub

        txtEditSubtitle.Text = Col(sb, "SUBTITLE")
        txtEditStatusBg.Text = ToColor(Col(sb, "STATUS_BG_COLOR"), ToColor(Col(st, "STATUS_BG_COLOR"), "#e2e8f0"))
        txtEditStatusFg.Text = ToColor(Col(sb, "STATUS_FG_COLOR"), ToColor(Col(st, "STATUS_FG_COLOR"), "#1e293b"))
        txtEditSubBg.Text = ToColor(Col(sb, "SUBTITLE_BG_COLOR"), ToColor(Col(st, "SUBTITLE_BG_COLOR"), "#ffffff"))
        txtEditSubFg.Text = ToColor(Col(sb, "SUBTITLE_FG_COLOR"), ToColor(Col(st, "SUBTITLE_FG_COLOR"), "#1e293b"))
        pnlEdit.Visible = True
    End Sub

    Protected Sub btnEditCancel_Click(sender As Object, e As EventArgs)
        pnlEdit.Visible = False
    End Sub

    Protected Sub btnEditSave_Click(sender As Object, e As EventArgs)
        Try
            If SelKind = "SS" Then
                If txtEditSubtitle.Text.Trim() = "" Then
                    ShowMessage("Enter a subtitle for the sub-status.", False)
                    Exit Sub
                End If
                Exec("UPDATE UNITSHUB_PROJECTSUBSTATUS SET " &
                     "SUBTITLE = " & Q(txtEditSubtitle.Text.Trim()) & ", " &
                     "STATUS_BG_COLOR = " & Q(txtEditStatusBg.Text) & ", STATUS_FG_COLOR = " & Q(txtEditStatusFg.Text) & ", " &
                     "SUBTITLE_BG_COLOR = " & Q(txtEditSubBg.Text) & ", SUBTITLE_FG_COLOR = " & Q(txtEditSubFg.Text) & " " &
                     "WHERE PROJECT_ID = " & Q(ProjectId) & " AND STATE_ID = " & Q(SelStatus) & " AND SUBSTATE_ID = " & Q(SelSub))
                ShowMessage("Sub-status saved.", True)
            End If
            pnlEdit.Visible = False
        Catch ex As Exception
            ShowMessage("Couldn't save: " & ex.Message, False)
        End Try
    End Sub

    Protected Sub btnUp_Click(sender As Object, e As EventArgs)
        MoveSubStatus(-1)
    End Sub

    Protected Sub btnDown_Click(sender As Object, e As EventArgs)
        MoveSubStatus(1)
    End Sub

    ''' <summary>Swaps the selected sub-status's SORT_ORDER with its neighbour (only the order changes, never the ID).</summary>
    Private Sub MoveSubStatus(Direction As Integer)
        If SelKind <> "SS" Then Exit Sub
        Dim subs As DataTable = SubStatuses(SelStatus)
        Dim idx As Integer = -1
        For i As Integer = 0 To subs.Rows.Count - 1
            If Col(subs.Rows(i), "SUBSTATE_ID") = SelSub Then idx = i
        Next
        Dim other As Integer = idx + Direction
        If idx < 0 OrElse other < 0 OrElse other >= subs.Rows.Count Then Exit Sub

        ' Re-number 1..n in the new order, so duplicate or missing SORT_ORDERs are fixed too
        Dim order As New List(Of String)
        For Each r As DataRow In subs.Rows
            order.Add(Col(r, "SUBSTATE_ID"))
        Next
        Dim tmp As String = order(idx) : order(idx) = order(other) : order(other) = tmp

        Try
            For i As Integer = 0 To order.Count - 1
                Exec("UPDATE UNITSHUB_PROJECTSUBSTATUS SET SORT_ORDER = " & (i + 1).ToString(CultureInfo.InvariantCulture) &
                     " WHERE PROJECT_ID = " & Q(ProjectId) & " AND STATE_ID = " & Q(SelStatus) & " AND SUBSTATE_ID = " & Q(order(i)))
            Next
        Catch ex As Exception
            ShowMessage("Couldn't reorder: " & ex.Message, False)
        End Try
    End Sub

    Protected Sub btnShowAddSub_Click(sender As Object, e As EventArgs)
        HidePanels()
        If SelKind = "ALL" Then Exit Sub
        Dim st As DataRow = StatusRow(SelStatus)
        litAddSubStatus.Text = Server.HtmlEncode(Col(st, "STATUS"))
        txtNewSubtitle.Text = ""
        txtNewStatusBg.Text = ToColor(Col(st, "STATUS_BG_COLOR"), "#e2e8f0")
        txtNewStatusFg.Text = ToColor(Col(st, "STATUS_FG_COLOR"), "#1e293b")
        txtNewSubBg.Text = ToColor(Col(st, "SUBTITLE_BG_COLOR"), "#ffffff")
        txtNewSubFg.Text = ToColor(Col(st, "SUBTITLE_FG_COLOR"), "#1e293b")
        pnlAddSub.Visible = True
    End Sub

    Protected Sub btnAddSubCancel_Click(sender As Object, e As EventArgs)
        pnlAddSub.Visible = False
    End Sub

    ''' <summary>
    ''' Adds a sub-status at the end of the selected status. New SUBSTATE_ID: 0000 for the
    ''' first one, then the highest + 1024 (as the existing ones are numbered), or + 1 once
    ''' that would pass 9999.
    ''' </summary>
    Protected Sub btnAddSubSave_Click(sender As Object, e As EventArgs)
        If SelKind = "ALL" Then Exit Sub
        Dim subtitle As String = txtNewSubtitle.Text.Trim()
        If subtitle = "" Then
            ShowMessage("Enter a subtitle for the new sub-status.", False)
            Exit Sub
        End If

        Try
            Dim maxIdText As String = Convert.ToString(DB.RetreiveScalarSTRING(EBDB,
                "SELECT MAX(TO_NUMBER(SUBSTATE_ID)) FROM UNITSHUB_PROJECTSUBSTATUS WHERE PROJECT_ID = " & Q(ProjectId) &
                " AND STATE_ID = " & Q(SelStatus) & " AND REGEXP_LIKE(SUBSTATE_ID, '^[0-9]+$')")).Trim()
            Dim newId As Integer
            If maxIdText = "" Then
                newId = 0
            Else
                Dim maxId As Integer = CInt(maxIdText)
                newId = If(maxId + 1024 <= 9999, maxId + 1024, maxId + 1)
            End If
            If newId > 9999 Then
                ShowMessage("No free sub-status number left for this status.", False)
                Exit Sub
            End If

            Dim maxSort As String = Convert.ToString(DB.RetreiveScalarSTRING(EBDB,
                "SELECT NVL(MAX(SORT_ORDER), 0) FROM UNITSHUB_PROJECTSUBSTATUS WHERE PROJECT_ID = " & Q(ProjectId) &
                " AND STATE_ID = " & Q(SelStatus))).Trim()

            Dim newSubId As String = newId.ToString("0000")
            Exec("INSERT INTO UNITSHUB_PROJECTSUBSTATUS (PROJECT_ID, STATE_ID, SUBSTATE_ID, SUBTITLE, SORT_ORDER, " &
                 "STATUS_BG_COLOR, STATUS_FG_COLOR, SUBTITLE_BG_COLOR, SUBTITLE_FG_COLOR) VALUES (" &
                 Q(ProjectId) & ", " & Q(SelStatus) & ", " & Q(newSubId) & ", " & Q(subtitle) & ", " &
                 (CInt(Val(maxSort)) + 1).ToString(CultureInfo.InvariantCulture) & ", " &
                 Q(txtNewStatusBg.Text) & ", " & Q(txtNewStatusFg.Text) & ", " & Q(txtNewSubBg.Text) & ", " & Q(txtNewSubFg.Text) & ")")

            SelKey = "SS|" & SelStatus & "|" & newSubId
            pnlAddSub.Visible = False
            ShowMessage("Sub-status """ & subtitle & """ added.", True)
        Catch ex As Exception
            ShowMessage("Couldn't add the sub-status: " & ex.Message, False)
        End Try
    End Sub

    ' ------------------------------------------------------------------
    ' Actions available at the selected node
    ' ------------------------------------------------------------------

    ''' <summary>
    ''' Placements that apply to the selected node:
    '''   All statuses : placements with STATUS_ID = '*'
    '''   A status     : that status's whole-status placements, plus '*'
    '''   A sub-status : its own placements, its status's whole-status ones, plus '*'
    ''' If an action fits in more than one way, only the most specific placement is shown
    ''' (the one that applies to units here, same rule as MainPage).
    ''' </summary>
    Private Function PlacementsHere() As DataTable
        Dim where As String
        Select Case SelKind
            Case "ALL"
                where = "P.STATUS_ID = '*'"
            Case "S"
                where = "(P.STATUS_ID = '*' OR (P.STATUS_ID = " & Q(SelStatus) & " AND P.SUBSTATE_ID = '*'))"
            Case Else
                where = "(P.STATUS_ID = '*' OR (P.STATUS_ID = " & Q(SelStatus) & " AND P.SUBSTATE_ID IN (" & Q(SelSub) & ", '*')))"
        End Select

        Dim all As DataTable = GetDataTable(EBDB,
            "SELECT P.ACTION_ID, P.STATUS_ID, P.SUBSTATE_ID, P.TO_STATUS_ID, P.TO_SUBSTATE_ID, NVL(P.SORT_ORDER, 0) AS PL_SORT, " &
            "A.ACTION_TITLE " &
            "FROM UNITSHUB_ACTION_PLACEMENTS P " &
            "JOIN UNITSHUB_ACTIONS A ON A.PROJECT_ID = P.PROJECT_ID AND A.ACTION_ID = P.ACTION_ID " &
            "WHERE P.PROJECT_ID = " & Q(ProjectId) & " AND " & where & " " &
            "ORDER BY P.ACTION_ID")

        ' Most specific placement per action
        Dim spec = Function(r As DataRow) As Integer
                       If Col(r, "STATUS_ID") = "*" Then Return 0
                       If Col(r, "SUBSTATE_ID") = "*" Then Return 1
                       Return 2
                   End Function

        Dim result As DataTable = all.Clone()
        For Each g In all.AsEnumerable().GroupBy(Function(r) Col(r, "ACTION_ID"))
            result.ImportRow(g.OrderByDescending(Function(r) spec(r)).First())
        Next

        ' Display order: whole-status, this sub-status, all statuses; then SORT_ORDER
        Dim sorted As DataTable = result.Clone()
        For Each r As DataRow In result.AsEnumerable().
                OrderBy(Function(x)
                            Select Case spec(x)
                                Case 1 : Return 0
                                Case 2 : Return 1
                                Case Else : Return 2
                            End Select
                        End Function).
                ThenBy(Function(x) CInt(Val(Col(x, "PL_SORT")))).
                ThenBy(Function(x) Col(x, "ACTION_ID"))
            sorted.ImportRow(r)
        Next
        Return sorted
    End Function

    Private Sub BindActions()
        Dim P As DataTable = PlacementsHere()
        P.Columns.Add("TargetText") : P.Columns.Add("TagText") : P.Columns.Add("TagClass")
        P.Columns.Add("UsersHtml") : P.Columns.Add("GroupsHtml") : P.Columns.Add("PlacementKey") : P.Columns.Add("IsExactlyHere", GetType(Boolean))

        For Each r As DataRow In P.Rows
            Dim pst As String = Col(r, "STATUS_ID")
            Dim psb As String = Col(r, "SUBSTATE_ID")

            ' Where it comes from, relative to the selected node
            If pst = "*" Then
                r("TagText") = "All statuses" : r("TagClass") = "all"
            ElseIf psb = "*" Then
                r("TagText") = If(SelKind = "S", "This status", "All of " & Col(StatusRow(pst), "STATUS"))
                r("TagClass") = If(SelKind = "S", "here", "status")
            Else
                r("TagText") = "This sub-status" : r("TagClass") = "here"
            End If
            r("IsExactlyHere") = (pst = SelStatus AndAlso psb = SelSub) OrElse (SelKind = "ALL" AndAlso pst = "*")

            ' Target from this placement
            Dim toSt As String = Col(r, "TO_STATUS_ID")
            r("TargetText") = If(toSt = "", "",
                                 "&rarr; " & Server.HtmlEncode(Col(StatusRow(toSt), "STATUS") &
                                 If(Col(r, "TO_SUBSTATE_ID") = "", "", " · " & Col(r, "TO_SUBSTATE_ID"))))

            ' Groups and users allowed it here (the placement's status; every status for '*')
            Dim actionHere As String = Col(r, "ACTION_ID")
            Dim states As List(Of String) = RightsStates(pst)
            r("GroupsHtml") = Chips(NamesOf("UNITSHUB_SEC_GROUPS", "GROUP_ID", "GROUP_NAME",
                                            AllowedInAll(states, Function(x) AllowedGroups(x, actionHere))), "No groups")
            r("UsersHtml") = Chips(NamesOf("UNITSHUB_USERS", "USER_ID", "FULL_NAME",
                                           AllowedInAll(states, Function(x) AllowedUsers(x, actionHere))), "No users")

            r("PlacementKey") = Col(r, "ACTION_ID") & "|" & pst & "|" & psb
        Next

        rptActions.DataSource = P
        rptActions.DataBind()
        lblNoActions.Visible = (P.Rows.Count = 0)
    End Sub

    ''' <summary>
    ''' Per row: "Remove from here" only for placements that belong exactly to the selected
    ''' node (an "All of Sold" action is removed from Sold itself, not from one of its
    ''' sub-statuses), and the Edit button opens the action editor for that placement.
    ''' </summary>
    Protected Sub rptActions_ItemDataBound(sender As Object, e As RepeaterItemEventArgs)
        If e.Item.ItemType <> ListItemType.Item AndAlso e.Item.ItemType <> ListItemType.AlternatingItem Then Exit Sub
        Dim r As DataRowView = CType(e.Item.DataItem, DataRowView)

        Dim btnRemove As LinkButton = CType(e.Item.FindControl("btnRemove"), LinkButton)
        btnRemove.Visible = CBool(r("IsExactlyHere"))

        ' Users / Groups pickers: everyone listed, ticked = allowed now (in every status for '*')
        Dim actionIdHere As String = Convert.ToString(r("ACTION_ID"))
        Dim statusHere As String = Convert.ToString(r("STATUS_ID"))
        Dim states As List(Of String) = RightsStates(statusHere)
        Dim titleTail As String = Convert.ToString(r("ACTION_TITLE")) & " in " & RightsPlaceName(statusHere)

        Dim btnUsers As LinkButton = CType(e.Item.FindControl("btnUsers"), LinkButton)
        Dim UserListParameters As New clsListProperties
        With UserListParameters
            .ItemsSQL = "Select USER_ID as ID, FULL_NAME as Name from UNITSHUB_USERS order by Name"
            .CheckedItemsSQL = "Select USER_ID as ID from UNITSHUB_USERS where " &
                               InListSql("USER_ID", AllowedInAll(states, Function(s) AllowedUsers(s, actionIdHere)))
            .FormTitle = "Users allowed to " & titleTail
            .ColumnHideAndShow = "YN"
            .EditableColumns = "NN"
            .ColumnsWidth = New Double() {1, 3}
            .HoverableList = "Y"
        End With
        VendorPopupHelper.RegisterVendorPopup(Me, btnUsers,
            "AddMultipleItemsFromList.aspx?Parameters=" & Server.UrlEncode(encryNdecry.EncryptObject(Of clsListProperties)(UserListParameters)),
            400, 600, PopupPlacement.Center, "Users", VendorPopupHelper.PopupDisplayMode.FrameOnly)

        Dim btnGroups As LinkButton = CType(e.Item.FindControl("btnGroups"), LinkButton)
        Dim GroupListParameters As New clsListProperties
        With GroupListParameters
            .ItemsSQL = "Select GROUP_ID as ID, GROUP_NAME as Name from UNITSHUB_SEC_GROUPS order by Name"
            .CheckedItemsSQL = "Select GROUP_ID as ID from UNITSHUB_SEC_GROUPS where " &
                               InListSql("GROUP_ID", AllowedInAll(states, Function(s) AllowedGroups(s, actionIdHere)))
            .FormTitle = "Groups allowed to " & titleTail
            .ColumnHideAndShow = "YN"
            .EditableColumns = "NN"
            .ColumnsWidth = New Double() {1, 3}
            .HoverableList = "Y"
        End With
        VendorPopupHelper.RegisterVendorPopup(Me, btnGroups,
            "AddMultipleItemsFromList.aspx?Parameters=" & Server.UrlEncode(encryNdecry.EncryptObject(Of clsListProperties)(GroupListParameters)),
            400, 600, PopupPlacement.Center, "Groups", VendorPopupHelper.PopupDisplayMode.FrameOnly)

        Dim btnEditAction As LinkButton = CType(e.Item.FindControl("btnEditAction"), LinkButton)
        VendorPopupHelper.RegisterVendorPopup(Me, btnEditAction,
            "ActionAddEdit.aspx?ProjectID=" & Server.UrlEncode(ProjectId) &
            "&StatusID=" & Server.UrlEncode(Convert.ToString(r("STATUS_ID"))) &
            "&SUBSTATEID=" & Server.UrlEncode(Convert.ToString(r("SUBSTATE_ID"))) &
            "&ActionID=" & Server.UrlEncode(Convert.ToString(r("ACTION_ID"))) & "&Mode=Edit",
            1100, 900, PopupPlacement.Center, "", VendorPopupHelper.PopupDisplayMode.Standard)
    End Sub

    Protected Sub rptActions_ItemCommand(source As Object, e As RepeaterCommandEventArgs)
        Dim parts() As String = Convert.ToString(e.CommandArgument).Split("|"c)
        If parts.Length < 3 Then Exit Sub
        Dim actionId As String = parts(0), pst As String = parts(1), psb As String = parts(2)

        Select Case e.CommandName
            Case "Remove"
                Try
                    ' Rights are per status and may serve other placements, so they're kept
                    Exec("DELETE FROM UNITSHUB_ACTION_PLACEMENTS WHERE PROJECT_ID = " & Q(ProjectId) &
                         " AND ACTION_ID = " & Q(actionId) & " AND STATUS_ID = " & Q(pst) & " AND SUBSTATE_ID = " & Q(psb))
                    ShowMessage("Action removed from " & PlaceName(pst, psb) & ".", True)
                Catch ex As Exception
                    ShowMessage("Couldn't remove it: " & ex.Message, False)
                End Try

            Case "EditAction"
                ' The editor popup has closed - PreRender redraws the list
        End Select
    End Sub

    ' ------------------------------------------------------------------
    ' Place an existing action here
    ' ------------------------------------------------------------------

    Protected Sub btnShowPlace_Click(sender As Object, e As EventArgs)
        HidePanels()

        ' Actions of the project that aren't already placed exactly here
        Dim placeStatus As String = SelStatus
        Dim placeSub As String = SelSub
        Dim DT As DataTable = GetDataTable(EBDB,
            "SELECT A.ACTION_ID, A.ACTION_TITLE FROM UNITSHUB_ACTIONS A " &
            "WHERE A.PROJECT_ID = " & Q(ProjectId) & " AND NOT EXISTS (SELECT 1 FROM UNITSHUB_ACTION_PLACEMENTS P " &
            "WHERE P.PROJECT_ID = A.PROJECT_ID AND P.ACTION_ID = A.ACTION_ID " &
            "AND P.STATUS_ID = " & Q(placeStatus) & " AND P.SUBSTATE_ID = " & Q(placeSub) & ") " &
            "ORDER BY A.ACTION_TITLE")
        ddlPlaceAction.Items.Clear()
        ddlPlaceAction.Items.Add(New ListItem("Choose an action", ""))
        For Each r As DataRow In DT.Rows
            ddlPlaceAction.Items.Add(New ListItem(Col(r, "ACTION_TITLE") & " (" & Col(r, "ACTION_ID") & ")", Col(r, "ACTION_ID")))
        Next

        ' How wide: only offered when a sub-status is selected
        rblPlaceScope.Items.Clear()
        If SelKind = "SS" Then
            rblPlaceScope.Items.Add(New ListItem("This sub-status", "SUB"))
            rblPlaceScope.Items.Add(New ListItem("All of " & Col(StatusRow(SelStatus), "STATUS"), "STATUS"))
            rblPlaceScope.SelectedValue = "SUB"
        End If
        lblPlaceScope.Visible = (SelKind = "SS")
        rblPlaceScope.Visible = (SelKind = "SS")

        ' Target
        ddlPlaceToStatus.Items.Clear()
        ddlPlaceToStatus.Items.Add(New ListItem("Doesn't move the unit", ""))
        For Each st As DataRow In Statuses().Rows
            ddlPlaceToStatus.Items.Add(New ListItem(Col(st, "STATUS"), Col(st, "STATE_ID")))
        Next
        FillPlaceToSub()

        FillCopyUsersFrom()
        pnlPlace.Visible = True
    End Sub

    Private Sub FillPlaceToSub()
        ddlPlaceToSub.Items.Clear()
        ddlPlaceToSub.Items.Add(New ListItem("No sub-status", ""))
        If ddlPlaceToStatus.SelectedValue = "" Then Exit Sub
        For Each sb As DataRow In SubStatuses(ddlPlaceToStatus.SelectedValue).Rows
            ddlPlaceToSub.Items.Add(New ListItem(Col(sb, "SUBTITLE"), Col(sb, "SUBSTATE_ID")))
        Next
    End Sub

    ''' <summary>"Copy rights from" list: the statuses where the chosen action is already placed.</summary>
    Private Sub FillCopyUsersFrom()
        ddlCopyUsersFrom.Items.Clear()
        ddlCopyUsersFrom.Items.Add(New ListItem("Don't copy rights (set them afterwards)", ""))
        If ddlPlaceAction.SelectedValue = "" Then Exit Sub
        Dim DT As DataTable = GetDataTable(EBDB,
            "SELECT DISTINCT STATUS_ID FROM UNITSHUB_ACTION_PLACEMENTS WHERE PROJECT_ID = " & Q(ProjectId) &
            " AND ACTION_ID = " & Q(ddlPlaceAction.SelectedValue) & " AND STATUS_ID <> '*' ORDER BY STATUS_ID")
        For Each r As DataRow In DT.Rows
            ddlCopyUsersFrom.Items.Add(New ListItem("Copy roles, groups and users from " & PlaceName(Col(r, "STATUS_ID"), "*"),
                                                    Col(r, "STATUS_ID")))
        Next
    End Sub

    ''' <summary>
    ''' Copies the action's rights from one status to the target statuses: the role rows
    ''' (UNITSHUB_SEC_ROLES_ACTIONS) and the group and user Allow/Deny rows
    ''' (UNITSHUB_SEC_GROUPS_ACTIONS, UNITSHUB_SEC_USER_ACTIONS). Rows already there are kept.
    ''' </summary>
    Private Sub CopyRights(ActionId As String, FromState As String, ToStates As List(Of String))
        For Each toState As String In ToStates
            If toState = FromState Then Continue For
            Exec("INSERT INTO UNITSHUB_SEC_ROLES_ACTIONS (ROLE_ID, PROJECT_ID, STATE_ID, ACTION_ID) " &
                 "SELECT DISTINCT S.ROLE_ID, S.PROJECT_ID, " & Q(toState) & ", S.ACTION_ID FROM UNITSHUB_SEC_ROLES_ACTIONS S " &
                 "WHERE S.PROJECT_ID = " & Q(ProjectId) & " AND S.STATE_ID = " & Q(FromState) & " AND S.ACTION_ID = " & Q(ActionId) &
                 " AND NOT EXISTS (SELECT 1 FROM UNITSHUB_SEC_ROLES_ACTIONS X WHERE X.ROLE_ID = S.ROLE_ID AND X.PROJECT_ID = S.PROJECT_ID " &
                 "AND X.STATE_ID = " & Q(toState) & " AND X.ACTION_ID = S.ACTION_ID)")
            Exec("INSERT INTO UNITSHUB_SEC_GROUPS_ACTIONS (GROUP_ID, PROJECT_ID, STATE_ID, ACTION_ID, ALLOW_DENY) " &
                 "SELECT S.GROUP_ID, S.PROJECT_ID, " & Q(toState) & ", S.ACTION_ID, MAX(S.ALLOW_DENY) FROM UNITSHUB_SEC_GROUPS_ACTIONS S " &
                 "WHERE S.PROJECT_ID = " & Q(ProjectId) & " AND S.STATE_ID = " & Q(FromState) & " AND S.ACTION_ID = " & Q(ActionId) &
                 " AND NOT EXISTS (SELECT 1 FROM UNITSHUB_SEC_GROUPS_ACTIONS X WHERE X.GROUP_ID = S.GROUP_ID AND X.PROJECT_ID = S.PROJECT_ID " &
                 "AND X.STATE_ID = " & Q(toState) & " AND X.ACTION_ID = S.ACTION_ID) GROUP BY S.GROUP_ID, S.PROJECT_ID, S.ACTION_ID")
            Exec("INSERT INTO UNITSHUB_SEC_USER_ACTIONS (USER_ID, PROJECT_ID, STATE_ID, ACTION_ID, ALLOW_DENY) " &
                 "SELECT S.USER_ID, S.PROJECT_ID, " & Q(toState) & ", S.ACTION_ID, MAX(S.ALLOW_DENY) FROM UNITSHUB_SEC_USER_ACTIONS S " &
                 "WHERE S.PROJECT_ID = " & Q(ProjectId) & " AND S.STATE_ID = " & Q(FromState) & " AND S.ACTION_ID = " & Q(ActionId) &
                 " AND NOT EXISTS (SELECT 1 FROM UNITSHUB_SEC_USER_ACTIONS X WHERE X.USER_ID = S.USER_ID AND X.PROJECT_ID = S.PROJECT_ID " &
                 "AND X.STATE_ID = " & Q(toState) & " AND X.ACTION_ID = S.ACTION_ID) GROUP BY S.USER_ID, S.PROJECT_ID, S.ACTION_ID")
        Next
    End Sub

    Protected Sub ddlPlaceAction_SelectedIndexChanged(sender As Object, e As EventArgs)
        FillCopyUsersFrom()
        pnlPlace.Visible = True
    End Sub

    Protected Sub ddlPlaceToStatus_SelectedIndexChanged(sender As Object, e As EventArgs)
        FillPlaceToSub()
        pnlPlace.Visible = True
    End Sub

    Protected Sub btnPlaceCancel_Click(sender As Object, e As EventArgs)
        pnlPlace.Visible = False
    End Sub

    Protected Sub btnPlaceSave_Click(sender As Object, e As EventArgs)
        Dim actionId As String = ddlPlaceAction.SelectedValue
        If actionId = "" Then
            ShowMessage("Choose the action to place here.", False)
            pnlPlace.Visible = True
            Exit Sub
        End If

        Dim pst As String = SelStatus
        Dim psb As String = If(SelKind = "SS" AndAlso rblPlaceScope.SelectedValue = "STATUS", "*", SelSub)

        Try
            Dim exists As String = Convert.ToString(DB.RetreiveScalarSTRING(EBDB,
                "SELECT COUNT(*) FROM UNITSHUB_ACTION_PLACEMENTS WHERE PROJECT_ID = " & Q(ProjectId) &
                " AND ACTION_ID = " & Q(actionId) & " AND STATUS_ID = " & Q(pst) & " AND SUBSTATE_ID = " & Q(psb))).Trim()
            If Val(exists) > 0 Then
                ShowMessage("That action is already placed in " & PlaceName(pst, psb) & ".", False)
                pnlPlace.Visible = True
                Exit Sub
            End If

            Exec("INSERT INTO UNITSHUB_ACTION_PLACEMENTS (PROJECT_ID, ACTION_ID, STATUS_ID, SUBSTATE_ID, TO_STATUS_ID, TO_SUBSTATE_ID, SORT_ORDER, IS_ACTIVE) " &
                 "VALUES (" & Q(ProjectId) & ", " & Q(actionId) & ", " & Q(pst) & ", " & Q(psb) & ", " &
                 QOrNull(ddlPlaceToStatus.SelectedValue) & ", " & QOrNull(ddlPlaceToSub.SelectedValue) & ", " &
                 CInt(Val(actionId)).ToString(CultureInfo.InvariantCulture) & ", 'Y')")

            ' Copy the action's rights from another status where it is placed
            If ddlCopyUsersFrom.SelectedValue <> "" Then
                CopyRights(actionId, ddlCopyUsersFrom.SelectedValue, RightsStates(pst))
                _allowedCache.Clear()
            End If

            pnlPlace.Visible = False
            ShowMessage(ddlPlaceAction.SelectedItem.Text & " is now available in " & PlaceName(pst, psb) & ".", True)
        Catch ex As Exception
            ShowMessage("Couldn't place the action: " & ex.Message, False)
            pnlPlace.Visible = True
        End Try
    End Sub

    ' ------------------------------------------------------------------
    ' Rights (security tables): who may use an action in a status
    ' ------------------------------------------------------------------
    '   UNITSHUB_SEC_USER_GROUPS    user -> groups
    '   UNITSHUB_SEC_GROUPS_ROLES   group -> role
    '   UNITSHUB_SEC_ROLES_ACTIONS  role allows action (PROJECT_ID + STATE_ID)
    '   UNITSHUB_SEC_GROUPS_ACTIONS group's own Allow 'A' / Deny 'D'
    '   UNITSHUB_SEC_USER_ACTIONS   user's own Allow 'A' / Deny 'D'
    ' A group is allowed when it has no Deny row and either an Allow row or its role
    ' allows it. A user is allowed when they have no Deny row and either an Allow row or
    ' an allowed group. Rights are per status: a sub-status placement uses its status's
    ' rights, and an "All statuses" placement uses every status of the project.

    ''' <summary>The statuses a placement's rights live in: its status, or all of them for '*'.</summary>
    Private Function RightsStates(PlacementStatus As String) As List(Of String)
        Dim result As New List(Of String)
        If PlacementStatus = "*" OrElse PlacementStatus = "" Then
            For Each st As DataRow In Statuses().Rows
                result.Add(Col(st, "STATE_ID"))
            Next
        Else
            result.Add(PlacementStatus)
        End If
        Return result
    End Function

    Private Function DenySql(RowAlias As String) As String
        Return "SUBSTR(UPPER(TRIM(" & RowAlias & ".ALLOW_DENY)), 1, 1) IN ('D', 'N', '0')"
    End Function

    ''' <summary>Rows of Table for this project/status/action (alias A), plus Extra.</summary>
    Private Function RightRowSql(Table As String, RowAlias As String, StateId As String, ActionId As String, Extra As String) As String
        Return "EXISTS (SELECT 1 FROM " & Table & " " & RowAlias &
               " WHERE " & RowAlias & ".PROJECT_ID = " & Q(ProjectId) &
               " AND " & RowAlias & ".STATE_ID = " & Q(StateId) &
               " AND " & RowAlias & ".ACTION_ID = " & Q(ActionId) & " AND " & Extra & ")"
    End Function

    ''' <summary>The role of group GroupExpr allows the action in the status.</summary>
    Private Function RoleGivesSql(GroupExpr As String, StateId As String, ActionId As String) As String
        Return "EXISTS (SELECT 1 FROM UNITSHUB_SEC_GROUPS_ROLES GR JOIN UNITSHUB_SEC_ROLES_ACTIONS RA " &
               "ON RA.ROLE_ID = GR.ROLE_ID AND RA.PROJECT_ID = " & Q(ProjectId) &
               " AND RA.STATE_ID = " & Q(StateId) & " AND RA.ACTION_ID = " & Q(ActionId) &
               " WHERE GR.GROUP_ID = " & GroupExpr & ")"
    End Function

    ''' <summary>Group GroupExpr is allowed the action in the status.</summary>
    Private Function GroupAllowedSql(GroupExpr As String, StateId As String, ActionId As String) As String
        Return "(NOT " & RightRowSql("UNITSHUB_SEC_GROUPS_ACTIONS", "GD", StateId, ActionId, "GD.GROUP_ID = " & GroupExpr & " AND " & DenySql("GD")) &
               " AND (" & RightRowSql("UNITSHUB_SEC_GROUPS_ACTIONS", "GA", StateId, ActionId, "GA.GROUP_ID = " & GroupExpr) &
               " OR " & RoleGivesSql(GroupExpr, StateId, ActionId) & "))"
    End Function

    ''' <summary>User UserExpr gets the action through at least one allowed group.</summary>
    Private Function UserViaGroupsSql(UserExpr As String, StateId As String, ActionId As String) As String
        Return "EXISTS (SELECT 1 FROM UNITSHUB_SEC_USER_GROUPS UG WHERE UG.USER_ID = " & UserExpr &
               " AND " & GroupAllowedSql("UG.GROUP_ID", StateId, ActionId) & ")"
    End Function

    ''' <summary>User UserExpr is allowed the action in the status.</summary>
    Private Function UserAllowedSql(UserExpr As String, StateId As String, ActionId As String) As String
        Return "(NOT " & RightRowSql("UNITSHUB_SEC_USER_ACTIONS", "UD", StateId, ActionId, "UD.USER_ID = " & UserExpr & " AND " & DenySql("UD")) &
               " AND (" & RightRowSql("UNITSHUB_SEC_USER_ACTIONS", "UA", StateId, ActionId, "UA.USER_ID = " & UserExpr) &
               " OR " & UserViaGroupsSql(UserExpr, StateId, ActionId) & "))"
    End Function

    ''' <summary>The first column of each row, trimmed, as a set.</summary>
    Private Function IdSet(SQL As String) As HashSet(Of String)
        Dim result As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        Dim DT As DataTable = GetDataTable(EBDB, SQL)
        If DT IsNot Nothing Then
            For Each r As DataRow In DT.Rows
                Dim v As String = Convert.ToString(r(0)).Trim()
                If v <> "" Then result.Add(v)
            Next
        End If
        Return result
    End Function

    ' Per-request cache of the allowed sets (cleared after every save)
    Private ReadOnly _allowedCache As New Dictionary(Of String, HashSet(Of String))

    Private Function Cached(Key As String, Loader As Func(Of HashSet(Of String))) As HashSet(Of String)
        Dim found As HashSet(Of String) = Nothing
        If Not _allowedCache.TryGetValue(Key, found) Then
            found = Loader()
            _allowedCache(Key) = found
        End If
        Return New HashSet(Of String)(found, StringComparer.OrdinalIgnoreCase)   ' a copy: callers may change it
    End Function

    Private Function AllowedUsers(StateId As String, ActionId As String) As HashSet(Of String)
        Return Cached("U|" & StateId & "|" & ActionId,
            Function() IdSet("SELECT U.USER_ID FROM UNITSHUB_USERS U WHERE " & UserAllowedSql("U.USER_ID", StateId, ActionId)))
    End Function

    Private Function UsersViaGroups(StateId As String, ActionId As String) As HashSet(Of String)
        Return IdSet("SELECT U.USER_ID FROM UNITSHUB_USERS U WHERE " & UserViaGroupsSql("U.USER_ID", StateId, ActionId))
    End Function

    Private Function AllowedGroups(StateId As String, ActionId As String) As HashSet(Of String)
        Return Cached("G|" & StateId & "|" & ActionId,
            Function() IdSet("SELECT G.GROUP_ID FROM UNITSHUB_SEC_GROUPS G WHERE " & GroupAllowedSql("G.GROUP_ID", StateId, ActionId)))
    End Function

    Private Function GroupsViaRole(StateId As String, ActionId As String) As HashSet(Of String)
        Return IdSet("SELECT G.GROUP_ID FROM UNITSHUB_SEC_GROUPS G WHERE " & RoleGivesSql("G.GROUP_ID", StateId, ActionId))
    End Function

    ''' <summary>IDs allowed in EVERY one of the statuses (one status = just that status).</summary>
    Private Function AllowedInAll(States As List(Of String), Finder As Func(Of String, HashSet(Of String))) As HashSet(Of String)
        Dim result As HashSet(Of String) = Nothing
        For Each s As String In States
            Dim one As HashSet(Of String) = Finder(s)
            If result Is Nothing Then result = one Else result.IntersectWith(one)
        Next
        Return If(result, New HashSet(Of String)(StringComparer.OrdinalIgnoreCase))
    End Function

    ''' <summary>"col IN (...)" for the IDs (chunks of 900 for Oracle's 1000 limit); "1 = 0" when empty.</summary>
    Private Function InListSql(Column As String, Ids As IEnumerable(Of String)) As String
        Dim all As List(Of String) = Ids.ToList()
        If all.Count = 0 Then Return "1 = 0"
        Dim parts As New List(Of String)
        For i As Integer = 0 To all.Count - 1 Step 900
            parts.Add(Column & " IN (" & String.Join(", ", all.Skip(i).Take(900).Select(Function(x) Q(x))) & ")")
        Next
        Return "(" & String.Join(" OR ", parts) & ")"
    End Function

    ''' <summary>Up to three name chips plus "+n"; Empty when there are none.</summary>
    Private Function Chips(Names As List(Of String), Empty As String) As String
        Dim sb As New System.Text.StringBuilder()
        For i As Integer = 0 To Math.Min(Names.Count, 3) - 1
            sb.Append("<span class=""chip"">" & Server.HtmlEncode(Names(i)) & "</span>")
        Next
        If Names.Count > 3 Then sb.Append("<span class=""chip"">+" & (Names.Count - 3).ToString() & "</span>")
        If Names.Count = 0 Then sb.Append("<span class=""chip"">" & Server.HtmlEncode(Empty) & "</span>")
        Return sb.ToString()
    End Function

    ''' <summary>Names (by ID) of the users / groups in the set, sorted.</summary>
    Private Function NamesOf(Table As String, IdColumn As String, NameColumn As String, Ids As HashSet(Of String)) As List(Of String)
        Dim result As New List(Of String)
        If Ids.Count = 0 Then Return result
        Dim DT As DataTable = GetDataTable(EBDB,
            "SELECT NVL(" & NameColumn & ", " & IdColumn & ") AS N FROM " & Table &
            " WHERE " & InListSql(IdColumn, Ids) & " ORDER BY 1")
        If DT IsNot Nothing Then
            For Each r As DataRow In DT.Rows
                result.Add(Convert.ToString(r("N")).Trim())
            Next
        End If
        Return result
    End Function

    ''' <summary>The IDs ticked in an AddMultipleItemsFromList popup ("ID" of each item).</summary>
    Private Function TickedIds(SelectedItems As List(Of Dictionary(Of String, Object))) As HashSet(Of String)
        Dim result As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        For Each item As Dictionary(Of String, Object) In SelectedItems
            Dim id As String = If(item.ContainsKey("ID"), Convert.ToString(item("ID")).Trim(), "")
            If id <> "" Then result.Add(id)
        Next
        Return result
    End Function

    ''' <summary>
    ''' Makes exactly the ticked users allowed the action in each status, with the fewest
    ''' rows in UNITSHUB_SEC_USER_ACTIONS: only users whose result changes are touched;
    ''' their own row is removed, then 'A' is added when no group gives it, or 'D' when a
    ''' group gives it but they were unticked. Returns how many users changed.
    ''' </summary>
    Private Function SaveUserRights(ActionId As String, States As List(Of String), Ticked As HashSet(Of String)) As Integer
        Dim changed As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        For Each s As String In States
            Dim allowedNow As HashSet(Of String) = AllowedUsers(s, ActionId)
            Dim viaGroups As HashSet(Of String) = UsersViaGroups(s, ActionId)
            Dim candidates As New HashSet(Of String)(allowedNow, StringComparer.OrdinalIgnoreCase)
            candidates.UnionWith(Ticked)

            For Each userId As String In candidates
                Dim wanted As Boolean = Ticked.Contains(userId)
                If wanted = allowedNow.Contains(userId) Then Continue For
                Exec("DELETE FROM UNITSHUB_SEC_USER_ACTIONS WHERE USER_ID = " & Q(userId) & " AND PROJECT_ID = " & Q(ProjectId) &
                     " AND STATE_ID = " & Q(s) & " AND ACTION_ID = " & Q(ActionId))
                Dim setting As String = ""
                If wanted AndAlso Not viaGroups.Contains(userId) Then setting = "A"
                If Not wanted AndAlso viaGroups.Contains(userId) Then setting = "D"
                If setting <> "" Then
                    Exec("INSERT INTO UNITSHUB_SEC_USER_ACTIONS (USER_ID, PROJECT_ID, STATE_ID, ACTION_ID, ALLOW_DENY) VALUES (" &
                         Q(userId) & ", " & Q(ProjectId) & ", " & Q(s) & ", " & Q(ActionId) & ", " & Q(setting) & ")")
                End If
                changed.Add(userId)
            Next
        Next
        _allowedCache.Clear()
        Return changed.Count
    End Function

    ''' <summary>Same as SaveUserRights for groups (UNITSHUB_SEC_GROUPS_ACTIONS; the role is the inherited part).</summary>
    Private Function SaveGroupRights(ActionId As String, States As List(Of String), Ticked As HashSet(Of String)) As Integer
        Dim changed As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        For Each s As String In States
            Dim allowedNow As HashSet(Of String) = AllowedGroups(s, ActionId)
            Dim viaRole As HashSet(Of String) = GroupsViaRole(s, ActionId)
            Dim candidates As New HashSet(Of String)(allowedNow, StringComparer.OrdinalIgnoreCase)
            candidates.UnionWith(Ticked)

            For Each groupId As String In candidates
                Dim wanted As Boolean = Ticked.Contains(groupId)
                If wanted = allowedNow.Contains(groupId) Then Continue For
                Exec("DELETE FROM UNITSHUB_SEC_GROUPS_ACTIONS WHERE GROUP_ID = " & Q(groupId) & " AND PROJECT_ID = " & Q(ProjectId) &
                     " AND STATE_ID = " & Q(s) & " AND ACTION_ID = " & Q(ActionId))
                Dim setting As String = ""
                If wanted AndAlso Not viaRole.Contains(groupId) Then setting = "A"
                If Not wanted AndAlso viaRole.Contains(groupId) Then setting = "D"
                If setting <> "" Then
                    Exec("INSERT INTO UNITSHUB_SEC_GROUPS_ACTIONS (GROUP_ID, PROJECT_ID, STATE_ID, ACTION_ID, ALLOW_DENY) VALUES (" &
                         Q(groupId) & ", " & Q(ProjectId) & ", " & Q(s) & ", " & Q(ActionId) & ", " & Q(setting) & ")")
                End If
                changed.Add(groupId)
            Next
        Next
        _allowedCache.Clear()
        Return changed.Count
    End Function

    ''' <summary>Where the rights of a placement live, for messages and popup titles.</summary>
    Private Function RightsPlaceName(PlacementStatus As String) As String
        Return If(PlacementStatus = "*" OrElse PlacementStatus = "", "every status", PlaceName(PlacementStatus, "*"))
    End Function

    ''' <summary>
    ''' Runs when the Users picker closes after Save: the ticked users become exactly the
    ''' users allowed this action in the placement's status (all statuses for '*').
    ''' </summary>
    Protected Sub btnUsers_Click(sender As Object, e As EventArgs)
        Dim selectedItems As List(Of Dictionary(Of String, Object)) =
            TryCast(VendorPopupHelper.GetPopupReturnValue(Me, "SelectedItems"),
                    List(Of Dictionary(Of String, Object)))
        If selectedItems Is Nothing Then Exit Sub    ' closed without saving

        Dim parts() As String = Convert.ToString(CType(sender, IButtonControl).CommandArgument).Split("|"c)
        If parts.Length < 3 Then Exit Sub
        Dim actionId As String = parts(0), pst As String = parts(1)

        Try
            Dim n As Integer = SaveUserRights(actionId, RightsStates(pst), TickedIds(selectedItems))
            ShowMessage("Users saved for " & RightsPlaceName(pst) & " (" & n & If(n = 1, " user", " users") & " changed).", True)
        Catch ex As Exception
            ShowMessage("Couldn't save the users: " & ex.Message, False)
        End Try
    End Sub

    ''' <summary>
    ''' Runs when the Groups picker closes after Save: the ticked groups become exactly the
    ''' groups allowed this action in the placement's status (all statuses for '*').
    ''' </summary>
    Protected Sub btnGroups_Click(sender As Object, e As EventArgs)
        Dim selectedItems As List(Of Dictionary(Of String, Object)) =
            TryCast(VendorPopupHelper.GetPopupReturnValue(Me, "SelectedItems"),
                    List(Of Dictionary(Of String, Object)))
        If selectedItems Is Nothing Then Exit Sub    ' closed without saving

        Dim parts() As String = Convert.ToString(CType(sender, IButtonControl).CommandArgument).Split("|"c)
        If parts.Length < 3 Then Exit Sub
        Dim actionId As String = parts(0), pst As String = parts(1)

        Try
            Dim n As Integer = SaveGroupRights(actionId, RightsStates(pst), TickedIds(selectedItems))
            ShowMessage("Groups saved for " & RightsPlaceName(pst) & " (" & n & If(n = 1, " group", " groups") & " changed).", True)
        Catch ex As Exception
            ShowMessage("Couldn't save the groups: " & ex.Message, False)
        End Try
    End Sub

    ' ------------------------------------------------------------------
    ' Popups: + Status (AddProjectStatus) and + New action (ActionAddEdit)
    ' ------------------------------------------------------------------

    ''' <summary>
    ''' + Status adds a status below the selected one (AddProjectStatus, Add State ▼), or
    ''' the project's first status (STATE_ID 0000) when it has none. + New action creates an
    ''' action and places it at the selected node. Both popups post back when closed, and
    ''' PreRender redraws the page.
    ''' </summary>
    Private Sub RegisterPopups()
        Dim statusCount As Integer = Statuses().Rows.Count
        Dim addStatusUrl As String
        If statusCount = 0 Then
            addStatusUrl = "AddProjectStatus.aspx?ProjectID=" & Server.UrlEncode(ProjectId) & "&StatusID=0000&MODE=FIRST"
        Else
            Dim refStatus As String = If(SelStatus = "*", Col(Statuses().Rows(statusCount - 1), "STATE_ID"), SelStatus)
            addStatusUrl = "AddProjectStatus.aspx?ProjectID=" & Server.UrlEncode(ProjectId) &
                           "&StatusID=" & Server.UrlEncode(refStatus) & "&MODE=NEW&Dir=Desc"
        End If
        VendorPopupHelper.RegisterVendorPopup(Me, btnAddStatus, addStatusUrl, 1000, 0,
                                              PopupPlacement.Center, "", VendorPopupHelper.PopupDisplayMode.Standard)

        VendorPopupHelper.RegisterVendorPopup(Me, btnNewAction,
            "ActionAddEdit.aspx?ProjectID=" & Server.UrlEncode(ProjectId) &
            "&StatusID=" & Server.UrlEncode(SelStatus) & "&SUBSTATEID=" & Server.UrlEncode(SelSub) & "&Mode=New",
            1100, 900, PopupPlacement.Center, "", VendorPopupHelper.PopupDisplayMode.Standard)

        RegisterStatusPopups()
    End Sub

    ''' <summary>Status buttons: Edit and Add state ▲/▼ open AddProjectStatus, as on the old page.</summary>
    Private Sub RegisterStatusPopups()
        If SelKind <> "S" Then Exit Sub
        Dim baseUrl As String = "AddProjectStatus.aspx?ProjectID=" & Server.UrlEncode(ProjectId) & "&StatusID=" & Server.UrlEncode(SelStatus)

        VendorPopupHelper.RegisterVendorPopup(Me, btnEditStatus, baseUrl & "&MODE=EDIT", 1000, 0,
                                              PopupPlacement.Center, "", VendorPopupHelper.PopupDisplayMode.Standard)
        If btnAddStateUp.Visible Then
            VendorPopupHelper.RegisterVendorPopup(Me, btnAddStateUp, baseUrl & "&MODE=NEW&Dir=Asc", 1000, 0,
                                                  PopupPlacement.Center, "", VendorPopupHelper.PopupDisplayMode.Standard)
        End If
        If btnAddStateDown.Visible Then
            VendorPopupHelper.RegisterVendorPopup(Me, btnAddStateDown, baseUrl & "&MODE=NEW&Dir=Desc", 1000, 0,
                                                  PopupPlacement.Center, "", VendorPopupHelper.PopupDisplayMode.Standard)
        End If
    End Sub

    Protected Sub btnNewAction_Click(sender As Object, e As EventArgs)
        ' The editor popup has closed - PreRender redraws the list
    End Sub

End Class
