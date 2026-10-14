Imports System.Data

''' <summary>
''' Popup page: sets up groups of accounts (UNITSHUB_ACCOUNTGROUP) and the accounts in
''' each group (UNITSHUB_ACCOUNTGROUPDETAILS). Same pattern as PaymentSchedule.aspx.
'''
''' Top options:
'''   New  : "New group name" box; Save creates the group (next GROUP_ID) and its accounts.
'''   Edit : group dropdown; picking a group shows its active accounts; Save replaces them.
'''   Copy : group dropdown + "New group name"; picking a group shows its accounts;
'''          Save creates a new group (next GROUP_ID) with the accounts in the grid.
'''
''' ADD    : the entry line (Account, Account type, Escrow type, Account no., Branch) + "Add" appends
'''          one row to the grid and clears the line.
''' DELETE : the bin icon removes a row from the grid (not from the database).
''' The grid's rows are kept in ViewState (AccountsData) between postbacks.
''' </summary>
Partial Class AccountsGroup
    Inherits System.Web.UI.Page

    ' Table names in one place, so they are easy to change
    Private Const GROUP_TABLE As String = "UNITSHUB_ACCOUNTGROUP"
    Private Const DETAIL_TABLE As String = "UNITSHUB_ACCOUNTGROUPDETAILS"

    ''' <summary>The DataTable bound to gvAccounts, kept in ViewState between postbacks.</summary>
    Private Property AccountsData As DataTable
        Get
            Return TryCast(ViewState("AccountsData"), DataTable)
        End Get
        Set(value As DataTable)
            ViewState("AccountsData") = value
        End Set
    End Property

    ''' <summary>
    ''' SQL condition for a detail line that is still active (not closed). Lines saved
    ''' here have END_IN = NULL; blanks or '0' count as active too. Used for loading AND
    ''' saving, so both always agree on which lines are current.
    ''' </summary>
    Private Shared Function ActiveLineCondition(Optional TableAlias As String = "") As String
        Dim c As String = If(TableAlias = "", "END_IN", TableAlias & ".END_IN")
        Return "(" & c & " IS NULL OR TRIM(" & c & ") IS NULL OR TRIM(" & c & ") = '0')"
    End Function

    ''' <summary>An empty table with the grid's columns.</summary>
    Private Shared Function NewAccountsTable() As DataTable
        Dim dt As New DataTable("GroupAccounts")
        dt.Columns.Add("DETAIL_ID", GetType(String))
        dt.Columns.Add("Account", GetType(String))
        dt.Columns.Add("AccountType", GetType(String))
        dt.Columns.Add("EscrowType", GetType(String))
        dt.Columns.Add("AccountNo", GetType(String))
        dt.Columns.Add("Branch", GetType(String))
        Return dt
    End Function

    Private Sub AccountsGroup_Load(sender As Object, e As EventArgs) Handles Me.Load
        If Not IsPostBack Then
            LoadExistingGroups()
            rblMode.SelectedValue = "New"
            BindGrid(Nothing)
        End If
    End Sub

    Private Sub LoadExistingGroups()
        ddlExistingGroup.Items.Clear()
        ddlExistingGroup.Items.Add(New ListItem("-- Select a group --", ""))

        Dim DT As DataTable = GetDataTable(EBDB, "SELECT GROUP_ID, NAME FROM " & GROUP_TABLE & " ORDER BY NAME")
        If DT IsNot Nothing Then
            For Each r As DataRow In DT.Rows
                ddlExistingGroup.Items.Add(New ListItem(Convert.ToString(r("NAME")), Convert.ToString(r("GROUP_ID"))))
            Next
        End If
    End Sub

    ' ==================================================================
    ' Top options: Add New Groups of Accounts / Edit Existing Group / Copy Existing Group
    ' ==================================================================

    Private ReadOnly Property SelectedMode As String
        Get
            Dim m As String = Convert.ToString(rblMode.SelectedValue)
            Return If(m = "", "New", m)
        End Get
    End Property

    ''' <summary>Switching option starts clean: no group picked, empty name, empty line and grid.</summary>
    Protected Sub rblMode_SelectedIndexChanged(sender As Object, e As EventArgs) Handles rblMode.SelectedIndexChanged
        If ddlExistingGroup.Items.Count = 0 Then LoadExistingGroups()
        ddlExistingGroup.SelectedIndex = 0
        txtNewGroupName.Text = ""
        ClearEntryLine()
        BindGrid(Nothing)
        lblMessage.Visible = False
    End Sub

    ''' <summary>Shows the controls the chosen option needs, every request.</summary>
    Protected Sub Page_PreRender(sender As Object, e As EventArgs) Handles Me.PreRender
        Dim mode As String = SelectedMode
        pnlExistingGroup.Visible = (mode = "Edit" OrElse mode = "Copy")
        pnlNewGroupName.Visible = (mode = "New" OrElse mode = "Copy")
        lblExistingGroup.Text = If(mode = "Copy", "Copy from", "Group")

        Select Case mode
            Case "Edit" : litPageTitle.Text = "Edit Accounts Group"
            Case "Copy" : litPageTitle.Text = "Copy Accounts Group"
            Case Else : litPageTitle.Text = "New Accounts Group"
        End Select
    End Sub

    ''' <summary>
    ''' Next GROUP_ID: the highest numeric GROUP_ID + 1, padded with zeros to the width
    ''' of the existing IDs (e.g. 00007 -> 00008). "1" if there are none.
    ''' </summary>
    Private Function GetNextGroupId() As String
        Dim DT As DataTable = GetDataTable(EBDB,
            "SELECT MAX(TO_NUMBER(GROUP_ID)) AS MAX_ID, MAX(LENGTH(GROUP_ID)) AS ID_LEN " &
            "FROM " & GROUP_TABLE & " WHERE REGEXP_LIKE(GROUP_ID, '^[0-9]+$')")

        Dim maxId As Long = 0
        Dim width As Integer = 1
        If DT IsNot Nothing AndAlso DT.Rows.Count > 0 Then
            If Not IsDBNull(DT.Rows(0)("MAX_ID")) Then maxId = Convert.ToInt64(DT.Rows(0)("MAX_ID"))
            If Not IsDBNull(DT.Rows(0)("ID_LEN")) Then width = Convert.ToInt32(DT.Rows(0)("ID_LEN"))
        End If
        Return (maxId + 1).ToString().PadLeft(width, "0"c)
    End Function

    Private Function GroupNameExists(Name As String) As Boolean
        Dim DT As DataTable = GetDataTable(EBDB,
            "SELECT GROUP_ID FROM " & GROUP_TABLE & " WHERE UPPER(TRIM(NAME)) = '" &
            Name.Trim().ToUpperInvariant().Replace("'", "''") & "'")
        Return DT IsNot Nothing AndAlso DT.Rows.Count > 0
    End Function

    ''' <summary>Picking a group loads its active accounts. "-- Select a group --" clears the grid.</summary>
    Protected Sub ddlExistingGroup_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ddlExistingGroup.SelectedIndexChanged
        LoadGroupDetails(ddlExistingGroup.SelectedValue)

        ' Copy: suggest a name for the new group
        If SelectedMode = "Copy" AndAlso ddlExistingGroup.SelectedValue <> "" AndAlso txtNewGroupName.Text.Trim() = "" Then
            txtNewGroupName.Text = "Copy of " & ddlExistingGroup.SelectedItem.Text
        End If
    End Sub

    Private Sub LoadGroupDetails(GroupId As String)
        lblMessage.Visible = False

        If String.IsNullOrEmpty(GroupId) Then
            BindGrid(Nothing)
            Exit Sub
        End If

        Dim SQL As String = ""
        SQL &= " SELECT D.DETAIL_ID, " & vbCrLf
        SQL &= "        D.ACCOUNT     AS ""Account"", " & vbCrLf
        SQL &= "        D.ACCOUNT_TYPE AS ""AccountType"", " & vbCrLf
        SQL &= "        D.ESCROW_TYPE AS ""EscrowType"", " & vbCrLf
        SQL &= "        D.ACCOUNT_NO  AS ""AccountNo"", " & vbCrLf
        SQL &= "        D.BRANCH      AS ""Branch"" " & vbCrLf
        SQL &= " FROM   " & DETAIL_TABLE & " D " & vbCrLf
        SQL &= " WHERE  D.GROUP_ID = '" & GroupId.Replace("'", "''") & "' " & vbCrLf
        SQL &= "   AND  " & ActiveLineCondition("D") & " " & vbCrLf
        ' DETAIL_ID as a number when it is one, so 10 comes after 9
        SQL &= " ORDER BY CASE WHEN REGEXP_LIKE(D.DETAIL_ID, '^[0-9]+$') THEN TO_NUMBER(D.DETAIL_ID) END, D.DETAIL_ID "

        Try
            ' Copy into a table with known columns/types, so ViewState and Add always work
            Dim src As DataTable = GetDataTable(EBDB, SQL)
            Dim dt As DataTable = NewAccountsTable()
            If src IsNot Nothing Then
                For Each r As DataRow In src.Rows
                    dt.Rows.Add(Convert.ToString(r("DETAIL_ID")),
                                Convert.ToString(r("Account")),
                                Convert.ToString(r("AccountType")),
                                Convert.ToString(r("EscrowType")),
                                Convert.ToString(r("AccountNo")),
                                Convert.ToString(r("Branch")))
                Next
            End If
            BindGrid(dt)

            If dt.Rows.Count = 0 Then ShowMessage("This group has no active accounts.", False)
        Catch ex As Exception
            BindGrid(Nothing)
            ShowMessage("The group's accounts couldn't be loaded: " & ex.Message, False)
        End Try
    End Sub

    ''' <summary>Binds the grid to Dt (Nothing = empty) and remembers it for later postbacks.</summary>
    Private Sub BindGrid(Dt As DataTable)
        If Dt IsNot Nothing AndAlso String.IsNullOrEmpty(Dt.TableName) Then Dt.TableName = "GroupAccounts"
        AccountsData = Dt
        gvAccounts.DataSource = Dt
        gvAccounts.DataBind()
    End Sub

    Private Sub ClearEntryLine()
        txtAccount.Text = ""
        txtAccountType.Text = ""
        txtEscrowType.Text = ""
        txtAccountNo.Text = ""
        txtBranch.Text = ""
    End Sub

    ''' <summary>
    ''' "+ Add": checks the entry line and appends it to the grid.
    ''' Account and Account no. are required; the same Account no. + Branch can't be added twice.
    ''' </summary>
    Protected Sub btnAddLine_Click(sender As Object, e As EventArgs) Handles btnAddLine.Click
        Dim account As String = txtAccount.Text.Trim()
        Dim accountType As String = txtAccountType.Text.Trim()
        Dim escrowType As String = txtEscrowType.Text.Trim()
        Dim accountNo As String = txtAccountNo.Text.Trim()
        Dim branch As String = txtBranch.Text.Trim()

        If account = "" Then
            ShowMessage("Enter the Account.", False)
            txtAccount.Focus()
            Exit Sub
        End If
        If accountNo = "" Then
            ShowMessage("Enter the Account no.", False)
            txtAccountNo.Focus()
            Exit Sub
        End If

        Dim dt As DataTable = AccountsData
        If dt Is Nothing Then dt = NewAccountsTable()

        For Each r As DataRow In dt.Rows
            If String.Equals(Convert.ToString(r("AccountNo")).Trim(), accountNo, StringComparison.OrdinalIgnoreCase) AndAlso
               String.Equals(Convert.ToString(r("Branch")).Trim(), branch, StringComparison.OrdinalIgnoreCase) Then
                ShowMessage("Account no. " & accountNo & If(branch = "", "", " (" & branch & ")") & " is already in the list.", False)
                txtAccountNo.Focus()
                Exit Sub
            End If
        Next

        Dim newRow As DataRow = dt.NewRow()
        newRow("DETAIL_ID") = DBNull.Value
        newRow("Account") = account
        newRow("AccountType") = accountType
        newRow("EscrowType") = escrowType
        newRow("AccountNo") = accountNo
        newRow("Branch") = branch
        dt.Rows.Add(newRow)

        BindGrid(dt)
        ClearEntryLine()
        lblMessage.Visible = False
        txtAccount.Focus()
    End Sub

    ''' <summary>The bin icon: removes that row from the grid only. "#" renumbers itself.</summary>
    Protected Sub gvAccounts_RowCommand(sender As Object, e As GridViewCommandEventArgs) Handles gvAccounts.RowCommand
        If e.CommandName <> "DeleteRow" Then Exit Sub

        Dim rowIndex As Integer
        If Not Integer.TryParse(Convert.ToString(e.CommandArgument), rowIndex) Then Exit Sub

        Dim dt As DataTable = AccountsData
        If dt Is Nothing OrElse rowIndex < 0 OrElse rowIndex >= dt.Rows.Count Then Exit Sub

        dt.Rows.RemoveAt(rowIndex)
        dt.AcceptChanges()
        BindGrid(dt)
        lblMessage.Visible = False
    End Sub

    ''' <summary>
    ''' Save, by option:
    '''   New  : creates the group (next GROUP_ID, NAME = "New group name") and its accounts.
    '''   Edit : saves the grid as the selected group's accounts (the old lines are closed).
    '''   Copy : like New, with the accounts shown in the grid.
    ''' Afterwards the group is shown in Edit mode.
    ''' </summary>
    Protected Sub btnSave_Click(sender As Object, e As EventArgs) Handles btnSave.Click
        Dim mode As String = SelectedMode
        Dim dt As DataTable = AccountsData

        ' --- checks ---
        If (mode = "Edit" OrElse mode = "Copy") AndAlso ddlExistingGroup.SelectedValue = "" Then
            ShowMessage(If(mode = "Copy", "Select the group to copy.", "Select a group first."), False)
            Exit Sub
        End If

        Dim newName As String = txtNewGroupName.Text.Trim()
        If mode = "New" OrElse mode = "Copy" Then
            If newName = "" Then
                ShowMessage("Enter the name of the new group.", False)
                Exit Sub
            End If
            If GroupNameExists(newName) Then
                ShowMessage("A group named '" & newName & "' already exists. Choose another name.", False)
                Exit Sub
            End If
        End If

        If dt Is Nothing OrElse dt.Rows.Count = 0 Then
            ShowMessage("Add at least one account before saving.", False)
            Exit Sub
        End If

        Dim lineCount As String = dt.Rows.Count & If(dt.Rows.Count = 1, " account", " accounts")

        ' --- Edit: replace the selected group's accounts ---
        If mode = "Edit" Then
            Dim groupId As String = ddlExistingGroup.SelectedValue
            Dim groupName As String = ddlExistingGroup.SelectedItem.Text
            Dim problem As String = SaveGroupLines(groupId, dt)
            If problem <> "" Then
                ShowMessage(problem, False)
                Exit Sub
            End If
            LoadGroupDetails(groupId)
            ShowMessage(lineCount & " saved for " & groupName & ".", True)
            Exit Sub
        End If

        ' --- New / Copy: a new group with the next GROUP_ID, then its accounts ---
        Dim newGroupId As String
        Try
            newGroupId = GetNextGroupId()
            DB.ExecuteNonQuery(EBDB_CS,
                "INSERT INTO " & GROUP_TABLE & " (GROUP_ID, NAME) VALUES ('" &
                newGroupId.Replace("'", "''") & "', '" & newName.Replace("'", "''") & "')")
        Catch ex As Exception
            ShowMessage("The group couldn't be created: " & ex.Message, False)
            Exit Sub
        End Try

        Dim linesProblem As String = SaveGroupLines(newGroupId, dt)
        If linesProblem <> "" Then
            ' Don't leave a group without accounts behind
            Try
                DB.ExecuteNonQuery(EBDB_CS, "DELETE FROM " & GROUP_TABLE & " WHERE GROUP_ID = '" & newGroupId.Replace("'", "''") & "'")
            Catch
            End Try
            ShowMessage(linesProblem, False)
            Exit Sub
        End If

        Dim copiedFrom As String = If(mode = "Copy", " (copied from " & ddlExistingGroup.SelectedItem.Text & ")", "")

        ' Show the new group, ready to edit
        LoadExistingGroups()
        rblMode.SelectedValue = "Edit"
        ddlExistingGroup.SelectedValue = newGroupId
        txtNewGroupName.Text = ""
        ClearEntryLine()
        LoadGroupDetails(newGroupId)
        ShowMessage("Group '" & newName & "' created (ID " & newGroupId & ") with " & lineCount & copiedFrom & ".", True)
    End Sub

    ''' <summary>
    ''' Writes the grid as a group's accounts in the detail table:
    '''   DETAIL_ID = the row's # (1, 2, 3 ...), GROUP_ID = GroupId,
    '''   ACCOUNT / ACCOUNT_TYPE / ESCROW_TYPE / ACCOUNT_NO / BRANCH = the grid's columns,
    '''   START_FROM = now in Unix seconds (UTC, same for every row), END_IN = NULL.
    ''' Then the group's previously active lines are closed with END_IN = the same moment.
    ''' If an insert fails, this save's rows are removed and the old lines stay as they were.
    ''' Returns "" when all went well, otherwise what went wrong.
    ''' </summary>
    Private Function SaveGroupLines(GroupId As String, dt As DataTable) As String
        Dim safeGroup As String = GroupId.Replace("'", "''")
        Dim startFrom As String = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()
        Dim inserted As Integer = 0

        Try
            For i As Integer = 0 To dt.Rows.Count - 1
                Dim r As DataRow = dt.Rows(i)
                Dim detailId As String = (i + 1).ToString()

                DB.ExecuteNonQuery(EBDB_CS,
                    "INSERT INTO " & DETAIL_TABLE & " (DETAIL_ID, GROUP_ID, ACCOUNT, ACCOUNT_TYPE, ESCROW_TYPE, ACCOUNT_NO, BRANCH, START_FROM, END_IN) " &
                    "VALUES ('" & detailId & "', '" & safeGroup & "', " &
                    SqlText(r("Account")) & ", " & SqlText(r("AccountType")) & ", " & SqlText(r("EscrowType")) & ", " &
                    SqlText(r("AccountNo")) & ", " & SqlText(r("Branch")) & ", '" & startFrom & "', NULL)")
                inserted += 1
            Next
        Catch ex As Exception
            If inserted > 0 Then
                Try
                    DB.ExecuteNonQuery(EBDB_CS,
                        "DELETE FROM " & DETAIL_TABLE & " " &
                        "WHERE GROUP_ID = '" & safeGroup & "' AND START_FROM = '" & startFrom & "' AND END_IN IS NULL")
                Catch
                End Try
            End If
            Return "The accounts couldn't be saved (nothing was changed): " & ex.Message
        End Try

        ' Close the previous active lines (same "active" rule as LoadGroupDetails)
        Try
            DB.ExecuteNonQuery(EBDB_CS,
                "UPDATE " & DETAIL_TABLE & " SET END_IN = '" & startFrom & "' " &
                "WHERE GROUP_ID = '" & safeGroup & "' AND " & ActiveLineCondition() & " " &
                "AND (START_FROM IS NULL OR START_FROM <> '" & startFrom & "')")
        Catch ex As Exception
            Return "The new accounts were saved, but the previous ones couldn't be closed: " & ex.Message
        End Try

        Return ""
    End Function

    ''' <summary>A value as a quoted SQL string ('' escaped), or NULL when empty.</summary>
    Private Shared Function SqlText(Value As Object) As String
        Dim s As String = Convert.ToString(Value).Trim()
        If s = "" Then Return "NULL"
        Return "'" & s.Replace("'", "''") & "'"
    End Function

    Private Sub ShowMessage(Text As String, IsOk As Boolean)
        lblMessage.Text = Server.HtmlEncode(Text)
        lblMessage.CssClass = "form-message " & If(IsOk, "ok", "err")
        lblMessage.Visible = (Text <> "")
    End Sub

    ''' <summary>POPUP close: closes without saving anything.</summary>
    Protected Sub imgClose_Click(sender As Object, e As ImageClickEventArgs) Handles imgClose.Click
        VendorPopupHelper.RegisterPopupSelectionAndClose(Me, False, skipPostBack:=False)
    End Sub
End Class
