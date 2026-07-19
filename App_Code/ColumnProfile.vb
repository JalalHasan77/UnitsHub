Imports Microsoft.VisualBasic

Public Class ColumnProfile

    ' Basic information
    Public Property ColumnName As String
    Public Property DataType As Type

    ' Row statistics
    Public Property TotalRows As Integer
    Public Property NullRows As Integer
    Public Property NonNullRows As Integer

    ' Value statistics
    Public Property DistinctValues As Integer
    Public Property DistinctRatio As Double

    ' Text statistics
    Public Property MinLength As Integer
    Public Property MaxLength As Integer
    Public Property AverageLength As Double

    ' Content detection
    Public Property ContainsNumbers As Boolean
    Public Property ContainsDates As Boolean
    Public Property ContainsBoolean As Boolean
    Public Property ContainsNulls As Boolean

    ' Numeric statistics
    Public Property MinimumNumber As Nullable(Of Double)
    Public Property MaximumNumber As Nullable(Of Double)
    Public Property AverageNumber As Nullable(Of Double)

    ' Date statistics
    Public Property MinimumDate As Nullable(Of DateTime)
    Public Property MaximumDate As Nullable(Of DateTime)

    ' Most common values
    Public Property MostCommonValue As String
    Public Property MostCommonCount As Integer

    ' Suggested UI filter
    Public Property SuggestedFilter As FilterType


    Public Overrides Function ToString() As String
        Return ColumnName & " (" & DataType.Name & ")"
    End Function

End Class
