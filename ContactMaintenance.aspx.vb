Imports System.Data
Imports System.DateTime
Imports System.Drawing
Imports System.Globalization

Partial Class ContactMaintenance
    Inherits System.Web.UI.Page

    Protected Sub Menu3_MenuItemClick(sender As Object, e As MenuEventArgs) Handles Menu3.MenuItemClick
        Try
            MultiView3.ActiveViewIndex = Menu3.Items.IndexOf(Menu3.SelectedItem)

        Catch ex As Exception

        End Try
    End Sub

    Protected Sub calDOB_SelectionChanged(sender As Object, e As EventArgs) Handles calDOB.SelectionChanged
        Try
            txtDOB.Text = calDOB.SelectedDate.ToString("yyyy-MM-dd")
            CalculateAge()

            ' Keep the calendar hidden again after a date has been picked
            pnlCalendar.Style("display") = "none"

        Catch ex As Exception

        End Try
    End Sub

    Protected Sub calIssueDate_SelectionChanged(sender As Object, e As EventArgs) Handles calIssueDate.SelectionChanged
        Try
            txtIssueDate.Text = calIssueDate.SelectedDate.ToString("yyyy-MM-dd")

            ' Keep the calendar hidden again after a date has been picked
            pnlCalendarIssue.Style("display") = "none"

        Catch ex As Exception

        End Try
    End Sub

    Protected Sub calExpiryDate_SelectionChanged(sender As Object, e As EventArgs) Handles calExpiryDate.SelectionChanged
        Try
            txtExpiryDate.Text = calExpiryDate.SelectedDate.ToString("yyyy-MM-dd")

            ' Keep the calendar hidden again after a date has been picked
            pnlCalendarExpiry.Style("display") = "none"

        Catch ex As Exception

        End Try
    End Sub

    Protected Sub btnCalculateAge_Click(sender As Object, e As EventArgs) Handles btnCalculateAge.Click
        CalculateAge()
    End Sub

    Protected Sub lnkContactDetails_Click(sender As Object, e As EventArgs) Handles lnkContactDetails.Click
        Try
            MultiView3.ActiveViewIndex = 1
            Menu3.Items(1).Selected = True

        Catch ex As Exception

        End Try
    End Sub

    Protected Sub btnLoad_Click(sender As Object, e As EventArgs) Handles btnLoad.Click
        'Try
        LoadContact("000527")

        'Catch ex As Exception
        '    lblMessage.Text = "An error occurred while loading: " & ex.Message
        '    lblMessage.Visible = True
        'End Try
    End Sub

    ''' <summary>
    ''' Loads the UNITSHUB_CONTACTS row with the given ID back into the form fields
    ''' across View1 (General), View2 (Contact Details / Email) and View3 (Address Details).
    ''' </summary>
    ''' <remarks>
    ''' TODO: "GetDataTable" below is the same placeholder used in SaveContact / ActionAddEdit
    ''' for whatever read helper your project actually exposes — swap it for your real one.
    ''' TODO: contactId is hard-coded to "000527" for now per the current requirement; wire this
    ''' up to however contacts are actually selected/navigated to once that's available.
    ''' </remarks>
    Private Sub LoadContact(ByVal contactId As String)
        Dim dt As New DataTable
        dt = GetDataTable(EBDB, "SELECT * FROM UNITSHUB_CONTACTS WHERE ID = '" & contactId.Replace("'", "''") & "'")

        If dt.Rows.Count = 0 Then
            lblMessage.Text = "No contact found with ID " & contactId & "."
            lblMessage.Visible = True
            Return
        End If

        Dim row As DataRow = dt.Rows(0)

        txtFullName.Text = DbString(row, "NAME")
        txtArabicName.Text = DbString(row, "ARABICNAME")
        SetDropDownValue(ddlGender, DbString(row, "GENDER"))
        txtDOB.Text = DbDateString(row, "DOB")
        txtAge.Text = DbString(row, "AGE")
        txtNationalID.Text = DbString(row, "NATIONALID")
        SetDropDownValue(ddlNationality, DbString(row, "NATIONALITY"))
        txtMobile1.Text = DbString(row, "MOBILE")
        txtMobile2.Text = DbString(row, "BUSINESSPHONE")
        txtHomePhone.Text = DbString(row, "HOMEPHONE")
        txtFax.Text = DbString(row, "FAX")
        txtPOBox.Text = DbString(row, "POBOX")
        txtEmail.Text = DbString(row, "EMAIL")
        txtPassportNo.Text = DbString(row, "PASSPORTNO")
        txtIssueDate.Text = DbDateString(row, "PASSPORTISSUE")
        txtExpiryDate.Text = DbDateString(row, "PASSPORTEXPIRY")
        SetDropDownValue(ddlCountry, DbString(row, "COUNTRY"))
        txtCity.Text = DbString(row, "CITY")
        txtBlock.Text = DbString(row, "BLOCK")
        txtRoad.Text = DbString(row, "ROAD")
        txtBuilding.Text = DbString(row, "BUILDING")
        txtFlat.Text = DbString(row, "FLAT")

        lblMessage.Text = "Loaded contact " & contactId & "."
        lblMessage.Visible = True
    End Sub

    ''' <summary>Reads a column as a plain string, treating DBNull as empty.</summary>
    Private Function DbString(ByVal row As DataRow, ByVal columnName As String) As String
        If row.IsNull(columnName) Then
            Return ""
        End If

        Return row(columnName).ToString()
    End Function

    ''' <summary>Reads a date-ish column and formats it as yyyy-MM-dd, treating DBNull as empty.</summary>
    Private Function DbDateString(ByVal row As DataRow, ByVal columnName As String) As String
        If row.IsNull(columnName) Then
            Return ""
        End If

        Dim value As Object = row(columnName)

        If TypeOf value Is Date Then
            Return CType(value, Date).ToString("yyyy-MM-dd")
        End If

        Dim parsed As Date
        If Date.TryParse(value.ToString(), parsed) Then
            Return parsed.ToString("yyyy-MM-dd")
        End If

        Return value.ToString()
    End Function

    ''' <summary>
    ''' Selects the dropdown item matching the given value, if one exists; leaves the
    ''' current selection unchanged otherwise (e.g. if the DB value isn't one of the
    ''' fixed list options, such as Gender/Nationality/Country only offering one choice today).
    ''' </summary>
    Private Sub SetDropDownValue(ByVal ddl As DropDownList, ByVal value As String)
        If String.IsNullOrEmpty(value) Then
            Return
        End If

        Dim item As ListItem = ddl.Items.FindByValue(value)
        If item IsNot Nothing Then
            ddl.ClearSelection()
            item.Selected = True
        End If
    End Sub

    Protected Sub btnCancel_Click(sender As Object, e As EventArgs) Handles btnCancel.Click
        ' TODO: navigate away from this page / discard changes as appropriate
        lblMessage.Visible = False
    End Sub

    Protected Sub imgClose_Click(sender As Object, e As ImageClickEventArgs) Handles imgClose.Click

        VendorPopupHelper.RegisterPopupSelectionAndClose(Me, False, skipPostBack:=False)

    End Sub

    Protected Sub btnSave_Click(sender As Object, e As EventArgs) Handles btnSave.Click
        Try
            If Not Page.IsValid Then
                Return
            End If

            SaveContact()

            lblMessage.Text = "Contact details saved successfully."
            lblMessage.Visible = True

            If lblIsDialogue.Text <> "yes" Then Exit Sub


            'Dim RowValues As New Dictionary(Of String, Object)
            Dim SelectedCustomer As New Dictionary(Of String, Object)
            SelectedCustomer.Add("NAME", txtFullName.Text)
            SelectedCustomer.Add("NATIONALID", txtNationalID.Text)
            SelectedCustomer.Add("ID", lblID.Text)



            Dim SelectedItems As New List(Of Dictionary(Of String, Object))
            SelectedItems.Add(SelectedCustomer)

            ' Use the key the opener passed in (?vpKey=...) so the opener can read the
            ' result back with the same key it registered the popup with.
            Dim ReturnKey As String = VendorPopupHelper.GetPopupReturnKey(Me)

            VendorPopupHelper.RegisterPopupSelectionAndClose(
                                    page:=Me,
                                    returnValue:=SelectedItems,
                                    startupScriptKey:=ReturnKey,
                                    skipPostBack:=False)

        Catch ex As Exception
            lblMessage.Text = "An error occurred while saving: " & ex.Message
            lblMessage.Visible = True
        End Try
    End Sub

    ''' <summary>
    ''' Inserts one row into UNITSHUB_CONTACTS from the values captured across
    ''' View1 (General), View2 (Contact Details / Email) and View3 (Address Details).
    ''' </summary>
    ''' <remarks>
    ''' TODO: "ExecuteNonQuery"/"GetDataTable" below are placeholders for whatever
    ''' read/write helpers your project actually exposes (same placeholders used in
    ''' ActionAddEdit's SaveActionControl) — swap them for your real ones. "EBDB" is
    ''' assumed to be the same shared connection object/string used elsewhere in the app.
    ''' TODO: ID is assumed to be a VARCHAR2 column (not a DB-generated identity), so it's
    ''' generated here as a zero-padded 6-digit numeric string (e.g. "000001") via
    ''' MAX(TO_NUMBER(ID))+1 — the same approach ActionAddEdit uses for ACTION_ID. If
    ''' UNITSHUB_CONTACTS.ID is instead an identity/sequence column, remove this step and
    ''' let the DB assign it.
    ''' TODO: DOB / PassportIssue / PassportExpiry are inserted via TO_DATE(..., 'YYYY-MM-DD')
    ''' on the assumption they are Oracle DATE columns; if they're actually VARCHAR2, use
    ''' SqlText(...) for them instead of SqlDate(...).
    ''' </remarks>
    Private Sub SaveContact()
        If String.Equals(lblMode.Text, "New", StringComparison.OrdinalIgnoreCase) Then

            ' 1. Generate the new zero-padded contact ID.
            Dim nextIdDT As New DataTable
            nextIdDT = GetDataTable(EBDB, "SELECT LPAD(NVL(MAX(TO_NUMBER(ID)), 0) + 1, 6, '0') AS NEXT_ID FROM UNITSHUB_CONTACTS")
            Dim contactId As String = nextIdDT.Rows(0)("NEXT_ID").ToString()

            ' 2. Insert the contact row.
            Dim insertSql As String =
                "INSERT INTO UNITSHUB_CONTACTS (ID, NAME, ARABICNAME, GENDER, DOB, AGE, NATIONALID, NATIONALITY, " &
                "MOBILE, BUSINESSPHONE, HOMEPHONE, FAX, POBOX, EMAIL, PASSPORTNO, PASSPORTISSUE, PASSPORTEXPIRY, " &
                "COUNTRY, CITY, BLOCK, ROAD, BUILDING, FLAT) VALUES (" &
                "'" & contactId & "', " &
                SqlText(txtFullName.Text) & ", " &
                SqlText(txtArabicName.Text) & ", " &
                SqlText(ddlGender.SelectedValue) & ", " &
                SqlDate(txtDOB.Text) & ", " &
                SqlNumber(txtAge.Text) & ", " &
                SqlText(txtNationalID.Text) & ", " &
                SqlText(ddlNationality.SelectedValue) & ", " &
                SqlText(txtMobile1.Text) & ", " &
                SqlText(txtMobile2.Text) & ", " &
                SqlText(txtHomePhone.Text) & ", " &
                SqlText(txtFax.Text) & ", " &
                SqlText(txtPOBox.Text) & ", " &
                SqlText(txtEmail.Text) & ", " &
                SqlText(txtPassportNo.Text) & ", " &
                SqlDate(txtIssueDate.Text) & ", " &
                SqlDate(txtExpiryDate.Text) & ", " &
                SqlText(ddlCountry.SelectedValue) & ", " &
                SqlText(txtCity.Text) & ", " &
                SqlText(txtBlock.Text) & ", " &
                SqlText(txtRoad.Text) & ", " &
                SqlText(txtBuilding.Text) & ", " &
                SqlText(txtFlat.Text) &
                ")"

            ExecuteNonQuery(EBDB, insertSql)

            ' 3. Switch this form over to editing the row that was just created, so a
            ' second Save (without reloading the page) updates it instead of inserting
            ' a duplicate.
            lblID.Text = contactId
            lblMode.Text = "Edit"

        Else
            ' Edit mode: update the existing row identified by lblID.Text instead of
            ' generating a new ID / inserting a new row.
            Dim updateSql As String =
                "UPDATE UNITSHUB_CONTACTS SET " &
                "NAME = " & SqlText(txtFullName.Text) & ", " &
                "ARABICNAME = " & SqlText(txtArabicName.Text) & ", " &
                "GENDER = " & SqlText(ddlGender.SelectedValue) & ", " &
                "DOB = " & SqlDate(txtDOB.Text) & ", " &
                "AGE = " & SqlNumber(txtAge.Text) & ", " &
                "NATIONALID = " & SqlText(txtNationalID.Text) & ", " &
                "NATIONALITY = " & SqlText(ddlNationality.SelectedValue) & ", " &
                "MOBILE = " & SqlText(txtMobile1.Text) & ", " &
                "BUSINESSPHONE = " & SqlText(txtMobile2.Text) & ", " &
                "HOMEPHONE = " & SqlText(txtHomePhone.Text) & ", " &
                "FAX = " & SqlText(txtFax.Text) & ", " &
                "POBOX = " & SqlText(txtPOBox.Text) & ", " &
                "EMAIL = " & SqlText(txtEmail.Text) & ", " &
                "PASSPORTNO = " & SqlText(txtPassportNo.Text) & ", " &
                "PASSPORTISSUE = " & SqlDate(txtIssueDate.Text) & ", " &
                "PASSPORTEXPIRY = " & SqlDate(txtExpiryDate.Text) & ", " &
                "COUNTRY = " & SqlText(ddlCountry.SelectedValue) & ", " &
                "CITY = " & SqlText(txtCity.Text) & ", " &
                "BLOCK = " & SqlText(txtBlock.Text) & ", " &
                "ROAD = " & SqlText(txtRoad.Text) & ", " &
                "BUILDING = " & SqlText(txtBuilding.Text) & ", " &
                "FLAT = " & SqlText(txtFlat.Text) & " " &
                "WHERE ID = '" & lblID.Text.Replace("'", "''") & "'"

            ExecuteNonQuery(EBDB, updateSql)
        End If
    End Sub

    ''' <summary>Wraps a string value as a quoted, apostrophe-escaped SQL literal, or NULL when empty.</summary>
    Private Function SqlText(ByVal value As String) As String
        If String.IsNullOrEmpty(value) Then
            Return "NULL"
        End If

        Return "'" & value.Replace("'", "''") & "'"
    End Function

    ''' <summary>Wraps a yyyy-MM-dd date string as an Oracle TO_DATE(...) literal, or NULL when empty.</summary>
    Private Function SqlDate(ByVal value As String) As String
        If String.IsNullOrEmpty(value) Then
            Return "NULL"
        End If

        Return "TO_DATE('" & value.Replace("'", "''") & "', 'YYYY-MM-DD')"
    End Function

    ''' <summary>Passes a numeric value through unquoted, or NULL when empty/non-numeric.</summary>
    Private Function SqlNumber(ByVal value As String) As String
        Dim result As Integer

        If Integer.TryParse(value, result) Then
            Return result.ToString()
        End If

        Return "NULL"
    End Function

    Private Sub CalculateAge()
        Dim dob As Date

        If Date.TryParseExact(txtDOB.Text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, dob) Then
            Dim today As Date = Date.Today
            Dim age As Integer = today.Year - dob.Year

            If today.Month < dob.Month OrElse (today.Month = dob.Month AndAlso today.Day < dob.Day) Then
                age -= 1
            End If

            If age < 0 Then
                txtAge.Text = ""
            Else
                txtAge.Text = age.ToString()
            End If
        Else
            txtAge.Text = ""
        End If
    End Sub

    Private Sub ContactMaintenance_Load(sender As Object, e As EventArgs) Handles Me.Load
        If Not Page.IsPostBack Then

            If Not String.IsNullOrEmpty(Request("ID")) Then
                lblID.Text = Request("ID")
            End If

            If Not String.IsNullOrEmpty(Request("mode")) Then
                lblMode.Text = Request("mode")
            End If

            If Not String.IsNullOrEmpty(Request("isDialogue")) Then
                lblIsDialogue.Text = Request("isDialogue")
            End If
        End If
    End Sub
End Class