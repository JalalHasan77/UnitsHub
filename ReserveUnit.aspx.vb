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

            ' NEED_PAYMENT: 1 = payment needed (show the panel), 0 / missing = no payment (hide it)
            pnlPayments.Visible = IsPaymentNeeded(NeedsPayment)
            BindPayments()




        End If

        VendorPopupHelper.RegisterVendorPopup(Me,
                                      lnkLinkPayment,
                                      "LinkPayment.aspx?ProjectID=" & lblPRJID.Text,
                                      1100,
                                      900,
                                      PopupPlacement.Center,
                                      "Select Adj",
                                      VendorPopupHelper.PopupDisplayMode.Standard,
                                      "SelectedPayment")


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
                              btnAddNewCustomer,
                              "ContactMaintenance.aspx?mode=New&isDialogue=yes",
                              950,
                              750,
                              PopupPlacement.Center,
                              "Select Adj",
                              VendorPopupHelper.PopupDisplayMode.FrameOnly,
                              "SelectedCustomer")

        UpdateControlsEnabledState()

    End Sub

    ''' <summary>
    ''' Everything on the form is disabled - except btnAddNewCustomer and
    ''' btnSelectExistingCustomer, which always stay enabled - until a customer has
    ''' actually been picked (lblCID.Text is set). Call this again any time lblCID.Text
    ''' changes, since Load runs before that change and won't see it otherwise.
    ''' </summary>
    Private Sub UpdateControlsEnabledState()
        Dim HasCustomer As Boolean = Not String.IsNullOrEmpty(lblCID.Text)

        txtCustomerName.Enabled = HasCustomer
        txtCustomerCPR.Enabled = HasCustomer
        ddlReservedBy.Enabled = HasCustomer
        lnkLinkPayment.Enabled = HasCustomer
        gvPayments.Enabled = HasCustomer
        txtReservationDate.Enabled = HasCustomer
        btnReservationDate.Disabled = Not HasCustomer   ' HtmlButton uses Disabled, not Enabled
        txtComments.Enabled = HasCustomer
        btnSave.Enabled = HasCustomer
        btnCancel.Enabled = HasCustomer
    End Sub
    Protected Sub btnSelectExistingCustomer_Click(sender As Object, e As EventArgs) Handles btnSelectExistingCustomer.Click, btnAddNewCustomer.Click
        Dim SelectedPayment As List(Of Dictionary(Of String, Object)) =
        TryCast(VendorPopupHelper.GetPopupReturnValue(Me, "SelectedCustomer"),
                List(Of Dictionary(Of String, Object)))

        If SelectedPayment Is Nothing OrElse SelectedPayment.Count = 0 Then Exit Sub

        Dim row As Dictionary(Of String, Object) = SelectedPayment(0)
        txtCustomerName.Text = Convert.ToString(row("NAME"))
        txtCustomerCPR.Text = Convert.ToString(row("NATIONALID"))
        lblCID.Text = Convert.ToString(row("ID"))

        UpdateControlsEnabledState()
        BindPayments()
    End Sub
    Protected Sub btnSave_Click(sender As Object, e As EventArgs) Handles btnSave.Click
        ' TODO: validate and save the reservation.
        ' Customer ID : lblCID.Text
        ' Customer     : txtCustomerName.Text / txtCustomerCPR.Text
        ' Reserved by  : ddlReservedBy.SelectedValue
        ' Date         : txtReservationDate.Text  (yyyy-MM-dd)
        ' Comments     : txtComments.Text
        '=====================================================================
        '=====================================================================
        ' Use the key the opener passed in (?vpKey=...) so the opener can read the
        ' result back with the same key it registered the popup with.
        'Dim RowValues As New Dictionary(Of String, Object)
        Dim SelectedCustomer As New Dictionary(Of String, Object)
        'SelectedCustomer.Add("NAME", "Roqaya and Elmeera")
        'SelectedCustomer.Add("NATIONALID", "770110266")
        SelectedCustomer.Add("CONTACT_ID", lblCID.Text)
        SelectedCustomer.Add("COMMENTS", txtComments.Text)

        Dim SelectedItems As New List(Of Dictionary(Of String, Object))
        SelectedItems.Add(SelectedCustomer)



        Dim ReturnKey As String = VendorPopupHelper.GetPopupReturnKey(Me)

        VendorPopupHelper.RegisterPopupSelectionAndClose(
                                    page:=Me,
                                    returnValue:=SelectedItems,
                                    startupScriptKey:=ReturnKey,
                                    skipPostBack:=False)

    End Sub
    Protected Sub lnkLinkPayment_Click(sender As Object, e As EventArgs) Handles lnkLinkPayment.Click
        Dim SelectedPayment As List(Of Dictionary(Of String, Object)) =
        TryCast(VendorPopupHelper.GetPopupReturnValue(Me, "SelectedPayment"),
                List(Of Dictionary(Of String, Object)))

        If SelectedPayment Is Nothing OrElse SelectedPayment.Count = 0 Then Exit Sub
        Dim Row As Dictionary(Of String, Object) = SelectedPayment(0)

        Dim PaymentPlan As String = getPaymentPlan()

        ' The next payment-plan line item this unit doesn't already have a linked
        ' (ACTIVE) UNITSHUB_PAYMENTS row for - e.g. if SEQ 1/2 are already linked,
        ' this is SEQ 3.
        Dim NextDetail As DataTable = GetNextPlanDetail(PaymentPlan)
        If NextDetail.Rows.Count = 0 Then Exit Sub   ' every line item is already linked
        Dim DetailRow As DataRow = NextDetail.Rows(0)

        Dim TransAmount As String = ParseDecimalLenient(Row("Amount")).ToString(CultureInfo.InvariantCulture)
        Dim DetailPercent As String = ParseDecimalLenient(DetailRow("PERCENT")).ToString(CultureInfo.InvariantCulture)
        Dim Narrative As String = (Convert.ToString(Row("Comment1")) & " " & Convert.ToString(Row("Comment2"))).Trim()

        Dim InsertSQL As String = ""
        InsertSQL = InsertSQL + vbCrLf + "INSERT INTO UNITSHUB_PAYMENTS "
        InsertSQL = InsertSQL + vbCrLf + "    (NODE_ID, CONTACT_ID, PLAN_ID, SEQ, DESCRIPTION, PERCENT, "
        InsertSQL = InsertSQL + vbCrLf + "     AMOUNT, DATEPAID, NARRATIVE, TRANSACTIONID, ACTIVE,DATECREATED) "
        InsertSQL = InsertSQL + vbCrLf + "VALUES ( "
        InsertSQL = InsertSQL + vbCrLf + "    '" & lblPID.Text.Replace("'", "''") & "', "
        InsertSQL = InsertSQL + vbCrLf + "    '" & lblCID.Text.Replace("'", "''") & "', "
        InsertSQL = InsertSQL + vbCrLf + "    '" & PaymentPlan.Replace("'", "''") & "', "
        InsertSQL = InsertSQL + vbCrLf + "    '" & Convert.ToString(DetailRow("SEQ")).Replace("'", "''") & "', "
        InsertSQL = InsertSQL + vbCrLf + "    '" & Convert.ToString(DetailRow("DESCRIPTION")).Replace("'", "''") & "', "
        InsertSQL = InsertSQL + vbCrLf + "    " & DetailPercent & ", "
        InsertSQL = InsertSQL + vbCrLf + "    " & TransAmount & ", "
        InsertSQL = InsertSQL + vbCrLf + "    '" & Convert.ToString(Row("Date")).Replace("'", "''") & "' , "
        InsertSQL = InsertSQL + vbCrLf + "    '" & Narrative.Replace("'", "''") & "', "
        InsertSQL = InsertSQL + vbCrLf + "    '" & Convert.ToString(Row("TranscationNum")).Replace("'", "''") & "', "
        InsertSQL = InsertSQL + vbCrLf + "    'Y','" & Now.Date.ToString("yyyy-MM-dd") & "'"
        InsertSQL = InsertSQL + vbCrLf + ") "

        ' TODO: swap for this app's actual write/execute helper - GetDataTable and
        ' DB.RetreiveScalarSTRING are read-only, so neither can run an INSERT.
        DB.ExecuteNonQuery(EBDB, InsertSQL)

        BindPayments()
    End Sub

    ''' <summary>
    ''' The first UNITSHUB_PAYMENTPLANDETAILS line item under PlanId that this unit
    ''' (lblPID.Text) doesn't already have an ACTIVE UNITSHUB_PAYMENTS row for - i.e.
    ''' the next installment due. Returns an empty DataTable once every line item has
    ''' been linked.
    ''' </summary>
    Private Function GetNextPlanDetail(PlanId As String) As DataTable
        Dim SQL As String = ""
        SQL = SQL + vbCrLf + "SELECT SEQ, DESCRIPTION, PERCENT FROM ( "
        SQL = SQL + vbCrLf + "    SELECT D.DETAIL_ID AS SEQ, D.DESCRIPTION, D.PERCENT "
        SQL = SQL + vbCrLf + "    FROM UNITSHUB_PAYMENTPLANDETAILS D "
        SQL = SQL + vbCrLf + "    WHERE D.PLAN_ID = '" & PlanId.Replace("'", "''") & "' "
        SQL = SQL + vbCrLf + "      AND NOT EXISTS ( "
        SQL = SQL + vbCrLf + "            SELECT 1 FROM UNITSHUB_PAYMENTS P "
        SQL = SQL + vbCrLf + "            WHERE P.NODE_ID = '" & lblPID.Text.Replace("'", "''") & "' "
        SQL = SQL + vbCrLf + "              AND P.PLAN_ID = D.PLAN_ID "
        SQL = SQL + vbCrLf + "              AND P.SEQ = D.DETAIL_ID "
        SQL = SQL + vbCrLf + "              AND P.ACTIVE = 'Y' "
        SQL = SQL + vbCrLf + "          ) "
        SQL = SQL + vbCrLf + "    ORDER BY D.DETAIL_ID "
        SQL = SQL + vbCrLf + ") WHERE ROWNUM = 1 "

        Return GetDataTable(EBDB, SQL)
    End Function

    ''' <summary>
    ''' Best-effort numeric parse: strips anything that isn't a digit, a decimal
    ''' point, or a leading minus sign (a trailing '%', a currency symbol, thousands
    ''' separators, stray whitespace) before parsing. UNITSHUB_PAYMENTPLANDETAILS.
    ''' PERCENT is stored/displayed as e.g. "5%", which plain Convert.ToDecimal
    ''' rejects outright.
    ''' </summary>
    Private Function ParseDecimalLenient(value As Object) As Decimal
        If value Is Nothing OrElse value Is DBNull.Value Then
            Throw New FormatException("Expected a numeric value but got none (Nothing/DBNull).")
        End If

        If TypeOf value Is Decimal OrElse TypeOf value Is Double OrElse
       TypeOf value Is Integer OrElse TypeOf value Is Long Then
            Return Convert.ToDecimal(value, CultureInfo.InvariantCulture)
        End If

        Dim Raw As String = Convert.ToString(value).Trim()
        Dim Cleaned As New System.Text.StringBuilder()
        For Each Ch As Char In Raw
            If Char.IsDigit(Ch) OrElse Ch = "."c OrElse (Ch = "-"c AndAlso Cleaned.Length = 0) Then
                Cleaned.Append(Ch)
            End If
        Next

        Dim Result As Decimal
        If Not Decimal.TryParse(Cleaned.ToString(), NumberStyles.Number, CultureInfo.InvariantCulture, Result) Then
            Throw New FormatException("Could not parse '" & Raw & "' as a number.")
        End If
        Return Result
    End Function

    ''' <summary>Interprets the NEED_PAYMENT flag passed in the query string.
    ''' "1" (or "True"/"Y") means payment is needed; "0", empty or anything else means
    ''' no payment, so the panel stays hidden.</summary>
    Private Function IsPaymentNeeded(Value As String) As Boolean
        If String.IsNullOrWhiteSpace(Value) Then Return False
        Select Case Value.Trim().ToUpperInvariant()
            Case "1", "TRUE", "Y", "YES"
                Return True
            Case Else
                Return False
        End Select
    End Function

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
    ''' UNITSHUB_PAYMENTS rows for this NODE_ID whose PLAN_ID/SEQ/DESCRIPTION match a
    ''' line in this action's payment plan (PLAN_ID from getPaymentPlan()) - the EXISTS
    ''' query combining UNITSHUB_PAYMENTPLANDETAILS / UNITSHUB_ACT_PAY_PLN_DETAILS.
    ''' </summary>
    Private Function GetPayments() As DataTable
        Dim PlanId As String = getPaymentPlan()

        Dim SQL As String = ""
        SQL = SQL + vbCrLf + " SELECT P.* "
        SQL = SQL + vbCrLf + " FROM   UNITSHUB_PAYMENTS P "
        SQL = SQL + vbCrLf + " WHERE  P.ACTIVE = 'Y' "
        SQL = SQL + vbCrLf + "   AND  P.NODE_ID = '" & lblPID.Text.Replace("'", "''") & "' "
        SQL = SQL + vbCrLf + "   AND  P.CONTACT_ID = '" & lblCID.Text.Replace("'", "''") & "' "
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
    Protected Sub imgClose_Click(sender As Object, e As ImageClickEventArgs) Handles imgClose.Click
        VendorPopupHelper.RegisterPopupSelectionAndClose(Me, False, skipPostBack:=False)
    End Sub
    Protected Sub btnCancel_Click(sender As Object, e As EventArgs) Handles btnCancel.Click
        VendorPopupHelper.RegisterPopupSelectionAndClose(Me, False, skipPostBack:=False)
    End Sub
End Class
