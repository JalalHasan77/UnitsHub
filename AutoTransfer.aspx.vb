Imports System.Data
Imports System.DateTime
Imports System.Drawing
Imports System.Globalization

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
        End If
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
    '''   Account     = ACCOUNT_NUMBER; if it contains "xxx" (any case), the Pre Sales
    '''                 Account (UNIT_ACCOUNT) is used instead
    '''   Type        = TYPE (or TYPE_OF_ESCROW if there is no TYPE column)
    '''   Transaction = "C" if the line has a Credit amount, "D" if it has a Debit amount
    '''   Amount      = that Credit or Debit amount
    '''   Reference   = the Unit Reference shown in UNIT DETAILS (lblUnitRef)
    '''   Description = the group's REFERENCE_PHRASE + " " + Unit Reference
    '''                 ("Reservation of Unit" if the group has no phrase)
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
            Dim Description As String = (Phrase & " " & UnitRef).Trim()
            Dim PreSalesAccount As String = lblPreSalesAccount.Text.Trim()

            If Details IsNot Nothing Then
                For Each Detail As DataRow In Details.Rows
                    Dim Credit As Decimal = ParseAmount(GetColumnValue(Detail, "CREDIT"))
                    Dim Debit As Decimal = ParseAmount(GetColumnValue(Detail, "DEBIT"))

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

                    ' Placeholder accounts (containing "xxx") become the unit's Pre Sales Account
                    Dim Account As String = GetColumnValue(Detail, "ACCOUNT_NUMBER")
                    If Account.IndexOf("xxx", StringComparison.OrdinalIgnoreCase) >= 0 Then
                        Account = PreSalesAccount
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
        End If

        gvTransactions.DataSource = Result
        gvTransactions.DataBind()
    End Sub

    ' ------------------------------------------------------------------
    ' Unit status change + history (same logic as MainPage.LinkButton3_Command)
    ' ------------------------------------------------------------------

    ''' <summary>
    ''' After a successful post: if the action is a "CHANGE" action with a target status,
    ''' moves the unit to that status (ApplyNodeStatusChange), then always writes a
    ''' UNITSHUB_UNITSHISTORY row with the AutoTransfer in its SUMMARY. If the status
    ''' didn't change, FROM_STATUS and TO_STATUS are both the current status.
    ''' Returns True when the status was changed.
    ''' </summary>
    Private Function ApplyActionAfterPost(UnitPaymentId As String, TraNo As Integer, EntryCount As Integer) As Boolean
        Dim FromStatus As String = lblSTATEID.Text.Trim()
        Dim ToStatus As String = lblToStatusID.Text.Trim()

        Dim ChangeStatus As Boolean =
            String.Equals(lblActionType.Text.Trim(), "CHANGE", StringComparison.OrdinalIgnoreCase) AndAlso
            ToStatus <> "" AndAlso ToStatus <> FromStatus

        If ChangeStatus Then ApplyNodeStatusChange(lblPID.Text.Trim(), ToStatus)

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
    Function Validate_Entries() As Boolean
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
            "LASTAUTOTRANSFERDATE = " & DateNTime.ToString(CultureInfo.InvariantCulture) &
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
End Class