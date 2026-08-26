
Partial Class DesignAction
    Inherits System.Web.UI.Page

    ' Simple model representing the fields on the form.
    Public Class ActionControlModel
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
        Public Property Script As String
        Public Property PreExecution As String
    End Class

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Load
        If Not IsPostBack Then
            ' TODO: replace with a real record load when editing an existing action.
            LoadDefaults()
            LoadPaymentPlans()
        End If

        UpdateNeedPaymentVisibility()
    End Sub

    ''' <summary>
    ''' Shows/hides the "Need Payment" checkbox row based on the currently selected Action Type,
    ''' and the Payment Plan row based on that checkbox. Mirrors the client-side toggle scripts
    ''' so both rows are also correct on the very first render and whenever JavaScript is unavailable.
    ''' </summary>
    Private Sub UpdateNeedPaymentVisibility()
        Dim needPaymentRowVisible As Boolean = (ddlActionType.SelectedValue = "CHANGE")
        rowNeedPayment.Style("display") = If(needPaymentRowVisible, "", "none")

        rowPaymentPlan.Style("display") = If(needPaymentRowVisible AndAlso chkNeedPayment.Checked, "", "none")
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

        ' TODO: persist "model" to your data store (e.g. call a service/repository here).
        SaveActionControl(model)

        lblMessage.Text = "Action '" & model.ActionTitle & "' saved successfully."
    End Sub

    Protected Sub btnCancel_Click(ByVal sender As Object, ByVal e As EventArgs)
        Response.Redirect(Request.RawUrl)
    End Sub

    ''' <summary>
    ''' Reads all form controls into a plain model object.
    ''' </summary>
    Private Function BuildModelFromForm() As ActionControlModel
        Dim model As New ActionControlModel()

        model.IsActive = (rblActionStatus.SelectedValue = "Active")
        model.ActionTitle = txtActionTitle.Text.Trim()
        model.ActionType = ddlActionType.SelectedValue
        model.ImplementerTitle = txtImplementerTitle.Text.Trim()

        model.ShowInDefault = cblShowIn.Items.FindByValue("Default").Selected
        model.ShowInPreview = cblShowIn.Items.FindByValue("Preview").Selected

        model.ReceiveParametersEnabled = chkReceiveParameters.Checked
        model.ReceiveParametersMode = rblReceiveParameters.SelectedValue
        model.NeedPayment = chkNeedPayment.Checked
        model.PaymentPlanId = If(model.NeedPayment, ddlPaymentPlan.SelectedValue, Nothing)

        model.Script = txtScript.Text
        model.PreExecution = rblPreExecution.SelectedValue

        Return model
    End Function

    ''' <summary>
    ''' Placeholder for actual save logic (database call, API call, etc.).
    ''' </summary>
    Private Sub SaveActionControl(ByVal model As ActionControlModel)
        ' Example:
        ' Using conn As New SqlConnection(ConfigurationManager.ConnectionStrings("Default").ConnectionString)
        '     Using cmd As New SqlCommand("usp_SaveActionControl", conn)
        '         cmd.CommandType = CommandType.StoredProcedure
        '         cmd.Parameters.AddWithValue("@IsActive", model.IsActive)
        '         cmd.Parameters.AddWithValue("@ActionTitle", model.ActionTitle)
        '         cmd.Parameters.AddWithValue("@ActionType", model.ActionType)
        '         cmd.Parameters.AddWithValue("@ImplementerTitle", model.ImplementerTitle)
        '         cmd.Parameters.AddWithValue("@ShowInDefault", model.ShowInDefault)
        '         cmd.Parameters.AddWithValue("@ShowInPreview", model.ShowInPreview)
        '         cmd.Parameters.AddWithValue("@ReceiveParametersEnabled", model.ReceiveParametersEnabled)
        '         cmd.Parameters.AddWithValue("@ReceiveParametersMode", model.ReceiveParametersMode)
        '         cmd.Parameters.AddWithValue("@NeedPayment", model.NeedPayment)
        '         cmd.Parameters.AddWithValue("@PaymentPlanId", model.PaymentPlanId)
        '         cmd.Parameters.AddWithValue("@Script", model.Script)
        '         cmd.Parameters.AddWithValue("@PreExecution", model.PreExecution)
        '         conn.Open()
        '         cmd.ExecuteNonQuery()
        '     End Using
        ' End Using
    End Sub
End Class
