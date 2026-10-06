Imports System.Data
Imports System.Collections.Generic
Imports System.DateTime
Imports System.Drawing
Imports System.Globalization
Imports System.IO
Imports CrystalDecisions.CrystalReports.Engine

Partial Class Unit
    Inherits System.Web.UI.Page

    ''' <summary>Entity this page's documents belong to (UNITSHUB_ENTITIES.ENTITY_NAME); also sent to AddAttachment.</summary>
    Private Const EntityName As String = "Unit"
    Private encryNdecry As New EncryDecry

    Private Sub AutoTransfer_Load(sender As Object, e As EventArgs) Handles Me.Load
        If Not Page.IsPostBack Then
            lblPID.Text = Request("NodeID")
            lblPRJID.Text = Request("ProjectId")

            LoadProject()
            LoadAttributes()

            ' Tabs under UNIT DETAILS - all three are filled once, switching tabs only
            ' changes which one is shown
            Dim NodeId As String = Convert.ToString(lblPID.Text).Trim()
            Dim ContactId As String = GetUnitContactId(NodeId)
            UnitContactId = ContactId
            LoadCustomer(ContactId)
            LoadDocCategories(NodeId)
            BindHistory()
            ' Comments are bound in Page_PreRender (every request), like the attachments,
            ' so each attachment link keeps its viewer popup after postbacks
        End If

    End Sub

    ''' <summary>
    ''' The project the unit belongs to: PROJECT_ID from UNITSHUB_NODES (the ProjectId passed
    ''' in is only used if the unit isn't found), name from UNITSHUB_PROJECTS.
    ''' </summary>
    Private Sub LoadProject()
        Dim NodeId As String = Convert.ToString(lblPID.Text).Trim()
        Dim projectId As String = ""
        If NodeId <> "" Then
            projectId = Convert.ToString(DB.RetreiveScalarSTRING(EBDB,
                "SELECT PROJECT_ID FROM UNITSHUB_NODES WHERE NODE_ID = '" & NodeId.Replace("'", "''") & "'")).Trim()
        End If
        If projectId = "" Then projectId = Convert.ToString(lblPRJID.Text).Trim()

        lblProjectId.Text = Server.HtmlEncode(projectId)
        If projectId <> "" Then
            lblProjectName.Text = Server.HtmlEncode(Convert.ToString(DB.RetreiveScalarSTRING(EBDB,
                "SELECT PROJECT_NAME_EN FROM UNITSHUB_PROJECTS WHERE PROJECT_ID = '" & projectId.Replace("'", "''") & "'")).Trim())
        End If
    End Sub

    ''' <summary>
    ''' Every attribute of the unit's node type (UNITSHUB_ATTRIBUTES, by DISPLAY_ORDER) with
    ''' the unit's value (UNITSHUB_NODE_ATTRIBUTE_VALUE; empty when not set), shown in two
    ''' columns: the first half on the left, the rest on the right.
    ''' Status / SubStatus show their names, and the payment plan its title
    ''' (UNITSHUB_PAYMENTPLAN.NAME), instead of the stored IDs.
    ''' </summary>
    Private Sub LoadAttributes()
        Dim NodeId As String = Convert.ToString(lblPID.Text).Trim()
        Dim DT As DataTable = Nothing

        If NodeId <> "" Then
            Dim SQL As String = ""
            SQL = SQL + vbCrLf + " SELECT A.ATTRIBUTE_NAME, A.DISPLAY_ORDER, A.DATA_TYPE, V.VALUE_TEXT, N.PROJECT_ID, "
            SQL = SQL + vbCrLf + "        PP.NAME AS PLAN_NAME "
            SQL = SQL + vbCrLf + " FROM   UNITSHUB_NODES N "
            SQL = SQL + vbCrLf + " JOIN   UNITSHUB_ATTRIBUTES A "
            SQL = SQL + vbCrLf + "        ON A.PROJECT_ID = N.PROJECT_ID AND A.NODE_TYPE_ID = N.NODE_TYPE_ID "
            SQL = SQL + vbCrLf + " LEFT JOIN UNITSHUB_NODE_ATTRIBUTE_VALUE V "
            SQL = SQL + vbCrLf + "        ON V.NODE_ID = N.NODE_ID AND TO_NUMBER(V.DISPLAY_ORDER) = TO_NUMBER(A.DISPLAY_ORDER) "
            ' Payment plan title for the payment-plan attribute ("PAYMENTPLAN" / "Payment Plan")
            SQL = SQL + vbCrLf + " LEFT JOIN UNITSHUB_PAYMENTPLAN PP "
            SQL = SQL + vbCrLf + "        ON UPPER(REPLACE(REPLACE(A.ATTRIBUTE_NAME, ' ', ''), '_', '')) = 'PAYMENTPLAN' "
            SQL = SQL + vbCrLf + "       AND ( PP.PLAN_ID = TRIM(V.VALUE_TEXT) "
            ' ...or the same number written differently, e.g. "1" = "00001"
            ' (CASE so TO_NUMBER only runs on values that are numbers)
            SQL = SQL + vbCrLf + "             OR CASE WHEN REGEXP_LIKE(TRIM(V.VALUE_TEXT), '^[0-9]+$') THEN TO_NUMBER(TRIM(V.VALUE_TEXT)) END "
            SQL = SQL + vbCrLf + "              = CASE WHEN REGEXP_LIKE(PP.PLAN_ID, '^[0-9]+$') THEN TO_NUMBER(PP.PLAN_ID) END ) "
            SQL = SQL + vbCrLf + " WHERE  N.NODE_ID = '" & NodeId.Replace("'", "''") & "' "
            SQL = SQL + vbCrLf + " ORDER BY TO_NUMBER(A.DISPLAY_ORDER) "
            DT = GetDataTable(EBDB, SQL)
        End If
        If DT Is Nothing Then DT = New DataTable()
        If Not DT.Columns.Contains("DISPLAY_VALUE") Then DT.Columns.Add("DISPLAY_VALUE", GetType(String))

        ' Readable values: Status -> status name, SubStatus -> sub-status subtitle
        Dim statusId As String = ""
        For Each r As DataRow In DT.Rows
            If String.Equals(Convert.ToString(r("ATTRIBUTE_NAME")).Trim(), "Status", StringComparison.OrdinalIgnoreCase) Then
                statusId = Convert.ToString(r("VALUE_TEXT")).Trim()
            End If
        Next
        For Each r As DataRow In DT.Rows
            Dim name As String = Convert.ToString(r("ATTRIBUTE_NAME")).Trim()
            Dim value As String = Convert.ToString(r("VALUE_TEXT")).Trim()
            Dim projectId As String = Convert.ToString(r("PROJECT_ID")).Trim()

            If String.Equals(name, "Status", StringComparison.OrdinalIgnoreCase) AndAlso value <> "" Then
                Dim statusName As String = Convert.ToString(DB.RetreiveScalarSTRING(EBDB,
                    "SELECT STATUS FROM UNITSHUB_PROJECTSTATUS WHERE PROJECT_ID = '" & projectId.Replace("'", "''") & "' " &
                    "AND STATE_ID = '" & value.Replace("'", "''") & "'")).Trim()
                If statusName <> "" Then value = statusName
            ElseIf Convert.ToString(r("PLAN_NAME")).Trim() <> "" Then
                ' Payment plan: its title from UNITSHUB_PAYMENTPLAN instead of the PLAN_ID
                value = Convert.ToString(r("PLAN_NAME")).Trim()
            ElseIf String.Equals(name, "SubStatus", StringComparison.OrdinalIgnoreCase) AndAlso value <> "" Then
                Dim subName As String = Convert.ToString(DB.RetreiveScalarSTRING(EBDB,
                    "SELECT SUBTITLE FROM UNITSHUB_PROJECTSUBSTATUS WHERE PROJECT_ID = '" & projectId.Replace("'", "''") & "' " &
                    "AND STATE_ID = '" & statusId.Replace("'", "''") & "' AND SUBSTATE_ID = '" & value.Replace("'", "''") & "'")).Trim()
                If subName <> "" Then value = subName
            End If

            r("DISPLAY_VALUE") = value
        Next

        ' Split over two columns
        Dim leftCount As Integer = CInt(Math.Ceiling(DT.Rows.Count / 2.0))
        Dim leftDT As DataTable = DT.Clone()
        Dim rightDT As DataTable = DT.Clone()
        For i As Integer = 0 To DT.Rows.Count - 1
            If i < leftCount Then leftDT.ImportRow(DT.Rows(i)) Else rightDT.ImportRow(DT.Rows(i))
        Next

        rptAttributesLeft.DataSource = leftDT
        rptAttributesLeft.DataBind()
        rptAttributesRight.DataSource = rightDT
        rptAttributesRight.DataBind()

        lblNoAttributes.Visible = (DT.Rows.Count = 0)

        ' Title: "Unit <Reference>" when the unit has a Reference attribute
        For Each r As DataRow In DT.Rows
            If String.Equals(Convert.ToString(r("ATTRIBUTE_NAME")).Trim(), "Reference", StringComparison.OrdinalIgnoreCase) AndAlso
               Convert.ToString(r("VALUE_TEXT")).Trim() <> "" Then
                litUnitTitle.Text = Server.HtmlEncode("Unit " & Convert.ToString(r("VALUE_TEXT")).Trim())
            End If
        Next
    End Sub

    ''' <summary>Fills one attribute line: name and value (HTML-encoded, "-" when empty).</summary>
    Protected Sub rptAttributes_ItemDataBound(sender As Object, e As RepeaterItemEventArgs)
        If e.Item.ItemType <> ListItemType.Item AndAlso e.Item.ItemType <> ListItemType.AlternatingItem Then Exit Sub
        Dim r As DataRowView = CType(e.Item.DataItem, DataRowView)

        Dim litName As Literal = CType(e.Item.FindControl("litName"), Literal)
        Dim litValue As Literal = CType(e.Item.FindControl("litValue"), Literal)

        litName.Text = Server.HtmlEncode(Convert.ToString(r("ATTRIBUTE_NAME")).Trim())
        Dim value As String = Convert.ToString(r("DISPLAY_VALUE")).Trim()
        litValue.Text = If(value = "", "<span style=""color:var(--muted)"">-</span>", Server.HtmlEncode(value))
    End Sub

    ' ------------------------------------------------------------------
    ' Tabs: Attachments | Comments | History
    ' ------------------------------------------------------------------

    ''' <summary>Switches the tab, same as ContactMaintenance's Menu3 / MultiView3.</summary>
    Protected Sub menuUnitTabs_MenuItemClick(sender As Object, e As MenuEventArgs)
        Try
            mvUnitTabs.ActiveViewIndex = menuUnitTabs.Items.IndexOf(menuUnitTabs.SelectedItem)
        Catch ex As Exception
        End Try
    End Sub

    ''' <summary>
    ''' The unit's current customer (latest UNITSHUB_CUSTOMERPROPERTIES row), so the tabs can
    ''' also show items about the customer. "" if the unit has none.
    ''' </summary>
    Private Function GetUnitContactId(NodeId As String) As String
        If String.IsNullOrWhiteSpace(NodeId) Then Return ""
        Dim SQL As String = ""
        SQL = SQL + vbCrLf + " SELECT CONTACT_ID FROM ( "
        SQL = SQL + vbCrLf + "     SELECT CONTACT_ID FROM UNITSHUB_CUSTOMERPROPERTIES "
        SQL = SQL + vbCrLf + "     WHERE  NODE_ID = '" & NodeId.Trim().Replace("'", "''") & "' AND CONTACT_ID IS NOT NULL "
        SQL = SQL + vbCrLf + "     ORDER BY CREATED_AT DESC "
        SQL = SQL + vbCrLf + " ) WHERE ROWNUM = 1 "
        Return Convert.ToString(DB.RetreiveScalarSTRING(EBDB, SQL)).Trim()
    End Function

    ''' <summary>
    ''' CUSTOMER box under UNIT DETAILS: the unit's current customer (UNITSHUB_CONTACTS.ID =
    ''' the CONTACT_ID found by GetUnitContactId). Shows Full Name (NAME), National ID/CPR
    ''' (NATIONALID), Mobile 1 (MOBILE) and Mobile 2 (BUSINESSPHONE, the field ContactMaintenance
    ''' saves "Mobile 2" in) - Mobile 2 only when it has a number.
    ''' </summary>
    Private Sub LoadCustomer(ContactId As String)
        phCustomer.Visible = False
        lblNoCustomer.Visible = False

        If ContactId = "" Then
            lblNoCustomer.Visible = True
            Exit Sub
        End If

        Try
            Dim DT As DataTable = GetDataTable(EBDB,
                "SELECT NAME, NATIONALID, MOBILE, BUSINESSPHONE FROM UNITSHUB_CONTACTS " &
                "WHERE ID = '" & ContactId.Replace("'", "''") & "'")
            If DT Is Nothing OrElse DT.Rows.Count = 0 Then
                lblNoCustomer.Text = "The linked customer (" & Server.HtmlEncode(ContactId) & ") wasn't found."
                lblNoCustomer.Visible = True
                Exit Sub
            End If

            Dim r As DataRow = DT.Rows(0)
            lnkCustomerName.Text = Server.HtmlEncode(Convert.ToString(r("NAME")).Trim())
            lblCustomerNationalId.Text = Server.HtmlEncode(Convert.ToString(r("NATIONALID")).Trim())
            lblCustomerMobile1.Text = Server.HtmlEncode(Convert.ToString(r("MOBILE")).Trim())

            Dim mobile2 As String = Convert.ToString(r("BUSINESSPHONE")).Trim()
            lblCustomerMobile2.Text = Server.HtmlEncode(mobile2)
            phCustomerMobile2.Visible = (mobile2 <> "")

            phCustomer.Visible = True
        Catch ex As Exception
            lblNoCustomer.Text = Server.HtmlEncode("Customer can't be shown: " & ex.Message)
            lblNoCustomer.Visible = True
        End Try
    End Sub

    ''' <summary>"WHERE" part for items about this unit, or about its current customer.</summary>
    Private Function UnitOrCustomerFilter(Alias_ As String, NodeId As String, ContactId As String) As String
        Dim f As String = Alias_ & ".NODE_ID = '" & NodeId.Trim().Replace("'", "''") & "'"
        If ContactId <> "" Then f = "(" & f & " OR " & Alias_ & ".CONTACT_ID = '" & ContactId.Replace("'", "''") & "')"
        Return f
    End Function

    ''' <summary>Small tag: Unit / Customer / Unit &amp; customer.</summary>
    Private Function AboutHtml(NodeIdValue As Object, ContactIdValue As Object) As String
        Dim hasUnit As Boolean = Convert.ToString(NodeIdValue).Trim() <> ""
        Dim hasCustomer As Boolean = Convert.ToString(ContactIdValue).Trim() <> ""
        Dim text As String = If(hasUnit AndAlso hasCustomer, "Unit & customer", If(hasUnit, "Unit", "Customer"))
        Return "<span class=""about-tag"">" & Server.HtmlEncode(text) & "</span>"
    End Function

    ''' <summary>The unit's current customer, kept for postbacks (the category filter).</summary>
    Private Property UnitContactId As String
        Get
            Return Convert.ToString(ViewState("UnitContactId"))
        End Get
        Set(value As String)
            ViewState("UnitContactId") = value
        End Set
    End Property

    ''' <summary>
    ''' Fills "Display Attachments" with "All" plus the active categories linked to the
    ''' "Unit" entity (EntityName) in UNITSHUB_DOC2ENTITY, in SORT_ORDER.
    ''' </summary>
    Private Sub LoadDocCategories(NodeId As String)
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
            ' Category tables not there yet - "All" only; the tab shows why below
        End Try
    End Sub

    ''' <summary>Shows only the chosen category's documents ("All" = every category).</summary>
    Protected Sub ddlDocCategory_SelectedIndexChanged(sender As Object, e As EventArgs)
        mvUnitTabs.ActiveViewIndex = 0
    End Sub

    Private Const OpenActionPopupReturnKey As String = "AddAttachmentPopup"

    ''' <summary>
    ''' "+" opens the AddAttachment popup for this unit, its project, its current customer
    ''' and the category picked in "Display Attachments" (pre-selected there). Registered on
    ''' every request, after the dropdown may have changed, so the popup always gets the
    ''' current category.
    ''' </summary>
    Protected Sub Page_PreRender(sender As Object, e As EventArgs) Handles Me.PreRender
        ' Customer name opens the customer in ContactMaintenance (read-only). Registered on
        ' every request so the link keeps its popup after postbacks.
        If UnitContactId <> "" AndAlso phCustomer.Visible Then
            VendorPopupHelper.RegisterVendorPopup(Me,
                                  lnkCustomerName,
                                  "ContactMaintenance.aspx?&ID=" & Server.UrlEncode(UnitContactId) & "&mode=ReadOnly&isDialogue=yes&",
                                  950,
                                  750,
                                  PopupPlacement.Center,
                                  "Select Adj",
                                  VendorPopupHelper.PopupDisplayMode.FrameOnly)
        End If

        ' Attachments: rebuilt on every request (after any filter / view change), so each
        ' file link and its viewer popup always exist
        BindAttachments(Convert.ToString(lblPID.Text).Trim(), UnitContactId)

        ' Comments: also rebuilt on every request, for the same reason (attachment links)
        BindComments(Convert.ToString(lblPID.Text).Trim(), UnitContactId)

        Dim projectId As String = Convert.ToString(lblPRJID.Text).Trim()
        If projectId = "" Then projectId = Convert.ToString(lblProjectId.Text).Trim()

        Dim Url As String = "AddAttachment.aspx?NodeID=" & Server.UrlEncode(Convert.ToString(lblPID.Text).Trim()) &
                            "&ProjectId=" & Server.UrlEncode(projectId) &
                            "&ContactId=" & Server.UrlEncode(UnitContactId) &
                            "&CategoryId=" & Server.UrlEncode(ddlDocCategory.SelectedValue) &
                            "&Entity=" & Server.UrlEncode(EntityName)

        VendorPopupHelper.RegisterVendorPopup(Me,
                                              btnAddAttachment,
                                              Url,
                                              1000, 0,
                                              PopupPlacement.Center,
                                              "",
                                              VendorPopupHelper.PopupDisplayMode.FrameOnly,
                                              returnKey:=OpenActionPopupReturnKey)

        ' "Add Comment" (Comments tab) opens AddComment for this unit, its project and its
        ' current customer - no category, comments don't have one
        Dim CommentUrl As String = "AddComment.aspx?NodeID=" & Server.UrlEncode(Convert.ToString(lblPID.Text).Trim()) &
                                   "&ProjectId=" & Server.UrlEncode(projectId) &
                                   "&ContactId=" & Server.UrlEncode(UnitContactId)

        VendorPopupHelper.RegisterVendorPopup(Me,
                                              btnAddComment,
                                              CommentUrl,
                                              1000, 0,
                                              PopupPlacement.Center,
                                              "",
                                              VendorPopupHelper.PopupDisplayMode.FrameOnly,
                                              returnKey:=OpenActionPopupReturnKey)
    End Sub

    ''' <summary>
    ''' Runs when the AddComment popup closes: reloads the Comments tab so a new comment
    ''' shows straight away, and keeps that tab selected.
    ''' </summary>
    Protected Sub btnAddComment_Click(sender As Object, e As ImageClickEventArgs)
        ' The list itself is rebuilt in Page_PreRender
        mvUnitTabs.ActiveViewIndex = 1
    End Sub

    ''' <summary>
    ''' Runs when the AddAttachment popup closes: reloads the Attachments tab so anything
    ''' added there shows straight away, and keeps that tab selected.
    ''' </summary>
    Protected Sub btnAddAttachment_Click(sender As Object, e As ImageClickEventArgs)
        mvUnitTabs.ActiveViewIndex = 0
    End Sub

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

    ''' <summary>Switches List / Icons, marks the active button and redraws the attachments.</summary>
    Private Sub SetAttachView(View As String)
        AttachView = View
        lnkViewList.CssClass = "view-btn" & If(View = "list", " active", "")
        lnkViewIcons.CssClass = "view-btn" & If(View = "icons", " active", "")
        mvUnitTabs.ActiveViewIndex = 0
    End Sub

    ''' <summary>
    ''' Attachments tab: files of the documents about this unit or its current customer,
    ''' grouped by category (rptAttachCategories). Inside each category, the List view shows
    ''' a grid (Seq | Date | Time | Added By | Document) and the Icons view shows tiles. In
    ''' both, each file is a link that opens it in the DisplayInvoice viewer (see
    ''' RegisterFilePopup). Bound on EVERY request from Page_PreRender, so the links and their
    ''' popups are always there after postbacks.
    ''' </summary>
    Private Sub BindAttachments(NodeId As String, ContactId As String)
        litAttachments.Text = ""
        _attachFiles = Nothing

        If NodeId = "" Then
            litAttachments.Text = "<span class=""tab-note"">No unit was given.</span>"
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
            SQL = SQL + vbCrLf + " WHERE  d.IS_ACTIVE = 'Y' AND " & UnitOrCustomerFilter("d", NodeId, ContactId)
            ' "Display Attachments" filter ("All" = no filter)
            If ddlDocCategory.SelectedValue <> "" Then
                SQL = SQL + vbCrLf + "   AND  d.CATEGORY_ID = '" & ddlDocCategory.SelectedValue.Replace("'", "''") & "' "
            End If
            SQL = SQL + vbCrLf + " ORDER BY NVL(c.SORT_ORDER, 0), c.TITLE, a.CREATED_AT, a.ATTACHMENT_ID "

            Dim DT As DataTable = GetDataTable(EBDB, SQL)
            If DT Is Nothing OrElse DT.Rows.Count = 0 Then
                litAttachments.Text = If(ddlDocCategory.SelectedValue = "",
                    "<span class=""tab-note"">No files for this unit or its customer yet.</span>",
                    "<span class=""tab-note"">No " & Server.HtmlEncode(ddlDocCategory.SelectedItem.Text) & " for this unit or its customer.</span>")
                BindAttachCategories(Nothing)
                Exit Sub
            End If

            ' Per-file display columns
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

    ' Files of the current bind, used by the nested category binding below
    Private _attachFiles As DataTable

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
                       "<span class=""doc"">" & Server.HtmlEncode(Convert.ToString(r("ADDED_BY")) & " · " & Convert.ToString(r("DATE_TEXT"))) & "</span>" &
                       AboutHtml(r("NODE_ID"), r("CONTACT_ID"))
        lnkTile.ToolTip = "Open " & fileName
        RegisterFilePopup(lnkTile, Convert.ToString(r("ATTACHMENT_ID")))
    End Sub

    ''' <summary>
    ''' Opens a stored file in the DisplayInvoice viewer - the same popup used for invoices.
    ''' The viewer looks the file up by its ATTACHMENT_ID.
    ''' </summary>
    Private Sub RegisterFilePopup(Link As LinkButton, AttachmentId As String)
        Dim Url As String = "DisplayInvoice.aspx?AttachmentId=" & Server.UrlEncode(AttachmentId)

        VendorPopupHelper.RegisterVendorPopup(Me,
                                              Link,
                                              Url,
                                              1000, 0,
                                              PopupPlacement.Center,
                                              "",
                                              VendorPopupHelper.PopupDisplayMode.FrameOnly,
                                              returnKey:=OpenActionPopupReturnKey)
    End Sub

    ''' <summary>Files of the comments being shown, by COMMENT_ID; filled by BindComments.</summary>
    Private _commentFiles As Dictionary(Of String, List(Of DataRow))

    ''' <summary>
    ''' Comments tab: comments about this unit or its current customer, newest first. Each
    ''' comment is a box of three rows (see rptComments in the markup):
    '''   1. Comment by: user name, ID(user id), On: yyyy MMM,dd HH:mm (Bahrain time)
    '''   2. The comment text
    '''   3. Its files (UNITSHUB_ATTACHMENTS whose COMMENT_ID is set) as links side by side,
    '''      separated by commas; each opens the file in the DisplayInvoice viewer.
    ''' Bound on EVERY request from Page_PreRender, so the links and their popups are always
    ''' there after postbacks.
    ''' </summary>
    Private Sub BindComments(NodeId As String, ContactId As String)
        lblCommentsNote.Visible = False
        lblNoComments.Visible = False
        _commentFiles = New Dictionary(Of String, List(Of DataRow))

        If NodeId = "" Then
            rptComments.DataSource = Nothing
            rptComments.DataBind()
            lblNoComments.Visible = True
            Exit Sub
        End If

        Try
            Dim SQL As String = ""
            SQL = SQL + vbCrLf + " SELECT c.COMMENT_ID, c.NODE_ID, c.CONTACT_ID, c.COMMENT_TEXT, c.CREATED_AT, c.CREATED_BY, "
            SQL = SQL + vbCrLf + "        NVL(u.FULL_NAME, c.CREATED_BY) AS CREATED_BY_NAME "
            SQL = SQL + vbCrLf + " FROM   UNITSHUB_COMMENTS c "
            SQL = SQL + vbCrLf + " LEFT JOIN UNITSHUB_USERS u ON u.USER_ID = c.CREATED_BY "
            SQL = SQL + vbCrLf + " WHERE  c.IS_ACTIVE = 'Y' AND " & UnitOrCustomerFilter("c", NodeId, ContactId)
            SQL = SQL + vbCrLf + " ORDER BY c.CREATED_AT DESC, c.COMMENT_ID DESC "

            Dim DT As DataTable = GetDataTable(EBDB, SQL)
            If DT Is Nothing Then DT = New DataTable()

            ' Files of all these comments in one query, grouped by comment
            If DT.Rows.Count > 0 Then
                Dim filesDT As DataTable = GetDataTable(EBDB,
                    "SELECT a.COMMENT_ID, a.ATTACHMENT_ID, a.FILE_NAME FROM UNITSHUB_ATTACHMENTS a " &
                    "JOIN UNITSHUB_COMMENTS c ON c.COMMENT_ID = a.COMMENT_ID " &
                    "WHERE a.IS_ACTIVE = 'Y' AND c.IS_ACTIVE = 'Y' AND " & UnitOrCustomerFilter("c", NodeId, ContactId) &
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
        Dim key As String = Convert.ToString(r("COMMENT_ID"))
        Dim files As List(Of DataRow) = Nothing
        If _commentFiles IsNot Nothing Then _commentFiles.TryGetValue(key, files)
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

    ''' <summary>
    ''' Comment time (Unix seconds, UTC) in Bahrain time as "yyyy MMM,dd HH:mm",
    ''' e.g. "2026 Oct,05 14:30"; shown as stored if it isn't a number.
    ''' </summary>
    Private Function FormatCommentTime(Value As String) As String
        Dim seconds As Long
        If Not Long.TryParse(If(Value, "").Trim(), seconds) Then Return If(Value, "")
        Dim bahrain As DateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(seconds).ToOffset(TimeSpan.FromHours(3))
        Return bahrain.ToString("yyyy MMM,dd HH:mm", CultureInfo.InvariantCulture)
    End Function

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

    ''' <summary>A DATE (or date text) as yyyy-MM-dd; "" when empty.</summary>
    Private Function FormatDateValue(Value As Object) As String
        If Value Is Nothing OrElse Value Is DBNull.Value Then Return ""
        If TypeOf Value Is DateTime Then Return CType(Value, DateTime).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
        Return Server.HtmlEncode(Convert.ToString(Value))
    End Function

    ''' <summary>
    ''' Fills gvHistory with this unit's UNITSHUB_UNITSHISTORY rows, newest first, with
    ''' readable names joined in:
    '''   From / To       status name (UNITSHUB_PROJECTSTATUS) + sub-status subtitle
    '''                   (UNITSHUB_PROJECTSUBSTATUS); the IDs when a name isn't found
    '''                   (e.g. old status IDs from before the sub-status change)
    '''   Action          UNITSHUB_ACTIONS.ACTION_TITLE
    '''   Implemented by  UNITSHUB_USERS.FULL_NAME
    '''   Customer        UNITSHUB_CONTACTS.NAME
    '''   Date and time   DATENTIME (Unix seconds) shown in Bahrain time
    ''' </summary>
    Private Sub BindHistory()
        Dim NodeId As String = Convert.ToString(lblPID.Text).Trim()
        If NodeId = "" Then
            gvHistory.DataSource = Nothing
            gvHistory.DataBind()
            Exit Sub
        End If

        Dim safeNode As String = NodeId.Replace("'", "''")
        Dim projectFilter As String = ""
        If Convert.ToString(lblPRJID.Text).Trim() <> "" Then
            projectFilter = " AND H.PROJECT_ID = '" & lblPRJID.Text.Trim().Replace("'", "''") & "' "
        End If

        Dim SQL As String = ""
        SQL = SQL + vbCrLf + " SELECT H.DATENTIME, "
        SQL = SQL + vbCrLf + "        H.FROM_STATUS, H.FROM_SUBSTATUS, H.TO_STATUS, H.TO_SUBSTATUS, "
        SQL = SQL + vbCrLf + "        FS.STATUS AS FROM_STATUS_NAME, "
        SQL = SQL + vbCrLf + "        NVL(FSS.SUBTITLE, CASE WHEN H.FROM_SUBSTATUS IS NULL THEN FS.SUBTITLE END) AS FROM_SUB_NAME, "
        SQL = SQL + vbCrLf + "        TS.STATUS AS TO_STATUS_NAME, "
        SQL = SQL + vbCrLf + "        NVL(TSS.SUBTITLE, CASE WHEN H.TO_SUBSTATUS IS NULL THEN TS.SUBTITLE END) AS TO_SUB_NAME, "
        ' Card colours: the sub-status's own where set, otherwise the status's (as on MainPage)
        SQL = SQL + vbCrLf + "        NVL(FSS.STATUS_BG_COLOR, FS.STATUS_BG_COLOR) AS FROM_BG, "
        SQL = SQL + vbCrLf + "        NVL(FSS.STATUS_FG_COLOR, FS.STATUS_FG_COLOR) AS FROM_FG, "
        SQL = SQL + vbCrLf + "        NVL(FSS.SUBTITLE_BG_COLOR, FS.SUBTITLE_BG_COLOR) AS FROM_SUB_BG, "
        SQL = SQL + vbCrLf + "        NVL(FSS.SUBTITLE_FG_COLOR, FS.SUBTITLE_FG_COLOR) AS FROM_SUB_FG, "
        SQL = SQL + vbCrLf + "        NVL(TSS.STATUS_BG_COLOR, TS.STATUS_BG_COLOR) AS TO_BG, "
        SQL = SQL + vbCrLf + "        NVL(TSS.STATUS_FG_COLOR, TS.STATUS_FG_COLOR) AS TO_FG, "
        SQL = SQL + vbCrLf + "        NVL(TSS.SUBTITLE_BG_COLOR, TS.SUBTITLE_BG_COLOR) AS TO_SUB_BG, "
        SQL = SQL + vbCrLf + "        NVL(TSS.SUBTITLE_FG_COLOR, TS.SUBTITLE_FG_COLOR) AS TO_SUB_FG, "
        SQL = SQL + vbCrLf + "        NVL(A.ACTION_TITLE, H.ACTION_ID) AS ACTION_TITLE, "
        SQL = SQL + vbCrLf + "        NVL(U.FULL_NAME, H.IMPLEMENTEDBY) AS IMPLEMENTED_BY_NAME, "
        SQL = SQL + vbCrLf + "        C.NAME AS CUSTOMER_NAME, "
        SQL = SQL + vbCrLf + "        H.SUMMARY "
        SQL = SQL + vbCrLf + " FROM   UNITSHUB_UNITSHISTORY H "
        SQL = SQL + vbCrLf + " LEFT JOIN UNITSHUB_PROJECTSTATUS FS "
        SQL = SQL + vbCrLf + "        ON FS.PROJECT_ID = H.PROJECT_ID AND FS.STATE_ID = H.FROM_STATUS "
        SQL = SQL + vbCrLf + " LEFT JOIN UNITSHUB_PROJECTSUBSTATUS FSS "
        SQL = SQL + vbCrLf + "        ON FSS.PROJECT_ID = H.PROJECT_ID AND FSS.STATE_ID = H.FROM_STATUS AND FSS.SUBSTATE_ID = H.FROM_SUBSTATUS "
        SQL = SQL + vbCrLf + " LEFT JOIN UNITSHUB_PROJECTSTATUS TS "
        SQL = SQL + vbCrLf + "        ON TS.PROJECT_ID = H.PROJECT_ID AND TS.STATE_ID = H.TO_STATUS "
        SQL = SQL + vbCrLf + " LEFT JOIN UNITSHUB_PROJECTSUBSTATUS TSS "
        SQL = SQL + vbCrLf + "        ON TSS.PROJECT_ID = H.PROJECT_ID AND TSS.STATE_ID = H.TO_STATUS AND TSS.SUBSTATE_ID = H.TO_SUBSTATUS "
        SQL = SQL + vbCrLf + " LEFT JOIN UNITSHUB_ACTIONS A "
        SQL = SQL + vbCrLf + "        ON A.PROJECT_ID = H.PROJECT_ID AND A.ACTION_ID = H.ACTION_ID "
        SQL = SQL + vbCrLf + " LEFT JOIN UNITSHUB_USERS U "
        SQL = SQL + vbCrLf + "        ON U.USER_ID = H.IMPLEMENTEDBY "
        SQL = SQL + vbCrLf + " LEFT JOIN UNITSHUB_CONTACTS C "
        SQL = SQL + vbCrLf + "        ON C.ID = H.CONTACT_ID "
        SQL = SQL + vbCrLf + " WHERE  H.NODE_ID = '" & safeNode & "' " & projectFilter
        SQL = SQL + vbCrLf + " ORDER BY CASE WHEN REGEXP_LIKE(TRIM(H.DATENTIME), '^[0-9]+$') THEN TO_NUMBER(TRIM(H.DATENTIME)) END DESC NULLS LAST "

        Dim DT As DataTable = GetDataTable(EBDB, SQL)
        If DT Is Nothing Then DT = New DataTable()

        DT.Columns.Add("WHEN_TEXT", GetType(String))
        DT.Columns.Add("FROM_HTML", GetType(String))
        DT.Columns.Add("TO_HTML", GetType(String))

        For Each r As DataRow In DT.Rows
            r("WHEN_TEXT") = FormatUnixTime(Convert.ToString(r("DATENTIME")))
            r("FROM_HTML") = StatusHtml(r, "FROM")
            r("TO_HTML") = StatusHtml(r, "TO")
        Next

        gvHistory.DataSource = DT
        gvHistory.DataBind()

    End Sub

    ''' <summary>
    ''' The status as a small card like the ones on MainPage: status name on the card colour,
    ''' and the sub-status (or the status's own subtitle) in a rounded box underneath, with
    ''' that box's colours. Side = "FROM" or "TO". Falls back to the raw IDs and default
    ''' colours when a status / sub-status isn't found (e.g. old IDs in early history rows).
    ''' </summary>
    Private Function StatusHtml(r As DataRow, Side As String) As String
        Dim statusId As String = Convert.ToString(r(Side & "_STATUS")).Trim()
        Dim subId As String = Convert.ToString(r(Side & "_SUBSTATUS")).Trim()
        Dim statusName As String = Convert.ToString(r(Side & "_STATUS_NAME")).Trim()
        Dim subName As String = Convert.ToString(r(Side & "_SUB_NAME")).Trim()

        If statusId = "" AndAlso statusName = "" Then Return "&ndash;"
        If statusName = "" Then statusName = statusId
        If subName = "" AndAlso subId <> "" Then subName = subId

        Dim cardStyle As String = ColorStyle(r(Side & "_BG"), r(Side & "_FG"))
        Dim subStyle As String = ColorStyle(r(Side & "_SUB_BG"), r(Side & "_SUB_FG"))

        Dim html As String = "<span class=""st-card""" & cardStyle & ">" &
                             "<span class=""st-title"">" & Server.HtmlEncode(statusName) & "</span>"
        If subName <> "" Then
            html &= "<span class=""st-sub""" & subStyle & ">" & Server.HtmlEncode(subName) & "</span>"
        End If
        Return html & "</span>"
    End Function

    ''' <summary>
    ''' style="background-color:..;color:.." from two colour values; only values that look
    ''' like colours (#hex or a plain colour name) are used, anything else is left out.
    ''' </summary>
    Private Function ColorStyle(Background As Object, Foreground As Object) As String
        Dim parts As New List(Of String)
        Dim bg As String = SafeColor(Background)
        Dim fg As String = SafeColor(Foreground)
        If bg <> "" Then parts.Add("background-color:" & bg)
        If fg <> "" Then parts.Add("color:" & fg)
        Return If(parts.Count = 0, "", " style=""" & String.Join(";", parts) & """")
    End Function

    Private Function SafeColor(Value As Object) As String
        Dim v As String = Convert.ToString(Value).Trim()
        If v = "" Then Return ""
        If System.Text.RegularExpressions.Regex.IsMatch(v, "^#?[0-9A-Fa-f]{3,8}$") Then
            Return If(v.StartsWith("#"), v, "#" & v)
        End If
        If System.Text.RegularExpressions.Regex.IsMatch(v, "^[A-Za-z]+$") Then Return v
        Return ""
    End Function

    ''' <summary>
    ''' DATENTIME is a Unix timestamp in seconds (UTC). Shown in Bahrain time (UTC+3) on two
    ''' lines - date above (yyyy-MM-dd), time below (HH:mm) - or as stored if it isn't a number.
    ''' </summary>
    ''' 
    Private Function FormatUnixTime(Value As String) As String
        Dim seconds As Long
        If Not Long.TryParse(If(Value, "").Trim(), seconds) Then Return Server.HtmlEncode(If(Value, ""))

        Dim utc As DateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(seconds)
        Dim bahrain As DateTimeOffset = utc.ToOffset(TimeSpan.FromHours(3))
        Return "<span class=""d"">" & bahrain.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) & "</span>" &
               "<span class=""t"">" & bahrain.ToString("HH:mm", CultureInfo.InvariantCulture) & "</span>"
    End Function

    Protected Sub imgClose_Click(sender As Object, e As ImageClickEventArgs) Handles imgClose.Click
        VendorPopupHelper.RegisterPopupSelectionAndClose(Me, False, skipPostBack:=False)
    End Sub

End Class