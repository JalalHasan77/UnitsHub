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

            LoadCustomer()      ' CUSTOMER box
            LoadUnitInfo()      ' Unit Reference, Price
            LoadAccountInfo()   ' Pre Sales Account (+ its type in the title), Balance
            LoadAmounts()       ' Paid Amount, Remaining
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

    Protected Sub imgClose_Click(sender As Object, e As ImageClickEventArgs) Handles imgClose.Click
        VendorPopupHelper.RegisterPopupSelectionAndClose(Me, False, skipPostBack:=False)
    End Sub
End Class