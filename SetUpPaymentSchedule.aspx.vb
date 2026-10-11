
Imports System.Data
Imports System.Collections.Generic

Partial Class Forms_SetUpPaymentSchedule
    Inherits System.Web.UI.Page
    Dim UDF As New GUDF
    Dim UTC As New UnixTimeConverter

    ''' <summary>
    ''' The DataTable currently bound to GridView2, persisted in ViewState so it
    ''' survives postbacks (needed so Delete can remove a row and rebind).
    ''' </summary>
    Private Property GridView2Data As DataTable
        Get
            Return TryCast(ViewState("GridView2Data"), DataTable)
        End Get
        Set(value As DataTable)
            ViewState("GridView2Data") = value
        End Set
    End Property

    Protected Sub GridView2_RowCommand(sender As Object, e As GridViewCommandEventArgs) Handles GridView2.RowCommand
        If e.CommandName = "DeleteRow" Then
            Dim rowIndex As Integer
            If Not Integer.TryParse(Convert.ToString(e.CommandArgument), rowIndex) Then Exit Sub

            Dim dt As DataTable = GridView2Data
            If dt Is Nothing OrElse rowIndex < 0 OrElse rowIndex >= dt.Rows.Count Then Exit Sub

            dt.Rows.RemoveAt(rowIndex)
            dt.AcceptChanges()

            GridView2Data = dt
            GridView2.DataSource = dt
            GridView2.DataBind()
        End If
    End Sub

    ''' <summary>
    ''' Collects every Title currently in GridView2, so AddFormula.aspx can check
    ''' against them (e.g. to block adding a duplicate).
    ''' </summary>
    Private Function GetExistingTitles() As List(Of String)
        Dim titles As New List(Of String)
        Dim dt As DataTable = GridView2Data
        If dt IsNot Nothing Then
            For Each row As DataRow In dt.Rows
                titles.Add(Convert.ToString(row("Title")))
            Next
        End If
        Return titles
    End Function

    Private Sub Forms_SetUpPaymentSchedule_Load(sender As Object, e As EventArgs) Handles Me.Load

        If Not IsPostBack Then

            UDF.FillCombo(UDF.GetSqlArray(UDF.EBDB_CS, "Select ID,Title from Select_Projects order by ID desc"), ddlProject,, False, False, False)
            Dim i As New ListItem("Select Project", "NoValue")
            ddlProject.Items.Insert(0, i)

            ddlProject.SelectedValue = "NoValue"

        End If

        VendorPopupHelper.RegisterVendorPopup(Me,
                              btnAddExpRdc,
                              "AddFormula.aspx",
                              600, 600,
                              PopupPlacement.Center,
                              "Select Adj",
                              VendorPopupHelper.PopupDisplayMode.FrameOnly, 100)
    End Sub


    ''' <summary>
    ''' Fires when the user changes the Project dropdown. Re-loads GridView2 with
    ''' whatever payment-schedule detail rows already exist for that project
    ''' (Select_PaymentSchedule joined to SELECT_PAYMENTSCHEDULEDETAIL).
    ''' </summary>
    Protected Sub ddlProject_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ddlProject.SelectedIndexChanged
        LoadPaymentScheduleForProject(ddlProject.SelectedValue)
    End Sub

    ''' <summary>
    ''' Queries the schedule + detail tables for the given ProjectID and binds the
    ''' result to GridView2 (and stashes it in GridView2Data so Delete/Add keep working).
    ''' If no project is selected, the grid is cleared.
    ''' </summary>
    Private Sub LoadPaymentScheduleForProject(projectId As String)

        If String.IsNullOrEmpty(projectId) OrElse projectId = "NoValue" Then
            GridView2Data = Nothing
            GridView2.DataSource = Nothing
            GridView2.DataBind()
            Session("ExistingPaymentTitles") = Nothing
            Exit Sub
        End If

        ' Pull the existing schedule detail rows for this project, most recent
        ' schedule first, in their original row order.
        Dim sql As String =
            "SELECT d.TITLE AS Title, d.PAYMENT AS Payment, d.LCCOMMENT AS ""Comment"" " &
            "FROM SELECT_PAYMENTSCHEDULEDETAIL d " &
            "INNER JOIN Select_PaymentSchedule s ON s.SCHEDULEID = d.SCHEDULEID " &
            "WHERE s.PROJECTID = '" & projectId.Replace("'", "''") & "' " &
            "ORDER BY s.SCHEDULEID DESC, d.ROWORDER"

        ' NOTE: UDF.GetDataTable is assumed to mirror UDF.GetSqlArray/UDF.GetNextID
        ' (same UDF.EBDB_CS connection string). If GUDF exposes a differently named
        ' DataTable helper, swap the call below to match it.
        Dim dt As DataTable = UDF.GetDataTable(UDF.EBDB_CS, sql)

        GridView2Data = dt
        GridView2.DataSource = dt
        GridView2.DataBind()

        Session("ExistingPaymentTitles") = GetExistingTitles()
    End Sub


    Protected Sub btnAddExpRdc_Click(sender As Object, e As EventArgs) Handles btnAddExpRdc.Click
        Dim returnValue As Object = VendorPopupHelper.GetPopupReturnValue(Me, "OnePayment")
        If returnValue Is Nothing Then Exit Sub

        Dim selectedRow As DataRow = TryCast(returnValue, DataRow)
        If selectedRow Is Nothing Then Exit Sub

        Dim dt As DataTable = GridView2Data

        If dt Is Nothing Then
            ' First item added - start a new table with the same columns as the
            ' popup's row, but none of its other rows.
            dt = selectedRow.Table.Clone()
        End If

        dt.ImportRow(selectedRow)

        GridView2Data = dt
        GridView2.DataSource = dt
        GridView2.DataBind()


        Session("ExistingPaymentTitles") = GetExistingTitles()
    End Sub


    Protected Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click

        Dim A As String = UTC(Now.Date).Date2Unix.asString

        Dim Table As String = "Select_PaymentSchedule"
        Dim Dic As New Dictionary(Of String, String)
        Dim SCHEDULEID As String = UDF.GetNextID(UDF.EBDB_CS, "Select MAX(to_number(ScheduleID)) as MAXID from Select_PaymentSchedule ", 3)
        Dic.Add("SCHEDULEID", SCHEDULEID)
        Dic.Add("PROJECTID", ddlProject.SelectedValue)
        Dic.Add("CREATEDBY", UDF.GetCurrentUser)
        Dic.Add("CREATEDDATE", UTC(Now.Date).Date2Unix.ToString)
        Dic.Add("ENDDATE", "")
        DB.InsertRecord(DB.EBDB, Table, Dic)

        Dic.Clear()
        Table = "SELECT_PAYMENTSCHEDULEDETAIL"
        Dim dt As DataTable = GridView2Data

        Dim Count As Integer = 1
        If dt IsNot Nothing Then
            For Each row As DataRow In dt.Rows
                Dic.Clear()
                Dim title As String = Convert.ToString(row("Title"))
                Dim payment As String = Convert.ToString(row("Payment"))
                Dim comment As String = Convert.ToString(row("Comment"))

                Dic.Add("DETAILID", UDF.GetNextID(UDF.EBDB_CS, "Select MAX(to_number(DETAILID)) as MAXID from SELECT_PAYMENTSCHEDULEDETAIL ", 3))
                Dic.Add("SCHEDULEID", SCHEDULEID)
                Dic.Add("ROWORDER", Count)
                Dic.Add("TITLE", title)
                Dic.Add("PAYMENT", payment)
                Dic.Add("LCCOMMENT", comment)

                DB.InsertRecord(DB.EBDB, Table, Dic)
                Count = Count + 1
            Next
        End If

    End Sub


End Class
