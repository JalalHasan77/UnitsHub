Imports System.Data
Imports System.Globalization
Imports System.Collections.Generic

''' <summary>
''' One screen to set up a project's statuses, sub-statuses, where each action is valid
''' (placements) and which users may use it there.
'''   Left : status tree - "All statuses", each status, and its sub-statuses.
'''   Right: the selected node - its card colours, and the actions available there with
'''          where each comes from (this sub-status / all of the status / all statuses)
'''          and its users.
''' Tables:
'''   UNITSHUB_PROJECTSTATUS      statuses
'''   UNITSHUB_PROJECTSUBSTATUS   sub-statuses (SORT_ORDER = position under the status)
'''   UNITSHUB_ACTIONS            what an action is
'''   UNITSHUB_ACTION_PLACEMENTS  where it's valid ('*' = all) and its target from there
'''   UNITSHUB_PRJ_STS_ACTN_USRS  who may use it in each placement ('*' = all placements)
''' </summary>
Partial Class StatusSetup
    Inherits System.Web.UI.Page

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

    ''' <summary>Placement whose users are being edited: "ACTION_ID|STATUS_ID|SUBSTATE_ID".</summary>
    Private Property UsersPlacementKey As String
        Get
            Return Convert.ToString(ViewState("UsersPlacementKey"))
        End Get
        Set(value As String)
            ViewState("UsersPlacementKey") = value
        End Set
    End Property

    Private ReadOnly Property ProjectId As String
        Get
            Return Convert.ToString(ddlProject.SelectedValue)
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
            If requested <> "" AndAlso ddlProject.Items.FindByValue(requested) IsNot Nothing Then
                ddlProject.SelectedValue = requested
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
        If pnlUsers.Visible Then BindUsers()
        RegisterPopups()
    End Sub

    Private Sub LoadProjects()
        Dim DT As DataTable = GetDataTable(EBDB, "SELECT PROJECT_ID, PROJECT_NAME_EN FROM UNITSHUB_PROJECTS ORDER BY PROJECT_NAME_EN")
        ddlProject.DataSource = DT
        ddlProject.DataTextField = "PROJECT_NAME_EN"
        ddlProject.DataValueField = "PROJECT_ID"
        ddlProject.DataBind()
    End Sub

    Protected Sub ddlProject_SelectedIndexChanged(sender As Object, e As EventArgs)
        SelKey = "ALL"
        HidePanels()
    End Sub

    Private Sub HidePanels()
        pnlEdit.Visible = False
        pnlAddSub.Visible = False
        pnlPlace.Visible = False
        pnlUsers.Visible = False
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

        btnUp.Visible = isSub
        btnDown.Visible = isSub
        btnEdit.Visible = Not isAll
        pnlPreview.Visible = Not isAll
        btnShowAddSub.Enabled = Not isAll

        If isAll Then
            litCrumb.Text = Server.HtmlEncode(ddlProject.SelectedItem.Text)
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

    Protected Sub btnEdit_Click(sender As Object, e As EventArgs)
        HidePanels()
        Dim st As DataRow = StatusRow(SelStatus)
        If st Is Nothing Then Exit Sub

        Dim isSub As Boolean = (SelKind = "SS")
        Dim sb As DataRow = If(isSub, SubRow(SelStatus, SelSub), Nothing)
        Dim src As DataRow = If(isSub, sb, st)

        lblEditStatusName.Visible = Not isSub
        txtEditStatus.Visible = Not isSub
        txtEditStatus.Text = Col(st, "STATUS")
        txtEditSubtitle.Text = Col(src, "SUBTITLE")
        txtEditStatusBg.Text = ToColor(Col(src, "STATUS_BG_COLOR"), ToColor(Col(st, "STATUS_BG_COLOR"), "#e2e8f0"))
        txtEditStatusFg.Text = ToColor(Col(src, "STATUS_FG_COLOR"), ToColor(Col(st, "STATUS_FG_COLOR"), "#1e293b"))
        txtEditSubBg.Text = ToColor(Col(src, "SUBTITLE_BG_COLOR"), ToColor(Col(st, "SUBTITLE_BG_COLOR"), "#ffffff"))
        txtEditSubFg.Text = ToColor(Col(src, "SUBTITLE_FG_COLOR"), ToColor(Col(st, "SUBTITLE_FG_COLOR"), "#1e293b"))
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
            ElseIf SelKind = "S" Then
                If txtEditStatus.Text.Trim() = "" Then
                    ShowMessage("Enter a status name.", False)
                    Exit Sub
                End If
                Exec("UPDATE UNITSHUB_PROJECTSTATUS SET " &
                     "STATUS = " & Q(txtEditStatus.Text.Trim()) & ", SUBTITLE = " & QOrNull(txtEditSubtitle.Text.Trim()) & ", " &
                     "STATUS_BG_COLOR = " & Q(txtEditStatusBg.Text) & ", STATUS_FG_COLOR = " & Q(txtEditStatusFg.Text) & ", " &
                     "SUBTITLE_BG_COLOR = " & Q(txtEditSubBg.Text) & ", SUBTITLE_FG_COLOR = " & Q(txtEditSubFg.Text) & " " &
                     "WHERE PROJECT_ID = " & Q(ProjectId) & " AND STATE_ID = " & Q(SelStatus))
                ShowMessage("Status saved.", True)
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
        P.Columns.Add("UsersHtml") : P.Columns.Add("PlacementKey") : P.Columns.Add("IsExactlyHere", GetType(Boolean))

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

            ' Users who may use it here: rights for this placement or for '*'
            Dim users As DataTable = GetDataTable(EBDB,
                "SELECT DISTINCT USER_ID FROM UNITSHUB_PRJ_STS_ACTN_USRS " &
                "WHERE PROJECT_ID = " & Q(ProjectId) & " AND ACTION_ID = " & Q(Col(r, "ACTION_ID")) & " " &
                "AND STATUS_ID IN (" & Q(pst) & ", '*') AND NVL(SUBSTATE_ID, '*') IN (" & Q(psb) & ", '*') ORDER BY USER_ID")
            Dim chips As New System.Text.StringBuilder()
            Dim n As Integer = 0
            For Each u As DataRow In users.Rows
                n += 1
                If n <= 3 Then chips.Append("<span class=""chip"">" & Server.HtmlEncode(Col(u, "USER_ID")) & "</span>")
            Next
            If n > 3 Then chips.Append("<span class=""chip"">+" & (n - 3).ToString() & "</span>")
            If n = 0 Then chips.Append("<span class=""chip"">No users</span>")
            r("UsersHtml") = chips.ToString()

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

        Dim btnRemove As Button = CType(e.Item.FindControl("btnRemove"), Button)
        btnRemove.Visible = CBool(r("IsExactlyHere"))

        Dim btnEditAction As Button = CType(e.Item.FindControl("btnEditAction"), Button)
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
            Case "Users"
                HidePanels()
                UsersPlacementKey = Convert.ToString(e.CommandArgument)
                pnlUsers.Visible = True

            Case "Remove"
                Try
                    Exec("DELETE FROM UNITSHUB_PRJ_STS_ACTN_USRS WHERE PROJECT_ID = " & Q(ProjectId) &
                         " AND ACTION_ID = " & Q(actionId) & " AND STATUS_ID = " & Q(pst) & " AND NVL(SUBSTATE_ID, '*') = " & Q(psb))
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

    ''' <summary>"Copy users from" list: the chosen action's other placements.</summary>
    Private Sub FillCopyUsersFrom()
        ddlCopyUsersFrom.Items.Clear()
        ddlCopyUsersFrom.Items.Add(New ListItem("Don't copy users (set them afterwards)", ""))
        If ddlPlaceAction.SelectedValue = "" Then Exit Sub
        Dim DT As DataTable = GetDataTable(EBDB,
            "SELECT STATUS_ID, SUBSTATE_ID FROM UNITSHUB_ACTION_PLACEMENTS WHERE PROJECT_ID = " & Q(ProjectId) &
            " AND ACTION_ID = " & Q(ddlPlaceAction.SelectedValue) & " ORDER BY STATUS_ID, SUBSTATE_ID")
        For Each r As DataRow In DT.Rows
            ddlCopyUsersFrom.Items.Add(New ListItem("Copy users from " & PlaceName(Col(r, "STATUS_ID"), Col(r, "SUBSTATE_ID")),
                                                    Col(r, "STATUS_ID") & "|" & Col(r, "SUBSTATE_ID")))
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

            ' Copy the users of another placement of the same action
            If ddlCopyUsersFrom.SelectedValue <> "" Then
                Dim from() As String = ddlCopyUsersFrom.SelectedValue.Split("|"c)
                Exec("INSERT INTO UNITSHUB_PRJ_STS_ACTN_USRS (PROJECT_ID, STATUS_ID, SUBSTATE_ID, ACTION_ID, USER_ID) " &
                     "SELECT DISTINCT U.PROJECT_ID, " & Q(pst) & ", " & Q(psb) & ", U.ACTION_ID, U.USER_ID " &
                     "FROM UNITSHUB_PRJ_STS_ACTN_USRS U WHERE U.PROJECT_ID = " & Q(ProjectId) & " AND U.ACTION_ID = " & Q(actionId) &
                     " AND U.STATUS_ID = " & Q(from(0)) & " AND NVL(U.SUBSTATE_ID, '*') = " & Q(from(1)) &
                     " AND NOT EXISTS (SELECT 1 FROM UNITSHUB_PRJ_STS_ACTN_USRS X WHERE X.PROJECT_ID = U.PROJECT_ID " &
                     "AND X.ACTION_ID = U.ACTION_ID AND X.USER_ID = U.USER_ID AND X.STATUS_ID = " & Q(pst) &
                     " AND NVL(X.SUBSTATE_ID, '*') = " & Q(psb) & ")")
            End If

            pnlPlace.Visible = False
            ShowMessage(ddlPlaceAction.SelectedItem.Text & " is now available in " & PlaceName(pst, psb) & ".", True)
        Catch ex As Exception
            ShowMessage("Couldn't place the action: " & ex.Message, False)
            pnlPlace.Visible = True
        End Try
    End Sub

    ' ------------------------------------------------------------------
    ' Users of one placement
    ' ------------------------------------------------------------------

    Private Sub BindUsers()
        Dim parts() As String = UsersPlacementKey.Split("|"c)
        If parts.Length < 3 Then
            pnlUsers.Visible = False
            Exit Sub
        End If
        Dim actionId As String = parts(0), pst As String = parts(1), psb As String = parts(2)

        Dim title As String = Convert.ToString(DB.RetreiveScalarSTRING(EBDB,
            "SELECT ACTION_TITLE FROM UNITSHUB_ACTIONS WHERE PROJECT_ID = " & Q(ProjectId) & " AND ACTION_ID = " & Q(actionId)))
        litUsersFor.Text = Server.HtmlEncode("Users of """ & title & """ in " & PlaceName(pst, psb))

        ' Rights for exactly this placement (editable here)
        rptUsers.DataSource = GetDataTable(EBDB,
            "SELECT DISTINCT USER_ID FROM UNITSHUB_PRJ_STS_ACTN_USRS WHERE PROJECT_ID = " & Q(ProjectId) &
            " AND ACTION_ID = " & Q(actionId) & " AND STATUS_ID = " & Q(pst) & " AND NVL(SUBSTATE_ID, '*') = " & Q(psb) & " ORDER BY USER_ID")
        rptUsers.DataBind()

        ' Rights given for all places ('*'), shown for information
        Dim inherited As DataTable = GetDataTable(EBDB,
            "SELECT DISTINCT USER_ID FROM UNITSHUB_PRJ_STS_ACTN_USRS WHERE PROJECT_ID = " & Q(ProjectId) &
            " AND ACTION_ID = " & Q(actionId) & " AND (STATUS_ID = '*' OR NVL(SUBSTATE_ID, '*') = '*') " &
            " AND NOT (STATUS_ID = " & Q(pst) & " AND NVL(SUBSTATE_ID, '*') = " & Q(psb) & ") ORDER BY USER_ID")
        Dim names As New List(Of String)
        For Each r As DataRow In inherited.Rows
            names.Add(Col(r, "USER_ID"))
        Next
        litInheritedUsers.Text = If(names.Count = 0, "",
            Server.HtmlEncode("Also allowed through a wider right: " & String.Join(", ", names)))
    End Sub

    Protected Sub btnAddUser_Click(sender As Object, e As EventArgs)
        Dim parts() As String = UsersPlacementKey.Split("|"c)
        Dim userId As String = txtAddUser.Text.Trim()
        If parts.Length < 3 OrElse userId = "" Then
            ShowMessage("Enter a user ID.", False)
            Exit Sub
        End If

        Try
            Exec("INSERT INTO UNITSHUB_PRJ_STS_ACTN_USRS (PROJECT_ID, STATUS_ID, SUBSTATE_ID, ACTION_ID, USER_ID) " &
                 "SELECT " & Q(ProjectId) & ", " & Q(parts(1)) & ", " & Q(parts(2)) & ", " & Q(parts(0)) & ", " & Q(userId) & " FROM DUAL " &
                 "WHERE NOT EXISTS (SELECT 1 FROM UNITSHUB_PRJ_STS_ACTN_USRS WHERE PROJECT_ID = " & Q(ProjectId) &
                 " AND ACTION_ID = " & Q(parts(0)) & " AND USER_ID = " & Q(userId) &
                 " AND STATUS_ID = " & Q(parts(1)) & " AND NVL(SUBSTATE_ID, '*') = " & Q(parts(2)) & ")")
            txtAddUser.Text = ""
        Catch ex As Exception
            ShowMessage("Couldn't add the user: " & ex.Message, False)
        End Try
    End Sub

    Protected Sub rptUsers_ItemCommand(source As Object, e As RepeaterCommandEventArgs)
        If e.CommandName <> "RemoveUser" Then Exit Sub
        Dim parts() As String = UsersPlacementKey.Split("|"c)
        If parts.Length < 3 Then Exit Sub
        Try
            Exec("DELETE FROM UNITSHUB_PRJ_STS_ACTN_USRS WHERE PROJECT_ID = " & Q(ProjectId) &
                 " AND ACTION_ID = " & Q(parts(0)) & " AND USER_ID = " & Q(Convert.ToString(e.CommandArgument)) &
                 " AND STATUS_ID = " & Q(parts(1)) & " AND NVL(SUBSTATE_ID, '*') = " & Q(parts(2)))
        Catch ex As Exception
            ShowMessage("Couldn't remove the user: " & ex.Message, False)
        End Try
    End Sub

    Protected Sub btnUsersClose_Click(sender As Object, e As EventArgs)
        pnlUsers.Visible = False
        UsersPlacementKey = ""
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
    End Sub

    Protected Sub btnNewAction_Click(sender As Object, e As EventArgs)
        ' The editor popup has closed - PreRender redraws the list
    End Sub

End Class
