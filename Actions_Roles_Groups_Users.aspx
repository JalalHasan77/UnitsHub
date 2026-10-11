<%@ Page Language="VB" AutoEventWireup="false" CodeFile="Actions_Roles_Groups_Users.aspx.vb" Inherits="Actions_Roles_Groups_Users" MaintainScrollPositionOnPostback="true" EnableEventValidation="false" %>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <link rel="stylesheet" href="styles.css" />
    <title>Actions, Roles, Groups and Users</title>
    <style type="text/css">
        *, ::after, ::before { box-sizing: border-box; }

        :root {
            --ink: #1e293b;
            --muted: #64748b;
            --border: #e2e8f0;
        }

        body {
            font-family: 'Segoe UI', Arial, Helvetica, sans-serif;
            margin: 0;
            padding: 0;
            color: var(--ink);
        }

        .top-strip {
            display: flex;
            align-items: center;
            justify-content: space-between;
            width: 100%;
            margin: 0;
            border: none;
            background: linear-gradient(120deg, #4f46e5, #3b82f6 75%);
            color: #ffffff;
            font-size: 18px;
            font-weight: 700;
            padding: 14px 20px;
        }

        .close-btn {
            width: 28px;
            height: 28px;
            padding: 4px;
            border: none;
            border-radius: 7px;
            background: transparent;
            cursor: pointer;
            transition: background .15s ease;
        }
        .close-btn:hover, .close-btn:focus-visible { background: rgba(255, 255, 255, 0.22); outline: none; }

        .MainContainer {
            width: 100%;
            border-collapse: collapse;
            border-style: solid;
            border-width: 0px;
        }

        .nestedTable {
            width: 100%;
            border-collapse: collapse;
            white-space: nowrap;
        }

        /* ---------- Tab menu ---------- */
        .itemCell, .itemCellSelecred, .itemCellSelecredBottmBorder {
            text-align: center;
            vertical-align: middle;
        }

        .itemCell a, .itemCellSelecred a, .itemCellSelecredBottmBorder a {
            display: block;
            text-align: center;
            line-height: 50px;
        }

        .full-width-menu {
            width: 100% !important;
            table-layout: fixed;
            border-collapse: collapse;
        }

        .itemCellSelecredBottmBorder,
        .itemCellSelecredBottmBorder a {
            border-bottom: none !important;
        }

        .tab-body-inner { padding: 22px 4px 6px; min-height: 300px; }

        /* ---------- Selection bar above the tabs ---------- */
        .select-bar {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
            gap: 12px 16px;
            margin: 0 0 16px 0;
            padding: 14px 16px;
            border: 1px solid #c7d2fe;
            border-radius: 10px;
            background: #eef2ff;
        }
        .select-field { display: flex; flex-direction: column; gap: 5px; min-width: 0; }
        .select-label { font-size: 13px; font-weight: 700; color: #1a1464; }
        .select-ddl {
            width: 100%;
            height: 38px;
            padding: 0 10px;
            border: 1.5px solid #cbd5e1;
            border-radius: 8px;
            background: #ffffff;
            color: var(--ink);
            font: inherit;
            font-size: 14px;
        }
        .select-ddl:focus { border-color: #4f46e5; outline: none; box-shadow: 0 0 0 3px rgba(79, 70, 229, 0.15); }
        .select-ddl:disabled { background: #f1f5f9; color: var(--muted); }
        .select-hint { font-size: 12px; color: var(--muted); }

        /* ---------- Roles tab ---------- */
        .roles-head {
            display: flex;
            flex-wrap: wrap;
            align-items: center;
            justify-content: space-between;
            gap: 10px;
            margin-bottom: 12px;
        }
        .roles-title { font-size: 14px; font-weight: 700; color: #1a1464; white-space: normal; }
        .roles-save {
            height: 38px;
            padding: 0 20px;
            border: none;
            border-radius: 8px;
            background: #1a1464;
            color: #ffffff;
            font: inherit;
            font-size: 14px;
            font-weight: 600;
            cursor: pointer;
        }
        .roles-save:hover { background: #2a2390; }
        .roles-save.aspNetDisabled, .roles-save[disabled] { opacity: .45; cursor: not-allowed; }

        .roles-list {
            list-style: none;
            margin: 0;
            padding: 0;
            display: flex;
            flex-direction: column;   /* one role per line, one column */
            gap: 6px;
        }
        .roles-list label { cursor: pointer; font-size: 14px; }
        .roles-list input[type="checkbox"] { width: 17px; height: 17px; margin-right: 8px; vertical-align: middle; accent-color: #1a1464; }
        .roles-list .aspNetDisabled { color: var(--muted); }

        .roles-note { display: block; margin-top: 12px; font-size: 13px; font-weight: 600; }
        .roles-note.ok { color: #047857; }
        .roles-note.info { color: var(--muted); font-weight: 400; }
        .roles-note.err { color: #b91c1c; }

        /* ---------- Groups tab ---------- */
        .groups-list {
            list-style: none;
            margin: 0;
            padding: 0;
            display: flex;
            flex-direction: column;   /* one group per line, one column */
            gap: 10px;
        }
        .groups-list label { cursor: pointer; font-size: 14px; white-space: normal; }
        .groups-list input[type="checkbox"] { width: 17px; height: 17px; margin-right: 8px; vertical-align: middle; accent-color: #1a1464; }
        .groups-list .g-name { font-weight: 600; }
        .groups-list .g-tag {
            display: inline-block;
            margin-left: 6px;
            padding: 1px 7px;
            border-radius: 6px;
            background: #e2e8f0;
            color: #334155;
            font-size: 11px;
            font-weight: 700;
            vertical-align: middle;
        }
        /* The "why" line under each group, lined up with the name */
        .groups-list .g-why { display: block; margin: 2px 0 0 25px; font-size: 12.5px; }
        .groups-list .g-why.inc { color: #047857; }
        .groups-list .g-why.exc { color: #b91c1c; }
        .groups-list .g-why.none { color: var(--muted); }
        .groups-list .g-id { color: var(--muted); font-size: 12.5px; }
        .groups-list .g-tag.off { background: #fee2e2; color: #991b1b; }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <div class="top-strip">
            <span>Actions, Roles, Groups and Users</span>
            <%-- Values received from the opener (?ProjectID=&StatusID=&SUBSTATEID=&ActionID=); kept, not shown --%>
            <asp:Label ID="lblProjectID" runat="server" Text="" Visible="false" />
            <asp:Label ID="lblStatusID" runat="server" Text="" Visible="false" />
            <asp:Label ID="lblSUBSTATEID" runat="server" Text="" Visible="false" />
            <asp:Label ID="lblActionID" runat="server" Text="" Visible="false" />
            <asp:ImageButton ID="imgClose"
                runat="server"
                CssClass="close-btn"
                CausesValidation="false"
                ToolTip="Close"
                AlternateText="Close"
                ImageUrl="data:image/svg+xml;utf8,&lt;svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='none' stroke='white' stroke-width='2.5' stroke-linecap='round' stroke-linejoin='round'&gt;&lt;line x1='18' y1='6' x2='6' y2='18'/&gt;&lt;line x1='6' y1='6' x2='18' y2='18'/&gt;&lt;/svg&gt;" />
        </div>

        <table class="MainContainer" cellpadding="0" cellspacing="0">
            <tr>
                <td>
                    <div style="padding:10px">
                        <%-- Project -> Status -> Sub-status (if the status has any) -> Action --%>
                        <div class="select-bar">
                            <div class="select-field">
                                <asp:Label ID="lblProjectCaption" runat="server" Text="Project" AssociatedControlID="ddlProject" CssClass="select-label" />
                                <asp:DropDownList ID="ddlProject" runat="server" CssClass="select-ddl" AutoPostBack="true" />
                            </div>
                            <div class="select-field">
                                <asp:Label ID="lblStatusCaption" runat="server" Text="Status" AssociatedControlID="ddlStatus" CssClass="select-label" />
                                <asp:DropDownList ID="ddlStatus" runat="server" CssClass="select-ddl" AutoPostBack="true" />
                            </div>
                            <asp:Panel ID="pnlSubStatus" runat="server" CssClass="select-field">
                                <asp:Label ID="lblSubStatusCaption" runat="server" Text="Sub-status" AssociatedControlID="ddlSubStatus" CssClass="select-label" />
                                <asp:DropDownList ID="ddlSubStatus" runat="server" CssClass="select-ddl" AutoPostBack="true" />
                            </asp:Panel>
                            <div class="select-field">
                                <asp:Label ID="lblActionCaption" runat="server" Text="Action" AssociatedControlID="ddlAction" CssClass="select-label" />
                                <asp:DropDownList ID="ddlAction" runat="server" CssClass="select-ddl" AutoPostBack="true" />
                                <asp:Label ID="lblActionHint" runat="server" CssClass="select-hint" Visible="false" />
                            </div>
                        </div>

                        <table cellpadding="0" class="nestedTable">
                            <tr>
                                <td style="border-width: 0;">
                                    <table cellpadding="0" cellspacing="0" style="width:100%;border-width:0">
                                        <tr>
                                            <td style="width: 100%">
                                                <div style="width:100%;">
                                                    <asp:Menu ID="Menu3" runat="server" Font-Names="Arial" Height="50px" Orientation="Horizontal" Width="100%" CssClass="full-width-menu">
                                                        <Items>
                                                            <asp:MenuItem Text="Roles" Value="Roles" Selected="True"></asp:MenuItem>
                                                            <asp:MenuItem Text="Groups" Value="Groups"></asp:MenuItem>
                                                            <asp:MenuItem Text="Users" Value="Users"></asp:MenuItem>
                                                        </Items>
                                                        <StaticHoverStyle BackColor="#000099" BorderColor="#000099" BorderWidth="1px" CssClass="itemCell" ForeColor="#F2F2F2" Height="50px" />
                                                        <StaticMenuItemStyle BorderWidth="1px" CssClass="itemCell" Height="50px" HorizontalPadding="0px" ItemSpacing="0px" VerticalPadding="0px" BackColor="#F2F2F2" />
                                                        <StaticMenuStyle BorderWidth="0px" Height="50px" HorizontalPadding="0px" VerticalPadding="0px" Width="100%" />
                                                        <StaticSelectedStyle BackColor="White" CssClass="itemCellSelecred itemCellSelecredBottmBorder" Height="50px" HorizontalPadding="0px" ItemSpacing="0px" VerticalPadding="0px" />
                                                    </asp:Menu>
                                                </div>
                                            </td>
                                            <td style="width: 100%; background-color:transparent">
                                                <table cellpadding="0" cellspacing="0" style="height:50px;width:100%;border-width:0">
                                                    <tr><td style="width: 100%; border-bottom-style: solid; border-bottom-width: 1px; border-bottom-color: darkgray;"></td></tr>
                                                </table>
                                            </td>
                                        </tr>
                                    </table>
                                </td>
                            </tr>
                            <tr>
                                <td>
                                    <table style="width:100%" cellpadding="0" cellspacing="0">
                                        <tr>
                                            <td style="background-color:white; padding: 0 15px 15px 15px; border-left: 1px solid black; border-right: 1px solid black; border-bottom: 1px solid black;">
                                                <asp:MultiView ID="MultiView3" runat="server" ActiveViewIndex="0">
                                                    <%-- ---------- Roles ---------- --%>
                                                    <asp:View ID="viewRoles" runat="server">
                                                        <asp:Panel ID="pnlRoles" runat="server" BackColor="White" Width="100%">
                                                            <div class="tab-body-inner">
                                                                <%-- Every role; ticked = allowed to do the selected action at the selected status --%>
                                                                <div class="roles-head">
                                                                    <span class="roles-title">Roles allowed to perform: <asp:Literal ID="litRolesAction" runat="server" /></span>
                                                                    <asp:Button ID="btnSaveRoles" runat="server" Text="Save Roles" CssClass="roles-save" CausesValidation="false" />
                                                                </div>
                                                                <asp:CheckBoxList ID="cblRoles" runat="server" CssClass="roles-list"
                                                                    RepeatLayout="UnorderedList" />
                                                                <asp:Label ID="lblRolesNote" runat="server" CssClass="roles-note" Visible="false" />
                                                            </div>
                                                        </asp:Panel>
                                                    </asp:View>

                                                    <%-- ---------- Groups ---------- --%>
                                                    <asp:View ID="viewGroups" runat="server">
                                                        <asp:Panel ID="pnlGroups" runat="server" BackColor="White" Width="100%">
                                                            <div class="tab-body-inner">
                                                                <%-- Every group; ticked = included for the selected action, with the reason under it --%>
                                                                <div class="roles-head">
                                                                    <span class="roles-title">Groups allowed to perform: <asp:Literal ID="litGroupsAction" runat="server" /></span>
                                                                    <asp:Button ID="btnSaveGroups" runat="server" Text="Save Groups" CssClass="roles-save" CausesValidation="false" />
                                                                </div>
                                                                <asp:CheckBoxList ID="cblGroups" runat="server" CssClass="groups-list"
                                                                    RepeatLayout="UnorderedList" />
                                                                <asp:Label ID="lblGroupsNote" runat="server" CssClass="roles-note" Visible="false" />
                                                            </div>
                                                        </asp:Panel>
                                                    </asp:View>

                                                    <%-- ---------- Users ---------- --%>
                                                    <asp:View ID="viewUsers" runat="server">
                                                        <asp:Panel ID="pnlUsers" runat="server" BackColor="White" Width="100%">
                                                            <div class="tab-body-inner">
                                                                <%-- Every user; ticked = included for the selected action, with the reason under it --%>
                                                                <div class="roles-head">
                                                                    <span class="roles-title">Users allowed to perform: <asp:Literal ID="litUsersAction" runat="server" /></span>
                                                                    <asp:Button ID="btnSaveUsers" runat="server" Text="Save Users" CssClass="roles-save" CausesValidation="false" />
                                                                </div>
                                                                <asp:CheckBoxList ID="cblUsers" runat="server" CssClass="groups-list"
                                                                    RepeatLayout="UnorderedList" />
                                                                <asp:Label ID="lblUsersNote" runat="server" CssClass="roles-note" Visible="false" />
                                                            </div>
                                                        </asp:Panel>
                                                    </asp:View>
                                                </asp:MultiView>
                                            </td>
                                        </tr>
                                    </table>
                                </td>
                            </tr>
                        </table>
                    </div>
                </td>
            </tr>
        </table>
    </form>
</body>
</html>
