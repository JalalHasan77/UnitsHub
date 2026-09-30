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
            ShowPdf(Request("PDF"))
        End If
    End Sub

    ''' <summary>
    ''' Shows ReportsTemplate\&lt;PDF&gt;.pdf in the iframe. PDF may be given with or without
    ''' ".pdf". Only the file name is used (any folder part is dropped), so the request
    ''' can't point outside ReportsTemplate. If the file isn't there, a message is shown.
    ''' </summary>
    Private Sub ShowPdf(PdfName As String)
        Dim FileName As String = Path.GetFileName(Convert.ToString(PdfName).Trim())
        If FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) Then
            FileName = FileName.Substring(0, FileName.Length - 4)
        End If

        If FileName = "" Then
            ShowPdfMessage("No invoice was given to display.")
            Exit Sub
        End If

        Dim PhysicalPath As String = Server.MapPath("~/ReportsTemplate/" & FileName & ".pdf")
        If Not File.Exists(PhysicalPath) Then
            ShowPdfMessage("The invoice file '" & FileName & ".pdf' was not found.")
            Exit Sub
        End If

        ' File name is URL-encoded so names with spaces etc. still load
        ifrInvoice.Attributes("src") = ResolveUrl("~/ReportsTemplate/" & Uri.EscapeDataString(FileName) & ".pdf")
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