Imports System.Data
Imports System.DateTime
Imports System.Drawing
Imports System.Globalization
Imports System.Collections.Generic
Imports System.IO

Partial Class ContactMaintenance
    Inherits System.Web.UI.Page

    ''' <summary>Entity this page's documents belong to (UNITSHUB_ENTITIES.ENTITY_NAME); also sent to AddAttachment.</summary>
    Private Const EntityName As String = "Customer"

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
        LoadedContactId = contactId   ' the Attachments / Comments tabs show this contact's items

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

            lblID.Text = "000324"
            lblMode.Text = "Edit"
            lblIsDialogue.Text = "No"

            ' "Display Attachments" filter of the Attachments tab
            LoadDocCategories()
            LoadContact(lblID.Text)
        End If
    End Sub
    ' ==================================================================
    ' Tabs 3 and 4: Attachments | Comments (taken from Unit.aspx)
    ' Tab order: 0 General | 1 Contact Details | 2 Attachments | 3 Comments | 4 Address
    ' Both tabs show the items about this contact (CONTACT_ID), whichever unit they
    ' were added from. Both are rebuilt on every request in Page_PreRender, so each file
    ' link keeps its DisplayInvoice popup after postbacks.
    ' ==================================================================

    Private Const AttachmentsTabIndex As Integer = 2
    Private Const CommentsTabIndex As Integer = 3
    Private Const TabPopupReturnKey As String = "AddAttachmentPopup"

    ''' <summary>Shows a tab and highlights its menu item.</summary>
    Private Sub ShowTab(Index As Integer)
        MultiView3.ActiveViewIndex = Index
        Menu3.Items(Index).Selected = True
    End Sub

    ''' <summary>Contact loaded with the Load button (when the page wasn't opened with ?ID=).</summary>
    Private Property LoadedContactId As String
        Get
            Return Convert.ToString(ViewState("LoadedContactId"))
        End Get
        Set(value As String)
            ViewState("LoadedContactId") = value
        End Set
    End Property

    ''' <summary>
    ''' The contact the Attachments / Comments tabs are about: the ID the page was opened
    ''' with (lblID), otherwise the one loaded with Load. "" for a new, unsaved contact.
    ''' </summary>
    Private ReadOnly Property CurrentContactId As String
        Get
            Dim id As String = Convert.ToString(lblID.Text).Trim()
            If id = "" Then id = Convert.ToString(LoadedContactId).Trim()
            Return id
        End Get
    End Property

    ''' <summary>"WHERE" part for items about this contact.</summary>
    Private Function ContactFilter(Alias_ As String, ContactId As String) As String
        Return Alias_ & ".CONTACT_ID = '" & ContactId.Replace("'", "''") & "'"
    End Function

    ''' <summary>
    ''' Fills "Display Attachments" with "All" plus the active categories linked to the
    ''' "Customer" entity (EntityName) in UNITSHUB_DOC2ENTITY, in SORT_ORDER.
    ''' </summary>
    Private Sub LoadDocCategories()
        ddlDocCategory.Items.Clear()
        ddlDocCategory.Items.Add(New ListItem("All", ""))
        Try
            ' Categories linked to this entity (UNITSHUB_DOC2ENTITY), compared upper-case
            Dim SQL As String = ""
            SQL = SQL + vbCrLf + " SELECT DISTINCT DC.CATEGORY_ID, DC.TITLE, DC.SORT_ORDER "
            SQL = SQL + vbCrLf + " FROM   UNITSHUB_DOC_CATEGORIES DC "
            SQL = SQL + vbCrLf + " INNER JOIN UNITSHUB_DOC2ENTITY DOC ON DC.CATEGORY_ID = DOC.CATEGORY_ID "
            SQL = SQL + vbCrLf + " INNER JOIN UNITSHUB_ENTITIES ENT ON DOC.ENTITY_ID = ENT.ENTITY_ID "
            SQL = SQL + vbCrLf + " WHERE  UPPER(ENT.ENTITY_NAME) = '" & EntityName.ToUpperInvariant().Replace("'", "''") & "' "
            SQL = SQL + vbCrLf + "   AND  DC.IS_ACTIVE = 'Y' "
            SQL = SQL + vbCrLf + " ORDER BY DC.SORT_ORDER, DC.TITLE "
            Dim DT As DataTable = GetDataTable(EBDB, SQL)
            If DT IsNot Nothing Then
                For Each r As DataRow In DT.Rows
                    ddlDocCategory.Items.Add(New ListItem(Convert.ToString(r("TITLE")), Convert.ToString(r("CATEGORY_ID"))))
                Next
            End If
        Catch ex As Exception
            ' Category tables not there yet - "All" only
        End Try
    End Sub

    Protected Sub ddlDocCategory_SelectedIndexChanged(sender As Object, e As EventArgs)
        ShowTab(AttachmentsTabIndex)
    End Sub

    ''' <summary>
    ''' Every request: rebuilds both tabs and wires the two "+" buttons. The "+" buttons are
    ''' only shown once the contact exists (has an ID).
    ''' </summary>
    Protected Sub Page_PreRender(sender As Object, e As EventArgs) Handles Me.PreRender
        Dim ContactId As String = CurrentContactId

        BindAttachments(ContactId)
        BindComments(ContactId)

        btnAddAttachment.Visible = (ContactId <> "")
        btnAddComment.Visible = (ContactId <> "")
        If ContactId = "" Then Exit Sub

        ' "+" (Attachments) opens AddAttachment for this contact, with the category picked
        ' in "Display Attachments" pre-selected. AddAttachment lists only the categories of
        ' the ProjectId it is given (like Unit.aspx does), so the project is sent too -
        ' without it a project's category isn't in AddAttachment's list and can't be selected.
        Dim CategoryId As String = Convert.ToString(ddlDocCategory.SelectedValue).Trim()
        Dim ProjectId As String = GetAttachmentProjectId(ContactId, CategoryId)
        Dim AttachUrl As String = "AddAttachment.aspx?ContactId=" & Server.UrlEncode(ContactId) &
                                  "&ProjectId=" & Server.UrlEncode(ProjectId) &
                                  "&CategoryId=" & Server.UrlEncode(CategoryId) &
                                  "&Entity=" & Server.UrlEncode(EntityName)

        ' Drop the click handler from the previous request (kept in ViewState) so the
        ' popup always opens with the category picked NOW, not the one of the first load
        btnAddAttachment.OnClientClick = ""
        btnAddAttachment.Attributes.Remove("onclick")

        VendorPopupHelper.RegisterVendorPopup(Me,
                                              btnAddAttachment,
                                              AttachUrl,
                                              1000, 0,
                                              PopupPlacement.Center,
                                              "",
                                              VendorPopupHelper.PopupDisplayMode.FrameOnly,
                                              returnKey:=TabPopupReturnKey)

        ' "+" (Comments) opens AddComment for this contact
        VendorPopupHelper.RegisterVendorPopup(Me,
                                              btnAddComment,
                                              "AddComment.aspx?ContactId=" & Server.UrlEncode(ContactId),
                                              1000, 0,
                                              PopupPlacement.Center,
                                              "",
                                              VendorPopupHelper.PopupDisplayMode.FrameOnly,
                                              returnKey:=TabPopupReturnKey)
    End Sub

    ''' <summary>
    ''' Project to send to AddAttachment: the picked category's own project; for a category
    ''' used by every project (PROJECT_ID empty) or "All", the project of the contact's
    ''' latest unit. "" if neither is known.
    ''' </summary>
    Private Function GetAttachmentProjectId(ContactId As String, CategoryId As String) As String
        Try
            If CategoryId <> "" Then
                Dim catDT As DataTable = GetDataTable(EBDB,
                    "SELECT PROJECT_ID FROM UNITSHUB_DOC_CATEGORIES WHERE CATEGORY_ID = '" & CategoryId.Replace("'", "''") & "'")
                If catDT IsNot Nothing AndAlso catDT.Rows.Count > 0 Then
                    Dim catProject As String = Convert.ToString(catDT.Rows(0)("PROJECT_ID")).Trim()
                    If catProject <> "" Then Return catProject
                End If
            End If

            Dim SQL As String = ""
            SQL = SQL + vbCrLf + " SELECT PROJECT_ID FROM ( "
            SQL = SQL + vbCrLf + "     SELECT n.PROJECT_ID FROM UNITSHUB_CUSTOMERPROPERTIES cp "
            SQL = SQL + vbCrLf + "     JOIN   UNITSHUB_NODES n ON n.NODE_ID = cp.NODE_ID "
            SQL = SQL + vbCrLf + "     WHERE  cp.CONTACT_ID = '" & ContactId.Replace("'", "''") & "' "
            SQL = SQL + vbCrLf + "     ORDER BY cp.CREATED_AT DESC "
            SQL = SQL + vbCrLf + " ) WHERE ROWNUM = 1 "
            Dim prjDT As DataTable = GetDataTable(EBDB, SQL)
            If prjDT IsNot Nothing AndAlso prjDT.Rows.Count > 0 Then
                Return Convert.ToString(prjDT.Rows(0)("PROJECT_ID")).Trim()
            End If
        Catch ex As Exception
            ' No project found - AddAttachment gets ProjectId empty
        End Try
        Return ""
    End Function

    ''' <summary>Runs when the AddAttachment popup closes: stays on the Attachments tab (list rebuilt in PreRender).</summary>
    Protected Sub btnAddAttachment_Click(sender As Object, e As ImageClickEventArgs)
        ShowTab(AttachmentsTabIndex)
    End Sub

    ''' <summary>Runs when the AddComment popup closes: stays on the Comments tab (list rebuilt in PreRender).</summary>
    Protected Sub btnAddComment_Click(sender As Object, e As ImageClickEventArgs)
        ShowTab(CommentsTabIndex)
    End Sub

    ' ---------------------------- Attachments ----------------------------

    ''' <summary>How attachments are shown: "list" (default) or "icons". Kept across postbacks.</summary>
    Private Property AttachView As String
        Get
            Dim v As String = Convert.ToString(ViewState("AttachView"))
            Return If(v = "", "list", v)
        End Get
        Set(value As String)
            ViewState("AttachView") = value
        End Set
    End Property

    Protected Sub lnkViewList_Click(sender As Object, e As EventArgs)
        SetAttachView("list")
    End Sub

    Protected Sub lnkViewIcons_Click(sender As Object, e As EventArgs)
        SetAttachView("icons")
    End Sub

    Private Sub SetAttachView(View As String)
        AttachView = View
        lnkViewList.CssClass = "view-btn" & If(View = "list", " active", "")
        lnkViewIcons.CssClass = "view-btn" & If(View = "icons", " active", "")
        ShowTab(AttachmentsTabIndex)
    End Sub

    ' Files of the current bind, used by the nested category binding below
    Private _attachFiles As DataTable

    ''' <summary>
    ''' Attachments tab: files of the documents about this contact, grouped by category.
    ''' List view = grid (Seq | Date | Time | Added By | Document), Icons view = tiles; each
    ''' file opens in the DisplayInvoice viewer.
    ''' </summary>
    Private Sub BindAttachments(ContactId As String)
        litAttachments.Text = ""
        _attachFiles = Nothing

        If ContactId = "" Then
            litAttachments.Text = "<span class=""tab-note"">Save the contact first to see or add its attachments.</span>"
            BindAttachCategories(Nothing)
            Exit Sub
        End If

        Try
            Dim SQL As String = ""
            SQL = SQL + vbCrLf + " SELECT c.CATEGORY_ID, c.TITLE AS CATEGORY, NVL(c.SORT_ORDER, 0) AS CAT_SORT, "
            SQL = SQL + vbCrLf + "        d.DOCUMENT_ID, d.TITLE, d.NODE_ID, d.CONTACT_ID, "
            SQL = SQL + vbCrLf + "        a.ATTACHMENT_ID, a.FILE_NAME, a.CREATED_AT AS FILE_CREATED_AT, "
            SQL = SQL + vbCrLf + "        NVL(u.FULL_NAME, a.CREATED_BY) AS ADDED_BY "
            SQL = SQL + vbCrLf + " FROM   UNITSHUB_DOCUMENTS d "
            SQL = SQL + vbCrLf + " JOIN   UNITSHUB_DOC_CATEGORIES c ON c.CATEGORY_ID = d.CATEGORY_ID "
            SQL = SQL + vbCrLf + " JOIN   UNITSHUB_ATTACHMENTS a "
            SQL = SQL + vbCrLf + "        ON a.DOCUMENT_ID = d.DOCUMENT_ID AND a.IS_ACTIVE = 'Y' "
            SQL = SQL + vbCrLf + " LEFT JOIN UNITSHUB_USERS u ON u.USER_ID = a.CREATED_BY "
            SQL = SQL + vbCrLf + " WHERE  d.IS_ACTIVE = 'Y' AND " & ContactFilter("d", ContactId)
            If ddlDocCategory.SelectedValue <> "" Then
                SQL = SQL + vbCrLf + "   AND  d.CATEGORY_ID = '" & ddlDocCategory.SelectedValue.Replace("'", "''") & "' "
            End If
            SQL = SQL + vbCrLf + " ORDER BY NVL(c.SORT_ORDER, 0), c.TITLE, a.CREATED_AT, a.ATTACHMENT_ID "

            Dim DT As DataTable = GetDataTable(EBDB, SQL)
            If DT Is Nothing OrElse DT.Rows.Count = 0 Then
                litAttachments.Text = If(ddlDocCategory.SelectedValue = "",
                    "<span class=""tab-note"">No files for this contact yet.</span>",
                    "<span class=""tab-note"">No " & Server.HtmlEncode(ddlDocCategory.SelectedItem.Text) & " for this contact.</span>")
                BindAttachCategories(Nothing)
                Exit Sub
            End If

            DT.Columns.Add("SEQ_NO", GetType(Integer))
            DT.Columns.Add("DATE_TEXT", GetType(String))
            DT.Columns.Add("TIME_TEXT", GetType(String))
            Dim seq As Integer = 0
            Dim lastCategory As String = Nothing
            For Each r As DataRow In DT.Rows
                Dim cat As String = Convert.ToString(r("CATEGORY_ID"))
                If cat <> lastCategory Then seq = 0 : lastCategory = cat
                seq += 1
                r("SEQ_NO") = seq
                Dim d As String = "", t As String = ""
                SplitUnixTime(Convert.ToString(r("FILE_CREATED_AT")), d, t)
                r("DATE_TEXT") = d
                r("TIME_TEXT") = t
            Next

            _attachFiles = DT
            BindAttachCategories(DT)
        Catch ex As Exception
            litAttachments.Text = "<span class=""tab-note"">Attachments can't be shown: " & Server.HtmlEncode(ex.Message) & "</span>"
            BindAttachCategories(Nothing)
        End Try
    End Sub

    ''' <summary>Binds one block per category (in the order the files came back).</summary>
    Private Sub BindAttachCategories(Files As DataTable)
        Dim cats As New DataTable()
        cats.Columns.Add("CATEGORY_ID", GetType(String))
        cats.Columns.Add("CATEGORY", GetType(String))
        If Files IsNot Nothing Then
            Dim seen As New List(Of String)
            For Each r As DataRow In Files.Rows
                Dim id As String = Convert.ToString(r("CATEGORY_ID"))
                If Not seen.Contains(id) Then
                    seen.Add(id)
                    cats.Rows.Add(id, Convert.ToString(r("CATEGORY")))
                End If
            Next
        End If
        rptAttachCategories.DataSource = cats
        rptAttachCategories.DataBind()
    End Sub

    ''' <summary>Fills one category block: its heading, and its files as a grid or as tiles.</summary>
    Protected Sub rptAttachCategories_ItemDataBound(sender As Object, e As RepeaterItemEventArgs)
        If e.Item.ItemType <> ListItemType.Item AndAlso e.Item.ItemType <> ListItemType.AlternatingItem Then Exit Sub
        Dim cat As DataRowView = CType(e.Item.DataItem, DataRowView)
        Dim categoryId As String = Convert.ToString(cat("CATEGORY_ID"))

        CType(e.Item.FindControl("litCategory"), Literal).Text = Server.HtmlEncode(Convert.ToString(cat("CATEGORY")))

        Dim files As DataTable = _attachFiles.Clone()
        For Each r As DataRow In _attachFiles.Rows
            If Convert.ToString(r("CATEGORY_ID")) = categoryId Then files.ImportRow(r)
        Next

        Dim showIcons As Boolean = (AttachView = "icons")
        Dim pnlList As Panel = CType(e.Item.FindControl("pnlList"), Panel)
        Dim pnlIcons As Panel = CType(e.Item.FindControl("pnlIcons"), Panel)
        pnlList.Visible = Not showIcons
        pnlIcons.Visible = showIcons

        If showIcons Then
            Dim rptTiles As Repeater = CType(e.Item.FindControl("rptTiles"), Repeater)
            rptTiles.DataSource = files
            rptTiles.DataBind()
        Else
            Dim gv As GridView = CType(e.Item.FindControl("gvAttachFiles"), GridView)
            gv.DataSource = files
            gv.DataBind()
        End If
    End Sub

    ''' <summary>List view: the Document cell is a link that opens the file.</summary>
    Protected Sub gvAttachFiles_RowDataBound(sender As Object, e As GridViewRowEventArgs)
        If e.Row.RowType <> DataControlRowType.DataRow Then Exit Sub
        Dim r As DataRowView = CType(e.Row.DataItem, DataRowView)
        Dim lnkFile As LinkButton = CType(e.Row.FindControl("lnkFile"), LinkButton)
        Dim fileName As String = Convert.ToString(r("FILE_NAME"))

        lnkFile.Text = Server.HtmlEncode(fileName)
        lnkFile.ToolTip = "Open " & fileName
        RegisterFilePopup(lnkFile, Convert.ToString(r("ATTACHMENT_ID")))
    End Sub

    ''' <summary>Icons view: the whole tile is a link that opens the file.</summary>
    Protected Sub rptTiles_ItemDataBound(sender As Object, e As RepeaterItemEventArgs)
        If e.Item.ItemType <> ListItemType.Item AndAlso e.Item.ItemType <> ListItemType.AlternatingItem Then Exit Sub
        Dim r As DataRowView = CType(e.Item.DataItem, DataRowView)
        Dim lnkTile As LinkButton = CType(e.Item.FindControl("lnkTile"), LinkButton)
        Dim litTile As Literal = CType(e.Item.FindControl("litTile"), Literal)

        Dim fileName As String = Convert.ToString(r("FILE_NAME")).Trim()
        Dim ext As String = Path.GetExtension(fileName).TrimStart("."c).ToLowerInvariant()
        Dim iconClass As String
        Select Case ext
            Case "pdf" : iconClass = "pdf"
            Case "jpg", "jpeg", "png", "gif", "bmp", "tif", "tiff" : iconClass = "img"
            Case "doc", "docx" : iconClass = "doc"
            Case "xls", "xlsx" : iconClass = "xls"
            Case Else : iconClass = ""
        End Select

        litTile.Text = "<span class=""file-icon " & iconClass & """>" & Server.HtmlEncode(If(ext = "", "-", ext.ToUpperInvariant())) & "</span>" &
                       "<span class=""name"">" & Server.HtmlEncode(fileName) & "</span>" &
                       "<span class=""doc"">" & Server.HtmlEncode(Convert.ToString(r("ADDED_BY")) & " · " & Convert.ToString(r("DATE_TEXT"))) & "</span>"
        lnkTile.ToolTip = "Open " & fileName
        RegisterFilePopup(lnkTile, Convert.ToString(r("ATTACHMENT_ID")))
    End Sub

    ''' <summary>Opens a stored file in the DisplayInvoice viewer, looked up by its ATTACHMENT_ID.</summary>
    Private Sub RegisterFilePopup(Link As LinkButton, AttachmentId As String)
        VendorPopupHelper.RegisterVendorPopup(Me,
                                              Link,
                                              "DisplayInvoice.aspx?AttachmentId=" & Server.UrlEncode(AttachmentId),
                                              1000, 0,
                                              PopupPlacement.Center,
                                              "",
                                              VendorPopupHelper.PopupDisplayMode.FrameOnly,
                                              returnKey:=TabPopupReturnKey)
    End Sub

    ' ----------------------------- Comments ------------------------------

    ''' <summary>Files of the comments being shown, by COMMENT_ID; filled by BindComments.</summary>
    Private _commentFiles As Dictionary(Of String, List(Of DataRow))

    ''' <summary>
    ''' Comments tab: comments about this contact, newest first. Each comment is a box of
    ''' three rows: who/when, the text, and its files as links separated by commas.
    ''' </summary>
    Private Sub BindComments(ContactId As String)
        lblCommentsNote.Visible = False
        lblNoComments.Visible = False
        _commentFiles = New Dictionary(Of String, List(Of DataRow))

        If ContactId = "" Then
            rptComments.DataSource = Nothing
            rptComments.DataBind()
            lblCommentsNote.Text = "Save the contact first to see or add its comments."
            lblCommentsNote.Visible = True
            Exit Sub
        End If

        Try
            Dim SQL As String = ""
            SQL = SQL + vbCrLf + " SELECT c.COMMENT_ID, c.NODE_ID, c.CONTACT_ID, c.COMMENT_TEXT, c.CREATED_AT, c.CREATED_BY, "
            SQL = SQL + vbCrLf + "        NVL(u.FULL_NAME, c.CREATED_BY) AS CREATED_BY_NAME "
            SQL = SQL + vbCrLf + " FROM   UNITSHUB_COMMENTS c "
            SQL = SQL + vbCrLf + " LEFT JOIN UNITSHUB_USERS u ON u.USER_ID = c.CREATED_BY "
            SQL = SQL + vbCrLf + " WHERE  c.IS_ACTIVE = 'Y' AND " & ContactFilter("c", ContactId)
            SQL = SQL + vbCrLf + " ORDER BY c.CREATED_AT DESC, c.COMMENT_ID DESC "

            Dim DT As DataTable = GetDataTable(EBDB, SQL)
            If DT Is Nothing Then DT = New DataTable()

            ' Files of all these comments in one query, grouped by comment
            If DT.Rows.Count > 0 Then
                Dim filesDT As DataTable = GetDataTable(EBDB,
                    "SELECT a.COMMENT_ID, a.ATTACHMENT_ID, a.FILE_NAME FROM UNITSHUB_ATTACHMENTS a " &
                    "JOIN UNITSHUB_COMMENTS c ON c.COMMENT_ID = a.COMMENT_ID " &
                    "WHERE a.IS_ACTIVE = 'Y' AND c.IS_ACTIVE = 'Y' AND " & ContactFilter("c", ContactId) &
                    " ORDER BY a.COMMENT_ID, a.SORT_ORDER, a.ATTACHMENT_ID")
                If filesDT IsNot Nothing Then
                    For Each f As DataRow In filesDT.Rows
                        Dim key As String = Convert.ToString(f("COMMENT_ID"))
                        If Not _commentFiles.ContainsKey(key) Then _commentFiles(key) = New List(Of DataRow)
                        _commentFiles(key).Add(f)
                    Next
                End If
            End If

            rptComments.DataSource = DT
            rptComments.DataBind()
            lblNoComments.Visible = (DT.Rows.Count = 0)
        Catch ex As Exception
            rptComments.DataSource = Nothing
            rptComments.DataBind()
            lblCommentsNote.Text = Server.HtmlEncode("Comments can't be shown: " & ex.Message)
            lblCommentsNote.Visible = True
        End Try
    End Sub

    ''' <summary>One comment box: header row, text row, and its files in the third row.</summary>
    Protected Sub rptComments_ItemDataBound(sender As Object, e As RepeaterItemEventArgs)
        If e.Item.ItemType <> ListItemType.Item AndAlso e.Item.ItemType <> ListItemType.AlternatingItem Then Exit Sub
        Dim r As DataRowView = CType(e.Item.DataItem, DataRowView)

        Dim litHead As Literal = CType(e.Item.FindControl("litCommentHead"), Literal)
        Dim litText As Literal = CType(e.Item.FindControl("litCommentText"), Literal)
        Dim rptFiles As Repeater = CType(e.Item.FindControl("rptCommentFiles"), Repeater)
        Dim lblNoFiles As Label = CType(e.Item.FindControl("lblNoFiles"), Label)

        ' Row 1: Comment by: <name>, ID(<user id>), On: <yyyy MMM,dd HH:mm>
        Dim userName As String = Convert.ToString(r("CREATED_BY_NAME")).Trim()
        Dim userId As String = Convert.ToString(r("CREATED_BY")).Trim()
        litHead.Text = "<span class=""lbl"">Comment by:</span> " & Server.HtmlEncode(userName) &
                       ", ID(" & Server.HtmlEncode(userId) & ")" &
                       ", <span class=""lbl"">On:</span> " & Server.HtmlEncode(FormatCommentTime(Convert.ToString(r("CREATED_AT"))))

        ' Row 2: the comment text (line breaks are kept by the CSS)
        litText.Text = Server.HtmlEncode(Convert.ToString(r("COMMENT_TEXT")))

        ' Row 3: its files, or "None"
        Dim files As List(Of DataRow) = Nothing
        If _commentFiles IsNot Nothing Then _commentFiles.TryGetValue(Convert.ToString(r("COMMENT_ID")), files)
        If files IsNot Nothing AndAlso files.Count > 0 Then
            rptFiles.DataSource = files
            rptFiles.DataBind()
            lblNoFiles.Visible = False
        Else
            rptFiles.DataSource = Nothing
            rptFiles.DataBind()
            lblNoFiles.Visible = True
        End If
    End Sub

    ''' <summary>One attachment of a comment: a link that opens the file in the DisplayInvoice viewer.</summary>
    Protected Sub rptCommentFiles_ItemDataBound(sender As Object, e As RepeaterItemEventArgs)
        If e.Item.ItemType <> ListItemType.Item AndAlso e.Item.ItemType <> ListItemType.AlternatingItem Then Exit Sub
        Dim f As DataRow = CType(e.Item.DataItem, DataRow)
        Dim lnk As LinkButton = CType(e.Item.FindControl("lnkCommentFile"), LinkButton)
        Dim fileName As String = Convert.ToString(f("FILE_NAME")).Trim()

        lnk.Text = Server.HtmlEncode(fileName)
        lnk.ToolTip = "Open " & fileName
        RegisterFilePopup(lnk, Convert.ToString(f("ATTACHMENT_ID")))
    End Sub

    ' ------------------------------ Time ---------------------------------

    ''' <summary>Unix seconds (UTC) as Bahrain date (yyyy-MM-dd) and time (HH:mm); both "" if not a number.</summary>
    Private Sub SplitUnixTime(Value As String, ByRef DateText As String, ByRef TimeText As String)
        DateText = ""
        TimeText = ""
        Dim seconds As Long
        If Not Long.TryParse(If(Value, "").Trim(), seconds) Then Exit Sub
        Dim bahrain As DateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(seconds).ToOffset(TimeSpan.FromHours(3))
        DateText = bahrain.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
        TimeText = bahrain.ToString("HH:mm", CultureInfo.InvariantCulture)
    End Sub

    ''' <summary>Comment time (Unix seconds, UTC) in Bahrain time as "yyyy MMM,dd HH:mm"; as stored if not a number.</summary>
    Private Function FormatCommentTime(Value As String) As String
        Dim seconds As Long
        If Not Long.TryParse(If(Value, "").Trim(), seconds) Then Return If(Value, "")
        Dim bahrain As DateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(seconds).ToOffset(TimeSpan.FromHours(3))
        Return bahrain.ToString("yyyy MMM,dd HH:mm", CultureInfo.InvariantCulture)
    End Function
End Class
