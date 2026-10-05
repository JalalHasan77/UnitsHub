Imports System.Data
Imports System.Collections.Generic
Imports System.DateTime
Imports System.Drawing
Imports System.Globalization
Imports System.IO
Imports CrystalDecisions.CrystalReports.Engine

''' <summary>
''' Add Attachment popup (opened from the Unit page's "+" button).
'''   Attachment Type  categories from UNITSHUB_DOC_CATEGORIES
'''   File upload      one or several files; each goes into a temporary folder for this
'''                    popup and is listed in gvFiles (Remove takes it out again)
'''   OK               saves them: one document per Attachment Type (UNITSHUB_DOCUMENTS)
'''                    with its files (UNITSHUB_ATTACHMENTS), files moved to ~/Attachments
'''   Cancel / X       deletes the uploaded files, saves nothing
''' </summary>
Partial Class AddAttachment
    Inherits System.Web.UI.Page
    Private encryNdecry As New EncryDecry

    Private Const MaxFileBytes As Integer = 20 * 1024 * 1024
    Private ReadOnly AllowedExtensions As String() =
        {".pdf", ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tif", ".tiff", ".doc", ".docx", ".xls", ".xlsx"}

    ' ------------------------------------------------------------------
    ' Page
    ' ------------------------------------------------------------------

    Private Sub AddAttachment_Load(sender As Object, e As EventArgs) Handles Me.Load
        If Not Page.IsPostBack Then
            lblPID.Text = Convert.ToString(Request("NodeID"))
            lblPRJID.Text = Convert.ToString(Request("ProjectId"))
            lblCID.Text = Convert.ToString(Request("ContactId"))

            LoadAttachmentTypes()

            ' Pre-select the category chosen on the Unit page, if any
            Dim requested As String = Convert.ToString(Request("CategoryId"))
            If requested <> "" AndAlso ddlAttachmentType.Items.FindByValue(requested) IsNot Nothing Then
                ddlAttachmentType.SelectedValue = requested
            End If

            BindFiles()
        End If
    End Sub

    ''' <summary>
    ''' "Attachment Type": active categories for this unit's project (its own and the
    ''' ones for every project), in SORT_ORDER, after "-- Select --".
    ''' </summary>
    Private Sub LoadAttachmentTypes()
        ddlAttachmentType.Items.Clear()
        ddlAttachmentType.Items.Add(New ListItem("-- Select --", ""))

        Dim projectId As String = Convert.ToString(lblPRJID.Text).Trim()
        If projectId = "" AndAlso Convert.ToString(lblPID.Text).Trim() <> "" Then
            projectId = Convert.ToString(DB.RetreiveScalarSTRING(EBDB,
                "SELECT PROJECT_ID FROM UNITSHUB_NODES WHERE NODE_ID = '" & lblPID.Text.Trim().Replace("'", "''") & "'")).Trim()
        End If

        Try
            Dim DT As DataTable = GetDataTable(EBDB,
                "SELECT CATEGORY_ID, TITLE FROM UNITSHUB_DOC_CATEGORIES " &
                "WHERE IS_ACTIVE = 'Y' AND (PROJECT_ID IS NULL OR PROJECT_ID = '" & projectId.Replace("'", "''") & "') " &
                "ORDER BY SORT_ORDER, TITLE")
            If DT IsNot Nothing Then
                For Each r As DataRow In DT.Rows
                    ddlAttachmentType.Items.Add(New ListItem(Convert.ToString(r("TITLE")), Convert.ToString(r("CATEGORY_ID"))))
                Next
            End If
        Catch ex As Exception
            ShowMessage("Attachment types couldn't be loaded: " & ex.Message, False)
        End Try
    End Sub

    Private Sub ShowMessage(Message As String, IsSuccess As Boolean)
        lblMessage.Text = Server.HtmlEncode(Message)
        lblMessage.CssClass = "msg-bar " & If(IsSuccess, "msg-success", "msg-error")
        lblMessage.Visible = True
    End Sub

    ' ------------------------------------------------------------------
    ' Uploaded files (kept in ViewState until they are saved)
    ' ------------------------------------------------------------------

    ''' <summary>
    ''' Files uploaded in this popup so far: one row per file with its original name, the
    ''' name it was saved under in the temporary folder, its type and size.
    ''' </summary>
    Private Property UploadedFiles As DataTable
        Get
            Dim dt As DataTable = TryCast(ViewState("UploadedFiles"), DataTable)
            If dt Is Nothing Then
                dt = New DataTable("UploadedFiles")
                dt.Columns.Add("FileKey", GetType(String))        ' temp file name (GUID + extension)
                dt.Columns.Add("FileName", GetType(String))       ' original name
                dt.Columns.Add("CategoryId", GetType(String))
                dt.Columns.Add("CategoryTitle", GetType(String))
                dt.Columns.Add("ContentType", GetType(String))
                dt.Columns.Add("FileSize", GetType(Long))
                dt.Columns.Add("UploadedAt", GetType(String))
            End If
            Return dt
        End Get
        Set(value As DataTable)
            ViewState("UploadedFiles") = value
        End Set
    End Property

    ''' <summary>
    ''' Temporary folder for this popup's uploads: ~/Attachments/Temp/&lt;id&gt;. The id is made
    ''' once per popup and kept in ViewState, so two popups never share a folder.
    ''' </summary>
    Private ReadOnly Property TempFolder As String
        Get
            Dim id As String = Convert.ToString(ViewState("UploadFolderId"))
            If id = "" Then
                id = Guid.NewGuid().ToString("N")
                ViewState("UploadFolderId") = id
            End If
            Dim folder As String = Server.MapPath("~/Attachments/Temp/" & id)
            If Not Directory.Exists(folder) Then Directory.CreateDirectory(folder)
            Return folder
        End Get
    End Property

    ''' <summary>
    ''' Upload: checks a type is chosen and each file's extension and size, saves each file
    ''' into the temporary folder under a new name (so names can't clash), and adds it to
    ''' the list. Files that fail a check are skipped and named in the message.
    ''' </summary>
    Protected Sub btnUpload_Click(sender As Object, e As EventArgs)
        If ddlAttachmentType.SelectedValue = "" Then
            ShowMessage("Choose the Attachment Type first.", False)
            BindFiles()
            Exit Sub
        End If

        If Not fuAttachment.HasFiles Then
            ShowMessage("Choose a file to upload.", False)
            BindFiles()
            Exit Sub
        End If

        Dim files As DataTable = UploadedFiles
        Dim added As Integer = 0
        Dim problems As New List(Of String)

        For Each posted As HttpPostedFile In fuAttachment.PostedFiles
            Dim originalName As String = Path.GetFileName(posted.FileName)
            Dim ext As String = Path.GetExtension(originalName).ToLowerInvariant()

            If originalName = "" Then Continue For
            If Array.IndexOf(AllowedExtensions, ext) < 0 Then
                problems.Add(originalName & " (file type not allowed)")
                Continue For
            End If
            If posted.ContentLength = 0 Then
                problems.Add(originalName & " (empty file)")
                Continue For
            End If
            If posted.ContentLength > MaxFileBytes Then
                problems.Add(originalName & " (larger than 20 MB)")
                Continue For
            End If

            Try
                Dim fileKey As String = Guid.NewGuid().ToString("N") & ext
                posted.SaveAs(Path.Combine(TempFolder, fileKey))

                files.Rows.Add(fileKey, originalName, ddlAttachmentType.SelectedValue, ddlAttachmentType.SelectedItem.Text,
                               posted.ContentType, CLng(posted.ContentLength),
                               DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture))
                added += 1
            Catch ex As Exception
                problems.Add(originalName & " (" & ex.Message & ")")
            End Try
        Next

        UploadedFiles = files
        BindFiles()

        If problems.Count = 0 Then
            ShowMessage(added & If(added = 1, " file uploaded.", " files uploaded."), True)
        ElseIf added = 0 Then
            ShowMessage("Not uploaded: " & String.Join(", ", problems), False)
        Else
            ShowMessage(added & " uploaded. Not uploaded: " & String.Join(", ", problems), False)
        End If
    End Sub

    ''' <summary>Remove: deletes the file from the temporary folder and from the list.</summary>
    Protected Sub gvFiles_RowCommand(sender As Object, e As GridViewCommandEventArgs)
        If e.CommandName <> "RemoveFile" Then Exit Sub

        Dim rowIndex As Integer
        If Not Integer.TryParse(Convert.ToString(e.CommandArgument), rowIndex) Then Exit Sub
        If rowIndex < 0 OrElse rowIndex >= gvFiles.DataKeys.Count Then Exit Sub

        Dim fileKey As String = Convert.ToString(gvFiles.DataKeys(rowIndex).Value)
        Dim files As DataTable = UploadedFiles

        For i As Integer = files.Rows.Count - 1 To 0 Step -1
            If Convert.ToString(files.Rows(i)("FileKey")) = fileKey Then
                Try
                    Dim fullPath As String = Path.Combine(TempFolder, Path.GetFileName(fileKey))
                    If File.Exists(fullPath) Then File.Delete(fullPath)
                Catch ex As Exception
                    ' the list entry is removed anyway; a leftover temp file is harmless
                End Try
                files.Rows.RemoveAt(i)
            End If
        Next

        UploadedFiles = files
        BindFiles()
    End Sub

    ''' <summary>Shows the uploaded files with row numbers and readable sizes.</summary>
    Private Sub BindFiles()
        Dim files As DataTable = UploadedFiles
        Dim view As DataTable = files.Copy()
        view.Columns.Add("RowNo", GetType(Integer))
        view.Columns.Add("SizeText", GetType(String))

        For i As Integer = 0 To view.Rows.Count - 1
            view.Rows(i)("RowNo") = i + 1
            view.Rows(i)("SizeText") = FormatSize(CLng(view.Rows(i)("FileSize")))
        Next

        gvFiles.DataSource = view
        gvFiles.DataBind()
    End Sub

    ''' <summary>Bytes as "850 KB" / "2.4 MB".</summary>
    Private Function FormatSize(Bytes As Long) As String
        If Bytes >= 1024L * 1024L Then
            Return (Bytes / (1024.0 * 1024.0)).ToString("0.0", CultureInfo.InvariantCulture) & " MB"
        End If
        Return Math.Max(1L, CLng(Math.Ceiling(Bytes / 1024.0))).ToString(CultureInfo.InvariantCulture) & " KB"
    End Function

    ' ------------------------------------------------------------------
    ' OK / Cancel
    ' ------------------------------------------------------------------

    ''' <summary>
    ''' OK: saves the listed files. Files are grouped by their Attachment Type; each group
    ''' becomes one document (UNITSHUB_DOCUMENTS) titled with the type, holding its files
    ''' (UNITSHUB_ATTACHMENTS). Each file is moved from the temporary folder to
    ''' ~/Attachments/Units or /Contacts under a name that shows what it belongs to (see
    ''' NextStoredPath). Then the popup closes and the
    ''' Unit page refreshes its Attachments tab.
    ''' </summary>
    Protected Sub btnOK_Click(sender As Object, e As EventArgs)
        Dim files As DataTable = UploadedFiles
        If files.Rows.Count = 0 Then
            ShowMessage("Upload at least one file first, or press Cancel.", False)
            BindFiles()
            Exit Sub
        End If

        Dim nodeId As String = Convert.ToString(lblPID.Text).Trim()
        Dim contactId As String = Convert.ToString(lblCID.Text).Trim()
        If nodeId = "" AndAlso contactId = "" Then
            ShowMessage("No unit or customer was given, so the files can't be saved.", False)
            BindFiles()
            Exit Sub
        End If

        Dim projectId As String = GetProjectId()
        Dim userId As String = GetCurrentUserID()
        Dim nowUnix As String = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)

        Dim saved As Integer = 0

        Try
            ' One document per Attachment Type, in the order the types were first used
            Dim categoryOrder As New List(Of String)
            For Each r As DataRow In files.Rows
                Dim cat As String = Convert.ToString(r("CategoryId"))
                If Not categoryOrder.Contains(cat) Then categoryOrder.Add(cat)
            Next

            For Each categoryId As String In categoryOrder
                Dim categoryTitle As String = ""
                For Each r As DataRow In files.Rows
                    If Convert.ToString(r("CategoryId")) = categoryId Then categoryTitle = Convert.ToString(r("CategoryTitle")) : Exit For
                Next

                ' What the document is about, from the category's APPLIES_TO
                Dim docNode As String = nodeId
                Dim docContact As String = contactId
                Dim appliesTo As String = Convert.ToString(DB.RetreiveScalarSTRING(EBDB,
                    "SELECT APPLIES_TO FROM UNITSHUB_DOC_CATEGORIES WHERE CATEGORY_ID = " & Q(categoryId))).Trim().ToUpperInvariant()
                If appliesTo = "UNIT" AndAlso nodeId <> "" Then docContact = ""
                If appliesTo = "CUSTOMER" AndAlso contactId <> "" Then docNode = ""

                Dim documentId As String = NextId("UNITSHUB_DOCUMENTS", "DOCUMENT_ID")
                DB.ExecuteNonQuery(EBDB_CS,
                    "INSERT INTO UNITSHUB_DOCUMENTS (DOCUMENT_ID, CATEGORY_ID, PROJECT_ID, NODE_ID, CONTACT_ID, TITLE, CREATED_BY, CREATED_AT, IS_ACTIVE) VALUES (" &
                    Q(documentId) & ", " & Q(categoryId) & ", " & QOrNull(projectId) & ", " & QOrNull(docNode) & ", " & QOrNull(docContact) & ", " &
                    Q(categoryTitle & " - " & DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)) & ", " &
                    Q(userId) & ", " & nowUnix & ", 'Y')")

                ' Where this document's files go and how they're named (see NextStoredPath)
                Dim folderRel As String = ""
                Dim prefix As String = ""
                GetDocumentFileNaming(projectId, docNode, docContact, categoryId, folderRel, prefix)

                Dim sortOrder As Integer = 0
                For Each r As DataRow In files.Rows
                    If Convert.ToString(r("CategoryId")) <> categoryId Then Continue For
                    sortOrder += 1

                    Dim fileKey As String = Path.GetFileName(Convert.ToString(r("FileKey")))
                    Dim ext As String = Path.GetExtension(fileKey).ToLowerInvariant()
                    Dim attachmentId As String = NextId("UNITSHUB_ATTACHMENTS", "ATTACHMENT_ID")
                    Dim storedRelative As String = NextStoredPath(folderRel, prefix, ext)

                    ' Move the file first, then record it - a row never points to a missing file
                    Dim source As String = Path.Combine(TempFolder, fileKey)
                    Dim target As String = Server.MapPath(storedRelative)
                    If File.Exists(target) Then File.Delete(target)
                    File.Move(source, target)

                    DB.ExecuteNonQuery(EBDB_CS,
                        "INSERT INTO UNITSHUB_ATTACHMENTS (ATTACHMENT_ID, DOCUMENT_ID, FILE_NAME, STORED_PATH, CONTENT_TYPE, FILE_SIZE, SORT_ORDER, CREATED_BY, CREATED_AT, IS_ACTIVE) VALUES (" &
                        Q(attachmentId) & ", " & Q(documentId) & ", " & Q(Convert.ToString(r("FileName"))) & ", " & Q(storedRelative) & ", " &
                        QOrNull(Convert.ToString(r("ContentType"))) & ", " & CLng(r("FileSize")).ToString(CultureInfo.InvariantCulture) & ", " &
                        sortOrder.ToString(CultureInfo.InvariantCulture) & ", " & Q(userId) & ", " & nowUnix & ", 'Y')")
                    saved += 1
                Next
            Next
        Catch ex As Exception
            ShowMessage(If(saved = 0, "Nothing was saved: ", saved & " file(s) were saved, then it stopped: ") & ex.Message, False)
            BindFiles()
            Exit Sub
        End Try

        DeleteTempFolder()
        UploadedFiles = Nothing
        VendorPopupHelper.RegisterPopupSelectionAndClose(Me, False, skipPostBack:=False)
    End Sub

    ''' <summary>Cancel: deletes the uploaded files and closes without saving anything.</summary>
    Protected Sub btnCancel_Click(sender As Object, e As EventArgs)
        DeleteTempFolder()
        UploadedFiles = Nothing
        VendorPopupHelper.RegisterPopupSelectionAndClose(Me, False, skipPostBack:=False)
    End Sub

    ''' <summary>Removes this popup's temporary upload folder (and every file in it).</summary>
    Private Sub DeleteTempFolder()
        Try
            Dim id As String = Convert.ToString(ViewState("UploadFolderId"))
            If id = "" Then Exit Sub
            Dim folder As String = Server.MapPath("~/Attachments/Temp/" & id)
            If Directory.Exists(folder) Then Directory.Delete(folder, True)
        Catch ex As Exception
            ' a leftover temp folder is harmless
        End Try
    End Sub

    ' ------------------------------------------------------------------
    ' Stored file names
    '   Unit (or unit + customer) :  ~/Attachments/Units/<ProjectID>_<UnitID>_<TypeID>_<Seq>.<ext>
    '   Customer only             :  ~/Attachments/Contacts/<ContactID>_<TypeID>_<Seq>.<ext>
    '   Comment                   :  ~/Attachments/Comments/<CommentID>_<Seq>.<ext>
    ' <TypeID> = CATEGORY_ID (e.g. 0001), <Seq> = 3 digits (001, 002 ...), counted per name
    ' prefix, so every name is unique and shows what the file belongs to.
    ' ------------------------------------------------------------------

    ''' <summary>
    ''' Folder (relative) and name prefix for a document's files:
    '''   a unit (alone or with its customer) -> Units,    "&lt;ProjectID&gt;_&lt;UnitID&gt;_&lt;TypeID&gt;"
    '''   a customer only                     -> Contacts, "&lt;ContactID&gt;_&lt;TypeID&gt;"
    ''' </summary>
    Private Sub GetDocumentFileNaming(ProjectId As String, NodeId As String, ContactId As String, CategoryId As String,
                                      ByRef FolderRel As String, ByRef Prefix As String)
        If Not String.IsNullOrWhiteSpace(NodeId) Then
            FolderRel = "~/Attachments/Units"
            Prefix = SafeNamePart(ProjectId) & "_" & SafeNamePart(NodeId) & "_" & SafeNamePart(CategoryId)
        Else
            FolderRel = "~/Attachments/Contacts"
            Prefix = SafeNamePart(ContactId) & "_" & SafeNamePart(CategoryId)
        End If
    End Sub

    ''' <summary>Folder and prefix for a comment's files: Comments, "&lt;CommentID&gt;".</summary>
    Private Sub GetCommentFileNaming(CommentId As String, ByRef FolderRel As String, ByRef Prefix As String)
        FolderRel = "~/Attachments/Comments"
        Prefix = SafeNamePart(CommentId)
    End Sub

    ''' <summary>
    ''' Next free stored name "&lt;Prefix&gt;_&lt;Seq&gt;&lt;ext&gt;" in FolderRel: Seq = highest Seq already used
    ''' with this prefix (in UNITSHUB_ATTACHMENTS, active or not) + 1, and moved on further
    ''' if such a file is already on disk. Returns the relative path, e.g.
    ''' ~/Attachments/Units/0017_001465_0001_003.pdf
    ''' </summary>
    Private Function NextStoredPath(FolderRel As String, Prefix As String, Ext As String) As String
        Dim folder As String = Server.MapPath(FolderRel)
        If Not Directory.Exists(folder) Then Directory.CreateDirectory(folder)

        ' Highest Seq used so far for this prefix (the digits between "<Prefix>_" and the extension)
        Dim maxSeq As Integer = 0
        Dim usedDT As DataTable = GetDataTable(EBDB,
            "SELECT STORED_PATH FROM UNITSHUB_ATTACHMENTS WHERE STORED_PATH LIKE " &
            Q(FolderRel & "/" & Prefix & "\_%") & " ESCAPE '\'")
        If usedDT IsNot Nothing Then
            For Each r As DataRow In usedDT.Rows
                Dim nameOnly As String = Path.GetFileNameWithoutExtension(Convert.ToString(r("STORED_PATH")))
                Dim seqText As String = nameOnly.Substring(Math.Min(nameOnly.Length, Prefix.Length + 1))
                Dim seq As Integer
                If Integer.TryParse(seqText, seq) AndAlso seq > maxSeq Then maxSeq = seq
            Next
        End If

        Dim nextSeq As Integer = maxSeq + 1
        Do While File.Exists(Path.Combine(folder, Prefix & "_" & nextSeq.ToString("000") & Ext))
            nextSeq += 1
        Loop

        Return FolderRel & "/" & Prefix & "_" & nextSeq.ToString("000") & Ext
    End Function

    ''' <summary>A name part with only letters, digits and "-" ("X" if empty), so it's safe in a file name.</summary>
    Private Function SafeNamePart(Value As String) As String
        Dim v As String = System.Text.RegularExpressions.Regex.Replace(If(Value, "").Trim(), "[^A-Za-z0-9-]", "")
        Return If(v = "", "X", v)
    End Function

    ' ------------------------------------------------------------------
    ' Helpers
    ' ------------------------------------------------------------------

    Private Function Q(Value As String) As String
        Return "'" & If(Value, "").Replace("'", "''") & "'"
    End Function

    Private Function QOrNull(Value As String) As String
        Return If(String.IsNullOrWhiteSpace(Value), "NULL", Q(Value.Trim()))
    End Function

    ''' <summary>Next 7-digit ID of a table (highest numeric + 1): 0000001, 0000002 ...</summary>
    Private Function NextId(TableName As String, ColumnName As String) As String
        Dim v As String = Convert.ToString(DB.RetreiveScalarSTRING(EBDB,
            "SELECT LPAD(TO_CHAR(NVL(MAX(TO_NUMBER(" & ColumnName & ")), 0) + 1), 7, '0') FROM " & TableName &
            " WHERE REGEXP_LIKE(" & ColumnName & ", '^[0-9]+$')")).Trim()
        Return If(v = "", "0000001", v)
    End Function

    ''' <summary>The unit's project (passed in, or read from the unit).</summary>
    Private Function GetProjectId() As String
        Dim projectId As String = Convert.ToString(lblPRJID.Text).Trim()
        If projectId = "" AndAlso Convert.ToString(lblPID.Text).Trim() <> "" Then
            projectId = Convert.ToString(DB.RetreiveScalarSTRING(EBDB,
                "SELECT PROJECT_ID FROM UNITSHUB_NODES WHERE NODE_ID = " & Q(lblPID.Text.Trim()))).Trim()
        End If
        Return projectId
    End Function

    ''' <summary>Folder-safe name (letters, digits, - and _ only); "General" if empty.</summary>
    Private Function SafeFolderName(Value As String) As String
        Dim v As String = System.Text.RegularExpressions.Regex.Replace(If(Value, ""), "[^A-Za-z0-9_-]", "")
        Return If(v = "", "General", v)
    End Function

    ''' <summary>Same as MainPage.GetCurrentUserID.</summary>
    Private Function GetCurrentUserID() As String
        Dim sessionUserID As String = Convert.ToString(Session("UserID"))
        If String.IsNullOrEmpty(sessionUserID) Then
            Return "2271" ' TEMP: fallback while real auth/session wiring is pending
        End If
        Return sessionUserID
    End Function

    ''' <summary>The X works like Cancel: the uploaded files are thrown away.</summary>
    Protected Sub imgClose_Click(sender As Object, e As ImageClickEventArgs) Handles imgClose.Click
        DeleteTempFolder()
        UploadedFiles = Nothing
        VendorPopupHelper.RegisterPopupSelectionAndClose(Me, False, skipPostBack:=False)
    End Sub

End Class