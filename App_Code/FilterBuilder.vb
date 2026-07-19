Imports System.Web.UI
Imports System.Web.UI.WebControls


Public Class FilterBuilder


    Public Shared Sub BuildFilters(
        container As Control,
        profiles As List(Of ColumnProfile))


        container.Controls.Clear()


        For Each lcProfile In profiles


            Dim filterControl As Control = Nothing


            Select Case lcProfile.SuggestedFilter


                Case FilterType.DropDownList

                    filterControl =
                        CreateDropDown(lcProfile)


                Case FilterType.DatePicker

                    filterControl =
                        CreateDateFilter(lcProfile)


                Case FilterType.NumericRange

                    filterControl =
                        CreateNumericFilter(lcProfile)


                Case FilterType.AutoComplete

                    filterControl =
                        CreateTextBox(lcProfile)


                Case FilterType.TextBox

                    filterControl =
                        CreateTextBox(lcProfile)


            End Select



            If filterControl IsNot Nothing Then

                Dim wrapper As New Panel()

                wrapper.CssClass = "filter-item"


                Dim lbl As New Label()

                lbl.Text = lcProfile.ColumnName
                lbl.CssClass = "filter-label"


                wrapper.Controls.Add(lbl)
                wrapper.Controls.Add(New LiteralControl("<br/>"))

                wrapper.Controls.Add(filterControl)


                container.Controls.Add(wrapper)


            End If


        Next


    End Sub



    Private Shared Function CreateTextBox(
        profile As ColumnProfile) As TextBox


        Dim txt As New TextBox()

        txt.ID =
            "txt_" & profile.ColumnName


        txt.CssClass =
            "form-control"


        txt.Attributes.Add(
            "data-column",
            profile.ColumnName)


        Return txt

    End Function



    Private Shared Function CreateDropDown(
        profile As ColumnProfile) As DropDownList


        Dim ddl As New DropDownList()


        ddl.ID =
            "ddl_" & profile.ColumnName


        ddl.CssClass =
            "form-select"



        ddl.Items.Add(
            New ListItem("--All--", ""))


        'Values will be filled later
        'from DataTable


        Return ddl


    End Function



    Private Shared Function CreateDateFilter(
        profile As ColumnProfile) As Panel


        Dim p As New Panel()


        Dim fromDate As New TextBox()

        fromDate.ID =
            "from_" & profile.ColumnName


        fromDate.TextMode =
            TextBoxMode.Date


        Dim toDate As New TextBox()

        toDate.ID =
            "to_" & profile.ColumnName


        toDate.TextMode =
            TextBoxMode.Date



        p.Controls.Add(fromDate)

        p.Controls.Add(
            New LiteralControl(" To "))

        p.Controls.Add(toDate)


        Return p


    End Function



    Private Shared Function CreateNumericFilter(
        profile As ColumnProfile) As Panel


        Dim p As New Panel()



        Dim min As New TextBox()

        min.ID =
            "min_" & profile.ColumnName


        min.Attributes.Add(
            "placeholder",
            "Min")



        Dim max As New TextBox()

        max.ID =
            "max_" & profile.ColumnName


        max.Attributes.Add(
            "placeholder",
            "Max")



        p.Controls.Add(min)

        p.Controls.Add(
            New LiteralControl(" - "))

        p.Controls.Add(max)



        Return p


    End Function


End Class