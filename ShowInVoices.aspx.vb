Imports System.Data
Imports System.Collections.Generic
Imports System.DateTime
Imports System.Drawing
Imports System.Globalization
Imports System.IO
Imports CrystalDecisions.CrystalReports.Engine

Partial Class ShowInVoices
    Inherits System.Web.UI.Page
    Private encryNdecry As New EncryDecry

    Private Const OpenActionPopupReturnKey As String = "DisplayInvoicePopup"

    ''' <summary>Folder under ReportsTemplate where this project's invoice PDFs are saved.</summary>
    Private Property InvoiceProject As String
        Get
            Return Convert.ToString(ViewState("InvoiceProject"))
        End Get
        Set(value As String)
            ViewState("InvoiceProject") = value
        End Set
    End Property

    Private Sub AutoTransfer_Load(sender As Object, e As EventArgs) Handles Me.Load
        If Not Page.IsPostBack Then
            lblPID.Text = Request("NodeID")
            lblPRJID.Text = Request("ProjectId")
            lblSTATEID.Text = Request("STATEID")
            lblActionID.Text = Request("ActionId")

            InvoiceProject = GetInvoiceProjectFolder(lblPID.Text)
            BindInvoices()
        End If
    End Sub

    ''' <summary>
    ''' Fills gvInvoices with this unit's active UNITSHUB_PAYMENTS rows, oldest first:
    '''   Seq          1, 2, 3 ... numbered by the query in DATECREATED order
    '''   Date         DATECREATED
    '''   Description  DESCRIPTION - a link that opens the invoice PDF (DisplayInvoice)
    '''                when the payment has an INVOICENUM, plain text otherwise
    '''   Invoice No.  INVOICENUM
    '''   Amount       AMOUNT & " BHD"
    ''' </summary>
    Private Sub BindInvoices()
        Dim NodeId As String = Convert.ToString(lblPID.Text).Trim()
        If NodeId = "" Then
            litInvoicesFor.Text = "No unit was given."
            gvInvoices.DataSource = Nothing
            gvInvoices.DataBind()
            Exit Sub
        End If

        Dim SQL As String = ""
        SQL = SQL + vbCrLf + " SELECT ROW_NUMBER() OVER (ORDER BY P.DATECREATED, P.PAYMENT_ID) AS SEQ_NO, "
        SQL = SQL + vbCrLf + "        P.PAYMENT_ID, P.DATECREATED, P.DESCRIPTION, P.AMOUNT, "
        SQL = SQL + vbCrLf + "        TRIM(P.INVOICENUM) AS INVOICENUM "
        SQL = SQL + vbCrLf + " FROM   UNITSHUB_PAYMENTS P "
        SQL = SQL + vbCrLf + " WHERE  P.NODE_ID = '" & NodeId.Replace("'", "''") & "' "
        SQL = SQL + vbCrLf + "   AND  P.ACTIVE = 'Y' "
        SQL = SQL + vbCrLf + " ORDER BY SEQ_NO "

        Dim DT As DataTable = GetDataTable(EBDB, SQL)
        If DT Is Nothing Then DT = New DataTable()

        DT.Columns.Add("DATE_TEXT", GetType(String))
        DT.Columns.Add("AMOUNT_TEXT", GetType(String))

        For Each r As DataRow In DT.Rows
            r("DATE_TEXT") = FormatDate(r("DATECREATED"))
            r("AMOUNT_TEXT") = FormatAmount(r("AMOUNT")) & " BHD"
        Next

        gvInvoices.DataSource = DT
        gvInvoices.DataBind()

        Dim unitRef As String = GetUnitReference(NodeId)
        Dim projectName As String = GetProjectName(NodeId)
        Dim parts As New List(Of String)
        parts.Add("Unit " & If(unitRef = "", NodeId, unitRef))
        If projectName <> "" Then parts.Add(projectName)
        parts.Add(DT.Rows.Count.ToString() & If(DT.Rows.Count = 1, " payment", " payments"))
        litInvoicesFor.Text = Server.HtmlEncode(String.Join(" · ", parts))
    End Sub

    ''' <summary>
    ''' Description cell: a link that opens DisplayInvoice.aspx with the payment's invoice
    ''' (same popup as AutoTransfer's Display Invoice), or plain text when the payment has
    ''' no invoice number yet.
    ''' </summary>
    Protected Sub gvInvoices_RowDataBound(sender As Object, e As GridViewRowEventArgs) Handles gvInvoices.RowDataBound
        If e.Row.RowType <> DataControlRowType.DataRow Then Exit Sub

        Dim r As DataRowView = CType(e.Row.DataItem, DataRowView)
        Dim description As String = Convert.ToString(r("DESCRIPTION")).Trim()
        Dim InvoiceNum As String = Convert.ToString(r("INVOICENUM")).Trim()
        If description = "" Then description = "Payment " & Convert.ToString(r("PAYMENT_ID"))

        Dim lnkInvoice As LinkButton = CType(e.Row.FindControl("lnkInvoice"), LinkButton)
        Dim lblNoInvoice As Label = CType(e.Row.FindControl("lblNoInvoice"), Label)

        If InvoiceNum = "" Then
            lnkInvoice.Visible = False
            lblNoInvoice.Text = Server.HtmlEncode(description)
            lblNoInvoice.ToolTip = "No invoice issued for this payment yet"
            lblNoInvoice.Visible = True
            Exit Sub
        End If

        lnkInvoice.Text = Server.HtmlEncode(description)
        lnkInvoice.ToolTip = "Open invoice " & InvoiceNum

        Dim Url As String = "DisplayInvoice.aspx?PDF=" & Server.UrlEncode(InvoiceNum) &
                            "&Project=" & Server.UrlEncode(InvoiceProject)

        VendorPopupHelper.RegisterVendorPopup(Me,
                                              lnkInvoice,
                                              Url,
                                              1000, 0,
                                              PopupPlacement.Center,
                                              "",
                                              VendorPopupHelper.PopupDisplayMode.FrameOnly,
                                              returnKey:=OpenActionPopupReturnKey)
    End Sub

    ''' <summary>
    ''' Folder the invoice PDFs of this unit's project are saved in (ReportsTemplate\&lt;folder&gt;):
    ''' AutoTransfer.GeneratePDF uses the project's TITLE (attribute of the project node,
    ''' NODE_TYPE_ID '000'). Falls back to PROJECT_NAME_EN.
    ''' </summary>
    Private Function GetInvoiceProjectFolder(NodeId As String) As String
        Dim projectId As String = Convert.ToString(lblPRJID.Text).Trim()
        If projectId = "" Then
            projectId = Convert.ToString(DB.RetreiveScalarSTRING(EBDB,
                "SELECT PROJECT_ID FROM UNITSHUB_NODES WHERE NODE_ID = '" & If(NodeId, "").Trim().Replace("'", "''") & "'")).Trim()
        End If
        If projectId = "" Then Return ""

        Dim SQL As String = ""
        SQL = SQL + vbCrLf + " SELECT v.VALUE_TEXT "
        SQL = SQL + vbCrLf + " FROM   UNITSHUB_NODES n "
        SQL = SQL + vbCrLf + " JOIN   UNITSHUB_ATTRIBUTES a "
        SQL = SQL + vbCrLf + "        ON a.PROJECT_ID = n.PROJECT_ID AND a.NODE_TYPE_ID = n.NODE_TYPE_ID "
        SQL = SQL + vbCrLf + "       AND UPPER(a.ATTRIBUTE_NAME) = 'TITLE' "
        SQL = SQL + vbCrLf + " JOIN   UNITSHUB_NODE_ATTRIBUTE_VALUE v "
        SQL = SQL + vbCrLf + "        ON v.NODE_ID = n.NODE_ID AND TO_NUMBER(v.DISPLAY_ORDER) = TO_NUMBER(a.DISPLAY_ORDER) "
        SQL = SQL + vbCrLf + " WHERE  n.PROJECT_ID = '" & projectId.Replace("'", "''") & "' "
        SQL = SQL + vbCrLf + "   AND  n.NODE_TYPE_ID = '000' "
        SQL = SQL + vbCrLf + "   AND  ROWNUM = 1 "

        Dim title As String = Convert.ToString(DB.RetreiveScalarSTRING(EBDB, SQL)).Trim()
        If title <> "" Then Return title

        Return Convert.ToString(DB.RetreiveScalarSTRING(EBDB,
            "SELECT PROJECT_NAME_EN FROM UNITSHUB_PROJECTS WHERE PROJECT_ID = '" & projectId.Replace("'", "''") & "'")).Trim()
    End Function

    ''' <summary>
    ''' The unit's "Reference" attribute (the attribute whose NAME_IN_UI is Reference in
    ''' UNITSHUB_ATTRIBUTES_PROPERTIES, value from UNITSHUB_NODE_ATTRIBUTE_VALUE) - the same
    ''' Reference MainPage shows. "" if the unit has none.
    ''' </summary>
    Private Function GetUnitReference(NodeId As String) As String
        Dim SQL As String = ""
        SQL = SQL + vbCrLf + " SELECT NAV.VALUE_TEXT "
        SQL = SQL + vbCrLf + " FROM   UNITSHUB_NODES N "
        SQL = SQL + vbCrLf + " JOIN   UNITSHUB_ATTRIBUTES_PROPERTIES AP "
        SQL = SQL + vbCrLf + "        ON AP.PROJECT_ID   = N.PROJECT_ID "
        SQL = SQL + vbCrLf + "       AND AP.NODE_TYPE_ID = N.NODE_TYPE_ID "
        SQL = SQL + vbCrLf + "       AND UPPER(REPLACE(AP.NAME_IN_UI, '""', '')) = 'REFERENCE' "
        SQL = SQL + vbCrLf + " JOIN   UNITSHUB_NODE_ATTRIBUTE_VALUE NAV "
        SQL = SQL + vbCrLf + "        ON NAV.NODE_ID = N.NODE_ID "
        SQL = SQL + vbCrLf + "       AND TO_NUMBER(NAV.DISPLAY_ORDER) = TO_NUMBER(AP.DISPLAY_ORDER) "
        SQL = SQL + vbCrLf + " WHERE  N.NODE_ID = '" & If(NodeId, "").Trim().Replace("'", "''") & "' "
        SQL = SQL + vbCrLf + "   AND  ROWNUM = 1 "
        Return Convert.ToString(DB.RetreiveScalarSTRING(EBDB, SQL)).Trim()
    End Function

    ''' <summary>
    ''' Project name (UNITSHUB_PROJECTS.PROJECT_NAME_EN) for the ProjectId passed in, or for
    ''' the unit's own project when none was passed. "" if not found.
    ''' </summary>
    Private Function GetProjectName(NodeId As String) As String
        Dim projectId As String = Convert.ToString(lblPRJID.Text).Trim()
        If projectId = "" Then
            projectId = Convert.ToString(DB.RetreiveScalarSTRING(EBDB,
                "SELECT PROJECT_ID FROM UNITSHUB_NODES WHERE NODE_ID = '" & If(NodeId, "").Trim().Replace("'", "''") & "'")).Trim()
        End If
        If projectId = "" Then Return ""
        Return Convert.ToString(DB.RetreiveScalarSTRING(EBDB,
            "SELECT PROJECT_NAME_EN FROM UNITSHUB_PROJECTS WHERE PROJECT_ID = '" & projectId.Replace("'", "''") & "'")).Trim()
    End Function

    ''' <summary>DATECREATED as yyyy-MM-dd (a DATE, or text in a common format).</summary>
    Private Function FormatDate(Value As Object) As String
        If Value Is Nothing OrElse Value Is DBNull.Value Then Return ""
        If TypeOf Value Is DateTime Then Return CType(Value, DateTime).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
        Dim d As DateTime
        Dim text As String = Convert.ToString(Value).Trim()
        If DateTime.TryParse(text, CultureInfo.GetCultureInfo("en-GB"), DateTimeStyles.None, d) Then
            Return d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
        End If
        Return text
    End Function

    ''' <summary>Amount with thousands separators and 3 decimals, e.g. 1,250.000.</summary>
    Private Function FormatAmount(Value As Object) As String
        If Value Is Nothing OrElse Value Is DBNull.Value Then Return "0.000"
        Dim amount As Decimal
        If Decimal.TryParse(Convert.ToString(Value).Replace(",", "").Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, amount) Then
            Return amount.ToString("N3", CultureInfo.InvariantCulture)
        End If
        Return Convert.ToString(Value)
    End Function

    Protected Sub imgClose_Click(sender As Object, e As ImageClickEventArgs) Handles imgClose.Click
        VendorPopupHelper.RegisterPopupSelectionAndClose(Me, False, skipPostBack:=False)
    End Sub

End Class