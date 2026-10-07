Imports System.Collections.Generic
Imports System.Linq

Partial Class AutoTransferPlanForm
    Inherits System.Web.UI.Page

    ' ------------------------------------------------------------------
    ' In-memory draft model. Nothing here touches the database until
    ' btnSave_Click runs — the whole plan (plan + groups + details) is
    ' built up across postbacks in Session, then persisted in one shot.
    ' ------------------------------------------------------------------

    Private Class DetailEntry
        Public Property DetailId As String
        Public Property AccountDescription As String
        Public Property TypeOfEscrow As String
        Public Property AccountNumber As String
        Public Property Branch As String
        Public Property SystemName As String
        Public Property Debit As String
        Public Property Credit As String
        Public Property SortOrder As Integer
    End Class

    Private Class GroupEntry
        Public Property GroupId As String
        Public Property Label As String
        Public Property Title As String
        Public Property SortOrder As Integer
        Public Property HeaderOption As String      ' ddlGroupOption  -> UNITSHUB_ATP_GROUPS.IMPLEMENT_ON (PLAN_ID.DETAIL_ID)
        Public Property HeaderValue As String       ' txtGroupValue   -> UNITSHUB_ATP_GROUPS.REFERENCE_PHRASE
        Public Property Details As New List(Of DetailEntry)
    End Class

    Private Class PlanDraftModel
        Public Property Groups As New List(Of GroupEntry)
    End Class

    ''' <summary>
    ''' The PLAN_ID being edited, from ?PlanID=... on the query string. Empty means this is
    ''' a brand-new plan (Save will INSERT). Backed by ViewState (plain string, safe there)
    ''' so it survives postbacks without needing to re-read the query string each time.
    ''' </summary>
    Private Property CurrentPlanId As String
        Get
            Return CStr(If(ViewState("CurrentPlanId"), String.Empty))
        End Get
        Set(ByVal value As String)
            ViewState("CurrentPlanId") = value
        End Set
    End Property

    ''' <summary>
    ''' The working draft for this session. Using Session (not ViewState) since it holds
    ''' nested custom objects — same pattern already used elsewhere in this app for
    ''' caching a DataTable across postbacks (Session(SessionDataKey) = dt).
    ''' </summary>
    Private Property PlanDraft As PlanDraftModel
        Get
            Dim draft = TryCast(Session("AutoTransferPlanDraft"), PlanDraftModel)
            If draft Is Nothing Then
                draft = New PlanDraftModel()
                Session("AutoTransferPlanDraft") = draft
            End If
            Return draft
        End Get
        Set(ByVal value As PlanDraftModel)
            Session("AutoTransferPlanDraft") = value
        End Set
    End Property

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Load
        If Not IsPostBack Then
            LoadExistingPlans()

            ' Opened with ?PlanID=... -> straight into Edit for that plan; otherwise Add New
            Dim qsPlanId As String = ""
            Try
                qsPlanId = Convert.ToString(Request.QueryString("PlanID")).Trim()
            Catch ex As Exception
                qsPlanId = ""
            End Try

            If qsPlanId <> "" AndAlso ddlExistingPlan.Items.FindByValue(qsPlanId) IsNot Nothing Then
                rblMode.SelectedValue = "Edit"
                ddlExistingPlan.SelectedValue = qsPlanId
                OpenPlanForEdit(qsPlanId)
            Else
                rblMode.SelectedValue = "New"
                ResetEditor()
            End If
        End If

        ApplyGroupOrderFromHiddenField()
    End Sub

    ' ==================================================================
    ' Top panel: Add New / Edit Existing / Copy Existing
    ' ==================================================================

    ' Set when a handler replaces the draft, so PreRender doesn't copy the OLD plan's
    ' group dropdown / textbox values into the newly loaded one
    Private _draftReplaced As Boolean = False

    Private ReadOnly Property SelectedMode As String
        Get
            Dim mode As String = Convert.ToString(rblMode.SelectedValue)
            Return If(mode = "", "New", mode)
        End Get
    End Property

    ''' <summary>Fills "Existing plan" from UNITSHUB_AUTOTRANSFERPLAN.</summary>
    Private Sub LoadExistingPlans()
        ddlExistingPlan.Items.Clear()
        ddlExistingPlan.Items.Add(New ListItem("-- Select a plan --", ""))

        Dim DT As Data.DataTable = GetDataTable(EBDB, "SELECT PLAN_ID, NAME FROM UNITSHUB_AUTOTRANSFERPLAN ORDER BY PLAN_ID")
        If DT IsNot Nothing Then
            For Each r As Data.DataRow In DT.Rows
                Dim id As String = Convert.ToString(r("PLAN_ID"))
                ddlExistingPlan.Items.Add(New ListItem(Convert.ToString(r("NAME")) & " (" & id & ")", id))
            Next
        End If
    End Sub

    ''' <summary>Empty editor: a new plan, nothing loaded.</summary>
    Private Sub ResetEditor()
        CurrentPlanId = ""
        PlanDraft = New PlanDraftModel()
        txtPlanName.Text = ""
        hdnGroupOrder.Value = ""
        lblMessage.Text = ""
        _draftReplaced = True
    End Sub

    ''' <summary>Loads a saved plan into the editor; Save will update it.</summary>
    Private Sub OpenPlanForEdit(PlanId As String)
        ResetEditor()
        CurrentPlanId = PlanId
        LoadPlanForEdit(PlanId)
    End Sub

    Protected Sub rblMode_SelectedIndexChanged(ByVal sender As Object, ByVal e As EventArgs)
        ddlExistingPlan.SelectedIndex = 0
        txtCopyTitle.Text = ""
        lblModeMessage.Visible = False
        ResetEditor()
    End Sub

    Protected Sub ddlExistingPlan_SelectedIndexChanged(ByVal sender As Object, ByVal e As EventArgs)
        lblModeMessage.Visible = False
        Dim planId As String = ddlExistingPlan.SelectedValue

        If SelectedMode = "Edit" Then
            If planId = "" Then ResetEditor() Else OpenPlanForEdit(planId)
        ElseIf SelectedMode = "Copy" Then
            ' Suggest a title for the copy
            txtCopyTitle.Text = If(planId = "", "", "Copy of " & GetPlanName(planId))
        End If
    End Sub

    ''' <summary>
    ''' Copies the selected plan (its groups and detail rows) into a NEW plan named
    ''' "Plan Title", saves it right away, then opens the copy in Edit mode.
    ''' </summary>
    Protected Sub btnCopy_Click(ByVal sender As Object, ByVal e As EventArgs)
        Dim sourceId As String = ddlExistingPlan.SelectedValue
        Dim newTitle As String = txtCopyTitle.Text.Trim()

        If sourceId = "" Then
            ShowModeMessage("Select the plan to copy.")
            Return
        End If
        If newTitle = "" Then
            ShowModeMessage("Enter the Plan Title of the new plan.")
            Return
        End If
        If PlanNameExists(newTitle) Then
            ShowModeMessage("A plan named '" & newTitle & "' already exists. Choose another title.")
            Return
        End If

        Try
            ' Read the source into the draft, then save it as a brand-new plan:
            ' PersistPlan gives new PLAN_ID / GROUP_IDs / DETAIL_IDs, so the source is untouched
            ResetEditor()
            LoadPlanForEdit(sourceId)
            txtPlanName.Text = newTitle
            CurrentPlanId = ""
            Dim newId As String = PersistPlan()

            ' Show the copy, ready to edit
            LoadExistingPlans()
            rblMode.SelectedValue = "Edit"
            ddlExistingPlan.SelectedValue = newId
            txtCopyTitle.Text = ""
            OpenPlanForEdit(newId)

            lblMessage.CssClass = "msg-success"
            lblMessage.Text = "Plan '" & Server.HtmlEncode(newTitle) & "' copied from '" &
                              Server.HtmlEncode(GetPlanName(sourceId)) & "' (new ID " & newId & ")."
        Catch ex As Exception
            ShowModeMessage("The plan couldn't be copied: " & ex.Message)
        End Try
    End Sub

    Private Sub ShowModeMessage(Text As String)
        lblModeMessage.Text = Server.HtmlEncode(Text)
        lblModeMessage.Visible = True
    End Sub

    Private Function GetPlanName(PlanId As String) As String
        Dim DT As Data.DataTable = GetDataTable(EBDB,
            "SELECT NAME FROM UNITSHUB_AUTOTRANSFERPLAN WHERE PLAN_ID = '" & PlanId.Replace("'", "''") & "'")
        If DT Is Nothing OrElse DT.Rows.Count = 0 Then Return ""
        Return Convert.ToString(DT.Rows(0)("NAME"))
    End Function

    Private Function PlanNameExists(Name As String) As Boolean
        Dim DT As Data.DataTable = GetDataTable(EBDB,
            "SELECT PLAN_ID FROM UNITSHUB_AUTOTRANSFERPLAN WHERE UPPER(TRIM(NAME)) = '" & Name.Trim().ToUpperInvariant().Replace("'", "''") & "'")
        Return DT IsNot Nothing AndAlso DT.Rows.Count > 0
    End Function

    ''' <summary>
    ''' Shows what the chosen option needs: the plan list for Edit / Copy, the title and
    ''' Copy button for Copy, and the editor for Add New (or Edit once a plan is picked).
    ''' </summary>
    Private Sub ApplyModeUI()
        Dim mode As String = SelectedMode
        pnlPlanPicker.Visible = (mode = "Edit" OrElse mode = "Copy")
        phCopy.Visible = (mode = "Copy")
        pnlEditor.Visible = (mode = "New") OrElse (mode = "Edit" AndAlso CurrentPlanId <> "")

        Select Case mode
            Case "Edit" : lblFormTitle.Text = "Edit Auto-Transfer Plan"
            Case "Copy" : lblFormTitle.Text = "Copy Auto-Transfer Plan"
            Case Else : lblFormTitle.Text = "Add Auto-Transfer Plan"
        End Select
    End Sub

    ''' <summary>
    ''' Loads an existing plan (name, groups, and each group's detail rows) from the
    ''' database into the working draft, for editing.
    ''' </summary>
    Private Sub LoadPlanForEdit(ByVal planId As String)
        Dim planDT As New Data.DataTable
        planDT = GetDataTable(EBDB, "SELECT NAME FROM UNITSHUB_AUTOTRANSFERPLAN WHERE PLAN_ID = '" & planId.Replace("'", "''") & "'")

        If planDT.Rows.Count = 0 Then
            lblMessage.CssClass = "msg-success"
            lblMessage.Text = "No plan found for ID " & planId & "."
            PlanDraft = New PlanDraftModel()
            Return
        End If

        txtPlanName.Text = Convert.ToString(planDT.Rows(0)("NAME"))

        Dim draft As New PlanDraftModel()

        Dim groupsDT As New Data.DataTable
        groupsDT = GetDataTable(EBDB,
            "SELECT GROUP_ID, GROUP_TITLE, SORT_ORDER, IMPLEMENT_ON, REFERENCE_PHRASE FROM UNITSHUB_ATP_GROUPS " &
            "WHERE PLAN_ID = '" & planId.Replace("'", "''") & "' ORDER BY SORT_ORDER")

        For Each groupRow As Data.DataRow In groupsDT.Rows
            Dim group As New GroupEntry With {
                .GroupId = Convert.ToString(groupRow("GROUP_ID")),
                .Title = Convert.ToString(groupRow("GROUP_TITLE")),
                .SortOrder = Convert.ToInt32(groupRow("SORT_ORDER")),
                .HeaderOption = Convert.ToString(groupRow("IMPLEMENT_ON")),
                .HeaderValue = Convert.ToString(groupRow("REFERENCE_PHRASE"))
            }

            Dim detailsDT As New Data.DataTable
            detailsDT = GetDataTable(EBDB,
                "SELECT DETAIL_ID, ACCOUNT_DESCRIPTION, TYPE_OF_ESCROW, ACCOUNT_NUMBER, BRANCH, SYSTEM_NAME, " &
                "SORT_ORDER, DEBIT, CREDIT FROM UNITSHUB_ATP_DETAILS " &
                "WHERE GROUP_ID = '" & group.GroupId.Replace("'", "''") & "' ORDER BY SORT_ORDER")

            For Each detailRow As Data.DataRow In detailsDT.Rows
                group.Details.Add(New DetailEntry With {
                    .DetailId = Convert.ToString(detailRow("DETAIL_ID")),
                    .AccountDescription = Convert.ToString(detailRow("ACCOUNT_DESCRIPTION")),
                    .TypeOfEscrow = Convert.ToString(detailRow("TYPE_OF_ESCROW")),
                    .AccountNumber = Convert.ToString(detailRow("ACCOUNT_NUMBER")),
                    .Branch = Convert.ToString(detailRow("BRANCH")),
                    .SystemName = Convert.ToString(detailRow("SYSTEM_NAME")),
                    .SortOrder = Convert.ToInt32(detailRow("SORT_ORDER")),
                    .Debit = Convert.ToString(detailRow("DEBIT")),
                    .Credit = Convert.ToString(detailRow("CREDIT"))
                })
            Next

            draft.Groups.Add(group)
        Next

        PlanDraft = draft
    End Sub

    ''' <summary>
    ''' Rebinds the groups repeater. Deliberately called from PreRender (after all postback
    ''' events have run), not Page_Load — rebinding a data-bound Repeater recreates its
    ''' child controls, which would wipe out whatever the user just typed into a mini-form
    ''' textbox before an event handler gets a chance to read it via FindControl.
    ''' </summary>
    Protected Sub Page_PreRender(ByVal sender As Object, ByVal e As EventArgs) Handles Me.PreRender
        If IsPostBack AndAlso Not _draftReplaced Then CaptureGroupHeaderValues()
        BindGroups()
        ApplyModeUI()
    End Sub

    ''' <summary>
    ''' Copies each group header's dropdown / textbox into the draft before the repeater
    ''' is rebound (the rebind recreates those controls, so without this the user's
    ''' choices would be lost on every postback). Groups removed during this postback
    ''' are simply skipped.
    ''' </summary>
    Private Sub CaptureGroupHeaderValues()
        For Each item As RepeaterItem In rptGroups.Items
            If item.ItemType <> ListItemType.Item AndAlso item.ItemType <> ListItemType.AlternatingItem Then Continue For

            Dim hdnGroupId As HiddenField = TryCast(item.FindControl("hdnHeaderGroupId"), HiddenField)
            If hdnGroupId Is Nothing Then Continue For

            Dim group = PlanDraft.Groups.FirstOrDefault(Function(g) g.GroupId = hdnGroupId.Value)
            If group Is Nothing Then Continue For

            Dim ddl As DropDownList = TryCast(item.FindControl("ddlGroupOption"), DropDownList)
            Dim txt As TextBox = TryCast(item.FindControl("txtGroupValue"), TextBox)
            If ddl IsNot Nothing Then group.HeaderOption = ddl.SelectedValue
            If txt IsNot Nothing Then group.HeaderValue = txt.Text.Trim()
        Next
    End Sub

    Private Sub BindGroups()
        rptGroups.DataSource = PlanDraft.Groups.OrderBy(Function(g) g.SortOrder).ToList()
        rptGroups.DataBind()
        lblNoGroups.Visible = (PlanDraft.Groups.Count = 0)
    End Sub

    ''' <summary>
    ''' If the group cards were drag-reordered client-side, hdnGroupOrder carries the new
    ''' order as a comma-separated list of GroupIds — applied here, every postback.
    ''' </summary>
    Private Sub ApplyGroupOrderFromHiddenField()
        Dim orderCsv As String = hdnGroupOrder.Value

        If String.IsNullOrEmpty(orderCsv) Then
            Return
        End If

        Dim orderedIds As String() = orderCsv.Split(","c)

        For i As Integer = 0 To orderedIds.Length - 1
            Dim groupId As String = orderedIds(i)
            Dim group = PlanDraft.Groups.FirstOrDefault(Function(g) g.GroupId = groupId)
            If group IsNot Nothing Then
                group.SortOrder = i + 1
            End If
        Next
    End Sub

    Private Function NewTempId(ByVal prefix As String) As String
        Return prefix & Guid.NewGuid().ToString("N").Substring(0, 8)
    End Function

    Protected Sub btnAddGroup_Click(ByVal sender As Object, ByVal e As EventArgs)
        Dim title As String = txtGroupTitle.Text.Trim()

        If String.IsNullOrEmpty(title) Then
            Return
        End If

        PlanDraft.Groups.Add(New GroupEntry With {
            .GroupId = NewTempId("G"),
            .Label = txtGroupLabel.Text.Trim(),
            .Title = title,
            .SortOrder = PlanDraft.Groups.Count + 1
        })

        txtGroupLabel.Text = ""
        txtGroupTitle.Text = ""
    End Sub

    ''' <summary>
    ''' [X]: drops the unsaved draft and closes the popup (same call ContactMaintenance's
    ''' close button uses).
    ''' </summary>
    Protected Sub imgClose_Click(ByVal sender As Object, ByVal e As ImageClickEventArgs)
        Session("AutoTransferPlanDraft") = Nothing
        VendorPopupHelper.RegisterPopupSelectionAndClose(Me, False, skipPostBack:=False)
    End Sub

    Protected Sub btnCancel_Click(ByVal sender As Object, ByVal e As EventArgs)
        Session("AutoTransferPlanDraft") = Nothing
        Response.Redirect(Request.RawUrl)
    End Sub

    ' Payment-plan lines for the "implement on.." dropdowns, read once per request
    ' (every group card uses the same list)
    Private _implementOnOptions As Data.DataTable

    Private Function GetImplementOnOptions() As Data.DataTable
        If _implementOnOptions Is Nothing Then
            Dim SQL As String = ""
            SQL = SQL + vbCrLf + " SELECT P.PLAN_ID || '.' || D.DETAIL_ID AS ID, "
            SQL = SQL + vbCrLf + "        P.NAME || ' (' || D.DESCRIPTION || ': ' || "
            SQL = SQL + vbCrLf + "            CASE "
            SQL = SQL + vbCrLf + "                WHEN D.AMOUNT IS NOT NULL THEN TO_CHAR(D.AMOUNT) "
            SQL = SQL + vbCrLf + "                ELSE TO_CHAR(D.PERCENT) "
            SQL = SQL + vbCrLf + "            END "
            SQL = SQL + vbCrLf + "        || ')' AS NAME "
            SQL = SQL + vbCrLf + " FROM   UNITSHUB_PAYMENTPLAN P "
            SQL = SQL + vbCrLf + " INNER JOIN UNITSHUB_PAYMENTPLANDETAILS D ON P.PLAN_ID = D.PLAN_ID "
            SQL = SQL + vbCrLf + " ORDER BY P.PLAN_ID, D.DETAIL_ID "

            _implementOnOptions = GetDataTable(EBDB, SQL)
            If _implementOnOptions Is Nothing Then _implementOnOptions = New Data.DataTable()
        End If
        Return _implementOnOptions
    End Function

    Protected Sub rptGroups_ItemDataBound(ByVal sender As Object, ByVal e As RepeaterItemEventArgs)
        If e.Item.ItemType <> ListItemType.Item AndAlso e.Item.ItemType <> ListItemType.AlternatingItem Then
            Return
        End If

        Dim group As GroupEntry = CType(e.Item.DataItem, GroupEntry)
        Dim rptDetails As Repeater = CType(e.Item.FindControl("rptDetails"), Repeater)

        rptDetails.DataSource = group.Details.OrderBy(Function(d) d.SortOrder).ToList()
        rptDetails.DataBind()

        ' Restore the header dropdown / textbox from the draft
        Dim ddl As DropDownList = CType(e.Item.FindControl("ddlGroupOption"), DropDownList)
        Dim txt As TextBox = CType(e.Item.FindControl("txtGroupValue"), TextBox)

        ' Options: payment-plan lines (ID = PLAN_ID.DETAIL_ID), after "implement on.."
        ddl.Items.Clear()
        ddl.Items.Add(New ListItem("implement on..", ""))
        For Each optionRow As Data.DataRow In GetImplementOnOptions().Rows
            ddl.Items.Add(New ListItem(Convert.ToString(optionRow("NAME")), Convert.ToString(optionRow("ID"))))
        Next

        Dim savedOption As String = If(group.HeaderOption, "")
        If ddl.Items.FindByValue(savedOption) IsNot Nothing Then ddl.SelectedValue = savedOption
        txt.Text = If(group.HeaderValue, "")
    End Sub

    ''' <summary>
    ''' Handles RemoveGroup and AddDetail — both fire on the outer rptGroups, since
    ''' AddDetail's mini-form textboxes live inside the same outer ItemTemplate as the
    ''' group card. No confirmation on RemoveGroup — it (and its detail rows) is removed
    ''' immediately.
    ''' </summary>
    Protected Sub rptGroups_ItemCommand(ByVal source As Object, ByVal e As RepeaterCommandEventArgs)
        Dim groupId As String = e.CommandArgument.ToString()
        Dim group = PlanDraft.Groups.FirstOrDefault(Function(g) g.GroupId = groupId)
        If group Is Nothing Then
            Return
        End If

        Select Case e.CommandName
            Case "RemoveGroup"
                PlanDraft.Groups.Remove(group)

            Case "AddDetail"
                Dim txtAccountDescription As TextBox = CType(e.Item.FindControl("txtAccountDescription"), TextBox)
                Dim txtTypeOfEscrow As TextBox = CType(e.Item.FindControl("txtTypeOfEscrow"), TextBox)
                Dim txtAccountNumber As TextBox = CType(e.Item.FindControl("txtAccountNumber"), TextBox)
                Dim txtBranch As TextBox = CType(e.Item.FindControl("txtBranch"), TextBox)
                Dim txtSystemName As TextBox = CType(e.Item.FindControl("txtSystemName"), TextBox)
                Dim txtDebit As TextBox = CType(e.Item.FindControl("txtDebit"), TextBox)
                Dim txtCredit As TextBox = CType(e.Item.FindControl("txtCredit"), TextBox)

                If String.IsNullOrEmpty(txtAccountDescription.Text.Trim()) Then
                    Return
                End If

                group.Details.Add(New DetailEntry With {
                    .DetailId = NewTempId("D"),
                    .AccountDescription = txtAccountDescription.Text.Trim(),
                    .TypeOfEscrow = txtTypeOfEscrow.Text.Trim(),
                    .AccountNumber = txtAccountNumber.Text.Trim(),
                    .Branch = txtBranch.Text.Trim(),
                    .SystemName = txtSystemName.Text.Trim(),
                    .Debit = txtDebit.Text.Trim(),
                    .Credit = txtCredit.Text.Trim(),
                    .SortOrder = group.Details.Count + 1
                })
        End Select
    End Sub

    ''' <summary>Handles RemoveDetail, MoveDetailUp, MoveDetailDown on the nested rptDetails.</summary>
    Protected Sub rptDetails_ItemCommand(ByVal source As Object, ByVal e As RepeaterCommandEventArgs)
        Dim detailsRepeater As Repeater = CType(source, Repeater)
        Dim groupItem As RepeaterItem = CType(detailsRepeater.NamingContainer, RepeaterItem)
        Dim group As GroupEntry = CType(groupItem.DataItem, GroupEntry)
        If group Is Nothing Then
            Return
        End If

        Dim detailId As String = e.CommandArgument.ToString()
        Dim ordered = group.Details.OrderBy(Function(d) d.SortOrder).ToList()
        Dim index As Integer = ordered.FindIndex(Function(d) d.DetailId = detailId)
        If index = -1 Then
            Return
        End If

        Select Case e.CommandName
            Case "RemoveDetail"
                group.Details.RemoveAll(Function(d) d.DetailId = detailId)

            Case "MoveDetailUp"
                If index > 0 Then
                    Dim tmp As Integer = ordered(index - 1).SortOrder
                    ordered(index - 1).SortOrder = ordered(index).SortOrder
                    ordered(index).SortOrder = tmp
                End If

            Case "MoveDetailDown"
                If index < ordered.Count - 1 Then
                    Dim tmp As Integer = ordered(index + 1).SortOrder
                    ordered(index + 1).SortOrder = ordered(index).SortOrder
                    ordered(index).SortOrder = tmp
                End If
        End Select
    End Sub

    ''' <summary>
    ''' Persists the whole draft. New plan (CurrentPlanId empty) inserts a fresh
    ''' UNITSHUB_AUTOTRANSFERPLAN row; editing an existing plan updates that row in place.
    ''' Either way, groups/details are (re)written via InsertGroupsAndDetails — for an edit,
    ''' the existing children are deleted first and every group/detail is re-inserted fresh,
    ''' the same "replace entirely rather than diff" approach already used elsewhere in this
    ''' app (UNITSHUB_ACT_PAY_PLN_DETAILS) — simpler and safer than reconciling which rows
    ''' were added, removed, edited, or reordered.
    ''' </summary>
    ''' <remarks>
    ''' TODO: "ExecuteNonQuery" is a placeholder for whatever write helper your project
    ''' actually exposes (this codebase only gave me GetDataTable(conn, sql) for reads) —
    ''' swap it for your real INSERT/UPDATE/DELETE helper.
    ''' TODO: ID generation assumes VARCHAR2(5)-style zero-padded numeric ids, matching the
    ''' convention used elsewhere in this app — adjust if these new tables use something
    ''' different.
    ''' </remarks>
    Protected Sub btnSave_Click(ByVal sender As Object, ByVal e As EventArgs)
        If Not Page.IsValid Then
            Return
        End If

        ' The header dropdown / textbox values are normally copied into the draft in
        ' PreRender, which runs AFTER this handler - copy them now so the save has them.
        CaptureGroupHeaderValues()

        If PlanDraft.Groups.Count = 0 Then
            lblMessage.CssClass = "msg-success"
            lblMessage.Text = "Add at least one group before saving."
            Return
        End If

        Dim wasNewPlan As Boolean = String.IsNullOrEmpty(CurrentPlanId)
        Dim planName As String = txtPlanName.Text.Trim()
        Dim planId As String = PersistPlan()

        ' Re-open the saved plan in Edit mode (a new plan joins the "Existing plan" list
        ' first), so further changes update it and the IDs shown are the saved ones
        If wasNewPlan Then LoadExistingPlans()
        rblMode.SelectedValue = "Edit"
        ddlExistingPlan.SelectedValue = planId
        OpenPlanForEdit(planId)

        lblMessage.CssClass = "msg-success"
        lblMessage.Text = "Plan '" & Server.HtmlEncode(planName) & "' " &
            If(wasNewPlan, "saved", "updated") & " (ID " & planId & ")."
    End Sub

    ''' <summary>
    ''' Writes the draft to the database and returns its PLAN_ID: a new plan (CurrentPlanId
    ''' empty) is inserted, an existing one updated and its groups / details replaced.
    ''' Used by Save and by Copy.
    ''' </summary>
    Private Function PersistPlan() As String
        Dim planId As String
        Dim wasNewPlan As Boolean = String.IsNullOrEmpty(CurrentPlanId)

        If wasNewPlan Then
            Dim nextPlanIdDT As New Data.DataTable
            nextPlanIdDT = GetDataTable(EBDB, "SELECT LPAD(NVL(MAX(TO_NUMBER(PLAN_ID)), 0) + 1, 5, '0') AS NEXT_ID FROM UNITSHUB_AUTOTRANSFERPLAN")
            planId = nextPlanIdDT.Rows(0)("NEXT_ID").ToString()

            Dim insertPlanSql As String =
                "INSERT INTO UNITSHUB_AUTOTRANSFERPLAN (PLAN_ID, NAME) VALUES (" &
                "'" & planId & "', '" & txtPlanName.Text.Trim().Replace("'", "''") & "')"
            ExecuteNonQuery(EBDB, insertPlanSql)
        Else
            planId = CurrentPlanId

            Dim updatePlanSql As String =
                "UPDATE UNITSHUB_AUTOTRANSFERPLAN SET NAME = '" & txtPlanName.Text.Trim().Replace("'", "''") & "' " &
                "WHERE PLAN_ID = '" & planId.Replace("'", "''") & "'"
            ExecuteNonQuery(EBDB, updatePlanSql)

            Dim deleteDetailsSql As String =
                "DELETE FROM UNITSHUB_ATP_DETAILS WHERE GROUP_ID IN " &
                "(SELECT GROUP_ID FROM UNITSHUB_ATP_GROUPS WHERE PLAN_ID = '" & planId.Replace("'", "''") & "')"
            ExecuteNonQuery(EBDB, deleteDetailsSql)

            Dim deleteGroupsSql As String =
                "DELETE FROM UNITSHUB_ATP_GROUPS WHERE PLAN_ID = '" & planId.Replace("'", "''") & "'"
            ExecuteNonQuery(EBDB, deleteGroupsSql)
        End If

        InsertGroupsAndDetails(planId)

        CurrentPlanId = planId
        Return planId
    End Function

    ''' <summary>Inserts every group and its detail rows from the current draft, for planId.</summary>
    Private Sub InsertGroupsAndDetails(ByVal planId As String)
        Dim nextGroupIdDT As New Data.DataTable
        nextGroupIdDT = GetDataTable(EBDB, "SELECT NVL(MAX(TO_NUMBER(GROUP_ID)), 0) AS MAX_ID FROM UNITSHUB_ATP_GROUPS")
        Dim nextGroupIdNumber As Integer = CInt(nextGroupIdDT.Rows(0)("MAX_ID"))

        Dim nextDetailIdDT As New Data.DataTable
        nextDetailIdDT = GetDataTable(EBDB, "SELECT NVL(MAX(TO_NUMBER(DETAIL_ID)), 0) AS MAX_ID FROM UNITSHUB_ATP_DETAILS")
        Dim nextDetailIdNumber As Integer = CInt(nextDetailIdDT.Rows(0)("MAX_ID"))

        For Each group As GroupEntry In PlanDraft.Groups.OrderBy(Function(g) g.SortOrder)
            nextGroupIdNumber += 1
            Dim groupId As String = nextGroupIdNumber.ToString("00000")

            Dim insertGroupSql As String =
                "INSERT INTO UNITSHUB_ATP_GROUPS (GROUP_ID, PLAN_ID, GROUP_TITLE, SORT_ORDER, IMPLEMENT_ON, REFERENCE_PHRASE) VALUES (" &
                "'" & groupId & "', '" & planId & "', " &
                "'" & group.Title.Replace("'", "''") & "', " &
                group.SortOrder & ", " &
                (If(String.IsNullOrEmpty(group.HeaderOption), "NULL", "'" & group.HeaderOption.Replace("'", "''") & "'")) & ", " &
                (If(String.IsNullOrEmpty(group.HeaderValue), "NULL", "'" & group.HeaderValue.Replace("'", "''") & "'")) & ")"
            ExecuteNonQuery(EBDB, insertGroupSql)

            For Each detail As DetailEntry In group.Details.OrderBy(Function(d) d.SortOrder)
                nextDetailIdNumber += 1
                Dim detailId As String = nextDetailIdNumber.ToString("00000")

                Dim insertDetailSql As String =
                    "INSERT INTO UNITSHUB_ATP_DETAILS (DETAIL_ID, GROUP_ID, ACCOUNT_DESCRIPTION, " &
                    "TYPE_OF_ESCROW, ACCOUNT_NUMBER, BRANCH, SYSTEM_NAME, SORT_ORDER, DEBIT, CREDIT) VALUES (" &
                    "'" & detailId & "', '" & groupId & "', " &
                    "'" & detail.AccountDescription.Replace("'", "''") & "', " &
                    "'" & detail.TypeOfEscrow.Replace("'", "''") & "', " &
                    "'" & detail.AccountNumber.Replace("'", "''") & "', " &
                    "'" & detail.Branch.Replace("'", "''") & "', " &
                    "'" & detail.SystemName.Replace("'", "''") & "', " &
                    detail.SortOrder & ", " &
                    (If(String.IsNullOrEmpty(detail.Debit), "NULL", detail.Debit.Replace("'", "''"))) & ", " &
                    (If(String.IsNullOrEmpty(detail.Credit), "NULL", detail.Credit.Replace("'", "''"))) & ")"
                ExecuteNonQuery(EBDB, insertDetailSql)
            Next
        Next
    End Sub

End Class