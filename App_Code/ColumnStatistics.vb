Imports Microsoft.VisualBasic

Public Class ColumnStatistics

    Public Property ColumnName As String
    Public Property DataType As Type

    Public Property TotalRows As Integer
    Public Property NonNullRows As Integer
    Public Property NullRows As Integer
    Public Property NullPercent As Double

    Public Property DistinctValues As Integer
    Public Property DistinctRatio As Double

    Public Property MaxLength As Integer
    Public Property MinLength As Integer = Integer.MaxValue
    Public Property AverageLength As Double

    Public Property ContainsNumbers As Boolean
    Public Property ContainsDates As Boolean
    Public Property ContainsBoolean As Boolean
    Public Property ContainsNulls As Boolean

    Public Property NumericCount As Integer
    Public Property DateCount As Integer
    Public Property BooleanCount As Integer
    Public Property TextCount As Integer

    Public Property MinNumber As Double?
    Public Property MaxNumber As Double?

    Public Property MinDate As Date?
    Public Property MaxDate As Date?

    Public Property MostCommonValue As String
    Public Property MostCommonCount As Integer

    Public Property IsUnique As Boolean
    Public Property IsLikelyPrimaryKey As Boolean

    Public Property SuggestedFilter As FilterType

End Class