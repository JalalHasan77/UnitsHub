<%@ Page Language="VB" AutoEventWireup="false" CodeFile="ProjectStatusAndAction.aspx.vb" Inherits="TR_StatusAndAction" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
            <title>Project Status</title>
    <meta charset="utf-8" />
    <link rel="preconnect" href="https://fonts.googleapis.com" />
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap" rel="stylesheet" />
    <style type="text/css">
        * { box-sizing: border-box; }

        body {
            font-family: 'Inter', 'Segoe UI', Arial, sans-serif;
            background-color: #eef1f8;
            margin: 0;
        }

        .page-header {
            background: linear-gradient(135deg, #3366FF 0%, #1f49d6 100%);
            box-shadow: 0 2px 10px rgba(31, 73, 214, 0.25);
        }

        .page-title {
            font-family: 'Inter', 'Segoe UI', Arial, sans-serif;
            font-size: 26px;
            font-weight: 700;
            color: #ffffff;
            letter-spacing: 0.3px;
        }

        .content-wrapper {
            padding: 28px 0 40px 0;
        }

        .content-card {
            background-color: #ffffff;
            border: 1px solid #e2e8f0;
            border-radius: 12px;
            box-shadow: 0 1px 4px rgba(30, 41, 59, 0.06);
            padding: 20px 24px;
        }

        .field-label {
            font-size: 13.5px;
            font-weight: 600;
            color: #334155;
            margin-right: 10px;
        }

        .field-dropdown {
            padding: 9px 14px;
            border: 1px solid #cbd5e1;
            border-radius: 8px;
            font-size: 14px;
            font-family: 'Inter', 'Segoe UI', Arial, sans-serif;
            color: #1e293b;
            background-color: #ffffff;
            min-width: 240px;
            cursor: pointer;
        }

        .field-dropdown:hover {
            border-color: #94a3b8;
        }

        .field-dropdown:focus {
            outline: none;
            border-color: #3366FF;
            box-shadow: 0 0 0 3px rgba(51, 102, 255, 0.15);
        }

        .status-panel {
            background-color: #1a1464;
            padding: 10px 10px 7px 10px;
            border-radius: 8px;
        }

        .status-grid {
            width: 100%;
            border-collapse: collapse;
        }

        .status-grid td {
            padding: 0 0 10px 0;
        }

        .status-box {
            display: inline-block;
            box-sizing: border-box;
            width: 175px;
            max-width: 100%;
            padding: 8px 8px;
            border-radius: 12px;
            box-shadow: 0 2px 5px rgba(0, 0, 0, 0.25);
            text-align: center;
        }

        .status-title {
            font-size: 19px;
            font-weight: 700;
            text-decoration: underline;
            display: block;
            text-align: center;
        }

        .subtitle-box {
            display: inline-block;
            margin-top: 10px;
            padding: 7px 16px;
            border-radius: 8px;
            font-size: 14px;
            font-weight: 500;
            box-shadow: 0 1px 3px rgba(0, 0, 0, 0.15);
            text-align: center;
        }

        .status-row {
            display: flex;
            align-items: center;
            justify-content: space-between;
            width: 100%;
        }

        .status-actions {
            white-space: nowrap;
            padding-right: 16px;
        }

        .status-action-link {
            color: #ffffff !important;
            text-decoration: underline;
            font-size: 14px;
            font-weight: 600;
            margin-left: 20px;
        }

        .status-action-link:first-child {
            margin-left: 0;
        }

        .status-action-link:hover {
            color: #d6e0ff !important;
        }

        .status-empty {
            padding: 16px 22px;
            color: #ffffff;
            font-size: 14px;
        }
        
            .lblSpacing2 
        { 
    padding-left: 3px;
    padding-right: 3px; 
    padding-top: 3px; 
    padding-bottom: 3px; 
    -moz-border-radius: 3px;
    -webkit-border-radius: 3px;
    border-radius: 7px;
    border: 1px solid grey;
    }

                    .auto-style4 {
            width: 100%;
            border-collapse: collapse;
        }


    </style>
</head>
<body style="margin: 0px;">
    <form id="form1" runat="server">
        <table cellpadding="0" style="width: 100%">
            <tr>
                <td class="page-header">
                    <table  style="width: 100%" cellpadding="15" cellspacing="15">
                        <tr>
                            <td style="width: 50%">
                    <asp:Label ID="Label1" runat="server" CssClass="page-title" Text="Status"></asp:Label>
                                </td>
                            <td>

                                &nbsp;</td>
                            </tr>
                        </table>
                    </td>
                </tr>
            <tr>
                <td align="center" class="content-wrapper">
                    <table style="border-spacing: 0px; border-collapse: 0; width: 100%"><tr><td style="vertical-align: top" valign="top" style="height: 0px" valign="top" >
                    <table cellpadding="5" cellspacing="5" style="width: 80%; margin: 0 auto;">
                        <tr>
                            <td style="vertical-align: top" align="Left" valign="top" class="content-card">

                                <asp:Label ID="lblProjectName" runat="server" CssClass="field-label" Text="Project Name" />
                                <asp:DropDownList ID="ddlProjectName" runat="server" CssClass="field-dropdown" AutoPostBack="true" OnSelectedIndexChanged="ddlProjectName_SelectedIndexChanged" />
                            </td>
                        </tr>
                        </table>
                        </td></tr>
                        <tr><td style="vertical-align: top" valign="top">
                    <table cellpadding="5" cellspacing="5" style="width: 80%; margin: 0 auto;">
                        <tr>
                            <td style="vertical-align: top" align="center" valign="top" class="content-card">

                                <div class="status-panel">
                                    <asp:GridView ID="gvProjectStatus" runat="server" AutoGenerateColumns="False"
                                        ShowHeader="False" GridLines="None" CellPadding="0" CellSpacing="0"
                                        CssClass="status-grid" Width="100%"
                                        OnRowDataBound="gvProjectStatus_RowDataBound">
                                        <Columns>
                                            <asp:TemplateField>
                                                <ItemTemplate>
                                                    <table style="width:100%">
                                                        <tr>
                                                            <td>
                                                    <div class="status-row">
                                                        <asp:Panel ID="pnlStatusBox" runat="server" CssClass="status-box">
                                                            <asp:Label ID="lblStatus" runat="server" CssClass="status-title" Text='<%# Eval("STATUS") %>' />
                                                            <asp:Panel ID="pnlSubtitle" runat="server" CssClass="subtitle-box">
                                                                <asp:Label ID="lblSubtitle" runat="server" Text='<%# Eval("SUBTITLE") %>' />
                                                            </asp:Panel>
                                                        </asp:Panel>
                                                        <div class="status-actions">
                                                            <asp:LinkButton ID="lnkAddStateUp" runat="server" CssClass="status-action-link" CommandArgument='<%# Eval("STATE_ID") %>' OnClick="lnkAddStateUp_Click">Add State &#9650;</asp:LinkButton>
                                                            <asp:LinkButton ID="lnkAddStateDown" runat="server" CssClass="status-action-link" CommandArgument='<%# Eval("STATE_ID") %>' OnClick="lnkAddStateDown_Click">Add State &#9660;</asp:LinkButton>
                                                            <asp:LinkButton ID="lnkAddNewAction" runat="server" CssClass="status-action-link" CommandArgument='<%# Eval("STATE_ID") %>' OnClick="lnkAddNewAction_Click">Add New Action</asp:LinkButton>
                                                        </div>
                                                    </div>
                                                            </td>
                                                        </tr>
                                                        <tr>
                                                            <td style="background-color:#9D8BE6">
                                                                <asp:GridView ID="GridView1" runat="server" AutoGenerateColumns="False" OnRowDataBound="GridView1_RowDataBound">
                                                                    <Columns>
                                                                        <asp:TemplateField>
                                                                            <ItemTemplate>
                                                                            <table cellpadding="4" class="auto-style4">
                                                                                <tr>
                                                                                    <td align="Left">
                                                                                        <asp:CheckBox ID="chkActionTitle" runat="server" Font-Names="Arial" AutoPostBack="True" />
                                                                                        <asp:LinkButton ID="lnkActionTitle" runat="server" Font-Names="Arial"></asp:LinkButton>
                                                                                    </td>
                                                                                    <td style="width: 1px">
                                                                                        <asp:Button ID="btnShowUsers" runat="server" Text="Show Users" CssClass="lblSpacing2" />
                                                                                    </td>
                                                                                </tr>
                                                                            </table>
                                                                            </ItemTemplate>
                                                                        </asp:TemplateField>
                                                                    </Columns>
                                                                </asp:GridView>



                                                             </td>
                                                        </tr>
                                                    </table>

                                                </ItemTemplate>
                                            </asp:TemplateField>
                                        </Columns>
                                        <EmptyDataTemplate>
                                            <div class="status-empty">No statuses defined for this project.</div>
                                        </EmptyDataTemplate>
                                    </asp:GridView>
                                </div>
                            </td>
                        </tr>
                        </table>
                </td></tr></table>

                </td>
                </tr>
            </table>
        
    </form>
</body>
</html>

