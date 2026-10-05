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
            ' An uploaded attachment (from the Unit page), or an invoice PDF
            If Convert.ToString(Request("AttachmentId")).Trim() <> "" Then
                ShowAttachment(Request("AttachmentId"))
            Else
                ShowPdf(Request("PDF"), Request("Project"))
            End If
        End If
    End Sub

    ''' <summary>
    ''' Shows an uploaded attachment (UNITSHUB_ATTACHMENTS) in the iframe, by its ATTACHMENT_ID.
    ''' Only files stored under ~/Attachments/ are shown. PDFs and images display in the
    ''' frame; other types (Word, Excel) are offered for download by the browser.
    ''' </summary>
    Private Sub ShowAttachment(AttachmentId As String)
        Dim id As String = Convert.ToString(AttachmentId).Trim()
        If id = "" Then
            ShowPdfMessage("No file was given to display.")
            Exit Sub
        End If

        Dim storedPath As String = Convert.ToString(DB.RetreiveScalarSTRING(EBDB,
            "SELECT STORED_PATH FROM UNITSHUB_ATTACHMENTS WHERE ATTACHMENT_ID = '" & id.Replace("'", "''") & "' AND IS_ACTIVE = 'Y'")).Trim()
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
        Dim FileName As String = Path.GetFileName(Convert.ToString(PdfName).Trim())
        If FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) Then
            FileName = FileName.Substring(0, FileName.Length - 4)
        End If

        If FileName = "" Then
            ShowPdfMessage("No invoice was given to display.")
            Exit Sub
        End If

        Dim Folder As String = Path.GetFileName(Convert.ToString(ProjectFolder).Trim())
        Dim RelativeFolder As String = If(Folder = "", "", Folder & "/")

        Dim PhysicalPath As String = Server.MapPath("~/ReportsTemplate/" & RelativeFolder & FileName & ".pdf")
        If Not File.Exists(PhysicalPath) Then
            ShowPdfMessage("The invoice file '" & RelativeFolder & FileName & ".pdf' was not found.")
            Exit Sub
        End If

        ' Folder and file names are URL-encoded so names with spaces etc. still load
        Dim UrlFolder As String = If(Folder = "", "", Uri.EscapeDataString(Folder) & "/")
        ifrInvoice.Attributes("src") = ResolveUrl("~/ReportsTemplate/" & UrlFolder & Uri.EscapeDataString(FileName) & ".pdf")
        ifrInvoice.Visible = True
        lblPdfMessage.Visible = False
    End Sub

    Private Sub ShowPdfMessage(Message As String)
        ifrInvoice.Visible = False
        lblPdfMessage.Text = Server.HtmlEncode(Message)
        lblPdfMessage.Visible = True
    End Sub

    Protected Sub imgClose_Click(sender As Object, e As ImageClickEventArgs) Handles imgClose.Click
        VendorPopupHelper.RegisterPopupSelectionAndClose(Me, False, skipPostBack:=False)
    End Sub
End Class