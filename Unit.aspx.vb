Imports System.Data
Imports System.Collections.Generic
Imports System.DateTime
Imports System.Drawing
Imports System.Globalization
Imports System.IO
Imports CrystalDecisions.CrystalReports.Engine

Partial Class Unit
    Inherits System.Web.UI.Page
    Private encryNdecry As New EncryDecry

    Private Sub AutoTransfer_Load(sender As Object, e As EventArgs) Handles Me.Load
        If Not Page.IsPostBack Then
            lblPID.Text = Request("NodeID")
            lblPRJID.Text = Request("ProjectId")
            lblSTATEID.Text = Request("STATEID")
            lblActionID.Text = Request("ActionId")

            LoadProject()
            LoadAttributes()

            ' Tabs under UNIT DETAILS - all three are filled once, switching tabs only
            ' changes which one is shown
            Dim NodeId As String = Convert.ToString(lblPID.Text).Trim()
            Dim ContactId As String = GetUnitContactId(NodeId)
            UnitContactId = ContactId
            LoadDocCategories(NodeId)
            BindAttachments(NodeId, ContactId)
            BindComments(NodeId, ContactId)
            BindHistory()
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
    ''' Fills "Display Attachments" with "All" plus the active categories of
    ''' UNITSHUB_DOC_CATEGORIES that apply to this unit's project (its own, and the ones
    ''' with PROJECT_ID empty = every project), in SORT_ORDER.
    ''' </summary>
    Private Sub LoadDocCategories(NodeId As String)
        ddlDocCategory.Items.Clear()
        ddlDocCategory.Items.Add(New ListItem("All", ""))

        Dim projectId As String = ""
        If NodeId <> "" Then
            projectId = Convert.ToString(DB.RetreiveScalarSTRING(EBDB,
                "SELECT PROJECT_ID FROM UNITSHUB_NODES WHERE NODE_ID = '" & NodeId.Replace("'", "''") & "'")).Trim()
        End If
        If projectId = "" Then projectId = Convert.ToString(lblPRJID.Text).Trim()

        Try
            Dim DT As DataTable = GetDataTable(EBDB,
                "SELECT CATEGORY_ID, TITLE FROM UNITSHUB_DOC_CATEGORIES " &
                "WHERE IS_ACTIVE = 'Y' AND (PROJECT_ID IS NULL OR PROJECT_ID = '" & projectId.Replace("'", "''") & "') " &
                "ORDER BY SORT_ORDER, TITLE")
            If DT IsNot Nothing Then
                For Each r As DataRow In DT.Rows
                    ddlDocCategory.Items.Add(New ListItem(Convert.ToString(r("TITLE")), Convert.ToString(r("CATEGORY_ID"))))
                Next
            End If
        Catch ex As Exception
            ' Categories table not there yet - "All" only; the tab shows why below
        End Try
    End Sub

    ''' <summary>Shows only the chosen category's documents ("All" = every category).</summary>
    Protected Sub ddlDocCategory_SelectedIndexChanged(sender As Object, e As EventArgs)
        BindAttachments(Convert.ToString(lblPID.Text).Trim(), UnitContactId)
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
        Dim projectId As String = Convert.ToString(lblPRJID.Text).Trim()
        If projectId = "" Then projectId = Convert.ToString(lblProjectId.Text).Trim()

        Dim Url As String = "AddAttachment.aspx?NodeID=" & Server.UrlEncode(Convert.ToString(lblPID.Text).Trim()) &
                            "&ProjectId=" & Server.UrlEncode(projectId) &
                            "&ContactId=" & Server.UrlEncode(UnitContactId) &
                            "&CategoryId=" & Server.UrlEncode(ddlDocCategory.SelectedValue)

        VendorPopupHelper.RegisterVendorPopup(Me,
                                              btnAddAttachment,
                                              Url,
                                              1000, 0,
                                              PopupPlacement.Center,
                                              "",
                                              VendorPopupHelper.PopupDisplayMode.FrameOnly,
                                              returnKey:=OpenActionPopupReturnKey)
    End Sub

    ''' <summary>
    ''' Runs when the AddAttachment popup closes: reloads the Attachments tab so anything
    ''' added there shows straight away, and keeps that tab selected.
    ''' </summary>
    Protected Sub btnAddAttachment_Click(sender As Object, e As ImageClickEventArgs)
        BindAttachments(Convert.ToString(lblPID.Text).Trim(), UnitContactId)
        mvUnitTabs.ActiveViewIndex = 0
    End Sub

    ''' <summary>
    ''' Attachments tab: documents about this unit or its current customer, grouped by
    ''' category (UNITSHUB_DOC_CATEGORIES), each with its files (UNITSHUB_ATTACHMENTS whose
    ''' DOCUMENT_ID is set). Comment files are not listed here - they're in Comments.
    ''' </summary>
    Private Sub BindAttachments(NodeId As String, ContactId As String)
        If NodeId = "" Then
            litAttachments.Text = "<span class=""tab-note"">No unit was given.</span>"
            Exit Sub
        End If

        Try
            Dim SQL As String = ""
            SQL = SQL + vbCrLf + " SELECT c.TITLE AS CATEGORY, d.DOCUMENT_ID, d.TITLE, d.DOC_DATE, d.EXPIRY_DATE, "
            SQL = SQL + vbCrLf + "        d.NODE_ID, d.CONTACT_ID, a.FILE_NAME "
            SQL = SQL + vbCrLf + " FROM   UNITSHUB_DOCUMENTS d "
            SQL = SQL + vbCrLf + " JOIN   UNITSHUB_DOC_CATEGORIES c ON c.CATEGORY_ID = d.CATEGORY_ID "
            SQL = SQL + vbCrLf + " LEFT JOIN UNITSHUB_ATTACHMENTS a "
            SQL = SQL + vbCrLf + "        ON a.DOCUMENT_ID = d.DOCUMENT_ID AND a.IS_ACTIVE = 'Y' "
            SQL = SQL + vbCrLf + " WHERE  d.IS_ACTIVE = 'Y' AND " & UnitOrCustomerFilter("d", NodeId, ContactId)
            ' "Display Attachments" filter ("All" = no filter)
            If ddlDocCategory.SelectedValue <> "" Then
                SQL = SQL + vbCrLf + "   AND  d.CATEGORY_ID = '" & ddlDocCategory.SelectedValue.Replace("'", "''") & "' "
            End If
            SQL = SQL + vbCrLf + " ORDER BY c.SORT_ORDER, c.TITLE, d.TITLE, d.DOCUMENT_ID, a.SORT_ORDER, a.ATTACHMENT_ID "

            Dim DT As DataTable = GetDataTable(EBDB, SQL)
            If DT Is Nothing OrElse DT.Rows.Count = 0 Then
                litAttachments.Text = If(ddlDocCategory.SelectedValue = "",
                    "<span class=""tab-note"">No documents for this unit or its customer yet.</span>",
                    "<span class=""tab-note"">No " & Server.HtmlEncode(ddlDocCategory.SelectedItem.Text) & " for this unit or its customer.</span>")
                Exit Sub
            End If

            ' One table per category, one row per document, its files as chips
            Dim html As New System.Text.StringBuilder()
            Dim currentCategory As String = Nothing
            Dim currentDoc As String = Nothing
            Dim files As New List(Of String)
            Dim docRow As DataRow = Nothing

            Dim flushDoc = Sub()
                               If docRow Is Nothing Then Return
                               html.Append("<tr><td>" & Server.HtmlEncode(Convert.ToString(docRow("TITLE"))) & "</td>")
                               html.Append("<td>" & AboutHtml(docRow("NODE_ID"), docRow("CONTACT_ID")) & "</td>")
                               html.Append("<td>" & FormatDateValue(docRow("DOC_DATE")) & "</td>")
                               html.Append("<td>" & FormatDateValue(docRow("EXPIRY_DATE")) & "</td>")
                               html.Append("<td>" & If(files.Count = 0, "<span class=""tab-note"" style=""padding:0"">No files</span>", String.Join("", files)) & "</td></tr>")
                               files.Clear()
                           End Sub

            For Each r As DataRow In DT.Rows
                Dim category As String = Convert.ToString(r("CATEGORY"))
                Dim docId As String = Convert.ToString(r("DOCUMENT_ID"))

                If docId <> currentDoc Then
                    flushDoc()
                    docRow = r
                    currentDoc = docId
                End If

                If category <> currentCategory Then
                    If currentCategory IsNot Nothing Then html.Append("</table></div>")
                    html.Append("<div class=""doc-category"">" & Server.HtmlEncode(category) & "</div>")
                    html.Append("<div class=""grid-wrap""><table class=""pay-grid history-grid""><tr><th>Document</th><th>About</th><th>Date</th><th>Expiry</th><th>Files</th></tr>")
                    currentCategory = category
                End If

                Dim fileName As String = Convert.ToString(r("FILE_NAME")).Trim()
                If fileName <> "" Then files.Add("<span class=""file-chip"">" & Server.HtmlEncode(fileName) & "</span>")
            Next
            flushDoc()
            html.Append("</table></div>")

            litAttachments.Text = html.ToString()
        Catch ex As Exception
            litAttachments.Text = "<span class=""tab-note"">Attachments can't be shown: " & Server.HtmlEncode(ex.Message) & "</span>"
        End Try
    End Sub

    ''' <summary>
    ''' Comments tab: comments about this unit or its current customer, newest first, with
    ''' their own files (UNITSHUB_ATTACHMENTS whose COMMENT_ID is set) and who wrote them.
    ''' </summary>
    Private Sub BindComments(NodeId As String, ContactId As String)
        If NodeId = "" Then
            gvComments.DataSource = Nothing
            gvComments.DataBind()
            Exit Sub
        End If

        Try
            Dim SQL As String = ""
            SQL = SQL + vbCrLf + " SELECT c.COMMENT_ID, c.NODE_ID, c.CONTACT_ID, c.COMMENT_TEXT, c.CREATED_AT, "
            SQL = SQL + vbCrLf + "        NVL(u.FULL_NAME, c.CREATED_BY) AS CREATED_BY_NAME "
            SQL = SQL + vbCrLf + " FROM   UNITSHUB_COMMENTS c "
            SQL = SQL + vbCrLf + " LEFT JOIN UNITSHUB_USERS u ON u.USER_ID = c.CREATED_BY "
            SQL = SQL + vbCrLf + " WHERE  c.IS_ACTIVE = 'Y' AND " & UnitOrCustomerFilter("c", NodeId, ContactId)
            SQL = SQL + vbCrLf + " ORDER BY c.CREATED_AT DESC, c.COMMENT_ID DESC "

            Dim DT As DataTable = GetDataTable(EBDB, SQL)
            If DT Is Nothing Then DT = New DataTable()
            DT.Columns.Add("WHEN_TEXT", GetType(String))
            DT.Columns.Add("ABOUT_HTML", GetType(String))
            DT.Columns.Add("FILES_HTML", GetType(String))

            ' Files of all these comments in one query
            Dim filesByComment As New Dictionary(Of String, List(Of String))
            If DT.Rows.Count > 0 Then
                Dim filesDT As DataTable = GetDataTable(EBDB,
                    "SELECT a.COMMENT_ID, a.FILE_NAME FROM UNITSHUB_ATTACHMENTS a " &
                    "JOIN UNITSHUB_COMMENTS c ON c.COMMENT_ID = a.COMMENT_ID " &
                    "WHERE a.IS_ACTIVE = 'Y' AND c.IS_ACTIVE = 'Y' AND " & UnitOrCustomerFilter("c", NodeId, ContactId) &
                    " ORDER BY a.COMMENT_ID, a.SORT_ORDER, a.ATTACHMENT_ID")
                If filesDT IsNot Nothing Then
                    For Each f As DataRow In filesDT.Rows
                        Dim key As String = Convert.ToString(f("COMMENT_ID"))
                        If Not filesByComment.ContainsKey(key) Then filesByComment(key) = New List(Of String)
                        filesByComment(key).Add("<span class=""file-chip"">" & Server.HtmlEncode(Convert.ToString(f("FILE_NAME"))) & "</span>")
                    Next
                End If
            End If

            For Each r As DataRow In DT.Rows
                r("WHEN_TEXT") = FormatUnixTime(Convert.ToString(r("CREATED_AT")))
                r("ABOUT_HTML") = AboutHtml(r("NODE_ID"), r("CONTACT_ID"))
                Dim key As String = Convert.ToString(r("COMMENT_ID"))
                r("FILES_HTML") = If(filesByComment.ContainsKey(key), String.Join("", filesByComment(key)), "")
            Next

            gvComments.DataSource = DT
            gvComments.DataBind()
        Catch ex As Exception
            gvComments.DataSource = Nothing
            gvComments.DataBind()
            lblCommentsNote.Text = Server.HtmlEncode("Comments can't be shown: " & ex.Message)
            lblCommentsNote.Visible = True
        End Try
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