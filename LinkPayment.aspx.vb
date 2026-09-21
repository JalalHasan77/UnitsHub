Imports System.Data
Imports System.DateTime
Imports System.Drawing
Imports System.Globalization

Partial Class LinkPayment
    Inherits System.Web.UI.Page

    Private Sub ContactMaintenance_Load(sender As Object, e As EventArgs) Handles Me.Load
        If Not Page.IsPostBack Then
            PopulateDropDownList(ddl:=DropDownList1)
            Refresh()

        End If
    End Sub

    ' Drop-in replacement for the existing Sub Refresh() in this class.
    ' Same query results/columns as the original - just restructured:
    '   - Guards against a malformed DropDownList value instead of risking an
    '     IndexOutOfRangeException from indexing into the split array directly.
    '   - Pulls the repeated "join the transaction back to its customer/property"
    '     fragment into one local function instead of duplicating it in two
    '     of the three UNION branches.
    '   - Replaces the "@ShowUnlinkedOnly" placeholder-and-Replace() trick with a
    '     direct conditional, and only builds the "linked history" branch when it
    '     will actually be used (skipped entirely when showing unlinked-only).
    '   - Consistent use of & for string concatenation (VB's dedicated string
    '     concatenation operator, unlike +, which is otherwise identical here but
    '     easy to confuse with numeric addition).

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

        ' Shared join: links an EFT transaction (keyed off its TRSH_NUM, from either
        ' the live table's alias or the history table's alias) back to whichever
        ' customer/property record it was collected as a down payment for.
        Dim CustomerJoinSql As Func(Of String, String) =
        Function(SourceAlias As String) As String
            Return " Left Join SELECT_CUSTOMERPROPERTIES S on S.DOWNPAYMENT = " & SourceAlias & ".TRSH_NUM " & vbCrLf &
                   " Left Join SELECT_CONTACTS C on S.CID = C.ID " & vbCrLf &
                   " Left Join SELECT_PROPERTIES P on S.PID = P.ID "
        End Function

        AccountNumber = "0000211020112"
        BranchCode = "100"

        ' Live transactions (BBSD_TRANSACTIONS_M/D). When ShowUnlinkedOnly is set,
        ' filter down to the rows with no matching customer/property record.
        Dim SQL As String =
        "SELECT to_char(D.TRSH_OPERATION_DATE,'yyyy-MM-dd') as ""Date"", " & vbCrLf &
        " D.TRSD_BRCH_CODE as Branch, " & vbCrLf &
        " D.TRSD_CACC_NUM as ""Account Number"", " & vbCrLf &
        " M.TRSH_NUM as ""TRANS NUM"", " & vbCrLf &
        " D.TRSD_DC as ""Transaction"", " & vbCrLf &
        " D.TRSD_AMOUNT as ""Amount"", " & vbCrLf &
        " TRSD_B_DESC as ""Comment 1"", " & vbCrLf &
        " TRSD_S_DESC as ""Comment 2"", " & vbCrLf &
        " S.DOWNPAYMENT, " & vbCrLf &
        " C.Name, C.NAtionalID, " & vbCrLf &
        " P.REFERENCE " & vbCrLf &
        " From BBSD_TRANSACTIONS_M@ICBS M LEFT JOIN BBSD_TRANSACTIONS_D@ICBS D ON M.TRSH_NUM = D.TRSH_NUM " & vbCrLf &
        CustomerJoinSql("M") & vbCrLf &
        " WHERE M.GOPM_CODE = 'EFT' AND D.TRSD_CACC_NUM = " & AccountNumber &
        " and D.TRSD_BRCH_CODE = " & BranchCode &
        If(ShowUnlinkedOnly, " and S.DOWNPAYMENT is Null ", " ")

        ' Transactions that have since aged out into BBSD_HIST_TRANSACTIONS and were
        ' never linked to a customer/property record - always included either way.
        SQL &=
        " Union Select to_char(H.TRSH_OPERATION_DATE,'yyyy-MM-dd') as ""Date"", " & vbCrLf &
        " H.BRCH_CODE as Branch, " & vbCrLf &
        " H.HTRS_CACC_NUM as ""Account Number"", " & vbCrLf &
        " H.TRSH_NUM as ""TRANS NUM"", " & vbCrLf &
        " H.HTRS_DC as ""Transaction"", " & vbCrLf &
        " H.HTRS_AMOUNT as ""Amount"", " & vbCrLf &
        " H.HTRS_B_DESC as ""Comment 1"", " & vbCrLf &
        " H.HTRS_S_DESC as ""Comment 2"", " & vbCrLf &
        " to_char(H.TRSH_NUM) as ""DOWNPAYMENT"", " & vbCrLf &
        " '' as Name, '' as NAtionalID, " & vbCrLf &
        " '' as REFERENCE " & vbCrLf &
        " From BBSD_HIST_TRANSACTIONS@ICBS H " & vbCrLf &
        " WHERE H.GOPM_CODE = 'EFT' AND H.HTRS_CACC_NUM = " & AccountNumber &
        " and H.HTRS_BRCH_CODE = " & BranchCode & " and " & vbCrLf &
        " H.TRSH_NUM not in (Select CP.DOWNPAYMENT from SELECT_CUSTOMERPROPERTIES CP where not CP.DOWNPAYMENT is Null) "

        ' Transactions that aged into history but WERE linked - only relevant when
        ' showing everything, so skip building this branch at all otherwise.
        If Not ShowUnlinkedOnly Then
            SQL &=
            " union Select to_char(H.TRSH_OPERATION_DATE,'yyyy-MM-dd') as ""Date"", " & vbCrLf &
            " H.BRCH_CODE as Branch, " & vbCrLf &
            " H.HTRS_CACC_NUM as ""Account Number"", " & vbCrLf &
            " H.TRSH_NUM as ""TRANS NUM"", " & vbCrLf &
            " H.HTRS_DC as ""Transaction"", " & vbCrLf &
            " H.HTRS_AMOUNT as ""Amount"", " & vbCrLf &
            " H.HTRS_B_DESC as ""Comment 1"", " & vbCrLf &
            " H.HTRS_S_DESC as ""Comment 2"", " & vbCrLf &
            " S.DOWNPAYMENT, " & vbCrLf &
            " C.Name, C.NAtionalID, " & vbCrLf &
            " P.REFERENCE " & vbCrLf &
            " From BBSD_HIST_TRANSACTIONS@ICBS H " & vbCrLf &
            CustomerJoinSql("H") & vbCrLf &
            " WHERE H.GOPM_CODE = 'EFT' AND H.HTRS_CACC_NUM = " & AccountNumber &
            " and H.HTRS_BRCH_CODE = " & BranchCode
        End If

        SQL = "Select * from (" & SQL & ") TBL order by ""Date"" desc"

        Dim DT As DataTable = GetDataTable(EBDB_CS, SQL)

        GridView2.DataSource = DT.DefaultView
        GridView2.DataBind()
    End Sub



    Private Sub PopulateDropDownList(ddl As DropDownList)
        Dim SQL As String = ""
        SQL = SQL + vbCrLf + "         SELECT "
        SQL = SQL + vbCrLf + "             MAX(CASE WHEN ATTRIBUTE_NAME IN ('NAME_EN','NAMEEN')  "
        SQL = SQL + vbCrLf + "                      THEN VALUE_TEXT END) AS PROJECT_NAME_EN, "
        SQL = SQL + vbCrLf + "             MAX(CASE WHEN UPPER(ATTRIBUTE_NAME) LIKE '%ACCOUNT%'  "
        SQL = SQL + vbCrLf + "                      THEN VALUE_TEXT END) AS ""Account"" "
        SQL = SQL + vbCrLf + "         FROM UNITSHUB_ALL "
        SQL = SQL + vbCrLf + "         WHERE NODE_TYPE_ID = '000'  "
        SQL = SQL + vbCrLf + "           AND PROJECT_ID IN (SELECT DISTINCT PROJECT_ID FROM UNITSHUB_ALL) "
        SQL = SQL + vbCrLf + "         GROUP BY PROJECT_ID "
        SQL = SQL + vbCrLf + "         HAVING MAX(CASE WHEN UPPER(ATTRIBUTE_NAME) LIKE '%ACCOUNT%'  "
        SQL = SQL + vbCrLf + "                         THEN VALUE_TEXT END) IS NOT NULL "
        SQL = SQL + vbCrLf + "         ORDER BY PROJECT_ID; "


        PF.PopulateDropDownList(ddl:=DropDownList1,
                                    sql:=SQL, DataConnection:=DB.EBDB,
                                    firstItemIs:="Select Project")

    End Sub

    Protected Sub GridView2_RowDataBound(sender As Object, e As GridViewRowEventArgs) Handles GridView2.RowDataBound
        Dim R As GridViewRow
        R = e.Row

        If R.RowType = DataControlRowType.DataRow Then
            Dim L As LinkButton
            L = R.FindControl("LinkButton1")

            'B.Attributes.Add("TRANUM", R.DataItem("TRANS NUM").ToString)
            'udf.SetOnClientClick(B, "/Forms/CustomersApartmentsList.aspx", 600, 800)
            'UDF.SetOnClientClick(L, UDF.GetAppPath + "/Forms/CustomersApartmentsList.aspx?TRNS=" & R.DataItem("TRANS NUM"), 600, 800)


            Dim L1 As Label
            L1 = R.FindControl("lblNameAndCPR")
            If R.DataItem("Name").ToString <> "" Then L1.Text = R.DataItem("Name") & " (" & R.DataItem("NAtionalID") & ")"

            Dim L2 As Label
            L2 = R.FindControl("lblUnit")

            If R.DataItem("REFERENCE").ToString <> "" Then L2.Text = R.DataItem("REFERENCE")
            'MsgBox(L.OnClientClick)
        End If
    End Sub




    Protected Sub btnLoad_Click(sender As Object, e As EventArgs) Handles btnLoad.Click

    End Sub
    Protected Sub btnCancel_Click(sender As Object, e As EventArgs) Handles btnCancel.Click

    End Sub
    Protected Sub btnSave_Click(sender As Object, e As EventArgs) Handles btnSave.Click

    End Sub

    Protected Sub LinkButton1_Click(sender As Object, e As EventArgs)

    End Sub
    Protected Sub DropDownList1_SelectedIndexChanged(sender As Object, e As EventArgs) Handles DropDownList1.SelectedIndexChanged
        Refresh()
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