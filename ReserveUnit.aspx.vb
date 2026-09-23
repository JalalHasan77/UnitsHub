Imports System.Data
Imports System.DateTime
Imports System.Drawing
Imports System.Globalization

Partial Class ReserveUnit
    Inherits System.Web.UI.Page
    Private encryNdecry As New EncryDecry

    Private Sub ReserveUnit_Load(sender As Object, e As EventArgs) Handles Me.Load
        ' Read-only in the browser, but still posted back so the picked date reaches the server
        txtReservationDate.Attributes("readonly") = "readonly"

        If Not Page.IsPostBack Then
            gvPayments.ShowHeaderWhenEmpty = True
            Dim NeedsPayment As String = Request("NeedsPayment")


            lblPID.Text = Request("NodeID")
            lblPRJID.Text = Request("ProjectId")
            lblSTATEID.Text = Request("STATEID")
            lblActionID.Text = Request("ActionId")


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


            VendorPopupHelper.RegisterVendorPopup(Me,
                                      lnkLinkPayment,
                                      "LinkPayment.aspx?ProjectID=" & lblPRJID.Text,
                                      1100,
                                      900,
                                      PopupPlacement.Center,
                                      "Select Adj",
                                      VendorPopupHelper.PopupDisplayMode.Standard,
                                      "SelectedPayment")


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
        Dim SelectedPayment As List(Of Dictionary(Of String, Object)) =
        TryCast(VendorPopupHelper.GetPopupReturnValue(Me, "SelectedPayment"),
                List(Of Dictionary(Of String, Object)))

        If SelectedPayment Is Nothing OrElse SelectedPayment.Count = 0 Then Exit Sub
        Dim Row As Dictionary(Of String, Object) = SelectedPayment(0)

        Dim PaymentPlan As String = getPaymentPlan()

        'needed details ==================================================
        'Select
        '    D.PLAN_ID,
        '    D.DETAIL_ID,
        '    D.DESCRIPTION,
        '    D.PERCENT,
        '    D.AMOUNT,
        '    CASE WHEN L.DETAIL_ID Is Not NULL THEN 1 ELSE 0 END AS IS_SELECTED
        'From UNITSHUB_PAYMENTPLANDETAILS D
        'Left Join UNITSHUB_ACT_PAY_PLN_DETAILS L
        '       On L.PLAN_ID    = '1'
        '      And L.DETAIL_ID  = D.DETAIL_ID
        '      And L.PROJECT_ID = '001'
        '      And L.STATUS_ID  = '0000'
        '      And L.ACTION_ID  = '00002'
        'WHERE D.PLAN_ID = L.PLAN_ID 
        'ORDER BY D.DETAIL_ID 




        BindPayments()
    End Sub

    Private Function getPaymentPlan() As String
        Dim SQL As String = ""
        SQL = SQL + vbCrLf + " SELECT NAV.VALUE_TEXT AS PAYMENTPLAN "
        SQL = SQL + vbCrLf + " FROM   UNITSHUB_NODES N "
        SQL = SQL + vbCrLf + " JOIN   UNITSHUB_ATTRIBUTES A "
        SQL = SQL + vbCrLf + "        ON A.PROJECT_ID     = N.PROJECT_ID "
        SQL = SQL + vbCrLf + "       AND A.NODE_TYPE_ID   = N.NODE_TYPE_ID "
        SQL = SQL + vbCrLf + "       And A.ATTRIBUTE_NAME = 'PAYMENTPLAN' "
        SQL = SQL + vbCrLf + " JOIN   UNITSHUB_NODE_ATTRIBUTE_VALUE NAV "
        SQL = SQL + vbCrLf + "        ON NAV.NODE_ID       = N.NODE_ID "
        SQL = SQL + vbCrLf + "       AND NAV.Display_order = A.Display_order "
        SQL = SQL + vbCrLf + " WHERE  N.NODE_ID = '" & lblPID.Text & "'"


        Return DB.RetreiveScalarSTRING(EBDB, SQL)


    End Function

    ''' <summary>Fills gvPayments for this unit's node/project/status/action. Does
    ''' nothing when the grid is hidden (Need Payment unticked for this action).</summary>
    Private Sub BindPayments()
        If Not pnlPayments.Visible Then Exit Sub
        gvPayments.DataSource = GetPayments()
        gvPayments.DataBind()
    End Sub

    ''' <summary>
    ''' UNITSHUB_PAYMENTS rows for this NODEID whose PLAN_ID/SEQ/DESCRIPTION match a
    ''' line in this action's payment plan (PLAN_ID from getPaymentPlan()) - the EXISTS
    ''' query combining UNITSHUB_PAYMENTPLANDETAILS / UNITSHUB_ACT_PAY_PLN_DETAILS.
    ''' </summary>
    Private Function GetPayments() As DataTable
        Dim PlanId As String = getPaymentPlan()

        Dim SQL As String = ""
        SQL = SQL + vbCrLf + " SELECT P.* "
        SQL = SQL + vbCrLf + " FROM   UNITSHUB_PAYMENTS P "
        SQL = SQL + vbCrLf + " WHERE  P.ACTIVE = 'Y' "
        SQL = SQL + vbCrLf + "   AND  P.NODEID = '" & lblPID.Text.Replace("'", "''") & "' "
        SQL = SQL + vbCrLf + "   AND  EXISTS ( "
        SQL = SQL + vbCrLf + "         SELECT 1 "
        SQL = SQL + vbCrLf + "         FROM   UNITSHUB_PAYMENTPLANDETAILS D "
        SQL = SQL + vbCrLf + "         LEFT JOIN UNITSHUB_ACT_PAY_PLN_DETAILS L "
        SQL = SQL + vbCrLf + "                ON L.PLAN_ID    = D.PLAN_ID "
        SQL = SQL + vbCrLf + "               AND L.DETAIL_ID  = D.DETAIL_ID "
        SQL = SQL + vbCrLf + "               AND L.PROJECT_ID = '" & lblPRJID.Text.Replace("'", "''") & "' "
        SQL = SQL + vbCrLf + "               AND L.STATUS_ID  = '" & lblSTATEID.Text.Replace("'", "''") & "' "
        SQL = SQL + vbCrLf + "               AND L.ACTION_ID  = '" & lblActionID.Text.Replace("'", "''") & "' "
        SQL = SQL + vbCrLf + "         WHERE  D.PLAN_ID     = '" & PlanId.Replace("'", "''") & "' "
        SQL = SQL + vbCrLf + "           AND  D.PLAN_ID     = P.PLAN_ID "
        SQL = SQL + vbCrLf + "           AND  D.DETAIL_ID   = P.SEQ "
        SQL = SQL + vbCrLf + "           AND  D.DESCRIPTION = P.DESCRIPTION "
        SQL = SQL + vbCrLf + "       ) "
        SQL = SQL + vbCrLf + " ORDER BY P.SEQ "

        Return GetDataTable(EBDB, SQL)
    End Function
End Class