Imports System.Data
Imports System.Collections.Generic
Imports System.DateTime
Imports System.Drawing
Imports System.Globalization
Imports System.IO
Imports CrystalDecisions.CrystalReports.Engine

''' <summary>
''' Add Comment popup: writes a comment about a unit, its customer, or both, with optional
''' files attached to it.
'''   About        Unit &amp; customer / Unit only / Customer only (only what was passed in)
'''   Comment      the text (required, up to 4000 characters)
'''   Attachments  optional; one or several files, kept in a temporary folder and listed
'''                in gvFiles until OK (Remove takes one out again)
'''   OK           saves the comment (UNITSHUB_COMMENTS) and its files
'''                (UNITSHUB_ATTACHMENTS with COMMENT_ID), files moved to
'''                ~/Attachments/Comments/&lt;CommentID&gt;_&lt;Seq&gt;.&lt;ext&gt;
'''   Cancel / X   deletes the uploaded files, saves nothing
''' Query string: NodeID, ProjectId, ContactId (same as AddAttachment).
''' </summary>
Partial Class AddComment
    Inherits System.Web.UI.Page
    Private encryNdecry As New EncryDecry

    Private Const MaxFileBytes As Integer = 20 * 1024 * 1024
    Private Const MaxCommentLength As Integer = 4000
    Private ReadOnly AllowedExtensions As String() =
        {".pdf", ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tif", ".tiff", ".doc", ".docx", ".xls", ".xlsx"}

    ' ------------------------------------------------------------------
    ' Page
    ' ------------------------------------------------------------------

    Private Sub AddComment_Load(sender As Object, e As EventArgs) Handles Me.Load
        If Not Page.IsPostBack Then
            lblPID.Text = Convert.ToString(Request("NodeID"))
            lblPRJID.Text = Convert.ToString(Request("ProjectId"))
            lblCID.Text = Convert.ToString(Request("ContactId"))

            LoadAboutOptions()
            BindFiles()
        End If
    End Sub

    ''' <summary>
    ''' "About" choices, only those possible with what was passed in:
    ''' unit + customer -> Unit &amp; customer (default) / Unit only / Customer only;
    ''' unit only -> Unit; customer only -> Customer.
    ''' </summary>
    Private Sub LoadAboutOptions()
        rblAbout.Items.Clear()
        Dim hasUnit As Boolean = Convert.ToString(lblPID.Text).Trim() <> ""
        Dim hasCustomer As Boolean = Convert.ToString(lblCID.Text).Trim() <> ""

        If hasUnit AndAlso hasCustomer Then rblAbout.Items.Add(New ListItem("Unit & customer", "BOTH"))
        If hasUnit Then rblAbout.Items.Add(New ListItem(If(hasCustomer, "Unit only", "Unit"), "UNIT"))
        If hasCustomer Then rblAbout.Items.Add(New ListItem(If(hasUnit, "Customer only", "Customer"), "CUSTOMER"))

        If rblAbout.Items.Count > 0 Then rblAbout.SelectedIndex = 0
    End Sub

    Private Sub ShowMessage(Message As String, IsSuccess As Boolean)
        lblMessage.Text = Server.HtmlEncode(Message)
        lblMessage.CssClass = "msg-bar " & If(IsSuccess, "msg-success", "msg-error")
        lblMessage.Visible = True
    End Sub

    ' ------------------------------------------------------------------
    ' Attached files (kept in ViewState until OK)
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
    ''' Upload: checks each file's extension and size, saves it into the temporary folder
    ''' under a new name (so names can't clash), and adds it to the list. Files that fail a
    ''' check are skipped and named in the message.
    ''' </summary>
    Protected Sub btnUpload_Click(sender As Object, e As EventArgs)
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

                files.Rows.Add(fileKey, originalName, posted.ContentType, CLng(posted.ContentLength),
                               DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture))
                added += 1
            Catch ex As Exception
                problems.Add(originalName & " (" & ex.Message & ")")
            End Try
        Next

        UploadedFiles = files
        BindFiles()

        If problems.Count = 0 Then
            ShowMessage(added & If(added = 1, " file attached.", " files attached."), True)
        ElseIf added = 0 Then
            ShowMessage("Not attached: " & String.Join(", ", problems), False)
        Else
            ShowMessage(added & " attached. Not attached: " & String.Join(", ", problems), False)
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
    ''' OK: saves the comment in UNITSHUB_COMMENTS (about the unit, the customer or both, as
    ''' chosen in About), then each attached file: moved from the temporary folder to
    ''' ~/Attachments/Comments/&lt;CommentID&gt;_&lt;Seq&gt;.&lt;ext&gt; and recorded in UNITSHUB_ATTACHMENTS
    ''' with this COMMENT_ID. Then the popup closes and the Unit page refreshes.
    ''' </summary>
    Protected Sub btnOK_Click(sender As Object, e As EventArgs)
        Dim commentText As String = Convert.ToString(txtComment.Text).Trim()
        If commentText = "" Then
            ShowMessage("Write the comment first, or press Cancel.", False)
            BindFiles()
            Exit Sub
        End If
        If commentText.Length > MaxCommentLength Then
            ShowMessage("The comment is too long (" & commentText.Length & " characters; up to " & MaxCommentLength & ").", False)
            BindFiles()
            Exit Sub
        End If

        ' What it's about
        Dim nodeId As String = Convert.ToString(lblPID.Text).Trim()
        Dim contactId As String = Convert.ToString(lblCID.Text).Trim()
        Select Case rblAbout.SelectedValue
            Case "UNIT" : contactId = ""
            Case "CUSTOMER" : nodeId = ""
        End Select
        If nodeId = "" AndAlso contactId = "" Then
            ShowMessage("No unit or customer was given, so the comment can't be saved.", False)
            BindFiles()
            Exit Sub
        End If

        Dim projectId As String = If(nodeId = "", "", GetProjectId())
        Dim userId As String = GetCurrentUserID()
        Dim nowUnix As String = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)
        Dim files As DataTable = UploadedFiles
        Dim commentId As String = ""
        Dim saved As Integer = 0

        Try
            commentId = NextId("UNITSHUB_COMMENTS", "COMMENT_ID")
            DB.ExecuteNonQuery(EBDB_CS,
                "INSERT INTO UNITSHUB_COMMENTS (COMMENT_ID, PROJECT_ID, NODE_ID, CONTACT_ID, COMMENT_TEXT, CREATED_BY, CREATED_AT, IS_ACTIVE) VALUES (" &
                Q(commentId) & ", " & QOrNull(projectId) & ", " & QOrNull(nodeId) & ", " & QOrNull(contactId) & ", " &
                Q(commentText) & ", " & Q(userId) & ", " & nowUnix & ", 'Y')")

            ' The comment's own files
            Dim folderRel As String = ""
            Dim prefix As String = ""
            GetCommentFileNaming(commentId, folderRel, prefix)

            Dim sortOrder As Integer = 0
            For Each r As DataRow In files.Rows
                sortOrder += 1
                Dim fileKey As String = Path.GetFileName(Convert.ToString(r("FileKey")))
                Dim ext As String = Path.GetExtension(fileKey).ToLowerInvariant()
                Dim attachmentId As String = NextId("UNITSHUB_ATTACHMENTS", "ATTACHMENT_ID")
                Dim storedRelative As String = NextStoredPath(folderRel, prefix, ext)

                ' Move the file first, then record it - a row never points to a missing file
                Dim target As String = Server.MapPath(storedRelative)
                If File.Exists(target) Then File.Delete(target)
                File.Move(Path.Combine(TempFolder, fileKey), target)

                DB.ExecuteNonQuery(EBDB_CS,
                    "INSERT INTO UNITSHUB_ATTACHMENTS (ATTACHMENT_ID, COMMENT_ID, FILE_NAME, STORED_PATH, CONTENT_TYPE, FILE_SIZE, SORT_ORDER, CREATED_BY, CREATED_AT, IS_ACTIVE) VALUES (" &
                    Q(attachmentId) & ", " & Q(commentId) & ", " & Q(Convert.ToString(r("FileName"))) & ", " & Q(storedRelative) & ", " &
                    QOrNull(Convert.ToString(r("ContentType"))) & ", " & CLng(r("FileSize")).ToString(CultureInfo.InvariantCulture) & ", " &
                    sortOrder.ToString(CultureInfo.InvariantCulture) & ", " & Q(userId) & ", " & nowUnix & ", 'Y')")
                saved += 1
            Next
        Catch ex As Exception
            If commentId = "" OrElse (saved = 0 AndAlso files.Rows.Count = 0) Then
                ShowMessage("The comment was not saved: " & ex.Message, False)
            Else
                ShowMessage("The comment was saved with " & saved & " of " & files.Rows.Count & " file(s), then it stopped: " & ex.Message, False)
            End If
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
    ' Stored file names - comment files: ~/Attachments/Comments/<CommentID>_<Seq>.<ext>
    ' ------------------------------------------------------------------

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