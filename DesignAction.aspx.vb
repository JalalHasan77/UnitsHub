
Partial Class DesignAction
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
        Public Property PaymentPlanId As String
        Public Property ToStatusId As String
        Public Property Script As String
        Public Property PreExecution As String
        Public Property ConfirmationText As String
        Public Property ParameterType As String
        Public Property FormTitle As String
        Public Property SelectSQL As String
    End Class

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Load
        If Not IsPostBack Then
            ' Only override the markup's placeholder text (e.g. "001" / "0000") when the query
            ' string actually supplies a value — otherwise Request.QueryString(...) returns
            ' Nothing and silently blanks out lblProjectID/lblSTATUSID.
            If Not String.IsNullOrEmpty(Request.QueryString("ProjectID")) Then
                lblProjectID.Text = Request.QueryString("ProjectID")
            End If

            If Not String.IsNullOrEmpty(Request.QueryString("StatusID")) Then
                lblSTATUSID.Text = Request.QueryString("StatusID")
            End If

            ' TODO: replace with a real record load when editing an existing action.
            LoadDefaults()
            LoadPaymentPlans()
            LoadStatuses()
        End If

        UpdateNeedPaymentVisibility()
        UpdatePreExecutionVisibility()
    End Sub

    ''' <summary>
    ''' Shows/hides the Confirmation Text row, the Parameter Type row, and (depending on the
    ''' selected Parameter Type) the Form Title / Select SQL rows. Mirrors the client-side
    ''' togglePreExecutionVisibility() / toggleParameterTypeVisibility() scripts.
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
    ''' Shows/hides the "Need Payment" checkbox row based on the currently selected Action Type,
    ''' and the Payment Plan row based on that checkbox. Mirrors the client-side toggle scripts
    ''' so both rows are also correct on the very first render and whenever JavaScript is unavailable.
    ''' </summary>
    Private Sub UpdateNeedPaymentVisibility()
        Dim needPaymentRowVisible As Boolean = (ddlActionType.SelectedValue = "CHANGE")
        rowNeedPayment.Style("display") = If(needPaymentRowVisible, "", "none")
        cellToStatus.Style("display") = If(needPaymentRowVisible, "", "none")

        Dim paymentPlanRowVisible As Boolean = (needPaymentRowVisible AndAlso chkNeedPayment.Checked)
        rowPaymentPlan.Style("display") = If(paymentPlanRowVisible, "", "none")

        Dim planDetailsRowVisible As Boolean = (paymentPlanRowVisible AndAlso Not String.IsNullOrEmpty(ddlPaymentPlan.SelectedValue))
        rowPlanDetails.Style("display") = If(planDetailsRowVisible, "", "none")
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
    ''' Populates ddlToStatus from UNITSHUB_UNITSSTATUS. Only called on the initial load
    ''' (not on postback) so the selection persists via ViewState instead of being re-bound.
    ''' </summary>
    Private Sub LoadStatuses()
        Dim DT As New Data.DataTable
        Dim SQL As String = "Select STATE_ID, CASE WHEN SUBTITLE IS NOT NULL OR TRIM(SUBTITLE) <> '' THEN STATUS || ': ' || SUBTITLE ELSE STATUS END AS STATUS from UNITSHUB_UNITSSTATUS"
        DT = GetDataTable(EBDB, SQL)

        ddlToStatus.Items.Clear()
        ddlToStatus.DataSource = DT
        ddlToStatus.DataTextField = "STATUS"
        ddlToStatus.DataValueField = "STATE_ID"
        ddlToStatus.DataBind()

        ddlToStatus.Items.Insert(0, New ListItem("To Status", ""))
    End Sub

    ''' <summary>
    ''' Fires when the user picks a different Payment Plan; reloads the corresponding
    ''' plan-detail rows into gvPlanDetails.
    ''' </summary>
    Protected Sub ddlPaymentPlan_SelectedIndexChanged(ByVal sender As Object, ByVal e As EventArgs)
        LoadPlanDetails()
    End Sub

    ''' <summary>
    ''' Loads UNITSHUB_PAYMENTPLANDETAILS rows for the currently selected Payment Plan
    ''' into gvPlanDetails.
    ''' </summary>
    Private Sub LoadPlanDetails()
        Dim PLAN_ID As String = ddlPaymentPlan.SelectedValue

        If String.IsNullOrEmpty(PLAN_ID) Then
            gvPlanDetails.DataSource = Nothing
            gvPlanDetails.DataBind()
            Return
        End If

        Dim DT As New Data.DataTable
        DT = GetDataTable(EBDB, "Select * from UNITSHUB_PAYMENTPLANDETAILS where PLAN_ID = '" & PLAN_ID & "'")

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

        Dim model As ActionControlModel = BuildModelFromForm()
        SaveActionControl(model)

        lblMessage.Text = "Action '" & model.ActionTitle & "' saved successfully."
    End Sub

    Protected Sub btnCancel_Click(ByVal sender As Object, ByVal e As EventArgs)
        Response.Redirect(Request.RawUrl)
    End Sub

    Protected Sub btnLoad_Click(ByVal sender As Object, ByVal e As EventArgs)
        LoadActionControl()
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
    Private Sub LoadActionControl()
        Const actionId As String = "00001"

        Dim actionDT As New Data.DataTable
        Dim actionSql As String =
            "SELECT * FROM UNITSHUB_ACTIONS WHERE PROJECT_ID = '" & lblProjectID.Text.Replace("'", "''") & "' " &
            "AND STATUS_ID = '" & lblSTATUSID.Text.Replace("'", "''") & "' AND ACTION_ID = '" & actionId & "'"
        actionDT = GetDataTable(EBDB, actionSql)

        If actionDT.Rows.Count = 0 Then
            lblMessage.Text = "No saved action found for this Project / Status / Action ID."
            Return
        End If

        Dim row As Data.DataRow = actionDT.Rows(0)

        rblActionStatus.SelectedValue = If(SafeBool(row("IS_ACTIVE")), "Active", "InActive")
        txtActionTitle.Text = SafeString(row("ACTION_TITLE"))
        SafeSetSelectedValue(ddlActionType, SafeString(row("ACTION_TYPE")))
        txtImplementerTitle.Text = SafeString(row("IMPLEMENTER_TITLE"))

        cblShowIn.Items.FindByValue("Default").Selected = SafeBool(row("SHOW_IN_DEFAULT"))
        cblShowIn.Items.FindByValue("Preview").Selected = SafeBool(row("SHOW_IN_PREVIEW"))

        chkReceiveParameters.Checked = SafeBool(row("RECEIVE_PARAMETERS_ENABLED"))
        SafeSetSelectedValue(rblReceiveParameters, SafeString(row("RECEIVE_PARAMETERS_MODE")))

        chkNeedPayment.Checked = SafeBool(row("NEED_PAYMENT"))
        Dim paymentPlanId As String = SafeString(row("PAYMENT_PLAN_ID"))
        SafeSetSelectedValue(ddlPaymentPlan, paymentPlanId)

        SafeSetSelectedValue(ddlToStatus, SafeString(row("TO_STATUS_ID")))

        txtScript.Text = SafeString(row("SCRIPT_TEXT"))
        SafeSetSelectedValue(rblPreExecution, SafeString(row("PRE_EXECUTION")))
        txtConfirmationText.Text = SafeString(row("CONFIRMATION_TEXT"))
        SafeSetSelectedValue(ddlParameterType, SafeString(row("PARAMETER_TYPE")))
        txtFormTitle.Text = SafeString(row("FORM_TITLE"))
        txtSelectSQL.Text = SafeString(row("SELECT_SQL"))

        ' Re-bind the Payment Plan Details grid for the loaded plan, then re-tick whichever
        ' rows were previously saved for this action.
        If chkNeedPayment.Checked AndAlso Not String.IsNullOrEmpty(paymentPlanId) Then
            LoadPlanDetails()

            Dim detailsDT As New Data.DataTable
            Dim detailsSql As String =
                "SELECT DETAIL_ID FROM UNITSHUB_ACT_PAY_PLN_DETAILS WHERE PROJECT_ID = '" & lblProjectID.Text.Replace("'", "''") & "' " &
                "AND STATUS_ID = '" & lblSTATUSID.Text.Replace("'", "''") & "' AND ACTION_ID = '" & actionId & "'"
            detailsDT = GetDataTable(EBDB, detailsSql)

            For Each gvRow As GridViewRow In gvPlanDetails.Rows
                If gvRow.RowType <> DataControlRowType.DataRow Then
                    Continue For
                End If

                Dim detailId As String = gvPlanDetails.DataKeys(gvRow.RowIndex).Value.ToString()
                Dim chk As CheckBox = CType(gvRow.FindControl("chkSelectDetail"), CheckBox)

                If chk IsNot Nothing Then
                    chk.Checked = (detailsDT.Select("DETAIL_ID = '" & detailId.Replace("'", "''") & "'").Length > 0)
                End If
            Next
        Else
            gvPlanDetails.DataSource = Nothing
            gvPlanDetails.DataBind()
        End If

        UpdateNeedPaymentVisibility()
        UpdatePreExecutionVisibility()

        lblMessage.Text = "Action '" & txtActionTitle.Text & "' loaded."
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

        model.ToStatusId = If(model.ActionType = "CHANGE", ddlToStatus.SelectedValue, Nothing)

        model.ShowInDefault = cblShowIn.Items.FindByValue("Default").Selected
        model.ShowInPreview = cblShowIn.Items.FindByValue("Preview").Selected

        model.ReceiveParametersEnabled = chkReceiveParameters.Checked
        model.ReceiveParametersMode = rblReceiveParameters.SelectedValue
        model.NeedPayment = chkNeedPayment.Checked
        model.PaymentPlanId = If(model.NeedPayment, ddlPaymentPlan.SelectedValue, Nothing)

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
    ''' UNITSHUB_ACT_PAY_PLN_DETAILS for every ticked checkbox in gvPlanDetails.
    ''' </summary>
    ''' <remarks>
    ''' TODO: "ExecuteNonQuery" below is a placeholder for whatever write helper your
    ''' project actually exposes (this codebase only gave me GetDataTable(conn, sql) for
    ''' reads) — swap it for your real INSERT/UPDATE/DELETE helper.
    ''' TODO: ACTION_ID / ID are VARCHAR2(5 BYTE), not DB-generated identities, so I generate
    ''' zero-padded numeric strings (e.g. "00001") via MAX(TO_NUMBER(...))+1. If your app has
    ''' an existing ID-generation routine/sequence for these tables, use that instead.
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
            "NEED_PAYMENT, PAYMENT_PLAN_ID, TO_STATUS_ID, SCRIPT_TEXT, PRE_EXECUTION, CONFIRMATION_TEXT, " &
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
            (If(String.IsNullOrEmpty(model.PaymentPlanId), "NULL", "'" & model.PaymentPlanId & "'")) & ", " &
            (If(String.IsNullOrEmpty(model.ToStatusId), "NULL", "'" & model.ToStatusId & "'")) & ", " &
            "'" & If(model.Script, "").Replace("'", "''") & "', " &
            "'" & model.PreExecution & "', " &
            (If(String.IsNullOrEmpty(model.ConfirmationText), "NULL", "'" & model.ConfirmationText.Replace("'", "''") & "'")) & ", " &
            (If(String.IsNullOrEmpty(model.ParameterType), "NULL", "'" & model.ParameterType & "'")) & ", " &
            (If(String.IsNullOrEmpty(model.FormTitle), "NULL", "'" & model.FormTitle.Replace("'", "''") & "'")) & ", " &
            (If(String.IsNullOrEmpty(model.SelectSQL), "NULL", "'" & model.SelectSQL.Replace("'", "''") & "'")) &
            ")"

        ExecuteNonQuery(EBDB, insertActionSql)

        ' 3. Insert one child row (with its own generated ID) for every ticked checkbox
        '    in the Payment Plan Details grid.
        If model.NeedPayment AndAlso Not String.IsNullOrEmpty(model.PaymentPlanId) Then
            Dim nextDetailRowIdDT As New Data.DataTable
            nextDetailRowIdDT = GetDataTable(EBDB, "SELECT NVL(MAX(TO_NUMBER(ID)), 0) AS MAX_ID FROM UNITSHUB_ACT_PAY_PLN_DETAILS")
            Dim nextDetailRowIdNumber As Integer = CInt(nextDetailRowIdDT.Rows(0)("MAX_ID"))

            For Each row As GridViewRow In gvPlanDetails.Rows
                If row.RowType <> DataControlRowType.DataRow Then
                    Continue For
                End If

                Dim chk As CheckBox = CType(row.FindControl("chkSelectDetail"), CheckBox)

                If chk IsNot Nothing AndAlso chk.Checked Then
                    nextDetailRowIdNumber += 1
                    Dim detailRowId As String = nextDetailRowIdNumber.ToString("00000")
                    Dim detailId As String = gvPlanDetails.DataKeys(row.RowIndex).Value.ToString()

                    Dim insertDetailSql As String =
                        "INSERT INTO UNITSHUB_ACT_PAY_PLN_DETAILS (ID, PROJECT_ID, STATUS_ID, ACTION_ID, PLAN_ID, DETAIL_ID) VALUES (" &
                        "'" & detailRowId & "', " &
                        "'" & lblProjectID.Text.Replace("'", "''") & "', " &
                        "'" & lblSTATUSID.Text.Replace("'", "''") & "', " &
                        "'" & actionId & "', " &
                        "'" & model.PaymentPlanId & "', " &
                        "'" & detailId.Replace("'", "''") & "')"

                    ExecuteNonQuery(EBDB, insertDetailSql)
                End If
            Next
        End If
    End Sub
End Class
