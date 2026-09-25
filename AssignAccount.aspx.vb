Imports System.Data
Imports System.DateTime
Imports System.Drawing
Imports System.Globalization

Partial Class AssignAccount
    Inherits System.Web.UI.Page
    Private encryNdecry As New EncryDecry

    Private Sub AssignAccount_Load(sender As Object, e As EventArgs) Handles Me.Load
        If Not Page.IsPostBack Then
            lblPID.Text = Request("NodeID") '"000004"
            lblPRJID.Text = Request("ProjectId") '"001" '
            lblSTATEID.Text = Request("STATEID")
            lblActionID.Text = Request("ActionId")

            LoadCustomer()      ' Customer Name, CPR (and lblCID)
            LoadUnitInfo()      ' Project, Unit Reference
            BindAccounts()      ' ICBS accounts for the customer's CPR
        End If
    End Sub

    ''' <summary>
    ''' Customer of this unit from UNITSHUB_CUSTOMERPROPERTIES (latest CREATED_AT if
    ''' there is more than one), then NAME / NATIONALID from UNITSHUB_CONTACTS.
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

        Dim ContactDT As DataTable = GetDataTable(EBDB,
            "SELECT NAME, NATIONALID FROM UNITSHUB_CONTACTS WHERE ID = '" & ContactId.Replace("'", "''") & "'")
        If ContactDT IsNot Nothing AndAlso ContactDT.Rows.Count > 0 Then
            lblCustomerName.Text = Convert.ToString(ContactDT.Rows(0)("NAME"))
            lblCPR.Text = Convert.ToString(ContactDT.Rows(0)("NATIONALID"))
        End If
    End Sub

    ''' <summary>
    ''' Project name from UNITSHUB_PROJECTS, and the unit's "Reference" attribute
    ''' (the attribute whose NAME_IN_UI is Reference in UNITSHUB_ATTRIBUTES_PROPERTIES,
    ''' value from UNITSHUB_NODE_ATTRIBUTE_VALUE) - the same Reference MainPage shows.
    ''' </summary>
    Private Sub LoadUnitInfo()
        If Not String.IsNullOrWhiteSpace(lblPRJID.Text) Then
            lblProject.Text = Convert.ToString(DB.RetreiveScalarSTRING(EBDB,
                "SELECT PROJECT_NAME_EN FROM UNITSHUB_PROJECTS WHERE PROJECT_ID = '" & lblPRJID.Text.Replace("'", "''") & "'"))
        End If

        If String.IsNullOrWhiteSpace(lblPID.Text) Then Exit Sub

        Dim SQL As String = ""
        SQL = SQL + vbCrLf + " SELECT NAV.VALUE_TEXT "
        SQL = SQL + vbCrLf + " FROM   UNITSHUB_NODES N "
        SQL = SQL + vbCrLf + " JOIN   UNITSHUB_ATTRIBUTES_PROPERTIES AP "
        SQL = SQL + vbCrLf + "        ON AP.PROJECT_ID   = N.PROJECT_ID "
        SQL = SQL + vbCrLf + "       AND AP.NODE_TYPE_ID = N.NODE_TYPE_ID "
        SQL = SQL + vbCrLf + "       AND UPPER(REPLACE(AP.NAME_IN_UI, '""', '')) = 'REFERENCE' "
        SQL = SQL + vbCrLf + " JOIN   UNITSHUB_NODE_ATTRIBUTE_VALUE NAV "
        SQL = SQL + vbCrLf + "        ON NAV.NODE_ID       = N.NODE_ID "
        SQL = SQL + vbCrLf + "       AND NAV.DISPLAY_ORDER = AP.DISPLAY_ORDER "
        SQL = SQL + vbCrLf + " WHERE  N.NODE_ID = '" & lblPID.Text.Replace("'", "''") & "' "
        SQL = SQL + vbCrLf + "   AND  ROWNUM = 1 "

        lblUnitRef.Text = Convert.ToString(DB.RetreiveScalarSTRING(EBDB, SQL))
    End Sub

    ''' <summary>
    ''' Fills gvAccounts with the customer's ICBS accounts (by CPR) of the allowed
    ''' account types / branches. Shows the empty message when there is no CPR.
    ''' </summary>
    Private Sub BindAccounts()
        Dim DT As New DataTable
        lblCPR.Text = "840805551"
        If Not String.IsNullOrWhiteSpace(lblCPR.Text) Then
            DT = GetDataTable(EBDB, "Select T3.CACC_NUM As Account,T3.BRCH_CODE As Branch ,'' as PropertyRef,T3.CUST_ID from bbsd_physical_persons@ICBS T1 " &
                " Left Join bbsd_cust_members@ICBS T2 on T1.PHPR_ID=T2.CUSM_ID " &
                " Left Join BBSD_CUST_ACCOUNTS@ICBS T3 on T2.CUST_ID=T3.CUST_ID WHERE PHPR_NATIONAL_NBR='" & lblCPR.Text.Replace("'", "''") & "'" &
                                 "  and T3.actp_type || T3.BRCH_CODE in (Select AccountType || Branch  from UNITSHUB_AccountBranchProjects)")
        End If
        '
        gvAccounts.DataSource = DT
        gvAccounts.DataBind()
    End Sub

    ''' <summary>
    ''' "Select" link in the first column: highlights the row and keeps the chosen
    ''' account, branch and ICBS customer ID in lblSelectedAccount / lblSelectedBranch /
    ''' lblSelectedCustId for the save step.
    ''' </summary>
    Protected Sub gvAccounts_RowCommand(sender As Object, e As GridViewCommandEventArgs) Handles gvAccounts.RowCommand
        If e.CommandName <> "SelectAccount" Then Exit Sub

        Dim RowIndex As Integer
        If Not Integer.TryParse(Convert.ToString(e.CommandArgument), RowIndex) Then Exit Sub
        If RowIndex < 0 OrElse RowIndex >= gvAccounts.DataKeys.Count Then Exit Sub

        gvAccounts.SelectedIndex = RowIndex

        Dim Keys As DataKey = gvAccounts.DataKeys(RowIndex)
        lblSelectedAccount.Text = Convert.ToString(Keys("ACCOUNT"))
        lblSelectedBranch.Text = Convert.ToString(Keys("BRANCH"))
        lblSelectedCustId.Text = Convert.ToString(Keys("CUST_ID"))

        lblSelectedNote.Text = "Selected account: " & lblSelectedAccount.Text & " (Branch " & lblSelectedBranch.Text & ")"

        ' TODO: save the selected account (e.g. UNITSHUB_CUSTOMERPROPERTIES.UNIT_ACCOUNT)
    End Sub

    Protected Sub imgClose_Click(sender As Object, e As ImageClickEventArgs) Handles imgClose.Click
        VendorPopupHelper.RegisterPopupSelectionAndClose(Me, False, skipPostBack:=False)
    End Sub
End Class