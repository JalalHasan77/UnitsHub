Imports System.Data
Imports System.Linq

Public Class DataProfiler

    Public Shared Function Analyze(dt As DataTable) As List(Of ColumnProfile)

        Dim Profiles As New List(Of ColumnProfile)

        For Each col As DataColumn In dt.Columns

            Dim profile As New ColumnProfile()

            profile.ColumnName = col.ColumnName
            profile.DataType = col.DataType

            profile.TotalRows = dt.Rows.Count


            Dim values = dt.AsEnumerable().
                Select(Function(r) r(col)).
                Where(Function(v) Not IsDBNull(v)).
                ToList()


            profile.NullRows = dt.Rows.Count - values.Count
            profile.NonNullRows = values.Count

            profile.ContainsNulls = profile.NullRows > 0


            If values.Count > 0 Then

                'Distinct values
                profile.DistinctValues =
                    values.Select(Function(x) x.ToString()).
                    Distinct().
                    Count()


                profile.DistinctRatio =
                    Math.Round(
                    profile.DistinctValues / values.Count,
                    2)


                'Most common value
                Dim common =
                    values.GroupBy(Function(x) x.ToString()).
                    OrderByDescending(Function(g) g.Count()).
                    FirstOrDefault()


                If common IsNot Nothing Then

                    profile.MostCommonValue = common.Key
                    profile.MostCommonCount = common.Count()

                End If


                AnalyzeContent(profile, values)

            End If


            profile.SuggestedFilter =
                DetectFilterType(profile)


            Profiles.Add(profile)

        Next


        Return Profiles

    End Function



    Private Shared Sub AnalyzeContent(
        profile As ColumnProfile,
        values As List(Of Object))


        'String analysis
        If profile.DataType = GetType(String) Then


            Dim lengths =
                values.Select(Function(x) x.ToString.Length)


            profile.MinLength = lengths.Min()
            profile.MaxLength = lengths.Max()
            profile.AverageLength =
                Math.Round(lengths.Average(), 2)


            profile.ContainsNumbers =
                values.Any(Function(x) Double.TryParse(x.ToString(), Nothing))


            profile.ContainsDates =
                values.Any(Function(x) DateTime.TryParse(x.ToString(), Nothing))


        End If



        'Numeric analysis
        If IsNumericType(profile.DataType) Then

            Dim nums =
                values.Select(Function(x) Convert.ToDouble(x))


            profile.MinimumNumber = nums.Min()
                                  profile.MaximumNumber = nums.Max()
                                  profile.AverageNumber =
                                      Math.Round(nums.Average(), 2)

        End If



        'Date analysis
        If profile.DataType = GetType(DateTime) Then

            Dim dates =
                values.Select(Function(x) Convert.ToDateTime(x))


            profile.MinimumDate = dates.Min()
                                  profile.MaximumDate = dates.Max()

                                  profile.ContainsDates = True

        End If


    End Sub



    Private Shared Function IsNumericType(t As Type) As Boolean

        Return t = GetType(Integer) OrElse
               t = GetType(Long) OrElse
               t = GetType(Decimal) OrElse
               t = GetType(Double) OrElse
               t = GetType(Single)

    End Function



    Private Shared Function DetectFilterType(
        profile As ColumnProfile) As FilterType


        'Dates
        If profile.ContainsDates _
            OrElse profile.DataType = GetType(DateTime) Then

            Return FilterType.DatePicker

        End If



        'Numbers
        If IsNumericType(profile.DataType) Then

            Return FilterType.NumericRange

        End If



        'Few unique values
        If profile.DistinctValues <= 15 Then

            Return FilterType.DropDownList

        End If



        'Medium number of unique values
        If profile.DistinctRatio < 0.3 Then

            Return FilterType.AutoComplete

        End If



        'Large text
        If profile.MaxLength > 100 Then

            Return FilterType.TextBox

        End If



        Return FilterType.TextBox


    End Function


End Class