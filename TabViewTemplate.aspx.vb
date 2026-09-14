Imports System.Data
Imports System.DateTime
Imports System.Drawing
Imports System.Globalization

Partial Class TabViewTemplate
    Inherits System.Web.UI.Page

    Protected Sub Menu3_MenuItemClick(sender As Object, e As MenuEventArgs) Handles Menu3.MenuItemClick
        Try
            MultiView3.ActiveViewIndex = Menu3.Items.IndexOf(Menu3.SelectedItem)

        Catch ex As Exception

        End Try
    End Sub
End Class

