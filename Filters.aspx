<%@ Page Language="VB" AutoEventWireup="false" CodeFile="Filters.aspx.vb" Inherits="Filters" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Filter</title>

    <style type="text/css">
        html, body, form {
            width: 100%;
            height: 100%;
            margin: 0;
        }

        body {
            background: transparent;
            overflow: hidden;
            font-family: Arial, Helvetica, sans-serif;
        }

        .adj-card {
            width: 100%;
            height: 100%;
            box-sizing: border-box;
            padding: 20px 22px;
            border: none;
            border-radius: 0;
            background: #ffffff;
            box-shadow: none;
            display: flex;
            flex-direction: column;
        }

        .adj-title {
            font-size: 18px;
            font-weight: 700;
            color: #1f2937;
            margin-bottom: 14px;
            flex: 0 0 auto;
        }

        .filters-scroll {
            flex: 1 1 auto;
            overflow-y: auto;
            overflow-x: hidden;
            border: 1px solid #e5e7eb;
            border-radius: 8px;
            background: #f8fafc;
            box-sizing: border-box;
            padding: 12px;
        }

        .tableContainer {
            border: 1px solid #c8c8c8;
            border-radius: 6px;
            margin-bottom: 15px;
            background: white;
        }

        .tableTitle {
            background: #f4f4f4;
            font-weight: bold;
            padding: 8px 12px;
            border-bottom: 1px solid #ddd;
            border-radius: 6px 6px 0 0;
        }

        .rowItem {
            display: flex;
            justify-content: space-between;
            align-items: center;
            padding: 6px 12px;
            border-bottom: 1px solid #eee;
        }

        .rowItem:last-child {
            border-bottom: none;
        }

        .leftCheck {
            margin-right: 10px;
        }

        .categoryName {
            flex: 1;
            text-align: left;
        }

        .rightCount {
            margin-left: 15px;
            text-align: right;
            color: #666;
        }

        .adj-buttons {
            display: flex;
            justify-content: flex-end;
            gap: 10px;
            margin-top: 18px;
            padding-top: 14px;
            border-top: 1px solid #e5e7eb;
            flex: 0 0 auto;
        }

        .btn-modern {
            min-width: 90px;
            height: 36px;
            padding: 0 16px;
            border: none;
            border-radius: 8px;
            font-size: 14px;
            font-weight: 700;
            cursor: pointer;
        }

        .btn-add {
            background: #2563eb;
            color: white;
        }

        .btn-add:hover {
            background: #1d4ed8;
        }

        .btn-cancel {
            background: #e5e7eb;
            color: #374151;
        }

        .btn-cancel:hover {
            background: #d1d5db;
        }
    </style>

    <script type="text/javascript">
        function closeParentVendorPopup() {
            if (window.parent && typeof window.parent.closeVendorDialog === 'function') {
                window.parent.closeVendorDialog();
            }
            return false;
        }
    </script>
</head>
<body>
    <form id="form1" runat="server">
        <div class="adj-card">
            <div class="adj-title">
                <asp:Label ID="Label2" runat="server" Text="Filter"></asp:Label>

                <%-- State holders: not user-facing, kept out of the visual flow --%>
                <asp:Label ID="Label3" runat="server" Text="Label" style="display:none"></asp:Label>
                <asp:HiddenField ID="hfFilter" runat="server" />
            </div>

            <div class="filters-scroll">
                <asp:GridView ID="GridView1" runat="server" AutoGenerateColumns="False"
                    GridLines="None" CellPadding="0" CellSpacing="0" BorderWidth="0"
                    ShowHeader="False" Width="100%">
                    <Columns>
                        <asp:TemplateField>
                            <ItemTemplate>
                                <div class="tableContainer">
                                    <div class="tableTitle">
                                        <asp:Label ID="Label1" runat="server" Text='<%# Bind("Title") %>'></asp:Label>
                                    </div>
                                    <asp:GridView ID="GridView2" runat="server" AutoGenerateColumns="False"
                                        GridLines="None" CellPadding="0" CellSpacing="0" BorderWidth="0"
                                        ShowHeader="False" Width="100%" OnRowDataBound="GridView2_RowDataBound">
                                        <Columns>
                                            <asp:TemplateField>
                                                <ItemTemplate>
                                                    <div class="rowItem">
                                                        <asp:CheckBox ID="CheckBox1" runat="server" CssClass="leftCheck" AutoPostBack="true" OnCheckedChanged="CheckBox1_CheckedChanged" />
                                                        <span class="rightCount"><%# Eval("Count") %></span>
                                                    </div>
                                                </ItemTemplate>
                                            </asp:TemplateField>
                                            <asp:BoundField DataField="Categories" Visible="False" />
                                        </Columns>
                                    </asp:GridView>
                                </div>
                            </ItemTemplate>
                        </asp:TemplateField>
                    </Columns>
                </asp:GridView>
            </div>

            <div class="adj-buttons">
                <asp:Button ID="Button1"
                            runat="server"
                            Text="OK"
                            CssClass="btn-modern btn-add"
                            OnClick="Button1_Click" />

                <asp:Button ID="Button2"
                            runat="server"
                            Text="Cancel"
                            CssClass="btn-modern btn-cancel"
                            OnClientClick="return closeParentVendorPopup();"
                            UseSubmitBehavior="false" />
            </div>
        </div>
    </form>
</body>
</html>
