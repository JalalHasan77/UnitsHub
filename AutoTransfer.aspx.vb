Imports System.Data
Imports System.DateTime
Imports System.Drawing
Imports System.Globalization
Imports System.IO
Imports CrystalDecisions.CrystalReports.Engine

Partial Class AutoTransfer
    Inherits System.Web.UI.Page
    Private encryNdecry As New EncryDecry

    Private Sub AutoTransfer_Load(sender As Object, e As EventArgs) Handles Me.Load
        If Not Page.IsPostBack Then
            lblPID.Text = Request("NodeID")
            lblPRJID.Text = Request("ProjectId")
            lblSTATEID.Text = Request("STATEID")
            lblActionID.Text = Request("ActionId")

            LoadActionAutoTransfer()   ' lblATPlanID, lblATGroupID, lblPlanID, lblPlanSeq, lblReferencePhrase

            LoadCustomer()      ' CUSTOMER box
            LoadUnitInfo()      ' Unit Reference, Price
            LoadAccountInfo()   ' Pre Sales Account (+ its type in the title), Balance
            LoadAmounts()       ' Paid Amount, Remaining
            BindTransactions()  ' Transactions grid (UNITSHUB_ATP_DETAILS)
            LoadNextInvoiceNo() ' lblInvoiceNo
        End If

        ' Once an invoice PDF exists (ViewState), keep Display / Re-Generate usable
        EnableInvoiceButtons()
    End Sub

    ''' <summary>
    ''' Next invoice number = highest INVOICE_NUM in INVOICING_INVOICES + 1 (1 if the
    ''' table is empty). Non-numeric INVOICE_NUM values, if any, are ignored.
    ''' Read when the form opens - see the note in the reply about two users at once.
    ''' </summary>
    Private Sub LoadNextInvoiceNo()
        Dim SQL As String =
            "SELECT NVL(MAX(TO_NUMBER(INVOICE_NUM)), 0) + 1 FROM INVOICING_INVOICES " &
            "WHERE REGEXP_LIKE(TRIM(INVOICE_NUM), '^[0-9]+$')"

        lblInvoiceNo.Text = Convert.ToString(DB.RetreiveScalarSTRING(EBDB, SQL)).Trim()
    End Sub

    ''' <summary>
    ''' AutoTransfer plan and group saved on this action (UNITSHUB_ACTIONS
    ''' AUTOTRANSFER_PLAN_ID / AUTOTRANSFER_GROUP), kept in lblATPlanID / lblATGroupID.
    ''' </summary>
    Private Sub LoadActionAutoTransfer()
        If String.IsNullOrWhiteSpace(lblActionID.Text) Then Exit Sub

        Dim SQL As String = ""
        SQL = SQL + vbCrLf + " SELECT AUTOTRANSFER_PLAN_ID, AUTOTRANSFER_GROUP, ACTION_TYPE, TO_STATUS_ID "
        SQL = SQL + vbCrLf + " FROM   UNITSHUB_ACTIONS "
        SQL = SQL + vbCrLf + " WHERE  PROJECT_ID = '" & lblPRJID.Text.Replace("'", "''") & "' "
        SQL = SQL + vbCrLf + "   AND  STATUS_ID  = '" & lblSTATEID.Text.Replace("'", "''") & "' "
        SQL = SQL + vbCrLf + "   AND  ACTION_ID  = '" & lblActionID.Text.Replace("'", "''") & "' "

        Dim DT As DataTable = GetDataTable(EBDB, SQL)
        If DT Is Nothing OrElse DT.Rows.Count = 0 Then Exit Sub

        lblATPlanID.Text = Convert.ToString(DT.Rows(0)("AUTOTRANSFER_PLAN_ID")).Trim()
        lblATGroupID.Text = Convert.ToString(DT.Rows(0)("AUTOTRANSFER_GROUP")).Trim()
        lblActionType.Text = Convert.ToString(DT.Rows(0)("ACTION_TYPE")).Trim()     ' "CHANGE" = moves the unit's status
        lblToStatusID.Text = Convert.ToString(DT.Rows(0)("TO_STATUS_ID")).Trim()

        LoadGroupSettings()
    End Sub

    ''' <summary>
    ''' From the action's AutoTransfer group (UNITSHUB_ATP_GROUPS):
    '''   IMPLEMENT_ON     = "PLAN_ID.DETAIL_ID" -> lblPlanID (payment plan) / lblPlanSeq (its line)
    '''   REFERENCE_PHRASE -> lblReferencePhrase (start of the Transactions Description)
    '''   GROUP_TITLE      -> lblATGroupTitle (saved on the payment as LASTAUTOTRANSFER)
    ''' </summary>
    Private Sub LoadGroupSettings()
        If String.IsNullOrWhiteSpace(lblATGroupID.Text) Then Exit Sub

        Dim DT As DataTable = GetDataTable(EBDB,
            "SELECT GROUP_TITLE, IMPLEMENT_ON, REFERENCE_PHRASE FROM UNITSHUB_ATP_GROUPS WHERE GROUP_ID = '" & lblATGroupID.Text.Replace("'", "''") & "'")
        If DT Is Nothing OrElse DT.Rows.Count = 0 Then Exit Sub

        lblATGroupTitle.Text = Convert.ToString(DT.Rows(0)("GROUP_TITLE")).Trim()
        lblReferencePhrase.Text = Convert.ToString(DT.Rows(0)("REFERENCE_PHRASE")).Trim()

        ' e.g. "00001.2" -> plan "00001", seq "2"
        Dim ImplementOn As String = Convert.ToString(DT.Rows(0)("IMPLEMENT_ON")).Trim()
        Dim DotPos As Integer = ImplementOn.IndexOf("."c)
        If DotPos > 0 Then
            lblPlanID.Text = ImplementOn.Substring(0, DotPos)
            lblPlanSeq.Text = ImplementOn.Substring(DotPos + 1)
        End If
    End Sub

    ' ------------------------------------------------------------------
    ' CUSTOMER box
    ' ------------------------------------------------------------------

    ''' <summary>
    ''' Customer of this unit from UNITSHUB_CUSTOMERPROPERTIES (latest CREATED_AT),
    ''' then CPR / NAME / Mobile / Land Line from UNITSHUB_CONTACTS, and Customer
    ''' Number / Person Number from ICBS by CPR.
    ''' </summary>
    Private Sub LoadCustomer()
        If String.IsNullOrWhiteSpace(lblPID.Text) Then Exit Sub

        Dim SQL As String = ""
        SQL = SQL + vbCrLf + " SELECT CONTACT_ID FROM ( "
        SQL = SQL + vbCrLf + "     SELECT CONTACT_ID "
        SQL = SQL + vbCrLf + "     FROM   UNITSHUB_CUSTOMERPROPERTIES "
        SQL = SQL + vbCrLf + "     WHERE  NODE_ID = '" & lblPID.Text.Replace("'", "''") & "' "
        SQL = SQL + vbCrLf + "       AND  CONTACT_ID IS NOT NULL "
        SQL = SQL + vbCrLf + "     ORDER BY CREATED_AT DESC "
        SQL = SQL + vbCrLf + " ) WHERE ROWNUM = 1 "

        Dim ContactId As String = Convert.ToString(DB.RetreiveScalarSTRING(EBDB, SQL)).Trim()
        If String.IsNullOrEmpty(ContactId) Then Exit Sub

        lblCID.Text = ContactId

        ' SELECT * so the phone columns can be picked up whatever they're called
        Dim ContactDT As DataTable = GetDataTable(EBDB,
            "SELECT * FROM UNITSHUB_CONTACTS WHERE ID = '" & ContactId.Replace("'", "''") & "'")
        If ContactDT IsNot Nothing AndAlso ContactDT.Rows.Count > 0 Then
            Dim Contact As DataRow = ContactDT.Rows(0)
            lblCustomerName.Text = GetColumnValue(Contact, "NAME")
            lblCPR.Text = GetColumnValue(Contact, "NATIONALID")
            lblMobile.Text = GetColumnValue(Contact, "MOBILE", "MOBILE_NO", "MOBILE_NUMBER", "MOBILENO")
            lblLandLine.Text = GetColumnValue(Contact, "LANDLINE", "LAND_LINE", "PHONE", "TELEPHONE", "TEL")
        End If

        LoadIcbsCustomerNumbers()
    End Sub

    ''' <summary>
    ''' ICBS person number (bbsd_physical_persons.PHPR_ID) and customer number
    ''' (bbsd_cust_members.CUST_ID) for the customer's CPR.
    ''' </summary>
    Private Sub LoadIcbsCustomerNumbers()
        If String.IsNullOrWhiteSpace(lblCPR.Text) Then Exit Sub

        Dim SQL As String = ""
        SQL = SQL + vbCrLf + " SELECT T1.PHPR_ID, T2.CUST_ID "
        SQL = SQL + vbCrLf + " FROM   bbsd_physical_persons@ICBS T1 "
        SQL = SQL + vbCrLf + " LEFT JOIN bbsd_cust_members@ICBS T2 ON T1.PHPR_ID = T2.CUSM_ID "
        SQL = SQL + vbCrLf + " WHERE  T1.PHPR_NATIONAL_NBR = '" & lblCPR.Text.Trim().Replace("'", "''") & "' "
        SQL = SQL + vbCrLf + "   AND  ROWNUM = 1 "

        Dim DT As DataTable = GetDataTable(EBDB, SQL)
        If DT IsNot Nothing AndAlso DT.Rows.Count > 0 Then
            lblPersonNumber.Text = Convert.ToString(DT.Rows(0)("PHPR_ID"))
            lblCustomerNumber.Text = Convert.ToString(DT.Rows(0)("CUST_ID"))
        End If
    End Sub

    ' ------------------------------------------------------------------
    ' Unit Reference, Price
    ' ------------------------------------------------------------------

    Private Sub LoadUnitInfo()
        lblUnitRef.Text = GetUnitAttribute("REFERENCE")
        lblPrice.Text = FormatAmount(ParseAmount(GetUnitAttribute("PRICE")))
    End Sub

    ''' <summary>
    ''' Value of one of the unit's attributes, by its NAME_IN_UI in
    ''' UNITSHUB_ATTRIBUTES_PROPERTIES (value from UNITSHUB_NODE_ATTRIBUTE_VALUE) -
    ''' the same way MainPage builds its columns.
    ''' </summary>
    Private Function GetUnitAttribute(NameInUi As String) As String
        If String.IsNullOrWhiteSpace(lblPID.Text) Then Return ""

        Dim SQL As String = ""
        SQL = SQL + vbCrLf + " SELECT NAV.VALUE_TEXT "
        SQL = SQL + vbCrLf + " FROM   UNITSHUB_NODES N "
        SQL = SQL + vbCrLf + " JOIN   UNITSHUB_ATTRIBUTES_PROPERTIES AP "
        SQL = SQL + vbCrLf + "        ON AP.PROJECT_ID   = N.PROJECT_ID "
        SQL = SQL + vbCrLf + "       AND AP.NODE_TYPE_ID = N.NODE_TYPE_ID "
        SQL = SQL + vbCrLf + "       AND UPPER(REPLACE(AP.NAME_IN_UI, '""', '')) = '" & NameInUi.ToUpperInvariant().Replace("'", "''") & "' "
        SQL = SQL + vbCrLf + " JOIN   UNITSHUB_NODE_ATTRIBUTE_VALUE NAV "
        SQL = SQL + vbCrLf + "        ON NAV.NODE_ID       = N.NODE_ID "
        SQL = SQL + vbCrLf + "       AND NAV.DISPLAY_ORDER = AP.DISPLAY_ORDER "
        SQL = SQL + vbCrLf + " WHERE  N.NODE_ID = '" & lblPID.Text.Replace("'", "''") & "' "
        SQL = SQL + vbCrLf + "   AND  ROWNUM = 1 "

        Return Convert.ToString(DB.RetreiveScalarSTRING(EBDB, SQL)).Trim()
    End Function

    ' ------------------------------------------------------------------
    ' Pre Sales Account (594), Balance
    ' ------------------------------------------------------------------

    ''' <summary>
    ''' Pre Sales Account = UNIT_ACCOUNT of this unit's customer row in
    ''' UNITSHUB_CUSTOMERPROPERTIES (NODE_ID + CONTACT_ID), shown as stored.
    ''' </summary>
    Private Sub LoadAccountInfo()
        If String.IsNullOrWhiteSpace(lblPID.Text) OrElse String.IsNullOrWhiteSpace(lblCID.Text) Then Exit Sub

        Dim SQL As String = ""
        SQL = SQL + vbCrLf + " SELECT UNIT_ACCOUNT "
        SQL = SQL + vbCrLf + " FROM   UNITSHUB_CUSTOMERPROPERTIES "
        SQL = SQL + vbCrLf + " WHERE  NODE_ID    = '" & lblPID.Text.Replace("'", "''") & "' "
        SQL = SQL + vbCrLf + "   AND  CONTACT_ID = '" & lblCID.Text.Replace("'", "''") & "' "

        Dim UnitAccount As String = Convert.ToString(DB.RetreiveScalarSTRING(EBDB, SQL)).Trim()

        lblSelectedAccount.Text = UnitAccount
        lblPreSalesAccount.Text = UnitAccount

        ' Title "Pre Sales Account (xxx)": xxx = 7th, 8th and 9th digits after the dash,
        ' e.g. 810-123456789012 -> (789). No brackets if the account is too short.
        Dim AccountType As String = GetAccountTypeFromAccount(UnitAccount)
        lblAccountType.Text = If(AccountType = "", "", " (" & AccountType & ")")

        ' Balance from ICBS, using the account number after the dash
        Dim AccountNumber As String = GetAccountNumber(UnitAccount)
        If AccountNumber = "" Then
            lblBalance.Text = ""
        Else
            lblBalance.Text = FormatAmount(CDec(GetAccountBalance(AccountNumber)))
        End If
    End Sub

    ' ------------------------------------------------------------------
    ' Paid Amount, Remaining
    ' ------------------------------------------------------------------

    ''' <summary>
    ''' Paid Amount = total of the ACTIVE UNITSHUB_PAYMENTS rows for this unit and
    ''' customer; Remaining = Price - Paid Amount.
    ''' </summary>
    Private Sub LoadAmounts()
        Dim Paid As Decimal = 0D

        If Not String.IsNullOrWhiteSpace(lblPID.Text) AndAlso Not String.IsNullOrWhiteSpace(lblCID.Text) Then
            Dim SQL As String = ""
            SQL = SQL + vbCrLf + " SELECT NVL(SUM(AMOUNT), 0) "
            SQL = SQL + vbCrLf + " FROM   UNITSHUB_PAYMENTS "
            SQL = SQL + vbCrLf + " WHERE  ACTIVE     = 'Y' "
            SQL = SQL + vbCrLf + "   AND  NODE_ID    = '" & lblPID.Text.Replace("'", "''") & "' "
            SQL = SQL + vbCrLf + "   AND  CONTACT_ID = '" & lblCID.Text.Replace("'", "''") & "' "
            Paid = ParseAmount(Convert.ToString(DB.RetreiveScalarSTRING(EBDB, SQL)))
        End If

        lblPaidAmount.Text = FormatAmount(Paid)
        lblRemaining.Text = FormatAmount(ParseAmount(lblPrice.Text) - Paid)
    End Sub

    ' ------------------------------------------------------------------
    ' Transactions grid
    ' ------------------------------------------------------------------

    ''' <summary>
    ''' Builds the Transactions grid from the UNITSHUB_ATP_DETAILS lines of the action's
    ''' AutoTransfer group (lblATGroupID):
    '''   Account     = ACCOUNT_NUMBER, exactly as stored in the plan
    '''   Type        = TYPE (or TYPE_OF_ESCROW if there is no TYPE column)
    '''   Transaction = "C" if the line has a Credit amount, "D" if it has a Debit amount
    '''   Amount      = that Credit or Debit amount
    '''   Reference   = the Unit Reference shown in UNIT DETAILS (lblUnitRef)
    '''   Description = the group's REFERENCE_PHRASE with [@Reference] replaced by the
    '''                 Unit Reference (see BuildDescription)
    ''' Runs after LoadUnitInfo / LoadAccountInfo, which fill those two values.
    ''' Lines with neither a Debit nor a Credit amount are skipped.
    ''' </summary>
    Private Sub BindTransactions()
        Dim Result As New DataTable
        Result.Columns.Add("Account", GetType(String))
        Result.Columns.Add("Type", GetType(String))
        Result.Columns.Add("Transaction", GetType(String))
        Result.Columns.Add("Amount", GetType(Decimal))
        Result.Columns.Add("Reference", GetType(String))
        Result.Columns.Add("Description", GetType(String))

        If Not String.IsNullOrWhiteSpace(lblATGroupID.Text) Then
            Dim Details As DataTable = GetDataTable(EBDB,
                "SELECT * FROM UNITSHUB_ATP_DETAILS WHERE GROUP_ID = '" & lblATGroupID.Text.Replace("'", "''") & "' ORDER BY SORT_ORDER, DETAIL_ID")

            Dim UnitRef As String = lblUnitRef.Text.Trim()
            Dim Phrase As String = If(lblReferencePhrase.Text.Trim() = "", "Reservation of Unit", lblReferencePhrase.Text.Trim())
            Dim Description As String = BuildDescription(Phrase, UnitRef)

            ' Unit price for the [UnitPrice] formulas in DEBIT / CREDIT
            Dim UnitPrice As Decimal = GetUnitPrice()
            lblUnitPrice.Text = UnitPrice.ToString("0.000", CultureInfo.InvariantCulture)
            Dim FormulaErrors As New List(Of String)
            Dim AccountProblems As New List(Of String)

            If Details IsNot Nothing Then
                For Each Detail As DataRow In Details.Rows
                    Dim Credit As Decimal
                    Dim Debit As Decimal
                    Try
                        Credit = EvaluateAmount(GetColumnValue(Detail, "CREDIT"), UnitPrice)
                        Debit = EvaluateAmount(GetColumnValue(Detail, "DEBIT"), UnitPrice)
                    Catch ex As Exception
                        ' Leave the line out rather than post a wrong amount, and say why
                        FormulaErrors.Add(GetColumnValue(Detail, "ACCOUNT_DESCRIPTION", "ACCOUNT_NUMBER") & ": " & ex.Message)
                        Continue For
                    End Try

                    Dim Transaction As String
                    Dim Amount As Decimal
                    If Credit <> 0D Then
                        Transaction = "C"
                        Amount = Credit
                    ElseIf Debit <> 0D Then
                        Transaction = "D"
                        Amount = Debit
                    Else
                        Continue For
                    End If

                    ' Account exactly as stored in the plan (ACCOUNT_NUMBER) - nothing is
                    ' substituted or calculated on it. Only an empty one is reported.
                    Dim LineName As String = GetColumnValue(Detail, "ACCOUNT_DESCRIPTION", "TYPE", "TYPE_OF_ESCROW")
                    Dim Account As String = GetColumnValue(Detail, "ACCOUNT_NUMBER")
                    If Account = "" Then
                        AccountProblems.Add(LineName & ": no account number in the AutoTransfer plan")
                    End If

                    Result.Rows.Add(
                        Account,
                        GetColumnValue(Detail, "TYPE", "TYPE_OF_ESCROW"),
                        Transaction,
                        Amount,
                        UnitRef,
                        Description)
                Next
            End If

            If AccountProblems.Count > 0 OrElse FormulaErrors.Count > 0 Then
                Dim Parts As New List(Of String)
                If AccountProblems.Count > 0 Then Parts.Add("Missing accounts - " & String.Join("; ", AccountProblems))
                If FormulaErrors.Count > 0 Then Parts.Add("Lines left out because their amount couldn't be worked out - " & String.Join("; ", FormulaErrors))
                ShowMessage(String.Join(" | ", Parts), False)
            ElseIf UnitPrice = 0D Then
                ShowMessage("This unit has no Price, so amounts based on [UnitPrice] are 0.", False)
            End If
        End If

        gvTransactions.DataSource = Result
        gvTransactions.DataBind()
    End Sub

    ' ------------------------------------------------------------------
    ' Unit status change + history (same logic as MainPage.LinkButton3_Command)
    ' ------------------------------------------------------------------

    ''' <summary>
    ''' After a successful post, does what MainPage's Case "CHANGE" does: if the action is
    ''' a "CHANGE" action with a target status, moves the unit to that status
    ''' (ApplyNodeStatusChange) and updates the customer's UNITSHUB_CUSTOMERPROPERTIES row
    ''' (UpsertCustomerProperty); then always writes a UNITSHUB_UNITSHISTORY row with the
    ''' AutoTransfer in its SUMMARY. If the status
    ''' didn't change, FROM_STATUS and TO_STATUS are both the current status.
    ''' Returns True when the status was changed.
    ''' </summary>
    Private Function ApplyActionAfterPost(UnitPaymentId As String, TraNo As Integer, EntryCount As Integer) As Boolean
        Dim FromStatus As String = lblSTATEID.Text.Trim()
        Dim ToStatus As String = lblToStatusID.Text.Trim()

        Dim ChangeStatus As Boolean =
            String.Equals(lblActionType.Text.Trim(), "CHANGE", StringComparison.OrdinalIgnoreCase) AndAlso
            ToStatus <> "" AndAlso ToStatus <> FromStatus

        ' Same steps as MainPage.LinkButton3_Command, Case "CHANGE":
        '   1. ApplyNodeStatusChange   2. UpsertCustomerProperty   3. InsertUnitsHistory
        If ChangeStatus Then ApplyNodeStatusChange(lblPID.Text.Trim(), ToStatus)

        If String.Equals(lblActionType.Text.Trim(), "CHANGE", StringComparison.OrdinalIgnoreCase) Then
            UpsertCustomerProperty(lblPID.Text.Trim(), lblCID.Text.Trim(), If(ChangeStatus, ToStatus, FromStatus))
        End If

        Dim Summary As String =
            "AutoTransfer " & If(lblATGroupTitle.Text.Trim() = "", "", "'" & lblATGroupTitle.Text.Trim() & "' ") &
            "posted: ID " & UnitPaymentId &
            ", Transaction No. " & TraNo.ToString(CultureInfo.InvariantCulture) &
            ", " & EntryCount.ToString(CultureInfo.InvariantCulture) & " entries"
        If ChangeStatus Then Summary &= "; Status changed from " & GetStatusName(FromStatus) & " to " & GetStatusName(ToStatus)

        InsertUnitsHistory(lblPRJID.Text.Trim(), lblPID.Text.Trim(), lblCID.Text.Trim(),
                           FromStatus, If(ChangeStatus, ToStatus, FromStatus),
                           lblPlanID.Text.Trim(), lblActionID.Text.Trim(),
                           Summary,
                           AutoTransfer:=TraNo.ToString(CultureInfo.InvariantCulture),
                           AutoTransferRows:=UnitPaymentId)

        Return ChangeStatus
    End Function

    ''' <summary>
    ''' AutoTransfer's version of MainPage.UpsertCustomerProperty for the unit's customer
    ''' (UNITSHUB_CUSTOMERPROPERTIES row for NODE_ID + CONTACT_ID):
    '''   - row exists  -> only STATUS is updated. START_DATE, UNIT_ACCOUNT and COMMENTS
    '''                    were set when the unit was reserved; AutoTransfer has no new
    '''                    values for them, so they are kept (MainPage's version would
    '''                    blank them, which would also wipe the Pre Sales Account).
    '''   - no row yet  -> inserted like MainPage does, with UNIT_ACCOUNT = the Pre Sales
    '''                    Account shown on this form.
    ''' </summary>
    Private Sub UpsertCustomerProperty(NodeId As String, ContactId As String, StatusId As String)
        If String.IsNullOrWhiteSpace(NodeId) OrElse String.IsNullOrWhiteSpace(ContactId) Then Exit Sub

        Dim safeNodeId As String = NodeId.Trim().Replace("'", "''")
        Dim safeContactId As String = ContactId.Trim().Replace("'", "''")
        Dim safeStatus As String = If(StatusId, "").Trim().Replace("'", "''")

        Dim whereClause As String = " WHERE NODE_ID = '" & safeNodeId & "' AND CONTACT_ID = '" & safeContactId & "'"

        Dim existingCount As Integer = 0
        Integer.TryParse(DB.RetreiveScalarSTRING(EBDB, "SELECT COUNT(*) FROM UNITSHUB_CUSTOMERPROPERTIES" & whereClause), existingCount)

        Dim SQL As String
        If existingCount > 0 Then
            SQL = "UPDATE UNITSHUB_CUSTOMERPROPERTIES SET STATUS = '" & safeStatus & "'" & whereClause
        Else
            Dim startDate As String = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            Dim createdAt As String = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)
            Dim safeAccount As String = lblPreSalesAccount.Text.Trim().Replace("'", "''")

            SQL = "INSERT INTO UNITSHUB_CUSTOMERPROPERTIES " &
                  "(NODE_ID, CONTACT_ID, STATUS, START_DATE, END_DATE, UNIT_ACCOUNT, CREATED_AT, COMMENTS) VALUES (" &
                  "'" & safeNodeId & "', " &
                  "'" & safeContactId & "', " &
                  "'" & safeStatus & "', " &
                  "'" & startDate & "', " &
                  "'', " &
                  "'" & safeAccount & "', " &
                  "'" & createdAt & "', " &
                  "'')"
        End If

        DB.ExecuteNonQuery(EBDB_CS, SQL)
    End Sub

    ''' <summary>
    ''' Display name of a status of this project: STATUS - SUBTITLE from
    ''' UNITSHUB_PROJECTSTATUS (just STATUS when there is no subtitle), e.g.
    ''' "Reserved - Pending Payment". Falls back to the status ID if it isn't found.
    ''' </summary>
    Private Function GetStatusName(StateId As String) As String
        Dim Id As String = If(StateId, "").Trim()
        If Id = "" Then Return ""

        Dim DT As DataTable = GetDataTable(EBDB,
            "SELECT STATUS, SUBTITLE FROM UNITSHUB_PROJECTSTATUS " &
            "WHERE PROJECT_ID = '" & lblPRJID.Text.Trim().Replace("'", "''") & "' " &
            "AND STATE_ID = '" & Id.Replace("'", "''") & "'")
        If DT Is Nothing OrElse DT.Rows.Count = 0 Then Return Id

        Dim StatusText As String = Convert.ToString(DT.Rows(0)("STATUS")).Trim()
        Dim SubTitle As String = Convert.ToString(DT.Rows(0)("SUBTITLE")).Trim()
        If StatusText = "" Then Return Id
        Return If(SubTitle = "", StatusText, StatusText & " - " & SubTitle)
    End Function

    ''' <summary>
    ''' Sets the unit's Status attribute to ToStatusId (UNITSHUB_NODE_ATTRIBUTE_VALUE row
    ''' at the DISPLAY_ORDER of the "Status" attribute for the unit's project and node
    ''' type) - same update as MainPage.ApplyNodeStatusChange.
    ''' </summary>
    Private Sub ApplyNodeStatusChange(NodeId As String, ToStatusId As String)
        Dim safeNodeId As String = If(NodeId, "").Replace("'", "''")
        Dim safeToStatusId As String = If(ToStatusId, "").Replace("'", "''")

        Dim DisplayOrder As String = GetStatusAttributeDisplayOrder(NodeId)
        Dim DisplayOrderNumber As Integer
        If Not Integer.TryParse(DisplayOrder, DisplayOrderNumber) Then
            Throw New ApplicationException("The Status attribute of this unit's project/type was not found.")
        End If

        Dim SQL As String = " UPDATE UNITSHUB_NODE_ATTRIBUTE_VALUE " &
                            " SET VALUE_TEXT = '" & safeToStatusId & "' " &
                            " WHERE NODE_ID = '" & safeNodeId & "' " &
                            " AND DISPLAY_ORDER = '" & DisplayOrderNumber.ToString("000") & "'"

        DB.ExecuteNonQuery(EBDB_CS, SQL)
    End Sub

    ''' <summary>
    ''' DISPLAY_ORDER of the "Status" attribute (UNITSHUB_ATTRIBUTES) for the unit's project
    ''' and node type. MainPage keeps this in lblDisplayOrder for the selected project; here
    ''' it's looked up from the unit itself (UNITSHUB_NODES).
    ''' </summary>
    Private Function GetStatusAttributeDisplayOrder(NodeId As String) As String
        Dim SQL As String = ""
        SQL = SQL + vbCrLf + " SELECT A.DISPLAY_ORDER "
        SQL = SQL + vbCrLf + " FROM   UNITSHUB_NODES N "
        SQL = SQL + vbCrLf + " JOIN   UNITSHUB_ATTRIBUTES A "
        SQL = SQL + vbCrLf + "        ON A.PROJECT_ID   = N.PROJECT_ID "
        SQL = SQL + vbCrLf + "       AND A.NODE_TYPE_ID = N.NODE_TYPE_ID "
        SQL = SQL + vbCrLf + " WHERE  A.ATTRIBUTE_NAME = 'Status' "
        SQL = SQL + vbCrLf + "   AND  N.NODE_ID = '" & If(NodeId, "").Replace("'", "''") & "' "
        SQL = SQL + vbCrLf + "   AND  ROWNUM = 1 "

        Return Convert.ToString(DB.RetreiveScalarSTRING(EBDB, SQL)).Trim()
    End Function

    ''' <summary>
    ''' Inserts a new UNITSHUB_UNITSHISTORY row - same as MainPage.InsertUnitsHistory,
    ''' plus AUTOTRANSFER / AUTOTRANSFERROWS, which are filled here (MainPage leaves them
    ''' empty): AUTOTRANSFER = TRANO of the posting, AUTOTRANSFERROWS = its UNITPAYMENT_ID
    ''' (the same way PAYMENTS / PAYMENTSROWS hold TRANSACTIONID / PAYMENT_ID).
    ''' </summary>
    Private Sub InsertUnitsHistory(ProjectId As String, NodeId As String, ContactId As String,
                                   FromStatus As String, ToStatus As String,
                                   PaymentPlan As String, ActionId As String,
                                   Optional Summary As String = "",
                                   Optional AutoTransfer As String = "",
                                   Optional AutoTransferRows As String = "")

        If String.IsNullOrWhiteSpace(NodeId) Then Exit Sub

        Dim safeProjectId As String = If(ProjectId, "").Trim().Replace("'", "''")
        Dim safeNodeId As String = NodeId.Trim().Replace("'", "''")
        Dim safeContactId As String = If(ContactId, "").Trim().Replace("'", "''")
        Dim safeFromStatus As String = If(FromStatus, "").Replace("'", "''")
        Dim safeToStatus As String = If(ToStatus, "").Replace("'", "''")
        Dim safePaymentPlan As String = If(PaymentPlan, "").Replace("'", "''")
        Dim safeActionId As String = If(ActionId, "").Trim().Replace("'", "''")
        Dim safeUserId As String = If(GetCurrentUserID(), "").Replace("'", "''")
        'TODO change Workstation to real station
        Dim safeWorkstation As String = "S069"

        ' Payments linked to this node + contact
        Dim payments As String = ""
        Dim paymentRows As String = ""
        GetNodeContactPayments(safeNodeId, safeContactId, payments, paymentRows)
        Dim safePayments As String = payments.Replace("'", "''")
        Dim safePaymentRows As String = paymentRows.Replace("'", "''")

        Dim DateNTime As String = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)

        Dim SQL As String =
            "INSERT INTO UNITSHUB_UNITSHISTORY " &
            "(PROJECT_ID, NODE_ID, CONTACT_ID, FROM_STATUS, TO_STATUS, PAYMENT_PLAN, ACTION_ID, " &
            "PAYMENTS, PAYMENTSROWS, AUTOTRANSFER, AUTOTRANSFERROWS, IMPLEMENTEDBY, WORKSTATION, DATENTIME, SUMMARY) VALUES (" &
            "'" & safeProjectId & "', " &
            "'" & safeNodeId & "', " &
            "'" & safeContactId & "', " &
            "'" & safeFromStatus & "', " &
            "'" & safeToStatus & "', " &
            "'" & safePaymentPlan & "', " &
            "'" & safeActionId & "', " &
            "'" & safePayments & "', " &
            "'" & safePaymentRows & "', " &
            "'" & If(AutoTransfer, "").Replace("'", "''") & "', " &
            "'" & If(AutoTransferRows, "").Replace("'", "''") & "', " &
            "'" & safeUserId & "', " &
            "'" & safeWorkstation & "', " &
            DateNTime & ", " &
            "'" & If(Summary, "").Replace("'", "''") & "')"

        DB.ExecuteNonQuery(EBDB_CS, SQL)
    End Sub

    ''' <summary>
    ''' TRANSACTIONID values (payments) and PAYMENT_ID values (paymentRows) of this
    ''' unit + customer's UNITSHUB_PAYMENTS rows, comma-separated - same as MainPage.
    ''' Expects already-escaped NodeId / ContactId.
    ''' </summary>
    Private Sub GetNodeContactPayments(safeNodeId As String, safeContactId As String,
                                       ByRef payments As String, ByRef paymentRows As String)
        payments = ""
        paymentRows = ""
        If String.IsNullOrEmpty(safeContactId) Then Exit Sub

        Dim DT As DataTable = GetDataTable(EBDB,
            "SELECT PAYMENT_ID, TRANSACTIONID FROM UNITSHUB_PAYMENTS " &
            " WHERE NODE_ID = '" & safeNodeId & "' AND CONTACT_ID = '" & safeContactId & "'" &
            " ORDER BY PAYMENT_ID")
        If DT Is Nothing OrElse DT.Rows.Count = 0 Then Exit Sub

        Dim txIds As New List(Of String)
        Dim payIds As New List(Of String)
        For Each row As DataRow In DT.Rows
            txIds.Add(Convert.ToString(row("TRANSACTIONID")))
            payIds.Add(Convert.ToString(row("PAYMENT_ID")))
        Next

        payments = String.Join(",", txIds)
        paymentRows = String.Join(",", payIds)
    End Sub

    ''' <summary>Same as MainPage.GetCurrentUserID.</summary>
    Private Function GetCurrentUserID() As String
        Dim sessionUserID As String = Convert.ToString(Session("UserID"))
        If String.IsNullOrEmpty(sessionUserID) Then
            Return "2271" ' TEMP: fallback while real auth/session wiring is pending
        End If
        Return sessionUserID
    End Function

    ''' <summary>
    ''' Transactions Description from the group's REFERENCE_PHRASE:
    '''   - "[@Reference]" (or "@Reference", any case) in the phrase is replaced by the
    '''     Unit Reference, e.g. "Reservation of Unit [@Reference] - 1st payment"
    '''     -> "Reservation of Unit 318724 - 1st payment"; the reference is NOT added again.
    '''   - No placeholder: the reference is added at the end, unless the phrase already
    '''     ends with it (so it never shows twice).
    ''' Double spaces are collapsed, and a word repeated right after itself where the phrase
    ''' meets the reference ("Unit Unit") is kept once.
    ''' </summary>
    Private Function BuildDescription(Phrase As String, UnitRef As String) As String
        Dim Text As String = If(Phrase, "").Trim()
        Dim Ref As String = If(UnitRef, "").Trim()

        Dim HasPlaceholder As Boolean =
            Text.IndexOf("[@Reference]", StringComparison.OrdinalIgnoreCase) >= 0 OrElse
            Text.IndexOf("@Reference", StringComparison.OrdinalIgnoreCase) >= 0

        If HasPlaceholder Then
            ' Bracketed form first, then any bare "@Reference" left
            Text = System.Text.RegularExpressions.Regex.Replace(Text, "\[@Reference\]", Ref.Replace("$", "$$"),
                                                                System.Text.RegularExpressions.RegexOptions.IgnoreCase)
            Text = System.Text.RegularExpressions.Regex.Replace(Text, "@Reference", Ref.Replace("$", "$$"),
                                                                System.Text.RegularExpressions.RegexOptions.IgnoreCase)
        ElseIf Ref <> "" AndAlso Not Text.EndsWith(Ref, StringComparison.OrdinalIgnoreCase) Then
            Text = Text & " " & Ref
        End If

        Text = System.Text.RegularExpressions.Regex.Replace(Text, "\s{2,}", " ").Trim()

        ' Drop a word repeated right after itself, e.g. phrase "Reservation of Unit [@Reference]"
        ' with reference "Unit 318724" -> "Reservation of Unit 318724", not "... Unit Unit 318724"
        Text = System.Text.RegularExpressions.Regex.Replace(Text, "\b(\w+)\s+\1\b", "$1",
                                                            System.Text.RegularExpressions.RegexOptions.IgnoreCase)

        Return Text
    End Function

    ' ------------------------------------------------------------------
    ' Unit price + DEBIT / CREDIT formulas
    ' ------------------------------------------------------------------

    ''' <summary>
    ''' The unit's price from UNITSHUB_NODE_ATTRIBUTE_VALUE: the unit's node type comes from
    ''' UNITSHUB_NODES, the DISPLAY_ORDER of its "Price" attribute from UNITSHUB_ATTRIBUTES
    ''' (for this project + node type). Returns 0 if there's no price.
    ''' </summary>
    Private Function GetUnitPrice() As Decimal
        If String.IsNullOrWhiteSpace(lblPID.Text) Then Return 0D

        Dim NodeId As String = lblPID.Text.Trim().Replace("'", "''")
        Dim ProjectID As String = lblPRJID.Text.Trim().Replace("'", "''")

        Dim Node_Type As String = Convert.ToString(DB.RetreiveScalarSTRING(EBDB,
            "SELECT NODE_TYPE_ID FROM UNITSHUB_NODES WHERE NODE_ID = '" & NodeId & "'")).Trim()
        If Node_Type = "" Then Return 0D

        Dim PriceDisplayOrder As String = Convert.ToString(DB.RetreiveScalarSTRING(EBDB,
            " SELECT DISPLAY_ORDER " &
            " FROM   UNITSHUB_ATTRIBUTES " &
            " WHERE  UPPER(ATTRIBUTE_NAME) = UPPER('Price') " &
            "   AND  PROJECT_ID = '" & ProjectID & "' AND NODE_TYPE_ID = '" & Node_Type.Replace("'", "''") & "'")).Trim()
        If PriceDisplayOrder = "" Then Return 0D

        ' DISPLAY_ORDER may be stored as 9 in one table and '009' in the other - compare as numbers
        Dim PriceText As String = Convert.ToString(DB.RetreiveScalarSTRING(EBDB,
            " SELECT VALUE_TEXT FROM UNITSHUB_NODE_ATTRIBUTE_VALUE " &
            " WHERE  NODE_ID = '" & NodeId & "' " &
            "   AND  TO_NUMBER(DISPLAY_ORDER) = TO_NUMBER('" & PriceDisplayOrder.Replace("'", "''") & "')"))

        Return ParseAmount(PriceText)
    End Function

    ''' <summary>
    ''' Works out a DEBIT / CREDIT value from UNITSHUB_ATP_DETAILS. It can be a plain number
    ''' ("200", "1,500.000") or a formula using the unit price, e.g.
    '''     [UnitPrice]*[30%]-[200]   with a price of 100000   ->   29800
    ''' Rules: [UnitPrice] (also [Unit Price]) = the unit's price; [30%] or 30% = 0.30;
    ''' [200] = 200; + - * / and brackets ( ) are allowed. The result is rounded to 3
    ''' decimals. Empty = 0. Anything else (letters, unknown [names]) throws a FormatException.
    ''' </summary>
    Private Function EvaluateAmount(Formula As String, UnitPrice As Decimal) As Decimal
        Dim Expr As String = Convert.ToString(Formula).Trim()
        If Expr = "" Then Return 0D

        Dim RX = System.Text.RegularExpressions.RegexOptions.IgnoreCase
        Dim Num = Function(Value As Decimal) "(" & Value.ToString("0.0############", CultureInfo.InvariantCulture) & ")"

        ' [UnitPrice] / [Unit Price]
        Expr = System.Text.RegularExpressions.Regex.Replace(Expr, "\[\s*Unit\s*Price\s*\]", Num(UnitPrice), RX)

        ' Thousands separators inside numbers: 1,500.5 -> 1500.5
        Expr = System.Text.RegularExpressions.Regex.Replace(Expr, "(?<=\d),(?=\d{3}\b)", "")

        ' Percentages, with or without brackets: [30%] / 30% -> (0.3)
        Expr = System.Text.RegularExpressions.Regex.Replace(Expr, "\[?\s*(\d+(?:\.\d+)?)\s*%\s*\]?",
            Function(m) Num(Decimal.Parse(m.Groups(1).Value, CultureInfo.InvariantCulture) / 100D))

        ' Bracketed numbers: [200] -> (200.0)
        Expr = System.Text.RegularExpressions.Regex.Replace(Expr, "\[\s*(\d+(?:\.\d+)?)\s*\]",
            Function(m) Num(Decimal.Parse(m.Groups(1).Value, CultureInfo.InvariantCulture)))

        ' Plain whole numbers get a ".0" so the calculation never does integer division
        Expr = System.Text.RegularExpressions.Regex.Replace(Expr, "(?<![\d.])(\d+)(?![\d.])", "$1.0")

        ' Only numbers, + - * / ( ) and spaces may be left
        If Not System.Text.RegularExpressions.Regex.IsMatch(Expr, "^[\d\.\s\+\-\*/\(\)]+$") Then
            Throw New FormatException("'" & Formula & "' is not a valid amount or formula.")
        End If

        Dim Result As Object = New DataTable().Compute(Expr, Nothing)
        Return Math.Round(Convert.ToDecimal(Result, CultureInfo.InvariantCulture), 3, MidpointRounding.AwayFromZero)
    End Function

    ' ------------------------------------------------------------------
    ' Helpers
    ' ------------------------------------------------------------------

    ''' <summary>
    ''' ICBS balance of an account (CACC_NUM), as a positive amount for a credit balance.
    ''' </summary>
    Function GetAccountBalance(ByVal AC As String) As Double

        If AC = "" Then Return (0)
        Dim lcSQL = ""
        lcSQL = lcSQL + vbCrLf + " SELECT ((NVL(T1.CACC_AVAIL_BAL,0) + "
        lcSQL = lcSQL + vbCrLf + "          NVL(T1.CACC_AVAIL_MV_BAL, 0) + "
        lcSQL = lcSQL + vbCrLf + "          NVL(T1.CACC_AVAIL_MV_OTHR_BR, 0))) As BAL  "
        lcSQL = lcSQL + vbCrLf + " FROM BBSD_CUST_ACCOUNTS@ICBS T1 "
        lcSQL = lcSQL + vbCrLf + " where CACC_NUM='" + AC.Replace("'", "''") + "'"
        Dim dr As Data.DataRow = GetDataRow(EBDB, lcSQL)
        GetAccountBalance = 0
        If Not IsNothing(dr) Then
            GetAccountBalance = dr("BAL") * (-1)
        End If
    End Function

    ''' <summary>
    ''' Account number part of UNIT_ACCOUNT: everything after the first dash
    ''' (810-123456789012 -> 123456789012). If there's no dash, the whole value.
    ''' </summary>
    Private Function GetAccountNumber(Account As String) As String
        Dim Text As String = Convert.ToString(Account).Trim()
        Dim DashPos As Integer = Text.IndexOf("-"c)
        If DashPos < 0 Then Return Text
        Return Text.Substring(DashPos + 1).Trim()
    End Function

    ''' <summary>
    ''' The 7th, 8th and 9th characters after the first dash of an account like
    ''' 810-123456789012 (-> "789"). Returns "" if there's no dash, the part after it
    ''' is shorter than 9 characters, or those three aren't all digits.
    ''' </summary>
    Private Function GetAccountTypeFromAccount(Account As String) As String
        Dim Text As String = Convert.ToString(Account).Trim()
        Dim DashPos As Integer = Text.IndexOf("-"c)
        If DashPos < 0 Then Return ""

        Dim AfterDash As String = Text.Substring(DashPos + 1)
        If AfterDash.Length < 9 Then Return ""

        Dim Digits As String = AfterDash.Substring(6, 3)
        For Each c As Char In Digits
            If Not Char.IsDigit(c) Then Return ""
        Next
        Return Digits
    End Function

    ''' <summary>First of the given columns that exists in the row, as text ("" if none).</summary>
    Private Function GetColumnValue(Row As DataRow, ParamArray Columns() As String) As String
        For Each Column As String In Columns
            If Row.Table.Columns.Contains(Column) Then Return Convert.ToString(Row(Column)).Trim()
        Next
        Return ""
    End Function

    ''' <summary>Reads an amount like "89,260.544" (0 if empty or not a number).</summary>
    Private Function ParseAmount(Text As String) As Decimal
        Dim Value As Decimal
        If Decimal.TryParse(Convert.ToString(Text).Replace(",", "").Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, Value) Then
            Return Value
        End If
        Return 0D
    End Function

    ''' <summary>Amount with thousands separators and 3 decimals, e.g. 89,260.544.</summary>
    Private Function FormatAmount(Value As Decimal) As String
        Return Value.ToString("N3", CultureInfo.InvariantCulture)
    End Function
    ''' <summary>
    ''' Checks the grid before posting: every line needs an account (not empty).
    ''' Shows a red message and returns False otherwise.
    ''' </summary>
    Function Validate_Entries() As Boolean
        Dim Bad As New List(Of String)
        Dim LineNo As Integer = 0
        For Each r As GridViewRow In gvTransactions.Rows
            If r.RowType <> DataControlRowType.DataRow Then Continue For
            LineNo += 1
            Dim Acc As String = GetCellText(r, 0)
            If Acc = "" Then
                Bad.Add("line " & LineNo & " (no account)")
            End If
        Next

        If LineNo = 0 Then
            ShowMessage("Nothing to post.", False)
            Return False
        End If

        If Bad.Count > 0 Then
            ShowMessage("Can't post - these lines have no account: " & String.Join(", ", Bad) & ".", False)
            Return False
        End If

        Return True
    End Function

    ''' <summary>One posted line of the Transactions grid, kept for UNITSHUB_AUTOTRANSFER_DONE_D.</summary>
    Private Class PostedLine
        Public Account As String
        Public EntryType As String
        Public Transaction As String
        Public Amount As Decimal
        Public Reference As String
        Public Description As String
        Public Entry As Integer
    End Class

    ''' <summary>
    ''' Posts the lines of gvTransactions one by one. If the loop finishes with every
    ''' line posted, the batch is recorded in UNITSHUB_AUTOTRANSFER_DONE_T/_D and a green
    ''' message is shown; otherwise nothing is recorded.
    ''' </summary>
    Protected Sub btnPost_Click(sender As Object, e As EventArgs) Handles btnPost.Click
        'If Get_Dannat_Status() = "Handed Over" Then
        '    udf.ShowMessage(Page, "Already Handed Over", True)
        '    reflectPropertyStatusOnButtons()
        '    Exit Sub
        'End If

        If Validate_Entries() Then
            '    'cmd_Post.Visible = False
            '    'cmd_Cancel.Visible = False
            'Dim Host As New clsCore
            'Host.InitializeHost("ICBS")
            Dim lnEntries As Integer = gvTransactions.Rows.Count
            Dim PdfTitle As String = ""
            Dim FolderName As String = ""
            Dim lnTraNo As Integer = 0
            Dim lnEntry As Integer = 0

            ' Lines posted in this run, saved to UNITSHUB_AUTOTRANSFER_DONE_T/_D only if
            ' every line went through (AllPosted stays True).
            Dim PostedLines As New List(Of PostedLine)
            Dim AllPosted As Boolean = True

            For Each r As GridViewRow In gvTransactions.Rows
                If r.RowType = DataControlRowType.DataRow Then
                    Dim Acc As String = GetCellText(r, 0)
                    Dim typ As String = GetCellText(r, 1)
                    Dim tra As String = GetCellText(r, 2)                       ' "D" or "C"
                    Dim amt As Double = CDbl(ParseAmount(GetCellText(r, 3)))    ' shown as "1,234.500"
                    Dim ref As String = GetCellText(r, 4)
                    Dim Nar As String = GetCellText(r, 5)

                    lnEntry = lnEntry + 1
                    Dim lcError As String = ""
                    'If Not Host.Pass_Batch_Entry("PSS", lnEntries, lnEntry, Acc, tra, amt, ref, Nar, lcError, lnTraNo) Then
                    '    ShowMessage(lcError, False)
                    '    AllPosted = False
                    '    If lnEntry = 1 Then
                    '        Exit Sub
                    '    End If
                    'End If

                    PostedLines.Add(New PostedLine With {
                        .Account = Acc,
                        .EntryType = typ,
                        .Transaction = tra,
                        .Amount = CDec(amt),
                        .Reference = ref,
                        .Description = Nar,
                        .Entry = lnEntry
                    })
                End If
            Next

            ' Loop finished: record the transfer only when every line was posted
            If AllPosted AndAlso PostedLines.Count > 0 Then
                Dim DateNTime As Long = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                Dim UnitPaymentId As String = ""

                Try
                    UnitPaymentId = SaveAutoTransferDone(PostedLines, lnTraNo, DateNTime)
                Catch ex As Exception
                    ' The entries are already posted in ICBS at this point - say so clearly
                    ShowMessage("Posted (Transaction No. " & lnTraNo & "), but saving the record in UNITSHUB_AUTOTRANSFER_DONE_T/_D failed: " & ex.Message, False)
                    Exit Sub
                End Try

                ' Each follow-up step reports its own problem; the message bar shows them all
                Dim Problems As New List(Of String)

                ' Take the invoice number now, at post time (it was only a preview when the
                ' form opened). The payment update below and GenerateInvoice both use this
                ' same lblInvoiceNo, so the payment and the invoice always agree.
                LoadNextInvoiceNo()

                Try
                    If UpdatePaymentAutoTransfer(UnitPaymentId, DateNTime) = 0 Then
                        Problems.Add("no matching payment was found in UNITSHUB_PAYMENTS to mark")
                    End If
                Catch ex As Exception
                    Problems.Add("updating UNITSHUB_PAYMENTS failed: " & ex.Message)
                End Try

                ' Change the unit's status if the action requires it, and write the history row
                Dim StatusChanged As Boolean = False
                Try
                    StatusChanged = ApplyActionAfterPost(UnitPaymentId, lnTraNo, PostedLines.Count)
                Catch ex As Exception
                    Problems.Add("changing the unit status / writing the history failed: " & ex.Message)
                End Try

                Dim Done As String = "Posted " & PostedLines.Count & " entries (Transaction No. " & lnTraNo & ", ID " & UnitPaymentId & ")" &
                                     If(StatusChanged, " and changed the unit status to " & GetStatusName(lblToStatusID.Text.Trim()), "")
                If Problems.Count = 0 Then
                    ShowMessage(Done & ".", True)
                Else
                    ShowMessage(Done & ", but " & String.Join("; ", Problems) & ".", False)
                End If




                ' Invoice: generated now, and the form stays open so the user can use
                ' Display Invoice / Re-Generate Invoice (Post is disabled at the same time).
                ' The form doesn't close itself; the user closes it with [x], which makes
                ' MainPage refresh its grid (the status/history were already done above).
                Try
                    GenerateInvoice()
                Catch ex As Exception
                    ShowMessage(Done & ", but generating the invoice failed: " & ex.Message, False)
                End Try



            ElseIf PostedLines.Count = 0 Then
                ShowMessage("Nothing to post.", False)
            End If



            '=========================================================================================
            '=========================================================================================

            'lnTraNo = 1234566
            'If lnTraNo = 0 Then
            '    udf.ShowMessage(Page, "Nothing Posted", True)
            'Else
            '    Try
            '        GenerateInvoice(InvoiceNum:=lnTraNo,
            '                        DisplayInvoice:=True,
            '                        PdfTitle:=PdfTitle,
            '                        FolderName:=FolderName)

            '        udf.SetOnClientClick(cmd_displayInvoice, "PDF_Viewer.aspx?FolderName=" & FolderName & "&FileName=" + PdfTitle, 500,, , True)
            '        Try
            '            UpdateInvoiceNoField(InvoiceNo:=lnTraNo)
            '            UpdatePDFTitleField(PdfTitle:=PdfTitle)
            '            UpdatePropertyStatus()
            '        Catch ex1 As Exception
            '            udf.ShowMessage(Page, "( posted ) but you need To change the status (manually) ", True)
            '        End Try
            '        reflectPropertyStatusOnButtons(lnTraNo:=lnTraNo)
            '        pnl_Entries.Visible = False
            '    Catch ex As Exception
            '        IO.File.AppendAllText(AppDomain.CurrentDomain.BaseDirectory & "\Logs.txt", ex.Message)
            '        udf.ShowMessage(Page, "An Error happened While generating invoice", True)
            '    End Try
            'End If



        End If

    End Sub


    ''' <summary>
    ''' Invoice step after a successful post. For now it works out and keeps the invoice
    ''' amounts (SaveInvoiceValues); building / saving the invoice document itself is
    ''' still to be added (TODO).
    ''' </summary>
    Private Sub GenerateInvoice()
        ' Unit price as loaded for the Transactions grid; look it up again if it's missing
        Dim lnUnit_Price As Decimal = ParseAmount(lblUnitPrice.Text)
        If lnUnit_Price = 0D Then lnUnit_Price = GetUnitPrice()

        SaveInvoiceValues(lnUnit_Price)

        ' TODO: build / save the invoice using Commission_fee_value.Text and VAT_output_value.Text
    End Sub

    ''' <summary>
    ''' Keeps the invoice amounts on the form (hidden labels in AutoTransfer.aspx):
    '''   Commission_fee_value = unit price x 0.7%   (0.007)
    '''   VAT_output_value     = unit price x 0.07%  (0.0007, i.e. 10% of the commission)
    ''' Both formatted "0.0##", e.g. price 100000 -> 700.0 and 70.0
    ''' </summary>
    Private Sub SaveInvoiceValues(lnUnit_Price As Decimal)
        Commission_fee_value.Text = Format(CDec(lnUnit_Price) * 0.007D, "0.0##")
        VAT_output_value.Text = Format(CDec(lnUnit_Price) * 0.0007D, "0.0##")

        Dim SQL As String = ""
        SQL = SQL + vbCrLf + " SELECT n.PROJECT_ID AS ID, "
        SQL = SQL + vbCrLf + "        MAX(CASE WHEN UPPER(a.ATTRIBUTE_NAME) = 'TITLE'            THEN v.VALUE_TEXT END) AS TITLE, "
        SQL = SQL + vbCrLf + "        MAX(CASE WHEN UPPER(a.ATTRIBUTE_NAME) = 'CONTRACTORCUSTNO' THEN v.VALUE_TEXT END) AS CONTRACTORCUSTNO, "
        SQL = SQL + vbCrLf + "        '" & VAT_output_value.Text & "' AS VAT, "
        SQL = SQL + vbCrLf + "        MAX(CASE WHEN UPPER(a.ATTRIBUTE_NAME) = 'VAT'              THEN v.VALUE_TEXT END) AS VATPercentage, "
        SQL = SQL + vbCrLf + "        MAX(CASE WHEN UPPER(a.ATTRIBUTE_NAME) = 'FEES'             THEN v.VALUE_TEXT END) AS FEES, "
        SQL = SQL + vbCrLf + "        MAX(CASE WHEN UPPER(a.ATTRIBUTE_NAME) = 'DISCOUNT'         THEN v.VALUE_TEXT END) AS DISCOUNT, "
        SQL = SQL + vbCrLf + "        '" & Commission_fee_value.Text & "' AS COMMISION "
        SQL = SQL + vbCrLf + " FROM   UNITSHUB_NODES n "
        SQL = SQL + vbCrLf + " JOIN   UNITSHUB_NODE_ATTRIBUTE_VALUE v "
        SQL = SQL + vbCrLf + "        ON v.NODE_ID = n.NODE_ID "
        SQL = SQL + vbCrLf + " JOIN   UNITSHUB_ATTRIBUTES a "
        SQL = SQL + vbCrLf + "        ON  a.PROJECT_ID   = n.PROJECT_ID "
        SQL = SQL + vbCrLf + "        AND a.NODE_TYPE_ID = n.NODE_TYPE_ID "
        SQL = SQL + vbCrLf + "        AND TO_NUMBER(a.DISPLAY_ORDER) = TO_NUMBER(v.DISPLAY_ORDER) "
        SQL = SQL + vbCrLf + " WHERE  n.PROJECT_ID   = '" & lblPRJID.Text & "' "
        SQL = SQL + vbCrLf + "   AND  n.NODE_TYPE_ID = '000' "
        SQL = SQL + vbCrLf + " GROUP BY n.PROJECT_ID, n.NODE_ID "
        Dim dtProjectsCharges As New Data.DataTable
        dtProjectsCharges = GetDataTable(EBDB_CS, SQL)

        Dim dtCustomer As New Data.DataTable
        dtCustomer = PF.DictionaryToDataTable(dict:=AddNameTINaddress(CustomerNo:=dtProjectsCharges.Rows(0)("CONTRACTORCUSTNO").ToString))


        '==========================================================================
        Dim dtUnits As New DataTable
        ' Add columns
        dtUnits.Columns.Add("REFERENCE", GetType(String))
        dtUnits.Columns.Add("ID", GetType(String))
        dtUnits.Columns.Add("DT", GetType(String))
        dtUnits.Columns.Add("TransactionNo", GetType(String))

        Dim DR As DataRow
        DR = dtUnits.NewRow
        DR("REFERENCE") = lblUnitRef.Text
        DR("ID") = lblPID.Text
        DR("DT") = Format(Now, "yyyy-MM-dd")
        DR("TransactionNo") = lblInvoiceNo.Text

        dtUnits.Rows.Add(DR)
        '=============================================================
        Dim dtONEINVOICE As New DataTable

        Dim GrandParameter As New Dictionary(Of String, String)
        GrandParameter = Bringparameters(dtCustomer:=dtCustomer, dtProject:=dtProjectsCharges, dtTansaction:=dtUnits)

        dtONEINVOICE.Rows.Clear()
        dtONEINVOICE.AcceptChanges()
        dtONEINVOICE = PF.DictionaryToDataTable(GrandParameter)

        GeneratePDF(dtONEINVOICE)

    End Sub

    Sub GeneratePDF(ByVal DT As DataTable)
        Dim DS As New Data.DataSet
        DT.TableName = "InvoiceComponents"
        Dim p As New ReportDocument
        IO.File.AppendAllText(AppDomain.CurrentDomain.BaseDirectory & "\Logs.txt", "I am Here 747")

        p.Load(Server.MapPath(GetAppPath() & "\ReportsTemplate\InvoiceReport.rpt"))
        IO.File.AppendAllText(AppDomain.CurrentDomain.BaseDirectory & "\Logs.txt", "I am Here 750")
        'IO.File.AppendAllText("C:\IntraApps\TST\Links\Modules\Select_Ext\logs.txt", vbCrLf & "Reached Here 748")

        Dim folderPath As String = Server.MapPath(GetAppPath() & "\ReportsTemplate\" & DT.Rows(0)("Project").ToString)
        Dim FileName As String
        'IO.File.AppendAllText("C:\IntraApps\TST\Links\Modules\Select_Ext\logs.txt", vbCrLf & folderPath)
        If Not Directory.Exists(folderPath) Then
            Directory.CreateDirectory(folderPath)
        End If

        DS.Tables.Add(DT)
        DS.DataSetName = "Invoices-01"
        p.SetDataSource(DS)
        p.Refresh()

        'IO.File.AppendAllText("C:\IntraApps\TST\Links\Modules\Select_Ext\logs.txt", "Reached Here 752")

        Dim CurrentUser As String = GetCurrentUserID()


        FileName = folderPath & "\" & DT.Rows(0)("InvoiceNum").ToString & ".pdf"
        p.ExportToDisk(CrystalDecisions.Shared.ExportFormatType.PortableDocFormat, FileName.Replace("\\", "\"))
        p.Close()

        ' PDF is on disk: ReportsTemplate\<Project>\<InvoiceNum>.pdf - remember it and
        ' switch on Display Invoice / Re-Generate Invoice
        ViewState("InvoiceProject") = DT.Rows(0)("Project").ToString
        ViewState("InvoiceNum") = DT.Rows(0)("InvoiceNum").ToString
        EnableInvoiceButtons()
    End Sub

    Private Const OpenActionPopupReturnKey As String = "DisplayInvoicePopup"
    Private InvoicePopupRegistered As Boolean = False

    ''' <summary>
    ''' After an invoice PDF has been generated (ViewState InvoiceNum / InvoiceProject):
    ''' enables btnDisplayInvoice and btnRegenerateInvoice, disables btnPost (the lines
    ''' are already posted - no second post), and attaches the DisplayInvoice.aspx popup
    ''' to btnDisplayInvoice (once per request).
    ''' Before that, Display / Re-Generate stay disabled as set in the markup.
    ''' </summary>
    Private Sub EnableInvoiceButtons()
        Dim InvoiceNum As String = Convert.ToString(ViewState("InvoiceNum"))
        If InvoiceNum = "" Then Exit Sub

        btnDisplayInvoice.Enabled = True
        btnRegenerateInvoice.Enabled = True
        btnPost.Enabled = False

        If InvoicePopupRegistered Then Exit Sub
        InvoicePopupRegistered = True

        Dim Url As String = "DisplayInvoice.aspx?PDF=" & Server.UrlEncode(InvoiceNum) &
                            "&Project=" & Server.UrlEncode(Convert.ToString(ViewState("InvoiceProject")))

        VendorPopupHelper.RegisterVendorPopup(Me,
                                              btnDisplayInvoice,
                                              Url,
                                              1000, 0,
                                              PopupPlacement.Center,
                                              "",
                                              VendorPopupHelper.PopupDisplayMode.FrameOnly,
                                              returnKey:=OpenActionPopupReturnKey)
    End Sub

    ''' <summary>Builds the invoice PDF again, with the same invoice number.</summary>
    Protected Sub btnRegenerateInvoice_Click(sender As Object, e As EventArgs) Handles btnRegenerateInvoice.Click
        Try
            GenerateInvoice()
            ShowMessage("Invoice " & Convert.ToString(ViewState("InvoiceNum")) & " was generated again.", True)
        Catch ex As Exception
            ShowMessage("Re-generating the invoice failed: " & ex.Message, False)
        End Try
    End Sub

    Function GetAppPath() As String
        Dim lcPath As String = HttpRuntime.AppDomainAppVirtualPath
        GetAppPath = lcPath
        If lcPath = "\" Or lcPath = "/" Then
            GetAppPath = ""
        End If
    End Function


    Public Function Bringparameters(dtCustomer As DataTable,
                            dtProject As DataTable,
                            dtTansaction As DataTable) As Dictionary(Of String, String)
        Return Bringparameters(drCustomerRow:=dtCustomer.Rows(0),
                               drProjectRow:=dtProject.Rows(0),
                               drTansactionRow:=dtTansaction.Rows(0))
    End Function

    ''' <summary>Number from a cell/text; "", NULL or anything non-numeric gives 0.</summary>
    Private Function ToDbl(Value As Object) As Double
        If Value Is Nothing OrElse Value Is DBNull.Value Then Return 0
        Dim Text As String = Convert.ToString(Value).Replace(",", "").Trim()
        Dim Result As Double
        'If Double.TryParse(Text, Globalization.NumberStyles.Number, Globalization.CultureInfo.InvariantCulture, Result) Then
        If Double.TryParse(Text, NumberStyles.Number, CultureInfo.InvariantCulture, Result) Then
            Return Result
        End If
        Return 0
    End Function

    Public Function Bringparameters(drCustomerRow As DataRow,
                            drProjectRow As DataRow,
                            drTansactionRow As DataRow) As Dictionary(Of String, String)

        Dim vat As Double = CDbl(drProjectRow("VAT")) 'VAT_percentage
        Dim GrandTotal As Double = 0.0
        GrandTotal = ToDbl(drProjectRow("COMMISION")) +
                      ToDbl(drProjectRow("Fees")) +
                      ToDbl(vat) -
                      ToDbl(drProjectRow("Discount"))

        GrandTotal = Format(GrandTotal, "#,###,##0.000")

        Dim INV_NUM As String = lblInvoiceNo.Text

        Dim parameters As New Dictionary(Of String, String)
        parameters.Add("TO", drCustomerRow("FULL_NAME").ToString)
        parameters.Add("Address", drCustomerRow("ADDRESS").ToString)
        parameters.Add("ClientID", Right("000000" & drCustomerRow("ClientNo").ToString, 6))
        parameters.Add("TIN", drCustomerRow("TIN").ToString)
        parameters.Add("DateOfSupply", drTansactionRow("DT").ToString)
        parameters.Add("Description", "Commission for assisting in the sale of unit (" & drTansactionRow("REFERENCE").ToString & ")")
        parameters.Add("DueAmount", CDbl(drProjectRow("COMMISION").ToString).ToString("#.000"))
        parameters.Add("InvoiceNum", INV_NUM)
        parameters.Add("DateIssues", Date.Now().ToString("yyyy-MM-dd"))
        parameters.Add("DueAmountTotal", CDbl(drProjectRow("COMMISION").ToString).ToString("#.000"))
        parameters.Add("TransactionFee", drProjectRow("Fees").ToString)
        parameters.Add("BankTIN", "210011959000002")
        parameters.Add("Discount", drProjectRow("Discount").ToString)
        parameters.Add("VAT", vat.ToString("#.000"))
        parameters.Add("VATPercentage", 100 * ToDbl(drProjectRow("VATPercentage")))
        parameters.Add("TOTAL", GrandTotal.ToString("#.000"))
        parameters.Add("UnitReference", drTansactionRow("REFERENCE").ToString)
        parameters.Add("Project", drProjectRow("TITLE").ToString)

        Return parameters
    End Function



    Function AddNameTINaddress(ByVal CustomerNo As String) As Generic.Dictionary(Of String, String)
        Dim parameters As New Generic.Dictionary(Of String, String)

        Dim custType = Chech_Moral_Physical(Cust_ID:=CustomerNo)

        Dim dr As Data.DataRow
        If custType("CUSM_TYPE") = "1" Then
            dr = Get_Physical(CustomerNo)
        Else custType("CUSM_TYPE") = "2"
            dr = Get_Moral(CustomerNo)
        End If


        'TODO: remove the following in live unit ==========================================
        Dim dt As New DataTable("Customers")
        ' --- Define schema ---
        dt.Columns.Add("ID", GetType(String))
        dt.Columns.Add("CUSTOMER_NUMBER", GetType(String))
        dt.Columns.Add("FULL_NAME", GetType(String))
        dt.Columns.Add("TIN", GetType(String))
        dt.Columns.Add("BLOCK", GetType(String))
        dt.Columns.Add("BUILDING", GetType(String))
        dt.Columns.Add("FLAT", GetType(String))
        dt.Columns.Add("ROAD", GetType(String))

        Dim dr1 As DataRow
        dr1 = dt.NewRow
        dt.AcceptChanges()

        Dim address As New List(Of String)
        If Not String.IsNullOrEmpty(dr("BLOCK").ToString()) Then
            address.Add("Block " + dr("BLOCK").ToString())
        End If
        If Not String.IsNullOrEmpty(dr("ROAD").ToString()) Then
            address.Add("Road " + dr("ROAD").ToString())
        End If
        If Not String.IsNullOrEmpty(dr("BUILDING").ToString()) Then
            address.Add("Biulding " + dr("BUILDING").ToString())
        End If
        If Not String.IsNullOrEmpty(dr("FLAT").ToString()) Then
            address.Add("Flat " + dr("FLAT").ToString())
        End If

        parameters.Add("TIN", dr("TIN").ToString)
        parameters.Add("FULL_NAME", dr("FULL_NAME").ToString)
        parameters.Add("ADDRESS", Join(address.ToArray, "-"))
        parameters.Add("ClientNo", CustomerNo)

        Return parameters
    End Function

    Protected Function Chech_Moral_Physical(ByVal Cust_ID As String) As Data.DataRow
        Dim lcSql As String = ""
        lcSql = lcSql + " Select  CUSM_TYPE FROM BBSD_CUST_MEMBERS@ICBS WHERE CUST_ID='" + Cust_ID + "' "
        Dim dr = GetDataRow(EBDB, lcSql)
        Return (dr)
    End Function

    Protected Function Get_Physical(ByVal Cust_ID As String) As Data.DataRow
        Dim lcSql As String = ""
        lcSql = lcSql + " Select  "
        lcSql = lcSql + "       T1.PHPR_ID AS ID, T3.CUST_ID AS Customer_Number, "
        lcSql = lcSql + "       T1.PHPR_FULL_NAME FULL_NAME, T1.PHPR_TAX_ID TIN, "
        lcSql = lcSql + "       T4.LCTY_CODE AS BLOCK, T4.PADR_B_LINE_2 AS BUILDING, T4.PADR_B_LINE_3 AS FLAT, T4.PADR_B_LINE_4 AS ROAD"
        lcSql = lcSql + " from bbsd_physical_persons@ICBS T1 "
        lcSql = lcSql + " left join bbsd_cust_members@ICBS T2 on T1.PHPR_ID=T2.CUSM_ID "
        lcSql = lcSql + " Left Join BBSD_CUSTOMERS@ICBS T3 on T2.CUST_ID=T3.CUST_ID "
        lcSql = lcSql + " Left Join BBSD_PHPR_ADDRESSES@ICBS T4 on T1.PHPR_ID=T4.PHPR_ID"
        lcSql = lcSql + " WHERE T3.CUST_ID='" + Cust_ID + "'"
        Dim dr = GetDataRow(EBDB, lcSql)
        Return (dr)
    End Function

    Protected Function Get_Moral(Cust_ID As String) As Data.DataRow
        Dim lcSql As String = ""
        lcSql = lcSql + vbCrLf + " Select distinct   "
        lcSql = lcSql + vbCrLf + " substr('000000' || T1.MRPR_ID, -6) AS ID,  "
        lcSql = lcSql + vbCrLf + " T1.MRPR_ID As Customer_Number,   "
        lcSql = lcSql + vbCrLf + " T1.MRPR_B_NAME FULL_NAME,  "
        lcSql = lcSql + vbCrLf + " replace(T1.MRPR_TAX_ID_NBR,'-','') TIN,   "
        lcSql = lcSql + vbCrLf + " replace(T4.LCTY_CODE,'-','') AS BLOCK,   "
        lcSql = lcSql + vbCrLf + " replace(T4.MADR_B_LINE_2,'-','') As BUILDING,   "
        lcSql = lcSql + vbCrLf + " replace(T4.MADR_B_LINE_3,'-','') AS FLAT,   "
        lcSql = lcSql + vbCrLf + " replace(T4.MADR_B_LINE_4,'-','') AS ROAD   "
        lcSql = lcSql + vbCrLf + " From bbsd_moral_persons@ICBS T1   "
        lcSql = lcSql + vbCrLf + " Left Join bbsd_cust_members@ICBS T2 on T1.MRPR_ID=T2.CUSM_ID  Left Join BBSD_CUSTOMERS@ICBS T3 on T2.CUST_ID=T3.CUST_ID   "
        lcSql = lcSql + vbCrLf + " Left Join BBSD_MRPR_ADDRESSES@ICBS T4 on T1.MRPR_ID=T4.MRPR_ID  WHERE T2.CUST_ID ='" & Cust_ID & "' "


        Dim dr = GetDataRow(EBDB, lcSql)
        Return (dr)
    End Function






    ''' <summary>
    ''' Text of a BoundField cell as plain text: BoundField HTML-encodes values
    ''' (e.g. "&amp;") and renders empty cells as "&nbsp;", so decode and trim.
    ''' </summary>
    Private Function GetCellText(Row As GridViewRow, ColumnIndex As Integer) As String
        If ColumnIndex < 0 OrElse ColumnIndex >= Row.Cells.Count Then Return ""
        Dim Text As String = Server.HtmlDecode(Row.Cells(ColumnIndex).Text)
        Return Text.Replace(ChrW(160), " ").Trim()
    End Function


    ''' <summary>
    ''' Records a successful AutoTransfer: one header row in UNITSHUB_AUTOTRANSFER_DONE_T
    ''' and one row per posted line in UNITSHUB_AUTOTRANSFER_DONE_D, all in ONE statement
    ''' (INSERT ALL) so either the whole batch is saved or nothing is.
    '''   UNITPAYMENT_ID = next number after the highest existing header, 7 digits
    '''                    (0000001 ...), shared by the header and all its lines; returned
    '''   Header: NODE_ID / CONTACT_ID = lblPID / lblCID, PLAN_ID / SEQ = lblPlanID /
    '''           lblPlanSeq, TRANO = lnTraNo returned by the posting
    '''   Lines:  ENTRY = entry number in the batch, ACCOUNT .. DESCRIPTION = the grid
    '''           line, DATENTIME = now in Unix seconds (same for the whole batch)
    ''' Throws if the insert fails (the caller shows the message).
    ''' </summary>
    Private Function SaveAutoTransferDone(Lines As List(Of PostedLine), TraNo As Integer, DateNTimeValue As Long) As String
        If Lines Is Nothing OrElse Lines.Count = 0 Then Return ""

        Dim Q = Function(Value As String) "'" & If(Value, "").Replace("'", "''") & "'"

        Dim DateNTime As String = DateNTimeValue.ToString(CultureInfo.InvariantCulture)

        ' Next header ID, worked out first so it can also be stamped on the payment.
        ' The primary key on _T rejects the batch if two posts ever get the same ID.
        Dim NewId As String = Convert.ToString(DB.RetreiveScalarSTRING(EBDB,
            "SELECT LPAD(TO_CHAR(NVL(MAX(TO_NUMBER(UNITPAYMENT_ID)), 0) + 1), 7, '0') FROM UNITSHUB_AUTOTRANSFER_DONE_T " &
            "WHERE REGEXP_LIKE(UNITPAYMENT_ID, '^[0-9]+$')")).Trim()
        If NewId = "" Then NewId = "0000001"
        Dim NewIdSql As String = Q(NewId)

        Dim SQL As New System.Text.StringBuilder()
        SQL.AppendLine("INSERT ALL")

        ' Header
        SQL.AppendLine("  INTO UNITSHUB_AUTOTRANSFER_DONE_T (UNITPAYMENT_ID, NODE_ID, CONTACT_ID, PLAN_ID, SEQ, TRANO)")
        SQL.AppendLine("  VALUES (" & NewIdSql & ", " &
                       Q(lblPID.Text.Trim()) & ", " & Q(lblCID.Text.Trim()) & ", " &
                       Q(lblPlanID.Text.Trim()) & ", " & Q(lblPlanSeq.Text.Trim()) & ", " &
                       Q(TraNo.ToString(CultureInfo.InvariantCulture)) & ")")

        ' Lines
        For Each L As PostedLine In Lines
            SQL.AppendLine("  INTO UNITSHUB_AUTOTRANSFER_DONE_D " &
                           "(UNITPAYMENT_ID, ENTRY, ACCOUNT, TYPE, TRANSACTION, AMOUNT, REFERENCE, DESCRIPTION, DATENTIME)")
            SQL.AppendLine("  VALUES (" & NewIdSql & ", " &
                           L.Entry.ToString(CultureInfo.InvariantCulture) & ", " &
                           Q(L.Account) & ", " & Q(L.EntryType) & ", " & Q(L.Transaction) & ", " &
                           L.Amount.ToString(CultureInfo.InvariantCulture) & ", " &
                           Q(L.Reference) & ", " & Q(L.Description) & ", " &
                           DateNTime & ")")
        Next

        SQL.AppendLine("SELECT 1 FROM DUAL")

        DB.ExecuteNonQuery(EBDB_CS, SQL.ToString())
        Return NewId
    End Function

    ''' <summary>
    ''' Marks the payment this AutoTransfer was implemented on (UNITSHUB_PAYMENTS row of
    ''' this unit + customer + PLAN_ID/SEQ from the group's IMPLEMENT_ON, ACTIVE = 'Y'):
    '''   LASTAUTOTRANSFER     = the AutoTransfer group's title
    '''   LASTAUTOTRANSFERID   = UNITPAYMENT_ID of the batch just saved
    '''   LASTAUTOTRANSFERDATE = same Unix timestamp as the batch
    '''   INVOICENUM           = the invoice number generated for this post (lblInvoiceNo)
    ''' All in ONE UPDATE - this is the only place the form writes to UNITSHUB_PAYMENTS.
    ''' Returns how many payment rows were updated (0 = no matching payment).
    ''' </summary>
    Private Function UpdatePaymentAutoTransfer(UnitPaymentId As String, DateNTime As Long) As Integer
        Dim Q = Function(Value As String) "'" & If(Value, "").Replace("'", "''") & "'"

        Dim WhereClause As String =
            " WHERE NODE_ID    = " & Q(lblPID.Text.Trim()) &
            "   AND CONTACT_ID = " & Q(lblCID.Text.Trim()) &
            "   AND PLAN_ID    = " & Q(lblPlanID.Text.Trim()) &
            "   AND SEQ        = " & Q(lblPlanSeq.Text.Trim()) &
            "   AND ACTIVE     = 'Y'"

        Dim Matches As Integer = 0
        Integer.TryParse(Convert.ToString(DB.RetreiveScalarSTRING(EBDB, "SELECT COUNT(*) FROM UNITSHUB_PAYMENTS" & WhereClause)), Matches)
        If Matches = 0 Then Return 0

        DB.ExecuteNonQuery(EBDB_CS,
            "UPDATE UNITSHUB_PAYMENTS SET " &
            "LASTAUTOTRANSFER = " & Q(lblATGroupTitle.Text.Trim()) & ", " &
            "LASTAUTOTRANSFERID = " & Q(UnitPaymentId) & ", " &
            "LASTAUTOTRANSFERDATE = " & DateNTime.ToString(CultureInfo.InvariantCulture) & ", " &
            "INVOICENUM = " & Q(lblInvoiceNo.Text.Trim()) &
            WhereClause)

        Return Matches
    End Function

    ''' <summary>
    ''' Shows the message bar above "Transactions": green for success, red otherwise.
    ''' It only shows for the request it was set in (EnableViewState is off), so it
    ''' disappears again on the next postback.
    ''' </summary>
    Private Sub ShowMessage(Message As String, IsSuccess As Boolean)
        lblMessage.Text = Server.HtmlEncode(Message)
        lblMessage.CssClass = "msg-bar " & If(IsSuccess, "msg-success", "msg-error")
        lblMessage.Visible = Not String.IsNullOrEmpty(Message)
    End Sub

    Protected Sub imgClose_Click(sender As Object, e As ImageClickEventArgs) Handles imgClose.Click
        VendorPopupHelper.RegisterPopupSelectionAndClose(Me, False, skipPostBack:=False)
    End Sub
    Protected Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        GenerateInvoice()
    End Sub
End Class