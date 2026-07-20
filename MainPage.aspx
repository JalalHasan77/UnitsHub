﻿<%@ Page Language="VB" AutoEventWireup="true" Codefile="MainPage.aspx.vb" Inherits="MainPage"%>
<%@ Register assembly="ServerControl1" namespace="ServerControl1" tagprefix="cc1" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
 <%--<link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css" rel="stylesheet" />--%>
    <style type="text/css">
        .auto-style2 {
            width: 100%;
            border-collapse: collapse;
        }
        .auto-style3 {
            width: 90%;
            border-collapse: collapse;
            table-layout: fixed;
        }
    .unit-filter {
        display: flex;
        flex-direction: column;
        gap: 6px;
        max-width: 320px;
        margin-bottom: 16px;
    }

    .unit-filter-label label {
        font-weight: 600;
        font-size: 14px;
        color: #333333;
    }

    .unit-filter-control {
        position: relative;
    }

    .styled-dropdown {
        width: 250px;
        appearance: none;
        -webkit-appearance: none;
        -moz-appearance: none;
        font-size: 14px;
        padding: 8px 36px 8px 12px;
        border: 1px solid #ccc;
        border-radius: 5px;
        background-color: #ffffff;
        color: #333333;
        cursor: pointer;
        transition: border-color 0.2s ease, box-shadow 0.2s ease;

        /* custom dropdown arrow */
        background-image: url("data:image/svg+xml;charset=UTF-8,%3csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='none' stroke='%23666' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'%3e%3cpolyline points='6 9 12 15 18 9'%3e%3c/polyline%3e%3c/svg%3e");
        background-repeat: no-repeat;
        background-position: right 10px center;
        background-size: 16px;
    }

    .styled-dropdown:hover {
        border-color: #999;
    }

    .styled-dropdown:focus {
        outline: none;
        border-color: #4a90d9;
        box-shadow: 0 0 0 3px rgba(74, 144, 217, 0.2);
    }

        .field-badge {
         display: inline-block;
        padding: 4px 10px;
        margin: 3px 0px 0px 0px;
        border-radius: 0px;
        border: 1.5px solid;
        font-size: 14px;
        font-family: 'Arial'; /*,'Franklin Gothic Medium', 'Arial Narrow',  'sans-serif'}*/
        /*font-weight: 600;*/
        background-color: transparent;
        white-space: nowrap;
        transition: background-color 0.15s ease, color 0.15s ease;
    }

    .badge-blue   { border-color: #4a90d9; color: #4a90d9; }
    .badge-green  { border-color: #4caf50; color: #4caf50; }
    .badge-orange { border-color: #f0932b; color: #f0932b; }
    .badge-purple { border-color: #9b59b6; color: #9b59b6; }
    .badge-teal   { border-color: #16a085; color: #16a085; }
    .badge-pink   { border-color: #e84393; color: #e84393; }

    .gridview-scroll-wrapper {
        width: 100%;
        overflow-x: auto;
        -webkit-overflow-scrolling: touch;
    }

    .gridview-scroll-top {
        width: 100%;
        overflow-x: auto;
        overflow-y: hidden;
        height: 17px;
    }

    .gridview-scroll-top-inner {
        height: 1px;
    }

    .dataItem1 {
    border-right: 1px solid #ddd;
    padding: 5px 25px 5px 5px;
}

    /* optional: fill in on hover, like a real button */
    /*.field-badge:hover {
        color: #ffffff;
    }
    .badge-blue:hover   { background-color: #4a90d9; }
    .badge-green:hover  { background-color: #4caf50; }
    .badge-orange:hover { background-color: #f0932b; }
    .badge-purple:hover { background-color: #9b59b6; }
    .badge-teal:hover   { background-color: #16a085; }
    .badge-pink:hover   { background-color: #e84393; }*/
.btn-bootstrap {
    display: inline-block;
    padding: 6px 12px;
    font-size: 14px;
    font-family: Arial, Helvetica, sans-serif;
    font-weight: 400;
    line-height: 1.5;
    color: #fff;
    text-align: center;
    text-decoration: none;
    vertical-align: middle;
    cursor: pointer;

    background-color: #0d6efd;
    border: 1px solid #0d6efd;
    border-radius: 4px;

    transition: all .15s ease-in-out;
}

.btn-bootstrap:hover {
    background-color: #0b5ed7;
    border-color: #0a58ca;
}

.btn-bootstrap:active {
    background-color: #0a58ca;
    border-color: #0a53be;
}

.btn-bootstrap:focus {
    outline: none;
    box-shadow: 0 0 0 0.2rem rgba(13,110,253,.25);
}

.btn-bootstrap:disabled {
    opacity: .65;
    cursor: not-allowed;
}

</style>
 

    <script type="text/javascript">
        function getSelectedElement(Select, ToTextBox) {
            //var e = document.getElementById("Select1");
            //var strUser = e.value;
            var e = document.getElementById(Select);
            var strUser = e.options[e.selectedIndex].text;

            // var txt = document.getElementById("TextBox2");
            if (strUser != 'From') {
                document.getElementById(ToTextBox).disabled = true;
            }
            else {
                document.getElementById(ToTextBox).disabled = false;
            }
            // alert(strUser);
        }

        function syncGridScrollbars() {
            var top = document.getElementById('gvScrollTop');
            var topInner = document.getElementById('gvScrollTopInner');
            var bottom = document.getElementById('gvScrollBottom');

            if (!top || !topInner || !bottom) { return; }

            // Make the dummy top strip exactly as wide as the actual scrollable content,
            // so its scrollbar behaves proportionally to the real one below.
            function resizeTopInner() {
                topInner.style.width = bottom.scrollWidth + 'px';
            }

            var isSyncing = false;

            top.addEventListener('scroll', function () {
                if (isSyncing) { return; }
                isSyncing = true;
                bottom.scrollLeft = top.scrollLeft;
                isSyncing = false;
            });

            bottom.addEventListener('scroll', function () {
                if (isSyncing) { return; }
                isSyncing = true;
                top.scrollLeft = bottom.scrollLeft;
                isSyncing = false;
            });

            resizeTopInner();
            window.addEventListener('resize', resizeTopInner);
        }

        window.addEventListener('load', syncGridScrollbars);
   </script>



</head>
<body style="margin: 0px;">
    <form id="form1" runat="server">

        <table cellpadding="0" class="auto-style2">
            <tr>
                <td style="background-color: #3366FF">
                    <table cellpadding="15" cellspacing="15" class="auto-style2">
                        <tr>
                            <td style="width: 50%">
                                <asp:Label ID="Label2" runat="server" Font-Names="Arial Black" Font-Size="36pt" ForeColor="White" Text="Label"></asp:Label>
                            </td>
                            <td style="vertical-align: top; width: 50%;" align="right">
                                <br />
                            </td>
                        </tr>
                    </table>
                </td>
            </tr>
            <tr>
                <td align="center" valign="top">
                    <table cellpadding="10" cellspacing="10" class="auto-style3">
                                <tr>
                                    <td style="width: 10%; vertical-align: top; text-align: center;" class="auto-style4">
                                        &nbsp;</td>
                                    <td style="width: 80%" align="left">
                                        <div class="unit-filter">
                                            <div class="unit-filter-label">
                                                <asp:Label ID="Label1" runat="server" Font-Names="Arial" Text="Show Units of Project:"></asp:Label>
                                            </div>
                                            <div class="unit-filter-control">
                                                <asp:DropDownList ID="DropDownList1" runat="server" CssClass="styled-dropdown" Font-Names="Arial" AutoPostBack="True">
                                                </asp:DropDownList>
                                            </div>
                                        </div>
                                        <table cellpadding="0" class="auto-style2">
                                            <tr>
                                                <td style="vertical-align: top;width:15%">
                                        <asp:GridView ID="GridView2" runat="server" CellPadding="4" Font-Names="Arial" ForeColor="#333333" BorderColor="Black" BorderStyle="Solid" BorderWidth="1px" Font-Size="13px" GridLines="Both">
                                            <AlternatingRowStyle BackColor="White" ForeColor="#284775" />
                                            <EditRowStyle BackColor="#999999" />
                                            <FooterStyle BackColor="#5D7B9D" Font-Bold="True" ForeColor="White" />
                                            <HeaderStyle BackColor="#5D7B9D" Font-Bold="True" ForeColor="White" />
                                            <PagerStyle BackColor="#284775" ForeColor="White" HorizontalAlign="Center" />
                                            <RowStyle BackColor="#F7F6F3" ForeColor="#333333" />
                                            <SelectedRowStyle BackColor="#E2DED6" Font-Bold="True" ForeColor="#333333" />
                                            <SortedAscendingCellStyle BackColor="#E9E7E2" />
                                            <SortedAscendingHeaderStyle BackColor="#506C8C" />
                                            <SortedDescendingCellStyle BackColor="#FFFDF8" />
                                            <SortedDescendingHeaderStyle BackColor="#6F8DAE" />
                                        </asp:GridView>
                                                </td>
                                                <td style="vertical-align: top;width:85%">
                                                    <asp:DataList ID="DataList2"     runat="server"
    RepeatColumns="3"
    Width="100%" CellPadding="4" Font-Names="Arial" ForeColor="#333333" GridLines="Both" Font-Size="13px">

                                                        <AlternatingItemStyle BackColor="White" ForeColor="#284775" CssClass="dataItem1" />
                                                        <FooterStyle BackColor="#5D7B9D" Font-Bold="True" ForeColor="White" />
                                                        <HeaderStyle BackColor="#5D7B9D" Font-Bold="True" ForeColor="White" />
                                                        <ItemStyle BackColor="#F7F6F3" ForeColor="#333333" CssClass="dataItem1" />

    <ItemTemplate>

        <table cellpadding="5">
            <tr>
                <td style="font-weight:bold; width:120px;">
                    <%# Eval("FIELD_NAME") %>
                </td>

                <td>
                    :
                </td>

                <td>
                    <%# Eval("FIELD_VALUE") %>
                </td>
            </tr>
        </table>

    </ItemTemplate>

                                                        <SelectedItemStyle BackColor="#E2DED6" Font-Bold="True" ForeColor="#333333" />

</asp:DataList>
                                                    </td>
                                            </tr>
                                        </table>
<asp:DataList ID="DataList1" runat="server"
    RepeatColumns="15">
    <ItemTemplate>
        <div class="unit-card">
            <asp:Literal ID="litFields" runat="server"></asp:Literal>
        </div>
    </ItemTemplate>
</asp:DataList>
                                        <br />

                                        <div style="gap: 8px;width:100%;align-content:flex-end">
                                            <asp:Button ID="btnColumns"
    runat="server"
    Text="Columns"
    CssClass="btn-bootstrap" />
                                            <asp:Button ID="btnFilter"
    runat="server"
    Text="Filter"
    CssClass="btn-bootstrap" />
                                        </div>
                                        <div class="gridview-scroll-top" id="gvScrollTop">
                                            <div class="gridview-scroll-top-inner" id="gvScrollTopInner">&nbsp;</div>
                                        </div>
                                        <div class="gridview-scroll-wrapper" id="gvScrollBottom">
                                        <asp:GridView ID="GridView1" runat="server" Width="100%" CellPadding="4" Font-Names="Arial" ForeColor="#333333">
                                            <AlternatingRowStyle BackColor="White" ForeColor="#284775" />
                                            <EditRowStyle BackColor="#999999" />
                                            <FooterStyle BackColor="#5D7B9D" Font-Bold="True" ForeColor="White" />
                                            <HeaderStyle BackColor="#5D7B9D" Font-Bold="True" ForeColor="White" HorizontalAlign="Center"/>
                                            <PagerStyle BackColor="#284775" ForeColor="White" HorizontalAlign="Center" />
                                            <RowStyle BackColor="#F7F6F3" ForeColor="#333333" />
                                            <SelectedRowStyle BackColor="#E2DED6" Font-Bold="True" ForeColor="#333333" />
                                            <SortedAscendingCellStyle BackColor="#E9E7E2" />
                                            <SortedAscendingHeaderStyle BackColor="#506C8C" />
                                            <SortedDescendingCellStyle BackColor="#FFFDF8" />
                                            <SortedDescendingHeaderStyle BackColor="#6F8DAE" />
                                        </asp:GridView>
                                        </div>
                                    </td>
                                    <td style="width: 10%; vertical-align: top;">
                                        &nbsp;</td>
                        </tr>
                                </table>
                        </td>
                     </tr>
                        </table>

</form>
</body>
</html>