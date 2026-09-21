Imports System.Data
Imports System.DateTime
Imports System.Drawing
Imports System.Globalization
Imports System.Configuration
Imports System.Data.SqlClient

Partial Class ReserveUnit
    Inherits System.Web.UI.Page
    Private encryNdecry As New EncryDecry

    ' Name of the web.config connection string that points at the UNITSHUB_PAYMENTS database
    Private Const PaymentsConnectionName As String = "UnitsHub"

    Private Sub ReserveUnit_Load(sender As Object, e As EventArgs) Handles Me.Load
        ' Read-only in the browser, but still posted back so the picked date reaches the server
        txtReservationDate.Attributes("readonly") = "readonly"

        If Not Page.IsPostBack Then
            gvPayments.ShowHeaderWhenEmpty = True
            Dim NeedsPayment As String = Request("NeedsPayment")

            txtReservationDate.Text = Date.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)

            ' The payments grid only exists (is not rendered) when the action needs payment
            pnlPayments.Visible = String.Equals(NeedsPayment, "True", StringComparison.OrdinalIgnoreCase)
            BindPayments()

            Dim MemberListParameters As New clsListProperties
            With MemberListParameters
                .ItemsSQL = "Select ID, NAME, NATIONALID from UNITSHUB_CONTACTS "
                .CheckedItemsSQL = ""
                .FormTitle = "Select Customer"
                .ColumnHideAndShow = "YNN"
                .EditableColumns = "NNN"
                .ColumnsWidth = New Double() {3, 1}
                .HoverableList = "Y"
            End With
            Dim SelectMembersParameters As String = encryNdecry.EncryptObject(Of clsListProperties)(MemberListParameters)

            VendorPopupHelper.RegisterVendorPopup(Me,
                                                  btnSelectExistingCustomer,
                                                  "SelectOneItemFromListMultiColumns.aspx?Parameters=" & Server.UrlEncode(SelectMembersParameters),
                                                  400,
                                                  500,
                                                  PopupPlacement.Center,
                                                  "Select Adj",
                                                  VendorPopupHelper.PopupDisplayMode.FrameOnly,
                                                  "SelectedCustomer")

        End If
    End Sub
    Protected Sub btnSelectExistingCustomer_Click(sender As Object, e As EventArgs) Handles btnSelectExistingCustomer.Click
        Dim selectedItems As List(Of Dictionary(Of String, Object)) =
        TryCast(VendorPopupHelper.GetPopupReturnValue(Me, "SelectedCustomer"),
                List(Of Dictionary(Of String, Object)))

        If selectedItems Is Nothing OrElse selectedItems.Count = 0 Then Exit Sub

        Dim row As Dictionary(Of String, Object) = selectedItems(0)
        txtCustomerName.Text = Convert.ToString(row("NAME"))
        txtCustomerCPR.Text = Convert.ToString(row("NATIONALID"))
        lblCID.Text = Convert.ToString(row("ID"))

        BindPayments()
    End Sub
    Protected Sub btnSave_Click(sender As Object, e As EventArgs) Handles btnSave.Click
        ' TODO: validate and save the reservation.
        ' Customer ID : lblCID.Text
        ' Customer     : txtCustomerName.Text / txtCustomerCPR.Text
        ' Reserved by  : ddlReservedBy.SelectedValue
        ' Date         : txtReservationDate.Text  (yyyy-MM-dd)
        ' Comments     : txtComments.Text
    End Sub
    Protected Sub lnkLinkPayment_Click(sender As Object, e As EventArgs) Handles lnkLinkPayment.Click
        ' TODO: link a payment to this customer / unit.
        ' Customer ID : lblCID.Text
        ' Node ID     : NodeId
        ' When done, call BindPayments() to refresh the grid.
    End Sub

    ''' <summary>NODEID of the unit being reserved, taken from the query string (?NodeID=...).</summary>
    Private ReadOnly Property NodeId As Integer
        Get
            Dim value As Integer
            Integer.TryParse(Request.QueryString("NodeID"), value)
            Return value
        End Get
    End Property

    ''' <summary>Fills the payments grid for the selected customer. Does nothing when the grid is hidden.</summary>
    Private Sub BindPayments()
        If Not pnlPayments.Visible Then Exit Sub

        Dim customerId As Integer
        If Integer.TryParse(lblCID.Text, customerId) Then
            gvPayments.DataSource = GetPayments(NodeId, customerId)
        Else
            gvPayments.DataSource = New DataTable()   ' no customer selected yet
        End If
        gvPayments.DataBind()
    End Sub

    Private Function GetPayments(nodeId As Integer, customerId As Integer) As DataTable
        Dim cs As ConnectionStringSettings = ConfigurationManager.ConnectionStrings(PaymentsConnectionName)
        If cs Is Nothing Then
            Throw New ConfigurationErrorsException("Connection string '" & PaymentsConnectionName & "' was not found in web.config.")
        End If

        Dim sql As String =
            "SELECT SEQ, DESCRIPTION, AMOUNT, DatePaid, " &
            "       CONVERT(VARCHAR(8), TimePaid, 108) AS TimePaid, Narrative " &
            "FROM UNITSHUB_PAYMENTS " &
            "WHERE NODEID = @NODEID AND CUSTOMER_ID = @CUSTOMER_ID " &
            "ORDER BY SEQ"

        Dim dt As New DataTable()
        Using con As New SqlConnection(cs.ConnectionString)
            Using cmd As New SqlCommand(sql, con)
                cmd.Parameters.AddWithValue("@NODEID", nodeId)
                cmd.Parameters.AddWithValue("@CUSTOMER_ID", customerId)
                Using da As New SqlDataAdapter(cmd)
                    da.Fill(dt)
                End Using
            End Using
        End Using
        Return dt
    End Function
End Class
