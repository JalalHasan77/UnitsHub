
Partial Class [Try]
    Inherits System.Web.UI.Page

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Load
        If Not IsPostBack Then
            LoadInitialValues()
        End If
    End Sub

    ''' <summary>
    ''' Fills ListBox1 with the starting values. Only called on the initial load (not on
    ''' postback) so items added by Button1_Click persist via ViewState instead of being
    ''' wiped out on every round-trip.
    ''' </summary>
    Private Sub LoadInitialValues()
        Dim initialValues As Integer() = {0, 9216} '  {1024, 9216} '2048, 3072, 4096, 5120, 6144, 7168, 8192, 9216}

        ListBox1.Items.Clear()
        For Each v As Integer In initialValues
            ListBox1.Items.Add(New ListItem(v.ToString(), v.ToString()))
        Next
    End Sub

    ''' <summary>
    ''' Computes the average of the two selected items and inserts it into the list in
    ''' ascending numeric order (i.e. between them, regardless of how far apart they are).
    ''' </summary>
    Protected Sub Button1_Click(ByVal sender As Object, ByVal e As EventArgs)
        Dim L As New List(Of Integer)
        L.Add(1024)
        L.Add(5120)
        GenerateIntegerAverages(L, 0)
    End Sub
    'Dim selectedItems As New List(Of ListItem)

    'For Each item As ListItem In ListBox1.Items
    '    If item.Selected Then
    '        selectedItems.Add(item)
    '    End If
    'Next

    '' The client-side script caps selection at 2, but guard here too in case
    '' JavaScript is disabled or the postback happened with a different selection.
    'If selectedItems.Count <> 2 Then
    '    Return
    'End If

    'Dim value1 As Double = Double.Parse(selectedItems(0).Value)
    'Dim value2 As Double = Double.Parse(selectedItems(1).Value)
    'Dim average As Double = (value1 + value2) / 2

    '' Avoid inserting a duplicate if this average is already in the list.
    'If ListBox1.Items.FindByValue(average.ToString()) IsNot Nothing Then
    '    Return
    'End If

    'Dim newItem As New ListItem(average.ToString(), average.ToString())

    'Dim insertIndex As Integer = ListBox1.Items.Count
    'For i As Integer = 0 To ListBox1.Items.Count - 1
    '    If Double.Parse(ListBox1.Items(i).Value) > average Then
    '        insertIndex = i
    '        Exit For
    '    End If
    'Next
    'newItem.Text = " " & newItem.Text
    'ListBox1.Items.Insert(insertIndex, newItem)


    Public Function GenerateIntegerAverages(ByRef L As List(Of Integer),
                                            StartPosition As Integer) As List(Of Integer)
        If StartPosition + 1 >= L.Count Then Exit Function

        Dim AVG As Decimal = (L(StartPosition) + L(StartPosition + 1)) / 2

            If AVG <> Math.Truncate(AVG) Then
                StartPosition = StartPosition + 1
            Else
                L.Add(AVG)
                L.Sort()
            End If
            GenerateIntegerAverages(L, StartPosition)






    End Function

End Class
