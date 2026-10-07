Imports System.Data
Imports System.DateTime
Imports System.Drawing
Imports System.Globalization
Imports System.IO
Imports CrystalDecisions.CrystalReports.Engine

Partial Class DisplayInvoice
    Inherits System.Web.UI.Page
    Private encryNdecry As New EncryDecry

    Private Sub AutoTransfer_Load(sender As Object, e As EventArgs) Handles Me.Load
        If Not Page.IsPostBack Then
            ' An uploaded attachment (from the Unit page), or an invoice PDF.
            ' Convert.ToString(String) returns Nothing for a parameter that wasn't sent (it is
            ' the String overload, not the Object one), so .Trim() on it failed - QueryValue
            ' always gives "" instead.
            Dim attachmentId As String = QueryValue("AttachmentId")
            If attachmentId <> "" Then
                ShowAttachment(attachmentId)
            Else
                ShowPdf(QueryValue("PDF"), QueryValue("Project"))
            End If
        End If
    End Sub

    Private Function QueryValue(Name As String) As String
        ' A request parameter, trimmed; "" when it wasn't sent (never Nothing)
        Return If(Request(Name), "").Trim()
    End Function

    ''' <summary>
    ''' Shows an uploaded attachment (UNITSHUB_ATTACHMENTS) in the iframe, by its ATTACHMENT_ID.
    ''' Only files stored under ~/Attachments/ are shown. PDFs and images display in the
    ''' frame; other types (Word, Excel) are offered for download by the browser.
    ''' </summary>
    Private Sub ShowAttachment(AttachmentId As String)
        Dim id As String = If(AttachmentId, "").Trim()
        If id = "" Then
            ShowPdfMessage("No file was given to display.")
            Exit Sub
        End If

        Dim storedPath As String = If(DB.RetreiveScalarSTRING(EBDB,
            "SELECT STORED_PATH FROM UNITSHUB_ATTACHMENTS WHERE ATTACHMENT_ID = '" & id.Replace("'", "''") & "' AND IS_ACTIVE = 'Y'"), "").Trim()
        If storedPath = "" Then
            ShowPdfMessage("This file wasn't found.")
            Exit Sub
        End If

        ' Only paths inside ~/Attachments/, without any ".." in them
        Dim normalized As String = storedPath.Replace("\", "/")
        If Not normalized.StartsWith("~/Attachments/", StringComparison.OrdinalIgnoreCase) OrElse normalized.Contains("..") Then
            ShowPdfMessage("This file can't be shown.")
            Exit Sub
        End If

        If Not File.Exists(Server.MapPath(normalized)) Then
            ShowPdfMessage("The file '" & Path.GetFileName(normalized) & "' is missing on the server.")
            Exit Sub
        End If

        ' Each folder / file name part URL-encoded, so names with spaces etc. still load
        Dim parts() As String = normalized.Substring(2).Split("/"c)
        For i As Integer = 0 To parts.Length - 1
            parts(i) = Uri.EscapeDataString(parts(i))
        Next
        ifrInvoice.Attributes("src") = ResolveUrl("~/" & String.Join("/", parts))
        ifrInvoice.Visible = True
        lblPdfMessage.Visible = False
    End Sub

    ''' <summary>
    ''' Shows ReportsTemplate\&lt;PDF&gt;.pdf in the iframe - or, when Project is given,
    ''' ReportsTemplate\&lt;Project&gt;\&lt;PDF&gt;.pdf (where AutoTransfer saves invoices).
    ''' PDF may be given with or without ".pdf". Only plain names are used (any folder part
    ''' is dropped), so the request can't point outside ReportsTemplate. If the file isn't
    ''' there, a message is shown.
    ''' </summary>
    Private Sub ShowPdf(PdfName As String, Optional ProjectFolder As String = "")
        Dim FileName As String = Path.GetFileName(If(PdfName, "").Trim())
        If FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) Then
            FileName = FileName.Substring(0, FileName.Length - 4)
        End If

        If FileName = "" Then
            ShowPdfMessage("No invoice was given to display.")
            Exit Sub
        End If

        Dim Folder As String = Path.GetFileName(If(ProjectFolder, "").Trim())
        Dim RelativeFolder As String = If(Folder = "", "", Folder & "/")

        Dim RootPath As String = Server.MapPath("~/ReportsTemplate")
        Dim PhysicalPath As String = Path.Combine(RootPath, Folder, FileName & ".pdf")

        ' Not in the project's folder: look for it anywhere under ReportsTemplate (an older
        ' invoice saved in the root, or a project folder spelled differently). Still only
        ' files inside ReportsTemplate can be shown.
        If Not File.Exists(PhysicalPath) Then
            Dim found As String = FindInvoiceFile(RootPath, FileName & ".pdf")
            If found = "" Then
                ShowPdfMessage("The invoice file '" & RelativeFolder & FileName & ".pdf' was not found " &
                               "(looked in " & Path.Combine(RootPath, Folder) & " and the other ReportsTemplate folders).")
                Exit Sub
            End If
            PhysicalPath = found
        End If

        ' URL of the file, relative to ReportsTemplate; each folder / file name part is
        ' URL-encoded so names with spaces etc. still load
        Dim relative As String = PhysicalPath.Substring(RootPath.TrimEnd("\"c).Length).TrimStart("\"c)
        Dim parts() As String = relative.Split("\"c)
        For i As Integer = 0 To parts.Length - 1
            parts(i) = Uri.EscapeDataString(parts(i))
        Next
        ifrInvoice.Attributes("src") = ResolveUrl("~/ReportsTemplate/" & String.Join("/", parts))
        ifrInvoice.Visible = True
        lblPdfMessage.Visible = False
    End Sub

    ''' <summary>Full path of the first file with this name anywhere under Root; "" if none.</summary>
    Private Function FindInvoiceFile(Root As String, FileName As String) As String
        Try
            If Not Directory.Exists(Root) Then Return ""
            Dim matches() As String = Directory.GetFiles(Root, FileName, SearchOption.AllDirectories)
            If matches.Length > 0 Then Return matches(0)
        Catch ex As Exception
            ' A folder that can't be read - treat as not found
        End Try
        Return ""
    End Function


    Private Sub ShowPdfMessage(Message As String)
        ifrInvoice.Visible = False
        lblPdfMessage.Text = Server.HtmlEncode(Message)
        lblPdfMessage.Visible = True
    End Sub

    Protected Sub imgClose_Click(sender As Object, e As ImageClickEventArgs) Handles imgClose.Click
        VendorPopupHelper.RegisterPopupSelectionAndClose(Me, False, skipPostBack:=False)
    End Sub
End Class