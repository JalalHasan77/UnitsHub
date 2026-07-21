<%@ Page Language="VB" AutoEventWireup="false" CodeFile="Filters.aspx.vb" Inherits="Filters" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
    <style>
        .auto-style2 {
            width: 100%;
            border-collapse: collapse;
        }
        .auto-style3 {
            width: 90%;
            border-collapse: collapse;
            table-layout: fixed;
        }

.group-title { font-weight: bold; background-color: #eee; text-align: left; }

.tableContainer{
    border:1px solid #c8c8c8;
    border-radius:6px;
    margin-bottom:15px;
    background:white;
}

.tableTitle{
    background:#f4f4f4;
    font-weight:bold;
    padding:8px 12px;
    border-bottom:1px solid #ddd;
}

.rowItem{
    display:flex;
    justify-content:space-between;
    align-items:center;
    padding:6px 12px;
    border-bottom:1px solid #eee;
}

.rowItem:last-child{
    border-bottom:none;
}

.leftCheck{
    margin-right:10px;
}

.categoryName{
    flex:1;
    text-align:left;
}

.rightCount{
    margin-left:15px;
    text-align:right;
    color:#666;
}

body{
    font-family: Arial, Helvetica, sans-serif;
}


    </style>
</head>
<body style="margin: 0px;" >
    <form id="form1" runat="server">

        <table cellpadding="0" class="auto-style2">
            <tr>
                <td style="background-color: #f4f4f4">
                    <table cellpadding="15" cellspacing="15" class="auto-style2">
                        <tr>
                            <td style="width: 50%">
                                <asp:Label ID="Label2" runat="server" Font-Names="Arial Black" Font-Size="28px" ForeColor="#C8C8C8" Text="Filter"></asp:Label>
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
                                    <td style="width: 100%" align="left">

                                        <asp:Repeater ID="rptTables" runat="server"
    OnItemDataBound="rptTables_ItemDataBound">

<ItemTemplate>

<div class="tableContainer">

    <div class="tableTitle">
        <%# Eval("Title") %>
    </div>

    <asp:Repeater ID="rptRows" runat="server">

        <ItemTemplate>

            <div class="rowItem">

                <asp:CheckBox ID="chkSelect"
                    runat="server"
                    CssClass="leftCheck" />

                <span class="categoryName">
                    <%# Eval("Key") %>
                </span>

                <span class="rightCount">
                    <%# Eval("Count") %>
                </span>

            </div>

        </ItemTemplate>

    </asp:Repeater>

</div>

</ItemTemplate>

</asp:Repeater>

                                    </td>
                        </tr>
                                </table>
                        </td>
                     </tr>
                        </table>

</form>
</body>
</html>
