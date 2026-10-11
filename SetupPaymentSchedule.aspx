<%@ Page Language="VB" AutoEventWireup="false" CodeFile="SetUpPaymentSchedule.aspx.vb" Inherits="Forms_SetUpPaymentSchedule" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
    <style type="text/css">

.pill-btn {
    border: 1px solid #d1d5db;
    border-radius: 20px;
    padding: 6px 14px;
    background: #fff;
    color: #374151;
    cursor: pointer;
    transition: background-color 0.15s ease, transform 0.1s ease, box-shadow 0.15s ease;
}

/* Mouse hovering over the button */
.pill-btn:hover {
    background: #eef2ff;
    border-color: #c7d2fe;
    color: #3730a3;
    box-shadow: 0 1px 3px rgba(0, 0, 0, 0.08);
}

/* The moment it's clicked / pressed down */
.pill-btn:active {
    background: #c7d2fe;
    transform: scale(0.97);
    box-shadow: inset 0 1px 2px rgba(0, 0, 0, 0.1);
}

/* Keyboard / tab focus — don't skip this, it matters for accessibility */
.pill-btn:focus {
    outline: none;
    box-shadow: 0 0 0 3px rgba(79, 70, 229, 0.25);
}

.delete-row-btn {
    opacity: 0.75;
    cursor: pointer;
    transition: opacity 0.15s ease, transform 0.1s ease;
}

.delete-row-btn:hover {
    opacity: 1;
}

.delete-row-btn:active {
    transform: scale(0.9);
}

    .section-panel {
        width: 800px;       /* needs an explicit width for margin auto to work */
        margin: 0 auto;
    }

    .MeBox {
    border: 1px solid  #CCCCCC;
    font-size: 14px;
    padding: 8px;
    box-sizing: border-box;
    border-radius: 6px;
	margin-bottom: 0px;
    direction: ltr;
}

    </style>
</head>
<body>
    <form id="form1" runat="server">

<div class="center-wrapper">
    <div class="section-panel">

            <div style="display:flex; align-items:center">
                <div>
                    <asp:Label ID="lblProject"
                               runat="server"
                               Text="Project"
                               AssociatedControlID="ddlProject"
                               Font-Names="Arial"
                               Font-Size="18px" Font-Bold="True" />
                </div>
                <div style="width:15px">&nbsp;</div>
                <div>
                                                                            <asp:DropDownList ID="ddlProject" runat="server" AutoPostBack="True" OnSelectedIndexChanged="ddlProject_SelectedIndexChanged" CssClass="MeBox" Font-Size="Small" Width="300px">
                                                                                <asp:ListItem>Any</asp:ListItem>
                                                                                <asp:ListItem>Danaat 001</asp:ListItem>
                                                                                <asp:ListItem>Danaat 002</asp:ListItem>
                                                                                <asp:ListItem Value="Danaat 003">Danaat 003</asp:ListItem>
                                                                            </asp:DropDownList>
                </div>
            </div>

            <br />
        <asp:Button ID="Button1" runat="server" Text="Save" />
        <br />

            <asp:LinkButton ID="btnAddExpRdc"
                            runat="server"
                            CssClass="exp-rdc-link"
                            Font-Names="Arial"
                            Font-Size="14px">
                Add Other Expenses/Reduction
            </asp:LinkButton>

            <asp:GridView ID="GridView2"
                  runat="server"
                  CssClass="exp-rdc-grid"
                  CellPadding="4"
                  ForeColor="#333333"
                  Font-Names="Arial"
                  Font-Size="12px"
                  Width="100%"
                  AutoGenerateColumns="False"
                  OnRowCommand="GridView2_RowCommand" ShowHeaderWhenEmpty="True">
        <AlternatingRowStyle BackColor="White" />

        <Columns>

            <asp:TemplateField HeaderText="" ItemStyle-Width="36px" ItemStyle-HorizontalAlign="Center">
                <ItemTemplate>
                    <asp:ImageButton ID="imgDeleteRow"
                                      runat="server"
                                      CssClass="delete-row-btn"
                                      ImageUrl="data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='16' height='16' viewBox='0 0 24 24' fill='none' stroke='%23dc2626' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'%3E%3Cpolyline points='3 6 5 6 21 6'%3E%3C/polyline%3E%3Cpath d='M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6'%3E%3C/path%3E%3Cpath d='M10 11v6'%3E%3C/path%3E%3Cpath d='M14 11v6'%3E%3C/path%3E%3Cpath d='M9 6V4a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v2'%3E%3C/path%3E%3C/svg%3E"
                                      CommandName="DeleteRow"
                                      CommandArgument='<%# Container.DataItemIndex %>'
                                      AlternateText="Delete row"
                                      ToolTip="Delete this row"
                                      OnClientClick="return confirm('Delete this row?');" />
                </ItemTemplate>

<ItemStyle HorizontalAlign="Center" Width="36px"></ItemStyle>
            </asp:TemplateField>

            <asp:TemplateField HeaderText="#" ItemStyle-Width="30px" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center">
                <ItemTemplate>
                    <%# Container.DataItemIndex + 1 %>
                </ItemTemplate>

<HeaderStyle HorizontalAlign="Center"></HeaderStyle>

<ItemStyle HorizontalAlign="Center" Width="30px"></ItemStyle>
            </asp:TemplateField>

            <asp:BoundField DataField="Title"
                            HeaderText="Title"
                            HeaderStyle-CssClass="col-80"
                            ItemStyle-CssClass="col-80" >

    <HeaderStyle CssClass="col-80"></HeaderStyle>

    <ItemStyle CssClass="col-80"></ItemStyle>
            </asp:BoundField>

            <asp:BoundField DataField="Payment"
                            HeaderText="Payment"
                            HeaderStyle-CssClass="col-80"
                            HeaderStyle-Width="100%"
                            ItemStyle-CssClass="col-80"
                            ItemStyle-Width="100%" >

<HeaderStyle CssClass="col-80" Width="100%"></HeaderStyle>

<ItemStyle CssClass="col-80" Width="100%"></ItemStyle>

            </asp:BoundField>

            <asp:BoundField DataField="Comment" HeaderText="Comment" />

        </Columns>

        <EditRowStyle BackColor="#2461BF" />
        <FooterStyle BackColor="#507CD1" Font-Bold="True" ForeColor="White" />
        <HeaderStyle BackColor="#507CD1" Font-Bold="True" ForeColor="White" />
        <PagerStyle BackColor="#2461BF" ForeColor="White" HorizontalAlign="Center" />
        <RowStyle BackColor="#EFF3FB" />
        <SelectedRowStyle BackColor="#D1DDF1" Font-Bold="True" ForeColor="#333333" />
        <SortedAscendingCellStyle BackColor="#F5F7FB" />
        <SortedAscendingHeaderStyle BackColor="#6D95E1" />
        <SortedDescendingCellStyle BackColor="#E9EBEF" />
        <SortedDescendingHeaderStyle BackColor="#4870BE" />
    </asp:GridView>
    </div>
</div>
    </form>
</body>
</html>
