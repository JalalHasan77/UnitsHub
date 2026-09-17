
Imports System.Data

Partial Class ConfirmBox
    Inherits System.Web.UI.Page

    Protected Sub btnCancel_Click(sender As Object, e As EventArgs) Handles btnCancel.Click
        Dim script As String = "(function () {" &
                       "    if (window.parent && typeof window.parent.closeVendorDialog === 'function') {" &
                       "        window.parent.closeVendorDialog();" &
                       "    }" &
                       "})();"

        If ScriptManager.GetCurrent(Me) IsNot Nothing Then
            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "ClosePopupOnly", script, True)
        Else
            Me.ClientScript.RegisterStartupScript(Me.GetType(), "ClosePopupOnly", script, True)
        End If

    End Sub
    Protected Sub btnOK_Click(sender As Object, e As EventArgs) Handles btnOK.Click
        'Dim returnValue As New DataTable("returnValue")

        '' Define columns
        'returnValue.Columns.Add("ID", GetType(Integer))
        'returnValue.Columns.Add("Name", GetType(String))
        'returnValue.Columns.Add("Value", GetType(String))

        '' Add sample rows
        'returnValue.Rows.Add(1, "S. Jalal Hasan", "Husband")
        'returnValue.Rows.Add(2, "Elmeera Yousif", "Wife")

        Dim returnValue As Boolean = True
        Dim returnKey As String = VendorPopupHelper.GetPopupReturnKey(Me)

        VendorPopupHelper.RegisterPopupSelectionAndClose(
            page:=Me,
            returnValue:=returnValue,
            startupScriptKey:=returnKey,
            skipPostBack:=False)
    End Sub
End Class
