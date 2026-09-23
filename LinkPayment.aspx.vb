Imports System.Data
Imports System.DateTime
Imports System.Drawing
Imports System.Globalization

Partial Class LinkPayment
    Inherits System.Web.UI.Page

    Private Sub ContactMaintenance_Load(sender As Object, e As EventArgs) Handles Me.Load
        If Not Page.IsPostBack Then
            ' A ProjectID on the query string locks the page to that one project:
            ' only it is loaded into the dropdown, and the dropdown becomes read-only.
            Dim ProjectIdParam As String = Request("ProjectID")
            If Not String.IsNullOrWhiteSpace(ProjectIdParam) Then
                lblPROJID.Text = ProjectIdParam.Trim()
            End If

            PopulateDropDownList(ddl:=DropDownList1)
            chkShowUnlinkedOnly.Checked = False

        End If

        ' Rebind on every postback, not just the first load: a row's LinkButton
        ' click needs GridViewRow.DataItem, which is only set while the grid is
        ' (re)bound during THIS request. By the time Load runs, DropDownList1 and
        ' chkShowUnlinkedOnly already carry the just-posted values, so this reflects
        ' whatever the user just changed too.
        Refresh()
    End Sub

    ''' <summary>
    ''' Reloads GridView2 for the branch/account selected in DropDownList1.
    ''' - chkShowUnlinkedOnly UNCHECKED (the default): the plain transaction list
    '''   (live + history, unioned), with no customer information.
    ''' - chkShowUnlinkedOnly CHECKED: the same transaction list, with NAME and
    '''   NATIONALID added by joining out to UNITSHUB_PAYMENTS / UNITSHUB_CONTACTS
    '''   on UNITSHUB_PAYMENTS.TRANSACTIONID = the transaction's TRSH_NUM ("TRANS NUM").
    ''' </summary>
    Sub Refresh()
        ' DropDownList1's value is "BRANCH-ACCOUNT", e.g. "001-123456"
        Dim SelectionParts() As String = DropDownList1.SelectedValue.ToString().Trim().Split("-"c)

        If SelectionParts.Length < 2 OrElse
       String.IsNullOrWhiteSpace(SelectionParts(0)) OrElse
       String.IsNullOrWhiteSpace(SelectionParts(1)) Then
            GridView2.DataSource = Nothing
            GridView2.DataBind()
            Exit Sub
        End If

        Dim BranchCode As String = SelectionParts(0).Trim()
        Dim AccountNumber As String = SelectionParts(1).Trim()
        Dim ShowUnlinkedOnly As Boolean = chkShowUnlinkedOnly.Checked

        ' Values come straight from the DropDownList selection - escape quotes before
        ' splicing them into the SQL text.
        Dim SafeAccountNumber As String = AccountNumber.Replace("'", "''")
        Dim SafeBranchCode As String = BranchCode.Replace("'", "''")

        ' Adds NAME/NATIONALID to one branch of the union, by joining the transaction
        ' (identified by TRSH_NUM, from either the live or the history table's alias)
        ' back to the payment it was linked to and that payment's customer.
        Dim CustomerJoinSql As Func(Of String, String) =
        Function(TransNumColumn As String) As String
            Return " Left Join UNITSHUB_PAYMENTS PMT on PMT.TRANSACTIONID = " & TransNumColumn & vbCrLf &
                   " Left Join UNITSHUB_CONTACTS CTC on CTC.ID = PMT.CUSTOMER_ID "
        End Function

        ' Live transactions (BBSD_TRANSACTIONS_M/D) for the selected branch/account.
        Dim SQL As String =
        "Select to_char(D.TRSH_OPERATION_DATE,'yyyy-MM-dd') as ""Date"", " & vbCrLf &
        "       D.TRSD_BRCH_CODE as Branch, " & vbCrLf &
        "       D.TRSD_CACC_NUM as ""Account Number"", " & vbCrLf &
        "       M.TRSH_NUM as ""TRANS NUM"", " & vbCrLf &
        "       D.TRSD_DC as ""Transaction"", " & vbCrLf &
        "       D.TRSD_AMOUNT as ""Amount"", " & vbCrLf &
        "       TRSD_B_DESC as ""Comment 1"", " & vbCrLf &
        "       TRSD_S_DESC as ""Comment 2"" " & vbCrLf &
        If(ShowUnlinkedOnly, "       , CTC.NAME, CTC.NATIONALID " & vbCrLf, "") &
        " From BBSD_TRANSACTIONS_M@ICBS M LEFT JOIN BBSD_TRANSACTIONS_D@ICBS D ON M.TRSH_NUM = D.TRSH_NUM " &
        If(ShowUnlinkedOnly, CustomerJoinSql("M.TRSH_NUM") & vbCrLf, "") &
        " WHERE M.GOPM_CODE = 'EFT' AND D.TRSD_CACC_NUM = '" & SafeAccountNumber & "'" &
        " and D.TRSD_BRCH_CODE = '" & SafeBranchCode & "' "

        ' Transactions that have since aged out into BBSD_HIST_TRANSACTIONS, for the
        ' same branch/account.
        SQL &= " Union " & vbCrLf &
        "Select to_char(H.TRSH_OPERATION_DATE,'yyyy-MM-dd') as ""Date"", " & vbCrLf &
        "       H.BRCH_CODE as Branch, " & vbCrLf &
        "       H.HTRS_CACC_NUM as ""Account Number"", " & vbCrLf &
        "       H.TRSH_NUM as ""TRANS NUM"", " & vbCrLf &
        "       H.HTRS_DC as ""Transaction"", " & vbCrLf &
        "       H.HTRS_AMOUNT as ""Amount"", " & vbCrLf &
        "       H.HTRS_B_DESC as ""Comment 1"", " & vbCrLf &
        "       H.HTRS_S_DESC as ""Comment 2"" " & vbCrLf &
        If(ShowUnlinkedOnly, "       , CTC.NAME, CTC.NATIONALID " & vbCrLf, "") &
        " From BBSD_HIST_TRANSACTIONS@ICBS H " &
        If(ShowUnlinkedOnly, CustomerJoinSql("H.TRSH_NUM") & vbCrLf, "") &
        " WHERE H.GOPM_CODE = 'EFT' AND H.HTRS_CACC_NUM = '" & SafeAccountNumber & "'" &
        " and H.HTRS_BRCH_CODE = '" & SafeBranchCode & "' "

        Dim DT As DataTable = GetDataTable(EBDB_CS, SQL)

        GridView2.DataSource = GetSampleTransactions() 'DT.DefaultView
        GridView2.DataBind()
    End Sub



    Private Sub PopulateDropDownList(ddl As DropDownList)
        ' When lblPROJID already holds a project (set in Page_Load from Request("ProjectID")),
        ' the list is narrowed down to just that project instead of every project.
        Dim ProjectFilter As String = lblPROJID.Text.Trim()

        Dim SQL As String = ""
        SQL = SQL + vbCrLf + "         SELECT "
        SQL = SQL + vbCrLf + "             MAX(CASE WHEN ATTRIBUTE_NAME IN ('NAME_EN','NAMEEN')  "
        SQL = SQL + vbCrLf + "                      THEN VALUE_TEXT END) AS PROJECT_NAME_EN, "
        SQL = SQL + vbCrLf + "             MAX(CASE WHEN UPPER(ATTRIBUTE_NAME) LIKE '%ACCOUNT%'  "
        SQL = SQL + vbCrLf + "                      THEN VALUE_TEXT END) AS ""Account"" "
        SQL = SQL + vbCrLf + "         FROM UNITSHUB_ALL "
        SQL = SQL + vbCrLf + "         WHERE NODE_TYPE_ID = '000'  "
        If ProjectFilter <> "" Then
            SQL = SQL + vbCrLf + "           AND PROJECT_ID = '" & ProjectFilter.Replace("'", "''") & "' "
        End If
        SQL = SQL + vbCrLf + "           AND PROJECT_ID IN (SELECT DISTINCT PROJECT_ID FROM UNITSHUB_ALL) "
        SQL = SQL + vbCrLf + "         GROUP BY PROJECT_ID "
        SQL = SQL + vbCrLf + "         HAVING MAX(CASE WHEN UPPER(ATTRIBUTE_NAME) LIKE 'ACCOUNT%'  "
        SQL = SQL + vbCrLf + "                         THEN VALUE_TEXT END) IS NOT NULL "
        SQL = SQL + vbCrLf + "         ORDER BY PROJECT_ID; "


        PF.PopulateDropDownList(ddl:=DropDownList1,
                                    sql:=SQL, DataConnection:=DB.EBDB,
                                    firstItemIs:=If(ProjectFilter <> "", "", "Select Project"))

        ' Only one project was loaded - lock the dropdown so it can't be changed.
        If ProjectFilter <> "" Then
            DropDownList1.Enabled = False
        End If

    End Sub

    Protected Sub GridView2_RowDataBound(sender As Object, e As GridViewRowEventArgs) Handles GridView2.RowDataBound
        Dim R As GridViewRow = e.Row

        If R.RowType = DataControlRowType.DataRow Then
            ' NAME/NATIONALID are only present in the result set when chkShowUnlinkedOnly
            ' is checked (see Refresh()) - and REFERENCE is not produced by that query at
            ' all - so check each column exists before reading it.
            Dim RowData As DataRowView = TryCast(R.DataItem, DataRowView)
            If RowData Is Nothing Then Exit Sub
            Dim Columns As DataColumnCollection = RowData.Row.Table.Columns

            Dim L1 As Label = TryCast(R.FindControl("lblNameAndCPR"), Label)
            If L1 IsNot Nothing AndAlso Columns.Contains("NAME") Then
                Dim CustomerName As String = Convert.ToString(RowData("NAME"))
                If CustomerName <> "" Then
                    L1.Text = CustomerName & " (" & Convert.ToString(RowData("NATIONALID")) & ")"
                End If
            End If

            Dim L2 As Label = TryCast(R.FindControl("lblUnit"), Label)
            If L2 IsNot Nothing AndAlso Columns.Contains("REFERENCE") Then
                Dim Reference As String = Convert.ToString(RowData("REFERENCE"))
                If Reference <> "" Then L2.Text = Reference
            End If

            Dim L As LinkButton
            L = R.FindControl("LinkButton1")
            L.Attributes.Add("TransationNum", RowData("TRANS NUM"))
            L.Attributes.Add("Amount", RowData("Amount"))
            L.Attributes.Add("Date", RowData("Date"))


        End If
    End Sub

    Protected Sub btnLoad_Click(sender As Object, e As EventArgs) Handles btnLoad.Click

    End Sub
    Protected Sub btnCancel_Click(sender As Object, e As EventArgs) Handles btnCancel.Click

    End Sub
    Protected Sub btnSave_Click(sender As Object, e As EventArgs) Handles btnSave.Click

    End Sub

    Protected Sub LinkButton1_Click(sender As Object, e As EventArgs)
        '' The clicked LinkButton's NamingContainer is its GridViewRow.
        'Dim ClickedRow As GridViewRow = TryCast(DirectCast(sender, LinkButton).NamingContainer, GridViewRow)
        'If ClickedRow Is Nothing Then Exit Sub

        '' GridViewRow.DataItem is only populated while the grid is (re)bound in THIS
        '' postback - see the Refresh() call moved into Load below. Without that, this
        '' is always Nothing on a row's postback event, even though the row itself and
        '' its LinkButton render and click perfectly normally.
        'Dim RowData As DataRowView = TryCast(ClickedRow.DataItem, DataRowView)
        'If RowData Is Nothing Then Exit Sub

        '' Same shape the opener (ReserveUnit) expects - see btnSelectExistingCustomer_Click
        '' there: a List(Of Dictionary(Of String, Object)), one dictionary per selected row.
        Dim RowValues As New Dictionary(Of String, Object)
        'For Each Col As DataColumn In RowData.Row.Table.Columns
        '    RowValues(Col.ColumnName) = RowData(Col.ColumnName)
        'Next
        'L.Attributes.Add("TransationNum"
        Dim TransactionNum As String = CType(sender, LinkButton).Attributes("TransationNum")
        Dim Amount As String = CType(sender, LinkButton).Attributes("Amount")


        'Dim RowValues As New Dictionary(Of String, Object)
        RowValues.Add("TranscationNum", TransactionNum)
        RowValues.Add("Amount", Amount)

        Dim SelectedItems As New List(Of Dictionary(Of String, Object))
        SelectedItems.Add(RowValues)

        ' Use the key the opener passed in (?vpKey=...) so the opener can read the
        ' result back with the same key it registered the popup with.
        Dim ReturnKey As String = VendorPopupHelper.GetPopupReturnKey(Me)

        VendorPopupHelper.RegisterPopupSelectionAndClose(
                                    page:=Me,
                                    returnValue:=SelectedItems,
                                    startupScriptKey:=ReturnKey,
                                    skipPostBack:=False)
    End Sub
    Protected Sub DropDownList1_SelectedIndexChanged(sender As Object, e As EventArgs) Handles DropDownList1.SelectedIndexChanged
        ' No-op: Load already reran Refresh() with the posted SelectedValue (see above).
    End Sub

    Protected Sub chkShowUnlinkedOnly_CheckedChanged(sender As Object, e As EventArgs) Handles chkShowUnlinkedOnly.CheckedChanged
        ' No-op: Load already reran Refresh() with the posted Checked state (see above).
    End Sub

    ''' <summary>
    ''' Returns a DataTable shaped exactly like the one Refresh() builds for GridView2
    ''' (Date, BRANCH, Account Number, TRANS NUM, Transaction, Amount, Comment 1,
    ''' Comment 2, NAME, NATIONALID) - filled with fixed sample rows.
    ''' Handy for testing the grid, paging, or the Name/CPR display logic without a
    ''' live connection to the Oracle side (@ICBS).
    ''' </summary>
    Public Function GetSampleTransactions() As DataTable

        Dim DT As New DataTable("Transactions")
        DT.Columns.Add("Date", GetType(String))
        DT.Columns.Add("BRANCH", GetType(String))
        DT.Columns.Add("Account Number", GetType(String))
        DT.Columns.Add("TRANS NUM", GetType(String))
        DT.Columns.Add("Transaction", GetType(String))
        DT.Columns.Add("Amount", GetType(Decimal))
        DT.Columns.Add("Comment 1", GetType(String))
        DT.Columns.Add("Comment 2", GetType(String))
        DT.Columns.Add("NAME", GetType(String))
        DT.Columns.Add("NATIONALID", GetType(String))

        AddRow(DT, "2025-09-23", "0", "0000211020112", "853178581", "C", 500, "001/FAWRI+ FROM /PHONE/97334499270", "BH59ALSA00642706150030/ALS103JHU003RWCC", "Mohamed Jaafar A.Wahab Ahmed Khamdan", "890109311")
        AddRow(DT, "2025-07-31", "0", "0000211020112", "852830631", "C", 500, "001/FAWRI+ FROM /PHONE/97337740188", "BH08KHCB00200053083001/KHC103JGC002PQS2", "Mohammed Khair Fouad Abdulmajeed Al Tubal", "800620453")
        AddRow(DT, "2025-03-09", "0", "0000211020112", "851820176", "C", 500, "001/FAWRI+ FROM /PHONE/97333300180", "BH71ABCO52991339101001/ABCO250309047793", "", "")
        AddRow(DT, "2025-03-04", "0", "0000211020112", "851817088", "C", 500, "001/FAWRI+ FROM /PHONE/97335996350", "BH74NBOB00000282336044/NBB040325ONKGCSJ", "Abdulghani Sarhan Deyab Aldhufeeri", "820406724")
        AddRow(DT, "2024-09-05", "0", "0000211020112", "850528778", "C", 500, "001/FAWRI+ FROM /PHONE/97339365104", "BH82ABIB01070524634001/ABI103B4249002X7", "", "")
        AddRow(DT, "2024-08-27", "0", "0000211020112", "850393775", "C", 500, "001/FAWRI+ FROM /PHONE/97339363193", "BH19ABIB01070524633001/ABI103B4240002LJ", "Amjad Hassan Najam Ulhassan Mohd.Azam Bhatti", "760810346")
        AddRow(DT, "2024-08-19", "0", "0000211020112", "850335515", "C", 500, "001/FAWRI+ FROM /PHONE/97334313056", "BH67ALSA00714471150000/ALS103IP40006IIP", "Mohamed Husam Nayef Sayed", "820111074")
        AddRow(DT, "2024-08-07", "0", "0000211020112", "850315955", "C", 500, "001/FAWRI+ FROM /PHONE/97339949299", "BH98BBKU00200004625786/BPA2408078091004", "", "")
        AddRow(DT, "2024-07-08", "0", "0000211020112", "850103131", "C", 500, "001/FAWRI+ FROM /PHONE/97336953993", "BH66BBKU00200005164712/BPA2407081901258", "Maha Jameel Abdulbaqi Mohamed Ahmed Al Bayat", "891204962")
        AddRow(DT, "2024-07-02", "0", "0000211020112", "850097834", "C", 500, "001/FAWRI+ FROM /PHONE/97336481144", "BH89KFHO00031020008110/KFH103B418400G6D", "", "")
        AddRow(DT, "2024-05-22", "0", "0000211020112", "849592971", "C", 500, "001/FAWRI+ FROM /PHONE/97336048848", "BH18NBOB00000272226645/NBB220524ONRT60L", "Hasan Abdulla Hasan Abdulla", "850300886")

        Return DT
    End Function

    Private Sub AddRow(DT As DataTable, DateText As String, Branch As String, AccountNumber As String,
                        TransNum As String, TransactionType As String, Amount As Decimal,
                        Comment1 As String, Comment2 As String, Name As String, NationalId As String)

        Dim R As DataRow = DT.NewRow()
        R("Date") = DateText
        R("BRANCH") = Branch
        R("Account Number") = AccountNumber
        R("TRANS NUM") = TransNum
        R("Transaction") = TransactionType
        R("Amount") = Amount
        R("Comment 1") = Comment1
        R("Comment 2") = Comment2
        R("NAME") = Name
        R("NATIONALID") = NationalId
        DT.Rows.Add(R)
    End Sub


End Class





'Important SQL
'===========================================================
'Live transactions
'===========================================================
'    Select Case to_char(D.TRSH_OPERATION_DATE,'yyyy-MM-dd') as "Date",
'       D.TRSD_BRCH_CODE                            As Branch,
'       D.TRSD_CACC_NUM                              as "Account Number",
'       M.TRSH_NUM                                   as "TRANS NUM",
'       D.TRSD_DC                                    as "Transaction",
'       D.TRSD_AMOUNT                                as "Amount",
'       TRSD_B_DESC                                  as "Comment 1",
'       TRSD_S_DESC                                  as "Comment 2"
'From BBSD_TRANSACTIONS_M@ICBS M
'LEFT JOIN BBSD_TRANSACTIONS_D@ICBS D On M.TRSH_NUM = D.TRSH_NUM
'WHERE M.GOPM_CODE = 'EFT'
'  And D.TRSD_CACC_NUM = '1265901020015'
'  And D.TRSD_BRCH_CODE ='100'

'===========================================================
'Historical transactions
'===========================================================
'  Select Case to_char(H.TRSH_OPERATION_DATE,'yyyy-MM-dd') as "Date",
'       H.BRCH_CODE                                 As Branch,
'       H.HTRS_CACC_NUM                              as "Account Number",
'       H.TRSH_NUM                                   as "TRANS NUM",
'       H.HTRS_DC                                    as "Transaction",
'       H.HTRS_AMOUNT                                as "Amount",
'       H.HTRS_B_DESC                                as "Comment 1",
'       H.HTRS_S_DESC                                as "Comment 2"
'From BBSD_HIST_TRANSACTIONS@ICBS H
'WHERE H.GOPM_CODE = 'EFT'
'  And H.HTRS_CACC_NUM ='1265901020015'
'  And H.HTRS_BRCH_CODE = '100'