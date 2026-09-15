Imports System.Data
Imports System.DateTime
Imports System.Drawing
Imports System.Globalization

Partial Class ContactMaintenance
    Inherits System.Web.UI.Page

    Protected Sub Menu3_MenuItemClick(sender As Object, e As MenuEventArgs) Handles Menu3.MenuItemClick
        Try
            MultiView3.ActiveViewIndex = Menu3.Items.IndexOf(Menu3.SelectedItem)

        Catch ex As Exception

        End Try
    End Sub

    Protected Sub calDOB_SelectionChanged(sender As Object, e As EventArgs) Handles calDOB.SelectionChanged
        Try
            txtDOB.Text = calDOB.SelectedDate.ToString("dd/MM/yyyy")
            CalculateAge()

            ' Keep the calendar hidden again after a date has been picked
            pnlCalendar.Style("display") = "none"

        Catch ex As Exception

        End Try
    End Sub

    Protected Sub calIssueDate_SelectionChanged(sender As Object, e As EventArgs) Handles calIssueDate.SelectionChanged
        Try
            txtIssueDate.Text = calIssueDate.SelectedDate.ToString("dd/MM/yyyy")

            ' Keep the calendar hidden again after a date has been picked
            pnlCalendarIssue.Style("display") = "none"

        Catch ex As Exception

        End Try
    End Sub

    Protected Sub calExpiryDate_SelectionChanged(sender As Object, e As EventArgs) Handles calExpiryDate.SelectionChanged
        Try
            txtExpiryDate.Text = calExpiryDate.SelectedDate.ToString("dd/MM/yyyy")

            ' Keep the calendar hidden again after a date has been picked
            pnlCalendarExpiry.Style("display") = "none"

        Catch ex As Exception

        End Try
    End Sub

    Protected Sub btnCalculateAge_Click(sender As Object, e As EventArgs) Handles btnCalculateAge.Click
        CalculateAge()
    End Sub

    Protected Sub lnkContactDetails_Click(sender As Object, e As EventArgs) Handles lnkContactDetails.Click
        Try
            MultiView3.ActiveViewIndex = 1
            Menu3.Items(1).Selected = True

        Catch ex As Exception

        End Try
    End Sub

    Protected Sub btnLoad_Click(sender As Object, e As EventArgs) Handles btnLoad.Click
        ' TODO: load an existing contact's saved details back into the form fields
    End Sub

    Protected Sub btnCancel_Click(sender As Object, e As EventArgs) Handles btnCancel.Click
        ' TODO: navigate away from this page / discard changes as appropriate
        lblMessage.Visible = False
    End Sub

    Protected Sub btnSave_Click(sender As Object, e As EventArgs) Handles btnSave.Click
        Try
            If Not Page.IsValid Then
                Return
            End If

            ' TODO: persist the General / Contact Details / Address values captured
            ' across View1, View2 and View3 to the database

            lblMessage.Text = "Contact details saved successfully."
            lblMessage.Visible = True

        Catch ex As Exception
            lblMessage.Text = "An error occurred while saving: " & ex.Message
            lblMessage.Visible = True
        End Try
    End Sub

    Private Sub CalculateAge()
        Dim dob As Date

        If Date.TryParseExact(txtDOB.Text, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, dob) Then
            Dim today As Date = Date.Today
            Dim age As Integer = today.Year - dob.Year

            If today.Month < dob.Month OrElse (today.Month = dob.Month AndAlso today.Day < dob.Day) Then
                age -= 1
            End If

            If age < 0 Then
                txtAge.Text = ""
            Else
                txtAge.Text = age.ToString()
            End If
        Else
            txtAge.Text = ""
        End If
    End Sub
End Class

