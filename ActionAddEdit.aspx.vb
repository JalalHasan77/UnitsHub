Imports System.Data
Imports System.DateTime
Imports System.Drawing
Imports System.Globalization
Imports System.Collections.Generic

Partial Class ActionAddEdit
    Inherits System.Web.UI.Page

    ' Simple model representing the fields on the form.
    Public Class ActionControlModel
        Public Property ProjectId As String
        Public Property StatusId As String
        Public Property IsActive As Boolean
        Public Property ActionTitle As String
        Public Property ActionType As String
        Public Property ImplementerTitle As String
        Public Property ShowInDefault As Boolean
        Public Property ShowInPreview As Boolean
        Public Property ReceiveParametersEnabled As Boolean
        Public Property ReceiveParametersMode As String
        Public Property NeedPayment As Boolean
        Public Property PaymentPlanIds As New List(Of String)
        Public Property ToStatusId As String
        Public Property Script As String
        Public Property PreExecution As String
        Public Property ConfirmationText As String
        Public Property ParameterType As String
        Public Property FormTitle As String
        Public Property SelectSQL As String
        Public Property NeedAutoTransfer As Boolean
        Public Property AutoTransferPlanId As String
        Public Property AutoTransferGroupId As String
        Public Property NeedDialogue As Boolean
        Public Property DialogueText As String
    End Class

    ''' <summary>
    ''' Fires when the user picks a different tab. Switches MultiView3 to match.
    ''' </summary>
    Protected Sub Menu3_MenuItemClick(sender As Object, e As MenuEventArgs) Handles Menu3.MenuItemClick
        Try
            MultiView3.ActiveViewIndex = Menu3.Items.IndexOf(Menu3.SelectedItem)

        Catch ex As Exception

        End Try
    End Sub

    ''' <summary>
    ''' The set of payment plans added so far via ddlPaymentPlan, in the order they were
    ''' added. Backed by ViewState so it survives postbacks (picking another plan, ticking
    ''' detail checkboxes, removing a plan, switching tabs) without earlier picks being lost.
    ''' </summary>
    Private Property SelectedPlanIds As List(Of String)
        Get
            Dim list = TryCast(ViewState("SelectedPlanIds"), List(Of String))
            If list Is Nothing Then
                list = New List(Of String)()
                ViewState("SelectedPlanIds") = list
            End If
            Return list
        End Get
        Set(ByVal value As List(Of String))
            ViewState("SelectedPlanIds") = value
        End Set
    End Property

    ''' <summary>
    ''' Which detail checkboxes are ticked, across every plan that's been added (not just
    ''' the one currently displayed), as "PLANID::DETAILID" entries. Backed by ViewState.
    ''' Only one plan's gvPlanDetails is ever bound in the DOM at a time, so this is what
    ''' remembers every other added plan's ticks while they're not on screen.
    ''' </summary>
    Private Property TickedDetailKeys As List(Of String)
        Get
            Dim list = TryCast(ViewState("TickedDetailKeys"), List(Of String))
            If list Is Nothing Then
                list = New List(Of String)()
                ViewState("TickedDetailKeys") = list
            End If
            Return list
        End Get
        Set(ByVal value As List(Of String))
            ViewState("TickedDetailKeys") = value
        End Set
    End Property

    ''' <summary>
    ''' Which single plan's details are currently displayed in gvPlanDetails. Backed by
    ''' ViewState. Only one plan's grid is shown at a time — switching plans (via the
    ''' dropdown or a chip) swaps what CurrentPlanId points to.
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
    ''' Snapshots which detail checkboxes are currently ticked for CurrentPlanId. Call this
    ''' BEFORE switching to a different plan or rebinding gvPlanDetails, while the controls
    ''' still reflect this postback's actual state.
    ''' </summary>
    Private Sub CaptureTickedDetails()
        If String.IsNullOrEmpty(CurrentPlanId) Then
            Return
        End If

        ' Replace this plan's previously captured ticks with whatever's actually ticked now.
        TickedDetailKeys.RemoveAll(Function(k) k.StartsWith(CurrentPlanId & "::"))

        For Each detailRow As GridViewRow In gvPlanDetails.Rows
            If detailRow.RowType <> DataControlRowType.DataRow Then
                Continue For
            End If

            Dim chk As CheckBox = CType(detailRow.FindControl("chkSelectDetail"), CheckBox)

            If chk IsNot Nothing AndAlso chk.Checked Then
                Dim detailId As String = gvPlanDetails.DataKeys(detailRow.RowIndex).Value.ToString()
                TickedDetailKeys.Add(CurrentPlanId & "::" & detailId)
            End If
        Next
    End Sub

    ''' <summary>
    ''' Re-ticks whichever detail checkboxes were previously captured for CurrentPlanId.
    ''' Call this AFTER rebinding gvPlanDetails to CurrentPlanId.
    ''' </summary>
    Private Sub ReapplyTickedDetails()
        If String.IsNullOrEmpty(CurrentPlanId) Then
            Return
        End If

        For Each detailRow As GridViewRow In gvPlanDetails.Rows
            If detailRow.RowType <> DataControlRowType.DataRow Then
                Continue For
            End If

            Dim chk As CheckBox = CType(detailRow.FindControl("chkSelectDetail"), CheckBox)
            If chk Is Nothing Then
                Continue For
            End If

            Dim detailId As String = gvPlanDetails.DataKeys(detailRow.RowIndex).Value.ToString()
            chk.Checked = TickedDetailKeys.Contains(CurrentPlanId & "::" & detailId)
        Next
    End Sub

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Load
        If Not IsPostBack Then
            Menu3.Items(0).Selected = True
            ' Only override the markup's placeholder text (e.g. "001" / "0000") when the query
            ' string actually supplies a value — otherwise Request.QueryString(...) returns
            ' Nothing and silently blanks out lblProjectID/lblSTATUSID.
            If Not String.IsNullOrEmpty(Request.QueryString("ProjectID")) Then
                lblProjectID.Text = Request.QueryString("ProjectID")
            End If

            If Not String.IsNullOrEmpty(Request.QueryString("StatusID")) Then
                lblSTATUSID.Text = Request.QueryString("StatusID")
            End If

            If Not String.IsNullOrEmpty(Request.QueryString("StatusID")) Then
                lblActionID.Text = Request.QueryString("ActionID")
            End If

            If Not String.IsNullOrEmpty(Request.QueryString("StatusID")) Then
                lblMode.Text = Request.QueryString("Mode")
            End If
            'lblMode.Text = "Edit"
            'lblSTATUSID.Text = "0000"
            'lblActionID.Text = "00001"
            LoadPaymentPlans()
            LoadAutoTransferPlans()
            LoadStatuses()
            ' TODO: replace with a real record load when editing an existing action.
            LoadDefaults()
            If lblMode.Text.ToLower = "Edit".ToLower Then
                LoadActionControl(ProjectID:=lblProjectID.Text, StatusID:=lblSTATUSID.Text, ActionID:=lblActionID.Text)
            Else


            End If


        End If

        UpdateNeedPaymentVisibility()
        UpdateAutoTransferVisibility()
        UpdateOpenADialogueVisibility()
        UpdatePreExecutionVisibility()
    End Sub

    ''' <summary>
    ''' Shows/hides the Confirmation Text row, the Parameter Type row, and (depending on the
    ''' selected Parameter Type) the Form Title / Select SQL rows. Mirrors the client-side
    ''' togglePreExecutionVisibility() / toggleParameterTypeVisibility() scripts. Fully
    ''' self-contained within the Pre-Execution tab.
    ''' </summary>
    Private Sub UpdatePreExecutionVisibility()
        rowConfirmationText.Style("display") = If(rblPreExecution.SelectedValue = "Confirmation", "", "none")

        Dim parameterRowVisible As Boolean = (rblPreExecution.SelectedValue = "Parameter")
        rowParameterType.Style("display") = If(parameterRowVisible, "", "none")

        Dim parameterType As String = ddlParameterType.SelectedValue

        Dim showFormTitle As Boolean = parameterRowVisible AndAlso
            (parameterType = "JUSTIFICATION" OrElse parameterType = "RESTRICTION_WINDOW" OrElse
             parameterType = "RESTRICTION_NO_WINDOW" OrElse parameterType = "RESTRICTION_JUSTIFICATION")

        Dim showSelectSql As Boolean = parameterRowVisible AndAlso
            (parameterType = "RESTRICTION_WINDOW" OrElse parameterType = "RESTRICTION_NO_WINDOW" OrElse
             parameterType = "RESTRICTION_JUSTIFICATION")

        rowFormTitle.Style("display") = If(showFormTitle, "", "none")
        rowSelectSQL.Style("display") = If(showSelectSql, "", "none")
    End Sub

    ''' <summary>
    ''' Shows/hides the Add Payment Plan row (and the chips / plan-details rows beneath it)
    ''' based on the Need Payment checkbox. Mirrors the client-side togglePaymentPlanVisibility()
    ''' script. NOTE: unlike the original DesignAction page, this no longer also depends on
    ''' Action Type ("Change Status") — Need Payment now lives on its own Payments tab, away
    ''' from Action Type on the General tab, so that cross-tab coupling no longer makes sense
    ''' (Action Type's control isn't even rendered while the Payments tab is active). The Need
    ''' Payment checkbox itself is therefore always visible; only what's below it is still
    ''' gated on whether it's checked.
    ''' </summary>
    Private Sub UpdateNeedPaymentVisibility()
        Dim paymentPlanRowVisible As Boolean = chkNeedPayment.Checked
        rowPaymentPlan.Style("display") = If(paymentPlanRowVisible, "", "none")

        Dim anyPlanSelected As Boolean = (SelectedPlanIds.Count > 0)
        rowAddedPlans.Style("display") = If(paymentPlanRowVisible AndAlso anyPlanSelected, "", "none")

        Dim planDetailsRowVisible As Boolean = (paymentPlanRowVisible AndAlso Not String.IsNullOrEmpty(CurrentPlanId))
        rowPlanDetails.Style("display") = If(planDetailsRowVisible, "", "none")
    End Sub

    ''' <summary>
    ''' Enables/disables txtOpenADialogueText based on the Open A Dialogue checkbox.
    ''' Mirrors the client-side toggleOpenADialogueVisibility() script; called on every
    ''' Page_Load (initial and postback) so the server-side Enabled state always matches
    ''' the checkbox's current (possibly just-restored-from-ViewState) value.
    ''' </summary>
    Private Sub UpdateOpenADialogueVisibility()
        txtOpenADialogueText.Enabled = chkOpenADialogue.Checked
    End Sub

    ''' <summary>
    ''' Populates ddlPaymentPlan from UNITSHUB_PAYMENTPLAN. Only called on the initial load
    ''' (not on postback) so selections persist via ViewState instead of being re-bound.
    ''' </summary>
    Private Sub LoadPaymentPlans()
        Dim DT As New Data.DataTable
        DT = GetDataTable(EBDB, "Select PLAN_ID, NAME from UNITSHUB_PAYMENTPLAN")

        ddlPaymentPlan.Items.Clear()
        ddlPaymentPlan.DataSource = DT
        ddlPaymentPlan.DataTextField = "NAME"
        ddlPaymentPlan.DataValueField = "PLAN_ID"
        ddlPaymentPlan.DataBind()

        ddlPaymentPlan.Items.Insert(0, New ListItem("Select Payment Plan", ""))
    End Sub

    ''' <summary>
    ''' Shows/hides the AutoTransfer groups block and enables/disables ddlAutoTransferPlan,
    ''' based on the Need AutoTransfer checkbox (and, for the groups block, whether a plan
    ''' is actually selected). Mirrors the client-side toggleAutoTransferVisibility() script
    ''' and the Payments tab's UpdateNeedPaymentVisibility().
    ''' </summary>
    Private Sub UpdateAutoTransferVisibility()
        ddlAutoTransferPlan.Enabled = chkNeedAutoTransfer.Checked
        ddlAutoTransferGroup.Enabled = chkNeedAutoTransfer.Checked AndAlso Not String.IsNullOrEmpty(ddlAutoTransferPlan.SelectedValue)

        Dim groupsVisible As Boolean = chkNeedAutoTransfer.Checked AndAlso Not String.IsNullOrEmpty(ddlAutoTransferPlan.SelectedValue)
        rowAutoTransferGroups.Style("display") = If(groupsVisible, "", "none")
    End Sub

    ''' <summary>
    ''' Populates ddlAutoTransferPlan from UNITSHUB_AUTOTRANSFERPLAN. Only called on the
    ''' initial load (not on postback) so the selection persists via ViewState instead of
    ''' being re-bound. Mirrors LoadPaymentPlans().
    ''' </summary>
    Private Sub LoadAutoTransferPlans()
        Dim DT As New Data.DataTable
        DT = GetDataTable(EBDB, "Select PLAN_ID, NAME from UNITSHUB_AUTOTRANSFERPLAN")

        ddlAutoTransferPlan.Items.Clear()
        ddlAutoTransferPlan.DataSource = DT
        ddlAutoTransferPlan.DataTextField = "NAME"
        ddlAutoTransferPlan.DataValueField = "PLAN_ID"
        ddlAutoTransferPlan.DataBind()

        ddlAutoTransferPlan.Items.Insert(0, New ListItem("Select Plan", ""))
    End Sub

    ''' <summary>
    ''' Fires when Need AutoTransfer is ticked/unticked. When unticked, the picked plan and
    ''' any loaded groups are cleared so a stale grid isn't left bound if the box is
    ''' re-ticked later with a different plan in mind.
    ''' </summary>
    Protected Sub chkNeedAutoTransfer_CheckedChanged(ByVal sender As Object, ByVal e As EventArgs)
        If Not chkNeedAutoTransfer.Checked Then
            ddlAutoTransferPlan.ClearSelection()
            ResetAutoTransferGroupList()
            rptAutoTransferGroups.DataSource = Nothing
            rptAutoTransferGroups.DataBind()
            lblNoAutoTransferGroups.Visible = False
        End If

        UpdateAutoTransferVisibility()
    End Sub

    ''' <summary>
    ''' Fires when a plan is picked from ddlAutoTransferPlan. Fills ddlAutoTransferGroup with
    ''' that plan's groups (starting at "Select Group") and clears the grid until a group
    ''' is picked.
    ''' </summary>
    Protected Sub ddlAutoTransferPlan_SelectedIndexChanged(ByVal sender As Object, ByVal e As EventArgs)
        LoadAutoTransferGroups()
        UpdateAutoTransferVisibility()
    End Sub

    ''' <summary>
    ''' Fires when a group is picked from ddlAutoTransferGroup: shows that group's card
    ''' with its UNITSHUB_ATP_DETAILS lines (or nothing for "Select Group").
    ''' </summary>
    Protected Sub ddlAutoTransferGroup_SelectedIndexChanged(ByVal sender As Object, ByVal e As EventArgs)
        BindSelectedAutoTransferGroup()
        UpdateAutoTransferVisibility()
    End Sub

    ''' <summary>Empties ddlAutoTransferGroup back to just "Select Group".</summary>
    Private Sub ResetAutoTransferGroupList()
        ddlAutoTransferGroup.Items.Clear()
        ddlAutoTransferGroup.Items.Add(New ListItem("Select Group", ""))
    End Sub

    ''' <summary>
    ''' Fills ddlAutoTransferGroup with the UNITSHUB_ATP_GROUPS of the selected AutoTransfer
    ''' plan ("Select Group" first), then shows the selected group's card - none right after
    ''' a plan change, since the list starts at "Select Group".
    ''' </summary>
    ''' <remarks>
    ''' UNITSHUB_ATP_GROUPS: PLAN_ID, GROUP_ID, GROUP_TITLE, SORT_ORDER.
    ''' UNITSHUB_ATP_DETAILS: DETAIL_ID, GROUP_ID, ACCOUNT_DESCRIPTION, TYPE_OF_ESCROW,
    ''' ACCOUNT_NUMBER, BRANCH, SYSTEM_NAME, SORT_ORDER, DEBIT, CREDIT.
    ''' </remarks>
    Private Sub LoadAutoTransferGroups()
        Dim planId As String = ddlAutoTransferPlan.SelectedValue

        ResetAutoTransferGroupList()

        If String.IsNullOrEmpty(planId) Then
            rptAutoTransferGroups.DataSource = Nothing
            rptAutoTransferGroups.DataBind()
            lblNoAutoTransferGroups.Visible = False
            Return
        End If

        Dim DT As New Data.DataTable
        DT = GetDataTable(EBDB, "Select GROUP_ID, GROUP_TITLE from UNITSHUB_ATP_GROUPS where PLAN_ID = '" & planId.Replace("'", "''") & "' Order By SORT_ORDER, GROUP_ID")

        If DT IsNot Nothing Then
            For Each groupRow As Data.DataRow In DT.Rows
                ddlAutoTransferGroup.Items.Add(New ListItem(groupRow("GROUP_TITLE").ToString(), groupRow("GROUP_ID").ToString()))
            Next
        End If

        lblNoAutoTransferGroups.Visible = (DT Is Nothing OrElse DT.Rows.Count = 0)

        BindSelectedAutoTransferGroup()
    End Sub

    ''' <summary>
    ''' Binds rptAutoTransferGroups with just the group selected in ddlAutoTransferGroup;
    ''' its lines are loaded into the card's GridView by rptAutoTransferGroups_ItemDataBound.
    ''' Nothing is shown while "Select Group" is selected.
    ''' </summary>
    Private Sub BindSelectedAutoTransferGroup()
        Dim groupId As String = ddlAutoTransferGroup.SelectedValue

        If String.IsNullOrEmpty(groupId) OrElse String.IsNullOrEmpty(ddlAutoTransferPlan.SelectedValue) Then
            rptAutoTransferGroups.DataSource = Nothing
            rptAutoTransferGroups.DataBind()
            Return
        End If

        Dim DT As New Data.DataTable
        DT = GetDataTable(EBDB, "Select GROUP_ID, GROUP_TITLE from UNITSHUB_ATP_GROUPS where PLAN_ID = '" & ddlAutoTransferPlan.SelectedValue.Replace("'", "''") & "' and GROUP_ID = '" & groupId.Replace("'", "''") & "'")

        rptAutoTransferGroups.DataSource = DT
        rptAutoTransferGroups.DataBind()
    End Sub

    ''' <summary>
    ''' For each group card bound into rptAutoTransferGroups, loads that group's own lines
    ''' from UNITSHUB_ATP_DETAILS into its nested gvGroupDetails GridView.
    ''' </summary>
    Protected Sub rptAutoTransferGroups_ItemDataBound(ByVal sender As Object, ByVal e As RepeaterItemEventArgs)
        If e.Item.ItemType <> ListItemType.Item AndAlso e.Item.ItemType <> ListItemType.AlternatingItem Then
            Return
        End If

        Dim groupRow As Data.DataRowView = TryCast(e.Item.DataItem, Data.DataRowView)
        If groupRow Is Nothing Then
            Return
        End If

        Dim groupId As String = groupRow("GROUP_ID").ToString()

        Dim gv As GridView = TryCast(e.Item.FindControl("gvGroupDetails"), GridView)
        If gv Is Nothing Then
            Return
        End If

        Dim DT As New Data.DataTable
        DT = GetDataTable(EBDB, "Select DETAIL_ID, ACCOUNT_DESCRIPTION, TYPE_OF_ESCROW, ACCOUNT_NUMBER, BRANCH, SYSTEM_NAME, DEBIT, CREDIT from UNITSHUB_ATP_DETAILS where GROUP_ID = '" & groupId.Replace("'", "''") & "' Order By SORT_ORDER, DETAIL_ID")

        gv.DataSource = DT
        gv.DataBind()
    End Sub

    ''' <summary>
    ''' Populates ddlToStatus from UNITSHUB_UNITSSTATUS. Only called on the initial load
    ''' (not on postback) so the selection persists via ViewState instead of being re-bound.
    ''' </summary>
    Private Sub LoadStatuses()
        Dim DT As New Data.DataTable
        Dim SQL As String = "Select STATE_ID, CASE WHEN SUBTITLE IS NOT NULL OR TRIM(SUBTITLE) <> '' THEN STATUS || ': ' || SUBTITLE ELSE STATUS END AS STATUS from UNITSHUB_PROJECTSTATUS where PROJECT_ID = '" & lblProjectID.Text & "'"
        DT = GetDataTable(EBDB, SQL)

        ddlToStatus.Items.Clear()
        ddlToStatus.DataSource = DT
        ddlToStatus.DataTextField = "STATUS"
        ddlToStatus.DataValueField = "STATE_ID"
        ddlToStatus.DataBind()

        ddlToStatus.Items.Insert(0, New ListItem("To Status", ""))
    End Sub

    ''' <summary>
    ''' Fires when a payment plan is picked from the dropdown. Adds it to the accumulated
    ''' set (if not already present), makes it the currently-displayed plan, and resets the
    ''' dropdown back to its placeholder so it's ready to add another plan.
    ''' </summary>
    Protected Sub ddlPaymentPlan_SelectedIndexChanged(ByVal sender As Object, ByVal e As EventArgs)
        CaptureTickedDetails()

        Dim planId As String = ddlPaymentPlan.SelectedValue

        If Not String.IsNullOrEmpty(planId) Then
            If Not SelectedPlanIds.Contains(planId) Then
                SelectedPlanIds.Add(planId)
            End If
            CurrentPlanId = planId
        End If

        LoadAddedPlansChips()
        LoadCurrentPlanDetails()
        ReapplyTickedDetails()

        ddlPaymentPlan.ClearSelection()
        UpdateNeedPaymentVisibility()
    End Sub

    ''' <summary>
    ''' Fires when a chip's name is clicked (switch to viewing that plan) or its × is
    ''' clicked (remove that plan entirely). CommandArgument carries the plan's PLAN_ID.
    ''' </summary>
    Protected Sub rptAddedPlans_ItemCommand(ByVal source As Object, ByVal e As RepeaterCommandEventArgs)
        Dim planId As String = e.CommandArgument.ToString()

        CaptureTickedDetails()

        If e.CommandName = "Remove" Then
            SelectedPlanIds.Remove(planId)
            TickedDetailKeys.RemoveAll(Function(k) k.StartsWith(planId & "::"))

            If CurrentPlanId = planId Then
                ' The removed plan was the one being viewed — fall back to whichever plan
                ' was added most recently, or none if that was the last one.
                CurrentPlanId = If(SelectedPlanIds.Count > 0, SelectedPlanIds(SelectedPlanIds.Count - 1), "")
            End If
        ElseIf e.CommandName = "View" Then
            CurrentPlanId = planId
        End If

        LoadAddedPlansChips()
        LoadCurrentPlanDetails()
        ReapplyTickedDetails()

        UpdateNeedPaymentVisibility()
    End Sub

    ''' <summary>Binds rptAddedPlans to SelectedPlanIds (the "chips" row).</summary>
    Private Sub LoadAddedPlansChips()
        Dim DT As New Data.DataTable
        DT.Columns.Add("PLAN_ID", GetType(String))
        DT.Columns.Add("NAME", GetType(String))

        For Each planId As String In SelectedPlanIds
            Dim item As ListItem = ddlPaymentPlan.Items.FindByValue(planId)
            Dim planName As String = If(item IsNot Nothing, item.Text, planId)
            DT.Rows.Add(planId, planName)
        Next

        rptAddedPlans.DataSource = DT
        rptAddedPlans.DataBind()
    End Sub

    ''' <summary>
    ''' Loads UNITSHUB_PAYMENTPLANDETAILS for CurrentPlanId into the single gvPlanDetails
    ''' grid, and updates its title label. Clears both if no plan is currently selected.
    ''' </summary>
    Private Sub LoadCurrentPlanDetails()
        If String.IsNullOrEmpty(CurrentPlanId) Then
            lblCurrentPlanName.Text = ""
            gvPlanDetails.DataSource = Nothing
            gvPlanDetails.DataBind()
            Return
        End If

        Dim item As ListItem = ddlPaymentPlan.Items.FindByValue(CurrentPlanId)
        lblCurrentPlanName.Text = If(item IsNot Nothing, item.Text, CurrentPlanId)

        Dim DT As New Data.DataTable
        DT = GetDataTable(EBDB, "Select * from UNITSHUB_PAYMENTPLANDETAILS where PLAN_ID = '" & CurrentPlanId.Replace("'", "''") & "'")

        gvPlanDetails.DataSource = DT
        gvPlanDetails.DataBind()
    End Sub

    ''' <summary>
    ''' Sets the initial/default state of the controls, matching the mock-up.
    ''' </summary>
    Private Sub LoadDefaults()
        rblActionStatus.SelectedValue = "Active"
        ddlActionType.SelectedValue = "Delete"
        cblShowIn.Items.FindByValue("Default").Selected = True
        rblReceiveParameters.SelectedValue = "No"
        rblPreExecution.SelectedValue = "None"
    End Sub

    Protected Sub btnSave_Click(ByVal sender As Object, ByVal e As EventArgs)
        If Not Page.IsValid Then
            Return
        End If

        CaptureTickedDetails()

        Dim model As ActionControlModel = BuildModelFromForm()

        If String.Equals(lblMode.Text, "Edit", StringComparison.OrdinalIgnoreCase) Then
            UpdateActionControl(model)
            lblMessage.Text = "Action '" & model.ActionTitle & "' updated successfully."
        Else
            SaveActionControl(model)
            lblMessage.Text = "Action '" & model.ActionTitle & "' saved successfully."
        End If
        lblMessage.Visible = True

        VendorPopupHelper.RegisterPopupSelectionAndClose(Me, True, skipPostBack:=False)

    End Sub

    Protected Sub btnCancel_Click(ByVal sender As Object, ByVal e As EventArgs)
        VendorPopupHelper.RegisterPopupSelectionAndClose(Me, False, skipPostBack:=False)
    End Sub

    Protected Sub btnLoad_Click(ByVal sender As Object, ByVal e As EventArgs)
        'LoadActionControl()
    End Sub

    ''' <summary>
    ''' Loads an existing action (and its previously-ticked Payment Plan Details) back into
    ''' the form, from UNITSHUB_ACTIONS / UNITSHUB_ACT_PAY_PLN_DETAILS, for
    ''' PROJECT_ID = lblProjectID.Text, STATUS_ID = lblSTATUSID.Text, ACTION_ID = '00001'.
    ''' </summary>
    ''' <remarks>
    ''' TODO: ACTION_ID is hardcoded to "00001" per the current requirement — swap this for
    ''' whichever action the user is actually meant to be loading (e.g. a query-string
    ''' parameter, or a row picked from a list) once that's available.
    ''' </remarks>
    Private Sub LoadActionControl(ByVal ProjectID As String, ByVal StatusID As String, ByVal ActionID As String)


        Dim actionDT As New Data.DataTable
        Dim actionSql As String =
            "SELECT * FROM UNITSHUB_ACTIONS WHERE PROJECT_ID = '" & ProjectID.Replace("'", "''") & "' " &
            "AND STATUS_ID = '" & StatusID.Replace("'", "''") & "' AND ACTION_ID = '" & ActionID & "'"
        actionDT = GetDataTable(EBDB, actionSql)

        If actionDT.Rows.Count = 0 Then
            lblMessage.Text = "No saved action found for this Project / Status / Action ID."
            lblMessage.Visible = True
            Return
        End If

        Dim row As Data.DataRow = actionDT.Rows(0)

        rblActionStatus.SelectedValue = If(SafeBool(row("IS_ACTIVE")), "Active", "InActive")
        txtActionTitle.Text = SafeString(row("ACTION_TITLE"))
        SafeSetSelectedValue(ddlActionType, SafeString(row("ACTION_TYPE")))
        txtImplementerTitle.Text = SafeString(row("IMPLEMENTER_TITLE"))

        chkOpenADialogue.Checked = SafeBool(row("NEED_DIALOGUE"))
        txtOpenADialogueText.Text = SafeString(row("DIALOGUE_TEXT"))


        cblShowIn.Items.FindByValue("Default").Selected = SafeBool(row("SHOW_IN_DEFAULT"))
        cblShowIn.Items.FindByValue("Preview").Selected = SafeBool(row("SHOW_IN_PREVIEW"))

        chkReceiveParameters.Checked = SafeBool(row("RECEIVE_PARAMETERS_ENABLED"))
        SafeSetSelectedValue(rblReceiveParameters, SafeString(row("RECEIVE_PARAMETERS_MODE")))

        chkNeedPayment.Checked = SafeBool(row("NEED_PAYMENT"))
        ' PAYMENT_PLAN_ID on the parent row is no longer used now that an action can have
        ' multiple payment plans — see the plan-reloading block below instead, which reads
        ' the actual set of plans from UNITSHUB_ACT_PAY_PLN_DETAILS.

        chkNeedAutoTransfer.Checked = SafeBool(row("NEED_AUTOTRANSFER"))
        ' AUTOTRANSFER_PLAN_ID is restored below, once chkNeedAutoTransfer is known, so the
        ' groups/details grid can be reloaded for that plan in the same step.

        SafeSetSelectedValue(ddlToStatus, SafeString(row("TO_STATUS_ID")))

        txtScript.Text = SafeString(row("SCRIPT_TEXT"))
        SafeSetSelectedValue(rblPreExecution, SafeString(row("PRE_EXECUTION")))
        txtConfirmationText.Text = SafeString(row("CONFIRMATION_TEXT"))
        SafeSetSelectedValue(ddlParameterType, SafeString(row("PARAMETER_TYPE")))
        txtFormTitle.Text = SafeString(row("FORM_TITLE"))
        txtSelectSQL.Text = SafeString(row("SELECT_SQL"))

        ' Re-check whichever payment plans were previously associated with this action,
        ' remember which specific line items were ticked per plan, then show the first
        ' plan's grid (the others stay remembered and reachable via their chips).
        If chkNeedPayment.Checked Then
            Dim planIdsDT As New Data.DataTable
            Dim planIdsSql As String =
                "SELECT DISTINCT PLAN_ID FROM UNITSHUB_ACT_PAY_PLN_DETAILS WHERE PROJECT_ID = '" & ProjectID.Replace("'", "''") & "' " &
                "AND STATUS_ID = '" & StatusID.Replace("'", "''") & "' AND ACTION_ID = '" & ActionID & "' AND PLAN_ID IS NOT NULL"
            planIdsDT = GetDataTable(EBDB, planIdsSql)

            Dim loadedPlanIds As New List(Of String)
            For Each planIdRow As Data.DataRow In planIdsDT.Rows
                loadedPlanIds.Add(planIdRow("PLAN_ID").ToString())
            Next
            SelectedPlanIds = loadedPlanIds

            Dim detailsDT As New Data.DataTable
            Dim detailsSql As String =
                "SELECT PLAN_ID, DETAIL_ID FROM UNITSHUB_ACT_PAY_PLN_DETAILS WHERE PROJECT_ID = '" & ProjectID.Replace("'", "''") & "' " &
                "AND STATUS_ID = '" & StatusID.Replace("'", "''") & "' AND ACTION_ID = '" & ActionID & "' AND DETAIL_ID IS NOT NULL"
            detailsDT = GetDataTable(EBDB, detailsSql)

            Dim loadedTicked As New List(Of String)
            For Each detailRow As Data.DataRow In detailsDT.Rows
                loadedTicked.Add(detailRow("PLAN_ID").ToString() & "::" & detailRow("DETAIL_ID").ToString())
            Next
            TickedDetailKeys = loadedTicked

            CurrentPlanId = If(SelectedPlanIds.Count > 0, SelectedPlanIds(0), "")

            ' Reflect that same "first designated plan" choice in the dropdown itself -
            ' otherwise it would sit on the "Select Payment Plan" placeholder while the
            ' chips/grid below already show a plan as selected.
            SafeSetSelectedValue(ddlPaymentPlan, CurrentPlanId)

            LoadAddedPlansChips()
            LoadCurrentPlanDetails()
            ReapplyTickedDetails()
        Else
            SelectedPlanIds = New List(Of String)
            TickedDetailKeys = New List(Of String)
            CurrentPlanId = ""
            rptAddedPlans.DataSource = Nothing
            rptAddedPlans.DataBind()
            gvPlanDetails.DataSource = Nothing
            gvPlanDetails.DataBind()
        End If

        ' Re-select the previously chosen AutoTransfer plan and reload its groups/details
        ' (from UNITSHUB_ATP_GROUPS / UNITSHUB_ATP_DETAILS) via the same code path the
        ' dropdown's SelectedIndexChanged uses.
        If chkNeedAutoTransfer.Checked Then
            SafeSetSelectedValue(ddlAutoTransferPlan, SafeString(row("AUTOTRANSFER_PLAN_ID")))
            LoadAutoTransferGroups()

            ' Re-select the saved group (AUTOTRANSFER_GROUP) and show its card
            If row.Table.Columns.Contains("AUTOTRANSFER_GROUP") Then
                SafeSetSelectedValue(ddlAutoTransferGroup, SafeString(row("AUTOTRANSFER_GROUP")))
                BindSelectedAutoTransferGroup()
            End If
        Else
            ddlAutoTransferPlan.ClearSelection()
            ResetAutoTransferGroupList()
            rptAutoTransferGroups.DataSource = Nothing
            rptAutoTransferGroups.DataBind()
            lblNoAutoTransferGroups.Visible = False
        End If

        UpdateNeedPaymentVisibility()
        UpdateAutoTransferVisibility()
        UpdateOpenADialogueVisibility()
        UpdatePreExecutionVisibility()

        lblMessage.Text = "Action '" & txtActionTitle.Text & "' loaded."
        lblMessage.Visible = True
    End Sub

    ''' <summary>Returns "" for Nothing/DBNull instead of throwing.</summary>
    Private Function SafeString(ByVal value As Object) As String
        If value Is Nothing OrElse value Is DBNull.Value Then
            Return String.Empty
        End If
        Return value.ToString()
    End Function

    ''' <summary>Treats NUMBER(1,0) flag columns (1/0) as True/False.</summary>
    Private Function SafeBool(ByVal value As Object) As Boolean
        Return SafeString(value) = "1"
    End Function

    ''' <summary>
    ''' Sets SelectedValue only if that value actually exists in the list — setting
    ''' SelectedValue to a value with no matching ListItem throws.
    ''' </summary>
    Private Sub SafeSetSelectedValue(ByVal list As ListControl, ByVal value As String)
        If Not String.IsNullOrEmpty(value) AndAlso list.Items.FindByValue(value) IsNot Nothing Then
            list.SelectedValue = value
        End If
    End Sub

    ''' <summary>
    ''' Reads all form controls into a plain model object.
    ''' </summary>
    Private Function BuildModelFromForm() As ActionControlModel
        Dim model As New ActionControlModel()

        model.ProjectId = lblProjectID.Text.Trim()
        model.StatusId = lblSTATUSID.Text.Trim()

        model.IsActive = (rblActionStatus.SelectedValue = "Active")
        model.ActionTitle = txtActionTitle.Text.Trim()
        model.ActionType = ddlActionType.SelectedValue
        model.ImplementerTitle = txtImplementerTitle.Text.Trim()

        model.NeedDialogue = chkOpenADialogue.Checked
        model.DialogueText = If(model.NeedDialogue, txtOpenADialogueText.Text.Trim(), Nothing)

        ' NOTE: preserved as-is from DesignAction - To Status is only saved when Action
        ' Type = "Change Status", even though the To Status field itself is now always
        ' visible on the General tab (rather than only appearing for that Action Type).
        model.ToStatusId = If(model.ActionType = "CHANGE", ddlToStatus.SelectedValue, Nothing)

        model.ShowInDefault = cblShowIn.Items.FindByValue("Default").Selected
        model.ShowInPreview = cblShowIn.Items.FindByValue("Preview").Selected

        model.ReceiveParametersEnabled = chkReceiveParameters.Checked
        model.ReceiveParametersMode = rblReceiveParameters.SelectedValue
        model.NeedPayment = chkNeedPayment.Checked
        model.PaymentPlanIds = If(model.NeedPayment, New List(Of String)(SelectedPlanIds), New List(Of String)())

        model.NeedAutoTransfer = chkNeedAutoTransfer.Checked
        model.AutoTransferPlanId = If(model.NeedAutoTransfer AndAlso Not String.IsNullOrEmpty(ddlAutoTransferPlan.SelectedValue), ddlAutoTransferPlan.SelectedValue, Nothing)
        ' Group only counts when a plan is saved too ("Select Group" = no group)
        model.AutoTransferGroupId = If(Not String.IsNullOrEmpty(model.AutoTransferPlanId) AndAlso Not String.IsNullOrEmpty(ddlAutoTransferGroup.SelectedValue), ddlAutoTransferGroup.SelectedValue, Nothing)

        model.Script = txtScript.Text
        model.PreExecution = rblPreExecution.SelectedValue
        model.ConfirmationText = If(model.PreExecution = "Confirmation", txtConfirmationText.Text.Trim(), Nothing)

        If model.PreExecution = "Parameter" Then
            model.ParameterType = ddlParameterType.SelectedValue

            Dim formTitleApplies As Boolean =
                (model.ParameterType = "JUSTIFICATION" OrElse model.ParameterType = "RESTRICTION_WINDOW" OrElse
                 model.ParameterType = "RESTRICTION_NO_WINDOW" OrElse model.ParameterType = "RESTRICTION_JUSTIFICATION")

            Dim selectSqlApplies As Boolean =
                (model.ParameterType = "RESTRICTION_WINDOW" OrElse model.ParameterType = "RESTRICTION_NO_WINDOW" OrElse
                 model.ParameterType = "RESTRICTION_JUSTIFICATION")

            model.FormTitle = If(formTitleApplies, txtFormTitle.Text.Trim(), Nothing)
            model.SelectSQL = If(selectSqlApplies, txtSelectSQL.Text.Trim(), Nothing)
        End If

        Return model
    End Function

    ''' <summary>
    ''' Persists the form: one row in UNITSHUB_ACTIONS, plus one row in
    ''' UNITSHUB_ACT_PAY_PLN_DETAILS for every ticked checkbox across every selected
    ''' payment plan. A plan with no ticked details still gets one row (DETAIL_ID = NULL)
    ''' so the plan association itself isn't lost.
    ''' </summary>
    ''' <remarks>
    ''' TODO: "ExecuteNonQuery" below is a placeholder for whatever write helper your
    ''' project actually exposes (this codebase only gave me GetDataTable(conn, sql) for
    ''' reads) — swap it for your real INSERT/UPDATE/DELETE helper.
    ''' TODO: ACTION_ID / ID are VARCHAR2(5 BYTE), not DB-generated identities, so I generate
    ''' zero-padded numeric strings (e.g. "00001") via MAX(TO_NUMBER(...))+1. If your app has
    ''' an existing ID-generation routine/sequence for these tables, use that instead.
    ''' NOTE: UNITSHUB_ACTIONS.PAYMENT_PLAN_ID is a single column and can't hold multiple
    ''' plan ids, so it's now always saved as NULL — the actual set of plans for this
    ''' action lives entirely in UNITSHUB_ACT_PAY_PLN_DETAILS (one row per plan, or per
    ''' plan+detail combination).
    ''' </remarks>
    Private Sub SaveActionControl(ByVal model As ActionControlModel)
        ' 1. Generate the new ACTION_ID.
        Dim nextActionIdDT As New Data.DataTable
        nextActionIdDT = GetDataTable(EBDB, "SELECT LPAD(NVL(MAX(TO_NUMBER(ACTION_ID)), 0) + 1, 5, '0') AS NEXT_ID FROM UNITSHUB_ACTIONS")
        Dim actionId As String = nextActionIdDT.Rows(0)("NEXT_ID").ToString()

        ' 2. Insert the parent Action row.
        Dim insertActionSql As String =
            "INSERT INTO UNITSHUB_ACTIONS (PROJECT_ID, STATUS_ID, ACTION_ID, IS_ACTIVE, ACTION_TITLE, ACTION_TYPE, " &
            "IMPLEMENTER_TITLE, SHOW_IN_DEFAULT, SHOW_IN_PREVIEW, RECEIVE_PARAMETERS_ENABLED, RECEIVE_PARAMETERS_MODE, " &
            "NEED_PAYMENT, PAYMENT_PLAN_ID, NEED_AUTOTRANSFER, AUTOTRANSFER_PLAN_ID, AUTOTRANSFER_GROUP, NEED_DIALOGUE, DIALOGUE_TEXT, " &
            "TO_STATUS_ID, SCRIPT_TEXT, PRE_EXECUTION, CONFIRMATION_TEXT, " &
            "PARAMETER_TYPE, FORM_TITLE, SELECT_SQL) VALUES (" &
            "'" & lblProjectID.Text.Replace("'", "''") & "', " &
            "'" & lblSTATUSID.Text.Replace("'", "''") & "', " &
            "'" & actionId & "', " &
            (If(model.IsActive, "1", "0")) & ", " &
            "'" & model.ActionTitle.Replace("'", "''") & "', " &
            "'" & model.ActionType & "', " &
            "'" & If(model.ImplementerTitle, "").Replace("'", "''") & "', " &
            (If(model.ShowInDefault, "1", "0")) & ", " &
            (If(model.ShowInPreview, "1", "0")) & ", " &
            (If(model.ReceiveParametersEnabled, "1", "0")) & ", " &
            "'" & If(model.ReceiveParametersMode, "") & "', " &
            (If(model.NeedPayment, "1", "0")) & ", " &
            "NULL, " &
            (If(model.NeedAutoTransfer, "1", "0")) & ", " &
            (If(String.IsNullOrEmpty(model.AutoTransferPlanId), "NULL", "'" & model.AutoTransferPlanId.Replace("'", "''") & "'")) & ", " &
            (If(String.IsNullOrEmpty(model.AutoTransferGroupId), "NULL", "'" & model.AutoTransferGroupId.Replace("'", "''") & "'")) & ", " &
            (If(model.NeedDialogue, "1", "0")) & ", " &
            (If(String.IsNullOrEmpty(model.DialogueText), "NULL", "'" & model.DialogueText.Replace("'", "''") & "'")) & ", " &
            (If(String.IsNullOrEmpty(model.ToStatusId), "NULL", "'" & model.ToStatusId & "'")) & ", " &
            "'" & If(model.Script, "").Replace("'", "''") & "', " &
            "'" & model.PreExecution & "', " &
            (If(String.IsNullOrEmpty(model.ConfirmationText), "NULL", "'" & model.ConfirmationText.Replace("'", "''") & "'")) & ", " &
            (If(String.IsNullOrEmpty(model.ParameterType), "NULL", "'" & model.ParameterType & "'")) & ", " &
            (If(String.IsNullOrEmpty(model.FormTitle), "NULL", "'" & model.FormTitle.Replace("'", "''") & "'")) & ", " &
            (If(String.IsNullOrEmpty(model.SelectSQL), "NULL", "'" & model.SelectSQL.Replace("'", "''") & "'")) &
            ")"

        ExecuteNonQuery(EBDB, insertActionSql)

        ' 3. Insert child detail rows for the newly generated action.
        SavePlanDetailRows(model, actionId)
    End Sub

    ''' <summary>
    ''' Updates the existing UNITSHUB_ACTIONS row that LoadActionControl loaded (identified
    ''' by lblProjectID/lblSTATUSID/lblActionID), and replaces its
    ''' UNITSHUB_ACT_PAY_PLN_DETAILS rows with the current selection.
    ''' </summary>
    ''' <remarks>
    ''' The existing plan/detail rows are deleted and re-inserted from scratch rather than
    ''' diffed — simpler and safer than trying to reconcile which specific rows changed.
    ''' </remarks>
    Private Sub UpdateActionControl(ByVal model As ActionControlModel)
        Dim actionId As String = lblActionID.Text.Trim()

        Dim updateActionSql As String =
            "UPDATE UNITSHUB_ACTIONS SET " &
            "IS_ACTIVE = " & (If(model.IsActive, "1", "0")) & ", " &
            "ACTION_TITLE = '" & model.ActionTitle.Replace("'", "''") & "', " &
            "ACTION_TYPE = '" & model.ActionType & "', " &
            "IMPLEMENTER_TITLE = '" & If(model.ImplementerTitle, "").Replace("'", "''") & "', " &
            "SHOW_IN_DEFAULT = " & (If(model.ShowInDefault, "1", "0")) & ", " &
            "SHOW_IN_PREVIEW = " & (If(model.ShowInPreview, "1", "0")) & ", " &
            "RECEIVE_PARAMETERS_ENABLED = " & (If(model.ReceiveParametersEnabled, "1", "0")) & ", " &
            "RECEIVE_PARAMETERS_MODE = '" & If(model.ReceiveParametersMode, "") & "', " &
            "NEED_PAYMENT = " & (If(model.NeedPayment, "1", "0")) & ", " &
            "PAYMENT_PLAN_ID = NULL, " &
            "NEED_AUTOTRANSFER = " & (If(model.NeedAutoTransfer, "1", "0")) & ", " &
            "AUTOTRANSFER_PLAN_ID = " & (If(String.IsNullOrEmpty(model.AutoTransferPlanId), "NULL", "'" & model.AutoTransferPlanId.Replace("'", "''") & "'")) & ", " &
            "AUTOTRANSFER_GROUP = " & (If(String.IsNullOrEmpty(model.AutoTransferGroupId), "NULL", "'" & model.AutoTransferGroupId.Replace("'", "''") & "'")) & ", " &
            "NEED_DIALOGUE = " & (If(model.NeedDialogue, "1", "0")) & ", " &
            "DIALOGUE_TEXT = " & (If(String.IsNullOrEmpty(model.DialogueText), "NULL", "'" & model.DialogueText.Replace("'", "''") & "'")) & ", " &
            "TO_STATUS_ID = " & (If(String.IsNullOrEmpty(model.ToStatusId), "NULL", "'" & model.ToStatusId & "'")) & ", " &
            "SCRIPT_TEXT = '" & If(model.Script, "").Replace("'", "''") & "', " &
            "PRE_EXECUTION = '" & model.PreExecution & "', " &
            "CONFIRMATION_TEXT = " & (If(String.IsNullOrEmpty(model.ConfirmationText), "NULL", "'" & model.ConfirmationText.Replace("'", "''") & "'")) & ", " &
            "PARAMETER_TYPE = " & (If(String.IsNullOrEmpty(model.ParameterType), "NULL", "'" & model.ParameterType & "'")) & ", " &
            "FORM_TITLE = " & (If(String.IsNullOrEmpty(model.FormTitle), "NULL", "'" & model.FormTitle.Replace("'", "''") & "'")) & ", " &
            "SELECT_SQL = " & (If(String.IsNullOrEmpty(model.SelectSQL), "NULL", "'" & model.SelectSQL.Replace("'", "''") & "'")) & " " &
            "WHERE PROJECT_ID = '" & model.ProjectId.Replace("'", "''") & "' " &
            "AND STATUS_ID = '" & model.StatusId.Replace("'", "''") & "' " &
            "AND ACTION_ID = '" & actionId & "'"

        ExecuteNonQuery(EBDB, updateActionSql)

        Dim deleteDetailsSql As String =
            "DELETE FROM UNITSHUB_ACT_PAY_PLN_DETAILS WHERE PROJECT_ID = '" & model.ProjectId.Replace("'", "''") & "' " &
            "AND STATUS_ID = '" & model.StatusId.Replace("'", "''") & "' AND ACTION_ID = '" & actionId & "'"
        ExecuteNonQuery(EBDB, deleteDetailsSql)

        SavePlanDetailRows(model, actionId)
    End Sub

    ''' <summary>
    ''' Inserts one child row per ticked detail (from TickedDetailKeys), across every
    ''' selected payment plan, for the given ACTION_ID. A plan with no ticked details still
    ''' gets one row (DETAIL_ID = NULL) so the plan association itself isn't lost. Shared by
    ''' SaveActionControl (insert) and UpdateActionControl (update).
    ''' </summary>
    Private Sub SavePlanDetailRows(ByVal model As ActionControlModel, ByVal actionId As String)
        If model.NeedPayment AndAlso model.PaymentPlanIds.Count > 0 Then
            Dim nextDetailRowIdDT As New Data.DataTable
            nextDetailRowIdDT = GetDataTable(EBDB, "SELECT NVL(MAX(TO_NUMBER(ID)), 0) AS MAX_ID FROM UNITSHUB_ACT_PAY_PLN_DETAILS")
            Dim nextDetailRowIdNumber As Integer = CInt(nextDetailRowIdDT.Rows(0)("MAX_ID"))

            For Each planId As String In model.PaymentPlanIds
                Dim anyDetailForPlan As Boolean = False

                For Each key As String In TickedDetailKeys
                    If key.StartsWith(planId & "::") Then
                        anyDetailForPlan = True
                        Dim detailId As String = key.Substring(planId.Length + 2)

                        nextDetailRowIdNumber += 1
                        Dim detailRowId As String = nextDetailRowIdNumber.ToString("00000")

                        InsertPlanDetailRow(detailRowId, actionId, planId, detailId)
                    End If
                Next

                If Not anyDetailForPlan Then
                    ' No specific line item chosen for this plan — still record that the
                    ' plan itself applies to this action.
                    nextDetailRowIdNumber += 1
                    Dim detailRowId As String = nextDetailRowIdNumber.ToString("00000")
                    InsertPlanDetailRow(detailRowId, actionId, planId, Nothing)
                End If
            Next
        End If
    End Sub

    ''' <summary>Inserts one UNITSHUB_ACT_PAY_PLN_DETAILS row. detailId may be Nothing.</summary>
    Private Sub InsertPlanDetailRow(ByVal detailRowId As String, ByVal actionId As String, ByVal planId As String, ByVal detailId As String)
        Dim insertDetailSql As String =
            "INSERT INTO UNITSHUB_ACT_PAY_PLN_DETAILS (ID, PROJECT_ID, STATUS_ID, ACTION_ID, PLAN_ID, DETAIL_ID) VALUES (" &
            "'" & detailRowId & "', " &
            "'" & lblProjectID.Text.Replace("'", "''") & "', " &
            "'" & lblSTATUSID.Text.Replace("'", "''") & "', " &
            "'" & actionId & "', " &
            "'" & planId.Replace("'", "''") & "', " &
            (If(String.IsNullOrEmpty(detailId), "NULL", "'" & detailId.Replace("'", "''") & "'")) & ")"

        ExecuteNonQuery(EBDB, insertDetailSql)
    End Sub
End Class
