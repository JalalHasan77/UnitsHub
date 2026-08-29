Imports Microsoft.VisualBasic

Public Class clsListProperties
    Public Property ItemsSQL As String
    Public Property CheckedItemsSQL As String
    Public Property FormTitle As String
    Public Property ColumnHideAndShow As String
    Public Property EditableColumns As String
    Public Property ColumnsWidth As Double()
    Public Property HoverableList As String

End Class

Public Class clsListPropertiesNoSQL
    Public Property FormTitle As String
    Public Property ColumnHideAndShow As String
    Public Property EditableColumns As String
    Public Property ColumnsWidth As Double()
    Public Property HoverableList As String
    Public Property WholeList As List(Of String)
    Public Property CheckedList As List(Of String)
End Class





