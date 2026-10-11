Imports System.Data
Imports System.Text.RegularExpressions

''' <summary>
''' Add a new user or edit an existing one in UNITSHUB_USERS
''' (USER_ID, FULL_NAME, EMAIL, IS_ENABLED, CREATED_ON, LAST_LOGIN).
'''
'''   New  : USER_ID typed, or left empty for the next number (highest numeric USER_ID + 1,
'''          same width); CREATED_ON = SYSDATE; LAST_LOGIN stays empty.
'''   Edit : USER_ID is fixed (the rights tables refer to it); FULL_NAME, EMAIL and
'''          IS_ENABLED can change. CREATED_ON / LAST_LOGIN are shown, never edited.
''' Rules: full name required; email optional but must look like an email and not be used
''' by another user; USER_ID must be new. IS_ENABLED is saved as 'Y' / 'N'.
''' Optional parameter: ?UserID= opens that user for editing.
''' Groups and rights are managed in ManageUsers.aspx.
''' </summary>
Partial Class UserAddEdit
    Inherits System.Web.UI.Page

    ''' <summary>The user being edited; "" while adding a new one.</summary>
    Private Property EditingUserId As String
        Get
            Return Convert.ToString(ViewState("EditingUserId"))
        End Get
        Set(value As String)
            ViewState("EditingUserId") = value
        End Set
    End Property

    Private ReadOnly Property IsNewUser As Boolean
        Get
            Return EditingUserId = ""
        End Get
    End Property

    Protected Sub Page_Load(sender As Object, e As EventArgs) Handles Me.Load
        If Not IsPostBack Then
            Dim requested As String = QueryValue("UserID")
            LoadUsers(requested)
            If ddlUser.SelectedValue <> "" Then
                ShowUser(ddlUser.SelectedValue)
            Else
                StartNew()
                If requested <> "" Then ShowNote("User '" & requested & "' wasn't found. You can add a new user instead.", "err")
            End If
        End If
    End Sub

    ''' <summary>A request parameter, trimmed; "" when it wasn't sent (never Nothing).</summary>
    Private Function QueryValue(Name As String) As String
        Return If(Request(Name), "").Trim()
    End Function

    ''' <summary>A value as a quoted, apostrophe-escaped SQL literal.</summary>
    Private Function Q(Value As String) As String
        Return "'" & If(Value, "").Replace("'", "''") & "'"
    End Function

    ''' <summary>True when IS_ENABLED says the account is off (N / NO / 0 / F).</summary>
    Private Function IsOff(Value As Object) As Boolean
        Dim s As String = Convert.ToString(Value).Trim().ToUpperInvariant()
        Return s.StartsWith("N") OrElse s = "0" OrElse s = "F" OrElse s = "FALSE"
    End Function

    ' ==================================================================
    ' Pick a user
    ' ==================================================================

    ''' <summary>All users as "Full name (USER_ID)"; disabled ones are marked.</summary>
    Private Sub LoadUsers(Preselect As String)
        ddlUser.Items.Clear()
        ddlUser.Items.Add(New ListItem("-- Choose a user to edit --", ""))
        Dim DT As DataTable = GetDataTable(EBDB,
            "SELECT USER_ID, FULL_NAME, IS_ENABLED FROM UNITSHUB_USERS ORDER BY NVL(FULL_NAME, USER_ID)")
        If DT IsNot Nothing Then
            For Each r As DataRow In DT.Rows
                Dim id As String = Convert.ToString(r("USER_ID")).Trim()
                Dim fullName As String = Convert.ToString(r("FULL_NAME")).Trim()
                Dim text As String = If(fullName = "", id, fullName & " (" & id & ")")
                If IsOff(r("IS_ENABLED")) Then text &= "  [Disabled]"
                ddlUser.Items.Add(New ListItem(text, id))
            Next
        End If
        If Preselect <> "" AndAlso ddlUser.Items.FindByValue(Preselect) IsNot Nothing Then
            ddlUser.SelectedValue = Preselect
        End If
    End Sub

    Protected Sub ddlUser_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ddlUser.SelectedIndexChanged
        lblNote.Visible = False
        If ddlUser.SelectedValue = "" Then
            StartNew()
        Else
            ShowUser(ddlUser.SelectedValue)
        End If
    End Sub

    Protected Sub btnNew_Click(sender As Object, e As EventArgs) Handles btnNew.Click
        lblNote.Visible = False
        ddlUser.SelectedIndex = 0
        StartNew()
    End Sub

    ''' <summary>Empty form for a new user.</summary>
    Private Sub StartNew()
        EditingUserId = ""
        txtUserId.Text = ""
        txtUserId.ReadOnly = False
        txtUserId.Attributes("placeholder") = "Leave empty for " & GetNextUserId()
        lblUserIdHint.Text = "Used to sign in and in every rights table; it can't be changed later."
        txtFullName.Text = ""
        txtEmail.Text = ""
        chkEnabled.Checked = True
        pnlFacts.Visible = False

        litDetailsTitle.Text = "New user"
        lblMode.Text = "NEW"
        lblMode.CssClass = "mode-tag new"
        btnSave.Text = "Add User"
        btnCancel.Text = "Clear"
        txtUserId.Focus()
    End Sub

    ''' <summary>Fills the form with an existing user (USER_ID locked).</summary>
    Private Sub ShowUser(UserId As String)
        Dim DT As DataTable = GetDataTable(EBDB,
            "SELECT USER_ID, FULL_NAME, EMAIL, IS_ENABLED, CREATED_ON, LAST_LOGIN FROM UNITSHUB_USERS WHERE USER_ID = " & Q(UserId))
        If DT Is Nothing OrElse DT.Rows.Count = 0 Then
            StartNew()
            ShowNote("That user no longer exists.", "err")
            Exit Sub
        End If
        Dim r As DataRow = DT.Rows(0)

        EditingUserId = Convert.ToString(r("USER_ID")).Trim()
        txtUserId.Text = EditingUserId
        txtUserId.ReadOnly = True
        lblUserIdHint.Text = "The user ID can't be changed: groups and rights refer to it."
        txtFullName.Text = Convert.ToString(r("FULL_NAME")).Trim()
        txtEmail.Text = Convert.ToString(r("EMAIL")).Trim()
        chkEnabled.Checked = Not IsOff(r("IS_ENABLED"))

        litCreatedOn.Text = FormatDate(r("CREATED_ON"), "unknown")
        litLastLogin.Text = FormatDate(r("LAST_LOGIN"), "never")
        pnlFacts.Visible = True

        litDetailsTitle.Text = Server.HtmlEncode(If(txtFullName.Text = "", EditingUserId, txtFullName.Text))
        lblMode.Text = "EDIT"
        lblMode.CssClass = "mode-tag edit"
        btnSave.Text = "Save Changes"
        btnCancel.Text = "Discard changes"
        txtFullName.Focus()
    End Sub

    ''' <summary>A date column as dd-MMM-yyyy HH:mm, or Fallback when empty.</summary>
    Private Function FormatDate(Value As Object, Fallback As String) As String
        If Value Is Nothing OrElse IsDBNull(Value) Then Return Fallback
        If TypeOf Value Is DateTime Then Return CType(Value, DateTime).ToString("dd-MMM-yyyy HH:mm")
        Dim s As String = Convert.ToString(Value).Trim()
        Return If(s = "", Fallback, Server.HtmlEncode(s))
    End Function

    ''' <summary>Clear (new) or reload the saved values (edit).</summary>
    Protected Sub btnCancel_Click(sender As Object, e As EventArgs) Handles btnCancel.Click
        lblNote.Visible = False
        If IsNewUser Then StartNew() Else ShowUser(EditingUserId)
    End Sub

    ' ==================================================================
    ' Save
    ' ==================================================================

    ''' <summary>
    ''' Next USER_ID: the highest numeric USER_ID + 1, padded with zeros to the width of the
    ''' existing IDs. "1" when there are none.
    ''' </summary>
    Private Function GetNextUserId() As String
        Dim DT As DataTable = GetDataTable(EBDB,
            "SELECT MAX(TO_NUMBER(USER_ID)) AS MAX_ID, MAX(LENGTH(USER_ID)) AS ID_LEN " &
            "FROM UNITSHUB_USERS WHERE REGEXP_LIKE(USER_ID, '^[0-9]+$')")
        Dim maxId As Long = 0
        Dim width As Integer = 1
        If DT IsNot Nothing AndAlso DT.Rows.Count > 0 Then
            If Not IsDBNull(DT.Rows(0)("MAX_ID")) Then maxId = Convert.ToInt64(DT.Rows(0)("MAX_ID"))
            If Not IsDBNull(DT.Rows(0)("ID_LEN")) Then width = Convert.ToInt32(DT.Rows(0)("ID_LEN"))
        End If
        Return (maxId + 1).ToString().PadLeft(width, "0"c)
    End Function

    ''' <summary>
    ''' Checks the form and adds (INSERT, CREATED_ON = SYSDATE) or updates the user.
    ''' The dropdown is reloaded and the saved user stays open for further edits.
    ''' </summary>
    Protected Sub btnSave_Click(sender As Object, e As EventArgs) Handles btnSave.Click
        Dim userId As String = If(IsNewUser, txtUserId.Text.Trim(), EditingUserId)
        Dim fullName As String = txtFullName.Text.Trim()
        Dim email As String = txtEmail.Text.Trim()
        Dim enabled As String = If(chkEnabled.Checked, "Y", "N")

        ' ---- checks ----
        If fullName = "" Then
            ShowNote("Enter the user's full name.", "err")
            txtFullName.Focus()
            Exit Sub
        End If
        If email <> "" AndAlso Not Regex.IsMatch(email, "^[^@\s]+@[^@\s]+\.[^@\s]+$") Then
            ShowNote("The email address doesn't look right.", "err")
            txtEmail.Focus()
            Exit Sub
        End If
        If IsNewUser AndAlso userId <> "" AndAlso Not Regex.IsMatch(userId, "^[A-Za-z0-9._\-]+$") Then
            ShowNote("The user ID may only contain letters, digits, dot, dash and underscore.", "err")
            txtUserId.Focus()
            Exit Sub
        End If

        Try
            If email <> "" Then
                Dim usedBy As DataTable = GetDataTable(EBDB,
                    "SELECT USER_ID, FULL_NAME FROM UNITSHUB_USERS WHERE UPPER(TRIM(EMAIL)) = " & Q(email.ToUpperInvariant()) &
                    If(IsNewUser, "", " AND USER_ID <> " & Q(userId)))
                If usedBy IsNot Nothing AndAlso usedBy.Rows.Count > 0 Then
                    ShowNote("That email is already used by " & Convert.ToString(usedBy.Rows(0)("FULL_NAME")).Trim() &
                             " (" & Convert.ToString(usedBy.Rows(0)("USER_ID")).Trim() & ").", "err")
                    txtEmail.Focus()
                    Exit Sub
                End If
            End If

            If IsNewUser Then
                If userId = "" Then userId = GetNextUserId()

                Dim exists As String = If(DB.RetreiveScalarSTRING(EBDB,
                    "SELECT COUNT(*) FROM UNITSHUB_USERS WHERE UPPER(USER_ID) = " & Q(userId.ToUpperInvariant())), "").Trim()
                If Val(exists) > 0 Then
                    ShowNote("User ID '" & userId & "' is already taken.", "err")
                    txtUserId.Focus()
                    Exit Sub
                End If

                DB.ExecuteNonQuery(EBDB_CS,
                    "INSERT INTO UNITSHUB_USERS (USER_ID, FULL_NAME, EMAIL, IS_ENABLED, CREATED_ON, LAST_LOGIN) VALUES (" &
                    Q(userId) & ", " & Q(fullName) & ", " & If(email = "", "NULL", Q(email)) & ", " & Q(enabled) & ", SYSDATE, NULL)")

                LoadUsers(userId)
                ShowUser(userId)
                ShowNote("User '" & fullName & "' added (ID " & userId & "). Give them groups and rights in Manage Users.", "ok")
            Else
                DB.ExecuteNonQuery(EBDB_CS,
                    "UPDATE UNITSHUB_USERS SET FULL_NAME = " & Q(fullName) &
                    ", EMAIL = " & If(email = "", "NULL", Q(email)) &
                    ", IS_ENABLED = " & Q(enabled) &
                    " WHERE USER_ID = " & Q(userId))

                LoadUsers(userId)
                ShowUser(userId)
                ShowNote("Changes to '" & fullName & "' saved.", "ok")
            End If
        Catch ex As Exception
            ShowNote("The user couldn't be saved: " & ex.Message, "err")
        End Try
    End Sub

    ' ==================================================================
    ' Helpers
    ' ==================================================================

    Private Sub ShowNote(Text As String, Kind As String)
        lblNote.Text = Server.HtmlEncode(Text)
        lblNote.CssClass = "note " & Kind
        lblNote.Visible = True
    End Sub

    ''' <summary>Closes the popup.</summary>
    Protected Sub imgClose_Click(sender As Object, e As ImageClickEventArgs) Handles imgClose.Click
        VendorPopupHelper.RegisterPopupSelectionAndClose(Me, False, skipPostBack:=False)
    End Sub
End Class
