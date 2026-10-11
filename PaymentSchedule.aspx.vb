Imports System.Data
Imports System.Collections.Generic

''' <summary>
''' Popup page: sets up a project's payment schedule. Controls and logic taken from
''' SetupPaymentSchedule.aspx, in this page's layout:
'''
''' LOAD   : ddlProject is filled from Select_Projects ("Select Project" first); the
'''          "Add Other Expenses/Reduction" link is wired to the AddFormula popup.
''' PROJECT: picking a project loads its saved schedule rows into GridView2
'''          (Select_PaymentSchedule + SELECT_PAYMENTSCHEDULEDETAIL).
''' ADD    : the row chosen in AddFormula is appended to the grid (btnAddExpRdc_Click).
''' DELETE : the bin icon removes a row from the grid.
''' SAVE   : writes a new Select_PaymentSchedule row for the project plus one
'''          SELECT_PAYMENTSCHEDULEDETAIL row per grid row, in grid order.
''' The grid's rows are kept in ViewState (GridView2Data) between postbacks, and their
''' titles in Session("ExistingPaymentTitles") so AddFormula can block duplicates.
''' </summary>
Partial Class PaymentSchedule
    Inherits System.Web.UI.Page

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

    Private Sub PaymentSchedule_Load(sender As Object, e As EventArgs) Handles Me.Load
        If Not IsPostBack Then
            LoadExistingPlans()
            rblMode.SelectedValue = "New"
            BindGrid(Nothing)
        End If

        VendorPopupHelper.RegisterVendorPopup(Me,
                              btnAddExpRdc,
                              "AddFormula.aspx",
                              600, 600,
                              PopupPlacement.Center,
                              "Select Adj",
                              VendorPopupHelper.PopupDisplayMode.FrameOnly, 100)
    End Sub

    Private Sub LoadExistingPlans()
        ddlExistingPlan.Items.Clear()
        ddlExistingPlan.Items.Add(New ListItem("-- Select a plan --", ""))

        Dim DT As Data.DataTable = GetDataTable(EBDB, "Select PLAN_ID, NAME from UNITSHUB_PAYMENTPLAN order by PLAN_ID")
        If DT IsNot Nothing Then
            For Each r As Data.DataRow In DT.Rows
                Dim id As String = Convert.ToString(r("PLAN_ID"))
                ddlExistingPlan.Items.Add(New ListItem(Convert.ToString(r("NAME")), id))
            Next
        End If
    End Sub

    ' ==================================================================
    ' Top options: Add New / Edit Existing / Copy Existing Schedule
    '   New  : "New plan name" box; Save creates the plan (next PLAN_ID) and its lines
    '   Edit : plan dropdown; picking a plan shows its lines; Save replaces its lines
    '   Copy : plan dropdown + "New plan name" box; picking a plan shows its lines;
    '          Save creates a new plan (next PLAN_ID) with the lines in the grid
    ' ==================================================================

    Private ReadOnly Property SelectedMode As String
        Get
            Dim m As String = Convert.ToString(rblMode.SelectedValue)
            Return If(m = "", "New", m)
        End Get
    End Property

    ''' <summary>Switching option starts clean: no plan picked, empty name, empty grid.</summary>
    Protected Sub rblMode_SelectedIndexChanged(sender As Object, e As EventArgs) Handles rblMode.SelectedIndexChanged
        ddlExistingPlan.SelectedIndex = 0
        txtNewPlanName.Text = ""
        BindGrid(Nothing)
        lblScheduleMessage.Visible = False
    End Sub

    ''' <summary>Shows the controls the chosen option needs, every request.</summary>
    Protected Sub Page_PreRender(sender As Object, e As EventArgs) Handles Me.PreRender
        Dim mode As String = SelectedMode
        pnlExistingPlan.Visible = (mode = "Edit" OrElse mode = "Copy")
        pnlNewPlanName.Visible = (mode = "New" OrElse mode = "Copy")
        lblPaymentPlan.Text = If(mode = "Copy", "Copy from", "Payment Plan")

        Select Case mode
            Case "Edit" : litPageTitle.Text = "Edit Payment Schedule"
            Case "Copy" : litPageTitle.Text = "Copy Payment Schedule"
            Case Else : litPageTitle.Text = "New Payment Schedule"
        End Select
    End Sub

    ''' <summary>
    ''' Next PLAN_ID of UNITSHUB_PAYMENTPLAN: the highest numeric PLAN_ID + 1, padded with
    ''' zeros to the same width as the existing IDs (e.g. 00007 -> 00008). "1" if none.
    ''' </summary>
    Private Function GetNextPlanId() As String
        Dim DT As DataTable = GetDataTable(EBDB,
            "SELECT MAX(TO_NUMBER(PLAN_ID)) AS MAX_ID, MAX(LENGTH(PLAN_ID)) AS ID_LEN " &
            "FROM UNITSHUB_PAYMENTPLAN WHERE REGEXP_LIKE(PLAN_ID, '^[0-9]+$')")

        Dim maxId As Long = 0
        Dim width As Integer = 1
        If DT IsNot Nothing AndAlso DT.Rows.Count > 0 Then
            If Not IsDBNull(DT.Rows(0)("MAX_ID")) Then maxId = Convert.ToInt64(DT.Rows(0)("MAX_ID"))
            If Not IsDBNull(DT.Rows(0)("ID_LEN")) Then width = Convert.ToInt32(DT.Rows(0)("ID_LEN"))
        End If
        Return (maxId + 1).ToString().PadLeft(width, "0"c)
    End Function

    Private Function PlanNameExists(Name As String) As Boolean
        Dim DT As DataTable = GetDataTable(EBDB,
            "SELECT PLAN_ID FROM UNITSHUB_PAYMENTPLAN WHERE UPPER(TRIM(NAME)) = '" &
            Name.Trim().ToUpperInvariant().Replace("'", "''") & "'")
        Return DT IsNot Nothing AndAlso DT.Rows.Count > 0
    End Function

    ''' <summary>
    ''' Fires when a plan is picked in ddlExistingPlan: loads that plan's lines from
    ''' UNITSHUB_PAYMENTPLANDETAILS into GridView2 (in DETAIL_ID order). "-- Select a
    ''' plan --" clears the grid.
    ''' </summary>
    Protected Sub ddlExistingPlan_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ddlExistingPlan.SelectedIndexChanged
        LoadPlanDetails(ddlExistingPlan.SelectedValue)

        ' Copy: suggest a name for the new plan
        If SelectedMode = "Copy" AndAlso ddlExistingPlan.SelectedValue <> "" AndAlso txtNewPlanName.Text.Trim() = "" Then
            txtNewPlanName.Text = "Copy of " & ddlExistingPlan.SelectedItem.Text
        End If
    End Sub

    ''' <summary>
    ''' The plan's lines, named for GridView2's columns (and AddFormula's returned row):
    ''' "Description", "Formula", and an empty "Comment" (the table has no comment). DETAIL_ID
    ''' comes along too, for saving the lines back to the plan later.
    ''' </summary>
    Private Sub LoadPlanDetails(PlanId As String)
        lblScheduleMessage.Visible = False

        If String.IsNullOrEmpty(PlanId) Then
            BindGrid(Nothing)
            Exit Sub
        End If

        Dim SQL As String = ""
        SQL = SQL + vbCrLf + " SELECT D.DETAIL_ID, "
        SQL = SQL + vbCrLf + "        D.DESCRIPTION AS ""Description"", "
        SQL = SQL + vbCrLf + "        D.FORMULA     AS ""Formula"", "
        SQL = SQL + vbCrLf + "        CAST(NULL AS VARCHAR2(200)) AS ""Comment"" "
        SQL = SQL + vbCrLf + " FROM   UNITSHUB_PAYMENTPLANDETAILS D "
        SQL = SQL + vbCrLf + " WHERE  D.PLAN_ID = '" & PlanId.Replace("'", "''") & "' "
        SQL = SQL + vbCrLf + "   AND  D.END_IN IS NULL "   ' only the active lines (closed ones are history)
        ' DETAIL_ID as a number when it is one, so 10 comes after 9
        SQL = SQL + vbCrLf + " ORDER BY CASE WHEN REGEXP_LIKE(D.DETAIL_ID, '^[0-9]+$') THEN TO_NUMBER(D.DETAIL_ID) END, D.DETAIL_ID "

        Try
            BindGrid(GetDataTable(EBDB, SQL))
        Catch ex As Exception
            BindGrid(Nothing)
            lblScheduleMessage.Text = Server.HtmlEncode("The plan's lines couldn't be loaded: " & ex.Message)
            lblScheduleMessage.CssClass = "schedule-message err"
            lblScheduleMessage.Visible = True
        End Try
    End Sub

    ''' <summary>Binds the grid to Dt (Nothing = empty) and remembers it for later postbacks.</summary>
    Private Sub BindGrid(Dt As DataTable)
        GridView2Data = Dt
        GridView2.DataSource = Dt
        GridView2.DataBind()
        Session("ExistingPaymentTitles") = If(Dt Is Nothing, Nothing, GetExistingTitles())
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
                titles.Add(Convert.ToString(row("Description")))
            Next
        End If
        Return titles
    End Function

    ''' <summary>
    ''' Runs when AddFormula closes with Save: appends the row it returned (key
    ''' "OnePayment": Description, Formula, Comment) to the end of the grid. The grid's
    ''' "#" numbers the rows in order, so the new row gets the next number.
    ''' </summary>
    Protected Sub btnAddExpRdc_Click(sender As Object, e As EventArgs) Handles btnAddExpRdc.Click
        Dim selectedRow As DataRow = TryCast(VendorPopupHelper.GetPopupReturnValue(Me, "OnePayment"), DataRow)
        If selectedRow Is Nothing Then Exit Sub   ' AddFormula closed without saving

        Dim dt As DataTable = GridView2Data
        If dt Is Nothing Then
            ' No plan loaded yet: start a table with the popup's columns
            dt = selectedRow.Table.Clone()
        End If

        ' Copy by column name, so it fits a plan's table too (whose DETAIL_ID stays empty
        ' for a new line)
        Dim newRow As DataRow = dt.NewRow()
        For Each col As DataColumn In selectedRow.Table.Columns
            If dt.Columns.Contains(col.ColumnName) Then newRow(col.ColumnName) = selectedRow(col.ColumnName)
        Next
        dt.Rows.Add(newRow)

        BindGrid(dt)
        lblScheduleMessage.Visible = False
    End Sub


    '''' <summary>Binds the grid to Dt (Nothing = empty) and remembers it for later postbacks.</summary>
    'Private Sub BindGrid(Dt As DataTable)
    '    GridView2Data = Dt
    '    GridView2.DataSource = Dt
    '    GridView2.DataBind()
    '    Session("ExistingPaymentTitles") = If(Dt Is Nothing, Nothing, GetExistingTitles())
    'End Sub

    '''' <summary>
    '''' Fires when the user changes the Project dropdown. Re-loads GridView2 with
    '''' whatever payment-schedule detail rows already exist for that project.
    '''' </summary>
    'Protected Sub ddlProject_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ddlProject.SelectedIndexChanged
    '    ShowScheduleMessage("", True)
    '    LoadPaymentScheduleForProject(ddlProject.SelectedValue)
    'End Sub

    '''' <summary>
    '''' Queries the schedule + detail tables for the given ProjectID and binds the
    '''' result to GridView2. If no project is selected, the grid is cleared.
    '''' </summary>
    'Private Sub LoadPaymentScheduleForProject(projectId As String)
    '    If String.IsNullOrEmpty(projectId) OrElse projectId = "NoValue" Then
    '        BindGrid(Nothing)
    '        Exit Sub
    '    End If

    '    ' Existing schedule detail rows for this project, most recent schedule first,
    '    ' in their original row order.
    '    Dim sql As String =
    '        "SELECT d.TITLE AS Title, d.PAYMENT AS Payment, d.LCCOMMENT AS ""Comment"" " &
    '        "FROM SELECT_PAYMENTSCHEDULEDETAIL d " &
    '        "INNER JOIN Select_PaymentSchedule s ON s.SCHEDULEID = d.SCHEDULEID " &
    '        "WHERE s.PROJECTID = '" & projectId.Replace("'", "''") & "' " &
    '        "ORDER BY s.SCHEDULEID DESC, d.ROWORDER"

    '    ' NOTE: UDF.GetDataTable is assumed to mirror UDF.GetSqlArray/UDF.GetNextID
    '    ' (same UDF.EBDB_CS connection string), as in SetupPaymentSchedule.
    '    BindGrid(UDF.GetDataTable(UDF.EBDB_CS, sql))
    'End Sub

    ''' <summary>
    ''' The bin icon of a row (CommandName "DeleteRow", CommandArgument = the row's
    ''' position): removes that row from the grid. The grid's "#" renumbers itself.
    ''' Nothing is deleted from the database here - only from the grid being edited.
    ''' </summary>
    Protected Sub GridView2_RowCommand(sender As Object, e As GridViewCommandEventArgs) Handles GridView2.RowCommand
        If e.CommandName <> "DeleteRow" Then Exit Sub

        Dim rowIndex As Integer
        If Not Integer.TryParse(Convert.ToString(e.CommandArgument), rowIndex) Then Exit Sub

        Dim dt As DataTable = GridView2Data
        If dt Is Nothing OrElse rowIndex < 0 OrElse rowIndex >= dt.Rows.Count Then Exit Sub

        dt.Rows.RemoveAt(rowIndex)
        dt.AcceptChanges()
        BindGrid(dt)
        lblScheduleMessage.Visible = False
    End Sub

    '''' <summary>
    '''' Collects every Title currently in GridView2, so AddFormula.aspx can check
    '''' against them (e.g. to block adding a duplicate).
    '''' </summary>
    'Private Function GetExistingTitles() As List(Of String)
    '    Dim titles As New List(Of String)
    '    Dim dt As DataTable = GridView2Data
    '    If dt IsNot Nothing Then
    '        For Each row As DataRow In dt.Rows
    '            titles.Add(Convert.ToString(row("Title")))
    '        Next
    '    End If
    '    Return titles
    'End Function

    '''' <summary>Runs when AddFormula closes: appends the row it returned (key "OnePayment").</summary>
    'Protected Sub btnAddExpRdc_Click(sender As Object, e As EventArgs) Handles btnAddExpRdc.Click
    '    Dim returnValue As Object = VendorPopupHelper.GetPopupReturnValue(Me, "OnePayment")
    '    If returnValue Is Nothing Then Exit Sub

    '    Dim selectedRow As DataRow = TryCast(returnValue, DataRow)
    '    If selectedRow Is Nothing Then Exit Sub

    '    Dim dt As DataTable = GridView2Data
    '    If dt Is Nothing Then
    '        ' First item added - start a new table with the same columns as the
    '        ' popup's row, but none of its other rows.
    '        dt = selectedRow.Table.Clone()
    '    End If

    '    dt.ImportRow(selectedRow)
    '    BindGrid(dt)
    '    ShowScheduleMessage("", True)
    'End Sub

    ''' <summary>
    ''' Save, by option:
    '''   Add New : creates the plan in UNITSHUB_PAYMENTPLAN (PLAN_ID = next in line,
    '''             NAME = "New plan name"), then saves the grid as its lines.
    '''   Edit    : saves the grid as the selected plan's lines (the old lines are closed).
    '''   Copy    : like Add New, with the lines shown in the grid - the copied plan's
    '''             lines, plus any changes made to them.
    ''' Afterwards the plan is shown in Edit mode.
    ''' </summary>
    Protected Sub btnSave_Click(sender As Object, e As EventArgs) Handles btnSave.Click
        Dim mode As String = SelectedMode
        Dim dt As DataTable = GridView2Data

        ' --- checks ---
        If mode = "Edit" OrElse mode = "Copy" Then
            If ddlExistingPlan.SelectedValue = "" Then
                ShowScheduleMessage(If(mode = "Copy", "Select the plan to copy.", "Select a payment plan first."), False)
                Exit Sub
            End If
        End If

        Dim newName As String = txtNewPlanName.Text.Trim()
        If mode = "New" OrElse mode = "Copy" Then
            If newName = "" Then
                ShowScheduleMessage("Enter the name of the new plan.", False)
                Exit Sub
            End If
            If PlanNameExists(newName) Then
                ShowScheduleMessage("A plan named '" & newName & "' already exists. Choose another name.", False)
                Exit Sub
            End If
        End If

        If dt Is Nothing OrElse dt.Rows.Count = 0 Then
            ShowScheduleMessage("Add at least one line before saving.", False)
            Exit Sub
        End If
        For i As Integer = 0 To dt.Rows.Count - 1
            If Convert.ToString(dt.Rows(i)("Description")).Trim() = "" OrElse
               Convert.ToString(dt.Rows(i)("Formula")).Trim() = "" Then
                ShowScheduleMessage("Line " & (i + 1) & " needs both a Description and a Formula.", False)
                Exit Sub
            End If
        Next

        ' --- Edit: replace the selected plan's lines ---
        If mode = "Edit" Then
            Dim planId As String = ddlExistingPlan.SelectedValue
            Dim problem As String = SavePlanLines(planId, dt)
            If problem <> "" Then
                ShowScheduleMessage(problem, False)
                Exit Sub
            End If
            LoadPlanDetails(planId)
            ShowScheduleMessage(dt.Rows.Count & If(dt.Rows.Count = 1, " line", " lines") &
                                " saved for " & ddlExistingPlan.SelectedItem.Text & ".", True)
            Exit Sub
        End If

        ' --- Add New / Copy: a new plan with the next PLAN_ID, then its lines ---
        Dim newPlanId As String
        Try
            newPlanId = GetNextPlanId()
            DB.ExecuteNonQuery(EBDB_CS,
                "INSERT INTO UNITSHUB_PAYMENTPLAN (PLAN_ID, NAME) VALUES ('" &
                newPlanId.Replace("'", "''") & "', '" & newName.Replace("'", "''") & "')")
        Catch ex As Exception
            ShowScheduleMessage("The plan couldn't be created: " & ex.Message, False)
            Exit Sub
        End Try

        Dim linesProblem As String = SavePlanLines(newPlanId, dt)
        If linesProblem <> "" Then
            ' Don't leave a plan without lines behind
            Try
                DB.ExecuteNonQuery(EBDB_CS, "DELETE FROM UNITSHUB_PAYMENTPLAN WHERE PLAN_ID = '" & newPlanId.Replace("'", "''") & "'")
            Catch
            End Try
            ShowScheduleMessage(linesProblem, False)
            Exit Sub
        End If

        Dim copiedFrom As String = If(mode = "Copy", " (copied from " & ddlExistingPlan.SelectedItem.Text & ")", "")

        ' Show the new plan, ready to edit
        LoadExistingPlans()
        rblMode.SelectedValue = "Edit"
        ddlExistingPlan.SelectedValue = newPlanId
        txtNewPlanName.Text = ""
        LoadPlanDetails(newPlanId)
        ShowScheduleMessage("Plan '" & newName & "' created (ID " & newPlanId & ") with " &
                            dt.Rows.Count & If(dt.Rows.Count = 1, " line", " lines") & copiedFrom & ".", True)
    End Sub

    ''' <summary>
    ''' Writes the grid as a plan's lines in UNITSHUB_PAYMENTPLANDETAILS:
    '''   DETAIL_ID = the row's # (1, 2, 3 ...), PLAN_ID = PlanId,
    '''   DESCRIPTION / FORMULA = the grid's columns,
    '''   START_FROM = now in Unix seconds (UTC, the same for every row), END_IN = empty.
    ''' Then the plan's previously active lines (END_IN empty) are closed with END_IN = the
    ''' same moment. If an insert fails, this save's rows are removed and the old lines stay
    ''' as they were. Returns "" when all went well, otherwise what went wrong.
    ''' </summary>
    Private Function SavePlanLines(PlanId As String, dt As DataTable) As String
        Dim safePlan As String = PlanId.Replace("'", "''")
        Dim startFrom As String = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()
        Dim inserted As Integer = 0

        Try
            For i As Integer = 0 To dt.Rows.Count - 1
                Dim detailId As String = (i + 1).ToString()
                Dim description As String = Convert.ToString(dt.Rows(i)("Description")).Trim().Replace("'", "''")
                Dim formula As String = Convert.ToString(dt.Rows(i)("Formula")).Trim().Replace("'", "''")

                DB.ExecuteNonQuery(EBDB_CS,
                    "INSERT INTO UNITSHUB_PAYMENTPLANDETAILS (DETAIL_ID, PLAN_ID, DESCRIPTION, FORMULA, START_FROM, END_IN) " &
                    "VALUES ('" & detailId & "', '" & safePlan & "', '" & description & "', '" & formula & "', '" & startFrom & "', NULL)")
                inserted += 1
            Next
        Catch ex As Exception
            If inserted > 0 Then
                Try
                    DB.ExecuteNonQuery(EBDB_CS,
                        "DELETE FROM UNITSHUB_PAYMENTPLANDETAILS " &
                        "WHERE PLAN_ID = '" & safePlan & "' AND START_FROM = '" & startFrom & "' AND END_IN IS NULL")
                Catch
                End Try
            End If
            Return "The lines couldn't be saved (nothing was changed): " & ex.Message
        End Try

        Try
            DB.ExecuteNonQuery(EBDB_CS,
                "UPDATE UNITSHUB_PAYMENTPLANDETAILS SET END_IN = '" & startFrom & "' " &
                "WHERE PLAN_ID = '" & safePlan & "' AND END_IN IS NULL " &
                "AND (START_FROM IS NULL OR START_FROM <> '" & startFrom & "')")
        Catch ex As Exception
            Return "The new lines were saved, but the previous ones couldn't be closed: " & ex.Message
        End Try

        Return ""
    End Function

    Private Sub ShowScheduleMessage(Text As String, IsOk As Boolean)
        lblScheduleMessage.Text = Server.HtmlEncode(Text)
        lblScheduleMessage.CssClass = "schedule-message " & If(IsOk, "ok", "err")
        lblScheduleMessage.Visible = (Text <> "")
    End Sub

    ''' <summary>POPUP close: closes without saving anything.</summary>
    Protected Sub imgClose_Click(sender As Object, e As ImageClickEventArgs) Handles imgClose.Click
        VendorPopupHelper.RegisterPopupSelectionAndClose(Me, False, skipPostBack:=False)
    End Sub
End Class