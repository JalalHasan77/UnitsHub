Imports System.Data.SqlClient
Imports System.Configuration
Imports System.Data
Imports System.Collections.Generic
Imports System.Linq

Partial Public Class AddFormula
    Inherits System.Web.UI.Page

    ''' <summary>
    ''' Titles already present in the parent page's GridView2, passed over via
    ''' Session by SetUpPaymentSchedule.aspx. Kept in ViewState so it survives
    ''' this page's own postback when Save is clicked.
    ''' </summary>
    Private Property ExistingTitles As List(Of String)
        Get
            Return TryCast(ViewState("ExistingTitles"), List(Of String))
        End Get
        Set(value As List(Of String))
            ViewState("ExistingTitles") = value
        End Set
    End Property

    Protected Sub Page_Load(sender As Object, e As EventArgs) Handles Me.Load
        If Not IsPostBack Then
            Dim titlesFromParent As List(Of String) = TryCast(Session("ExistingPaymentTitles"), List(Of String))
            ExistingTitles = If(titlesFromParent, New List(Of String))

            BindButtons()
        End If
    End Sub

    Private Sub BindButtons()
        Dim dt As DataTable = GetButtonData() ' however you're getting your DataTable
        rptButtons.DataSource = dt
        rptButtons.DataBind()
    End Sub


    Protected Sub rptButtons_ItemCommand(source As Object, e As RepeaterCommandEventArgs)
        If e.CommandName = "Select" Then
            Dim fieldLabel As String = Convert.ToString(e.CommandArgument)
            txtFormulaText.Text &= "[" & fieldLabel & "]"
        End If
    End Sub

    Private Function GetButtonData() As DataTable
        Dim dt As New DataTable()
        dt.Columns.Add("Id", GetType(Integer))
        dt.Columns.Add("Label", GetType(String))

        dt.Rows.Add(1, "Unit Price")

        If ExistingTitles IsNot Nothing AndAlso ExistingTitles.Count > 0 Then
            For i As Integer = 0 To ExistingTitles.Count - 1
                dt.Rows.Add(i + 2, ExistingTitles(i))
            Next

        End If


        Return dt
    End Function


    Protected Sub btnInsert_Click(sender As Object, e As EventArgs) Handles btnInsert.Click

        Dim title As String = txtTitle.Text.Trim()

        If ExistingTitles IsNot Nothing AndAlso ExistingTitles.Any(Function(t) t.Equals(title, StringComparison.OrdinalIgnoreCase)) Then
            lblMessage.CssClass = "validation-error"
            lblMessage.Text = title & " has already been added."
            Return
        End If

        ' Returned row, named like the parent grid's columns (PaymentSchedule.GridView2):
        '   Title -> Description, Formula -> Formula, Comments -> Comment
        ' The grid's "#" is generated there from the row's position.
        Dim returnTable As New DataTable("OnePayment")
        returnTable.Columns.Add("Description", GetType(String))
        returnTable.Columns.Add("Formula", GetType(String))
        returnTable.Columns.Add("Comment", GetType(String))

        Dim returnValue As DataRow = returnTable.NewRow()
        returnValue("Description") = title
        returnValue("Formula") = txtFormulaText.Text.Trim()
        returnValue("Comment") = txtComments.Text.Trim()

        returnTable.Rows.Add(returnValue)

        VendorPopupHelper.RegisterPopupSelectionAndClose(
            page:=Me,
            returnValue:=returnValue,
            startupScriptKey:="OnePayment",
            skipPostBack:=False)
    End Sub

    ''' <summary>
    ''' Inserts a new row into the Formulas table.
    ''' Run the CREATE TABLE script at the bottom of this file once, against your database,
    ''' before using this form.
    ''' </summary>
    'Private Sub InsertFormula(title As String, formulaText As String)
    '    Dim connStr As String = ConfigurationManager.ConnectionStrings("MyConnectionString").ConnectionString

    '    Using conn As New SqlConnection(connStr)
    '        Dim cmd As New SqlCommand("INSERT INTO Formulas (Title, FormulaText) VALUES (@Title, @FormulaText)", conn)
    '        cmd.Parameters.AddWithValue("@Title", title)
    '        cmd.Parameters.AddWithValue("@FormulaText", formulaText)

    '        conn.Open()
    '        cmd.ExecuteNonQuery()
    '    End Using
    'End Sub

End Class

' One-time setup — run this against your database before using the form:
'
' CREATE TABLE Formulas (
'     Id INT IDENTITY(1,1) PRIMARY KEY,
'     Title NVARCHAR(200) NOT NULL,
'     FormulaText NVARCHAR(MAX) NOT NULL,
'     CreatedDate DATETIME NOT NULL DEFAULT GETDATE()
' );
'
' And add the matching connection string to Web.config:
'
' <connectionStrings>
'     <add name="MyConnectionString"
'          connectionString="Server=YOUR_SERVER;Database=YOUR_DB;Trusted_Connection=True;"
'          providerName="System.Data.SqlClient" />
' </connectionStrings>