Imports System.Data
Imports System.Collections.Generic

''' <summary>
''' Popup page: sets up payment plans (UNITSHUB_PAYMENTPLAN) and their lines
''' (UNITSHUB_PAYMENTPLANDETAILS).
'''
''' Top options:
'''   Add New : "New plan name" box; Save creates the plan (next PLAN_ID) and its lines.
'''   Edit    : plan dropdown; picking a plan shows its active lines; Save replaces them.
'''   Copy    : plan dropdown + "New plan name"; picking a plan shows its lines;
'''             Save creates a new plan (next PLAN_ID) with the lines in the grid.
'''
''' ADD    : the row chosen in AddFormula is appended to the grid (btnAddExpRdc_Click).
''' DELETE : the bin icon removes a row from the grid (not from the database).
''' The grid's rows are kept in ViewState (GridView2Data) between postbacks, and their
''' descriptions in Session("ExistingPaymentTitles") so AddFormula can block duplicates.
''' </summary>
Partial Class PaymentSchedule
    Inherits System.Web.UI.Page

    ''' <summary>
    ''' The DataTable currently bound to GridView2, persisted in ViewState so it
    ''' survives postbacks (needed so Add / Delete / Save see the current rows).
    ''' </summary>
    Private Property GridView2Data As DataTable
        Get
            Return TryCast(ViewState("GridView2Data"), DataTable)
        End Get
        Set(value As DataTable)
            ViewState("GridView2Data") = value
        End Set
    End Property

    ''' <summary>
    ''' SQL condition for a plan line that is still active (not closed).
    ''' Lines saved by this page have END_IN = NULL; lines from elsewhere may use
    ''' blanks or '0', so those count as active too. Used for loading AND saving,
    ''' so both always agree on which lines are "current".
    ''' </summary>
    Private Shared Function ActiveLineCondition(Optional TableAlias As String = "") As String
        Dim c As String = If(TableAlias = "", "END_IN", TableAlias & ".END_IN")
        Return "(" & c & " IS NULL OR TRIM(" & c & ") IS NULL OR TRIM(" & c & ") = '0')"
    End Function

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

        Dim DT As DataTable = GetDataTable(EBDB, "SELECT PLAN_ID, NAME FROM UNITSHUB_PAYMENTPLAN ORDER BY PLAN_ID")
        If DT IsNot Nothing Then
            For Each r As DataRow In DT.Rows
                ddlExistingPlan.Items.Add(New ListItem(Convert.ToString(r("NAME")), Convert.ToString(r("PLAN_ID"))))
            Next
        End If
    End Sub

    ' ==================================================================
    ' Top options: Add New / Edit Existing / Copy Existing Schedule
    ' ==================================================================

    Private ReadOnly Property SelectedMode As String
        Get
            Dim m As String = Convert.ToString(rblMode.SelectedValue)
            Return If(m = "", "New", m)
        End Get
    End Property

    ''' <summary>Switching option starts clean: no plan picked, empty name, empty grid.</summary>
    Protected Sub rblMode_SelectedIndexChanged(sender As Object, e As EventArgs) Handles rblMode.SelectedIndexChanged
        If ddlExistingPlan.Items.Count = 0 Then LoadExistingPlans()
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
    ''' Fires when a plan is picked in ddlExistingPlan: loads that plan's active lines
    ''' into GridView2. "-- Select a plan --" clears the grid.
    ''' </summary>
    Protected Sub ddlExistingPlan_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ddlExistingPlan.SelectedIndexChanged
        LoadPlanDetails(ddlExistingPlan.SelectedValue)

        ' Copy: suggest a name for the new plan
        If SelectedMode = "Copy" AndAlso ddlExistingPlan.SelectedValue <> "" AndAlso txtNewPlanName.Text.Trim() = "" Then
            txtNewPlanName.Text = "Copy of " & ddlExistingPlan.SelectedItem.Text
        End If
    End Sub

    ''' <summary>
    ''' The plan's active lines, named for GridView2's columns (and AddFormula's returned
    ''' row): "Description", "Formula", and an empty "Comment". DETAIL_ID comes along too.
    ''' </summary>
    Private Sub LoadPlanDetails(PlanId As String)
        lblScheduleMessage.Visible = False

        If String.IsNullOrEmpty(PlanId) Then
            BindGrid(Nothing)
            Exit Sub
        End If

        Dim SQL As String = ""
        SQL &= " SELECT D.DETAIL_ID, " & vbCrLf
        SQL &= "        D.DESCRIPTION AS ""Description"", " & vbCrLf
        SQL &= "        D.FORMULA     AS ""Formula"", " & vbCrLf
        SQL &= "        CAST(NULL AS VARCHAR2(200)) AS ""Comment"" " & vbCrLf
        SQL &= " FROM   UNITSHUB_PAYMENTPLANDETAILS D " & vbCrLf
        SQL &= " WHERE  D.PLAN_ID = '" & PlanId.Replace("'", "''") & "' " & vbCrLf
        SQL &= "   AND  " & ActiveLineCondition("D") & " " & vbCrLf
        ' DETAIL_ID as a number when it is one, so 10 comes after 9
        SQL &= " ORDER BY CASE WHEN REGEXP_LIKE(D.DETAIL_ID, '^[0-9]+$') THEN TO_NUMBER(D.DETAIL_ID) END, D.DETAIL_ID "

        Try
            Dim dt As DataTable = GetDataTable(EBDB, SQL)
            BindGrid(dt)

            If dt Is Nothing OrElse dt.Rows.Count = 0 Then
                ShowScheduleMessage("This plan has no active lines.", False)
            End If
        Catch ex As Exception
            BindGrid(Nothing)
            ShowScheduleMessage("The plan's lines couldn't be loaded: " & ex.Message, False)
        End Try
    End Sub

    ''' <summary>Binds the grid to Dt (Nothing = empty) and remembers it for later postbacks.</summary>
    Private Sub BindGrid(Dt As DataTable)
        ' A DataTable must have a name to be stored in ViewState
        ' ("Cannot serialize the DataTable. DataTable name is not set.")
        If Dt IsNot Nothing AndAlso String.IsNullOrEmpty(Dt.TableName) Then Dt.TableName = "PlanLines"

        GridView2Data = Dt
        GridView2.DataSource = Dt
        GridView2.DataBind()
        Session("ExistingPaymentTitles") = If(Dt Is Nothing, Nothing, GetExistingTitles())
    End Sub

    ''' <summary>
    ''' Collects every Description currently in GridView2, so AddFormula.aspx can check
    ''' against them (e.g. to block adding a duplicate).
    ''' </summary>
    Private Function GetExistingTitles() As List(Of String)
        Dim titles As New List(Of String)
        Dim dt As DataTable = GridView2Data
        If dt IsNot Nothing AndAlso dt.Columns.Contains("Description") Then
            For Each row As DataRow In dt.Rows
                titles.Add(Convert.ToString(row("Description")))
            Next
        End If
        Return titles
    End Function

    ''' <summary>
    ''' Runs when AddFormula closes with Save: appends the row it returned (key
    ''' "OnePayment": Description, Formula, Comment) to the end of the grid.
    ''' </summary>
    Protected Sub btnAddExpRdc_Click(sender As Object, e As EventArgs) Handles btnAddExpRdc.Click
        Dim selectedRow As DataRow = TryCast(VendorPopupHelper.GetPopupReturnValue(Me, "OnePayment"), DataRow)
        If selectedRow Is Nothing Then Exit Sub   ' AddFormula closed without saving

        Dim dt As DataTable = GridView2Data
        If dt Is Nothing Then
            ' Nothing loaded yet: start a table with the popup's columns
            dt = selectedRow.Table.Clone()
        End If

        ' Copy by column name, so it fits a plan's table too (DETAIL_ID stays empty for a new line)
        Dim newRow As DataRow = dt.NewRow()
        For Each col As DataColumn In selectedRow.Table.Columns
            If dt.Columns.Contains(col.ColumnName) Then newRow(col.ColumnName) = selectedRow(col.ColumnName)
        Next
        dt.Rows.Add(newRow)

        BindGrid(dt)
        lblScheduleMessage.Visible = False
    End Sub

    ''' <summary>
    ''' The bin icon of a row (CommandName "DeleteRow", CommandArgument = the row's
    ''' position): removes that row from the grid only. The "#" column renumbers itself.
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

    ''' <summary>
    ''' Save, by option:
    '''   Add New : creates the plan (next PLAN_ID, NAME = "New plan name") and its lines.
    '''   Edit    : saves the grid as the selected plan's lines (the old lines are closed).
    '''   Copy    : like Add New, with the lines shown in the grid.
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

        Dim lineCount As String = dt.Rows.Count & If(dt.Rows.Count = 1, " line", " lines")

        ' --- Edit: replace the selected plan's lines ---
        If mode = "Edit" Then
            Dim planId As String = ddlExistingPlan.SelectedValue
            Dim planName As String = ddlExistingPlan.SelectedItem.Text
            Dim problem As String = SavePlanLines(planId, dt)
            If problem <> "" Then
                ShowScheduleMessage(problem, False)
                Exit Sub
            End If
            LoadPlanDetails(planId)
            ShowScheduleMessage(lineCount & " saved for " & planName & ".", True)
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
        ShowScheduleMessage("Plan '" & newName & "' created (ID " & newPlanId & ") with " & lineCount & copiedFrom & ".", True)
    End Sub

    ''' <summary>
    ''' Writes the grid as a plan's lines in UNITSHUB_PAYMENTPLANDETAILS:
    '''   DETAIL_ID = the row's # (1, 2, 3 ...), PLAN_ID = PlanId,
    '''   DESCRIPTION / FORMULA = the grid's columns,
    '''   START_FROM = now in Unix seconds (UTC, same for every row), END_IN = NULL.
    ''' Then the plan's previously active lines are closed with END_IN = the same moment.
    ''' If an insert fails, this save's rows are removed and the old lines stay as they were.
    ''' Returns "" when all went well, otherwise what went wrong.
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

        ' Close the previous active lines (same "active" rule as LoadPlanDetails)
        Try
            DB.ExecuteNonQuery(EBDB_CS,
                "UPDATE UNITSHUB_PAYMENTPLANDETAILS SET END_IN = '" & startFrom & "' " &
                "WHERE PLAN_ID = '" & safePlan & "' AND " & ActiveLineCondition() & " " &
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
