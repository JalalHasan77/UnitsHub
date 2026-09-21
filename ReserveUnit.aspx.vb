Imports System.Data
Imports System.DateTime
Imports System.Drawing
Imports System.Globalization

Partial Class ReserveUnit
    Inherits System.Web.UI.Page
    Private encryNdecry As New EncryDecry

    Private Sub ReserveUnit_Load(sender As Object, e As EventArgs) Handles Me.Load
        If Not Page.IsPostBack Then

            txtReservationDate.Text = Date.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)

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
    End Sub
    Protected Sub btnSave_Click(sender As Object, e As EventArgs) Handles btnSave.Click
        ' TODO: validate and save the reservation.
        ' Customer ID : lblCID.Text
        ' Customer     : txtCustomerName.Text / txtCustomerCPR.Text
        ' Reserved by  : ddlReservedBy.SelectedValue
        ' Date         : txtReservationDate.Text  (yyyy-MM-dd)
        ' Comments     : txtComments.Text
    End Sub
End Class
