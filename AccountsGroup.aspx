<%@ Page Language="VB" AutoEventWireup="false" CodeFile="AccountsGroup.aspx.vb" Inherits="AccountsGroup" MaintainScrollPositionOnPostback="true" EnableEventValidation="false" %>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Accounts Group</title>
    <style type="text/css">
        *, ::after, ::before { box-sizing: border-box; }

        :root {
            --brand-1: #4f46e5;
            --brand-2: #3b82f6;
            --ink: #1e293b;
            --muted: #64748b;
            --border: #e2e8f0;
            --bg: #eef1f8;
        }

        html, body, form {
            width: 100%;
            min-height: 100%;
            margin: 0;
            padding: 0;
        }

        body {
            background: var(--bg);
            color: var(--ink);
            font-family: Arial, 'Segoe UI', sans-serif;
            min-height: 100vh;
        }

        .top-strip {
            display: flex;
            align-items: center;
            justify-content: space-between;
            width: 100%;
            min-height: 58px;
            background: linear-gradient(120deg, var(--brand-1), var(--brand-2) 75%);
            color: #ffffff;
            font-size: 20px;
            font-weight: 700;
            padding: 0 24px;
        }

        .close-btn {
            display: flex;
            align-items: center;
            justify-content: center;
            width: 34px;
            height: 34px;
            padding: 0;
            border: none;
            border-radius: 8px;
            background: transparent;
            cursor: pointer;
            transition: background .15s ease;
        }
        .close-btn:hover,
        .close-btn:focus-visible { background: rgba(255, 255, 255, 0.22); outline: none; }

        .page-container { width: 100%; margin: 0; padding: 0; }

        .reserve-card {
            width: 100%;
            min-height: calc(100vh - 58px);
            padding: 28px 32px 30px;
        }

        .page-title {
            margin: 0 0 24px 0;
            font-size: 22px;
            font-weight: 700;
            color: var(--ink);
        }

        .form-row {
            display: flex;
            align-items: flex-start;
            gap: 18px;
            margin-bottom: 18px;
        }

        .form-label {
            width: 150px;
            flex-shrink: 0;
            padding-top: 10px;
            font-size: 14px;
            font-weight: 600;
            color: var(--ink);
        }

        .form-control { flex: 1; min-width: 0; }

        .select-input {
            width: 100%;
            max-width: 360px;
            height: 40px;
            padding: 0 12px;
            border: 1.5px solid var(--border);
            border-radius: 9px;
            background: #ffffff;
            color: var(--ink);
            font: inherit;
            font-size: 14px;
        }
        .select-input:focus { border-color: var(--brand-1); outline: none; box-shadow: 0 0 0 3px rgba(79, 70, 229, 0.12); }

        /* ---------- Top panel: the three options ---------- */
        .mode-card {
            margin: 0 0 22px 0;
            padding: 14px 16px;
            border: 1px solid #c7d2fe;
            border-left: 5px solid var(--brand-1);
            border-radius: 10px;
            background: linear-gradient(135deg, #eef2ff 0%, #e0e7ff 100%);
        }
        .mode-options { display: inline-flex; flex-wrap: wrap; gap: 8px; }
        .mode-options input[type="radio"] { position: absolute; opacity: 0; width: 0; height: 0; }
        .mode-options label {
            display: inline-block;
            padding: 9px 16px;
            border: 1.5px solid var(--border);
            border-radius: 8px;
            background: #ffffff;
            color: var(--ink);
            font-size: 13.5px;
            font-weight: 600;
            cursor: pointer;
        }
        .mode-options label:hover { border-color: var(--brand-1); }
        .mode-options input[type="radio"]:checked + label { background: var(--brand-1); border-color: var(--brand-1); color: #ffffff; }
        .mode-options input[type="radio"]:focus-visible + label { outline: 2px solid var(--brand-1); outline-offset: 2px; }

        /* ---------- Entry line: Account | Escrow type | Account no. | Branch | Add ---------- */
        .lines-section { margin-bottom: 18px; }

        .lines-title {
            display: block;
            margin-bottom: 8px;
            font-size: 14px;
            font-weight: 600;
            color: var(--ink);
        }

        .entry-line {
            display: flex;
            align-items: flex-end;
            flex-wrap: wrap;
            gap: 12px;
            margin-bottom: 12px;
            padding: 14px;
            border: 1.5px dashed #c7d2fe;
            border-radius: 9px;
            background: #f8faff;
        }

        .entry-field { display: flex; flex-direction: column; gap: 5px; min-width: 0; }
        .entry-field.wide   { flex: 1.4 1 200px; }
        .entry-field.medium { flex: 1 1 150px; }
        .entry-field.narrow { flex: 0.7 1 110px; }

        .entry-field label {
            font-size: 13px;
            font-weight: 600;
            color: var(--muted);
        }

        .entry-field .select-input { max-width: none; }

        .add-line-button {
            height: 40px;
            padding: 0 18px;
            border: none;
            border-radius: 9px;
            background: linear-gradient(120deg, var(--brand-1), var(--brand-2) 75%);
            color: #ffffff;
            font: inherit;
            font-size: 14px;
            font-weight: 600;
            cursor: pointer;
            white-space: nowrap;
        }
        .add-line-button:hover { filter: brightness(1.08); }

        /* ---------- Grid ---------- */
        .grid-wrap {
            overflow-x: auto;
            border: 1.5px solid var(--border);
            border-radius: 9px;
            background: #ffffff;
        }

        .pay-grid { width: 100%; border-collapse: collapse; font-size: 13px; }

        .pay-grid th {
            padding: 10px 12px;
            background: #f8fafc;
            border-bottom: 1.5px solid var(--border);
            color: var(--muted);
            font-weight: 700;
            text-align: left;
            white-space: nowrap;
        }

        .pay-grid td {
            padding: 10px 12px;
            border-bottom: 1px solid var(--border);
            color: var(--ink);
        }

        .pay-grid tr:last-child td { border-bottom: none; }
        .pay-grid .empty { padding: 18px; text-align: center; color: var(--muted); }
        .pay-grid .seq { width: 40px; text-align: center; color: var(--muted); }
        .pay-grid .del { width: 44px; text-align: center; }
        .pay-grid .code { white-space: nowrap; font-variant-numeric: tabular-nums; }

        .delete-row-btn {
            opacity: 0.75;
            cursor: pointer;
            transition: opacity 0.15s ease, transform 0.1s ease;
        }
        .delete-row-btn:hover { opacity: 1; transform: scale(1.1); }

        /* ---------- Message + Save ---------- */
        .form-message {
            display: block;
            margin-top: 14px;
            font-size: 13px;
            font-weight: 600;
        }
        .form-message.ok { color: #047857; }
        .form-message.err { color: #b91c1c; }

        .form-actions {
            display: flex;
            gap: 12px;
            margin: 28px 0 0 0;
            padding-top: 22px;
            border-top: 1px solid var(--border);
        }

        .save-button {
            padding: 10px 20px;
            min-width: 100px;
            border: none;
            border-radius: 9px;
            background: linear-gradient(120deg, var(--brand-1), var(--brand-2) 75%);
            color: #ffffff;
            font: inherit;
            font-size: 14px;
            font-weight: 600;
            cursor: pointer;
        }
        .save-button:hover { filter: brightness(1.08); }

        @media (max-width: 600px) {
            .reserve-card { padding: 22px 18px; }
            .form-row { flex-direction: column; gap: 6px; }
            .form-label { width: 100%; padding-top: 0; }
            .entry-field.wide, .entry-field.medium, .entry-field.narrow { flex-basis: 100%; }
            .add-line-button { width: 100%; }
        }
    </style>
</head>

<body>
    <form id="form1" runat="server">
        <div class="top-strip">
            <span>Accounts Group</span>
            <asp:ImageButton ID="imgClose"
                runat="server"
                CssClass="close-btn"
                CausesValidation="false"
                ToolTip="Close"
                AlternateText="Close"
                ImageUrl="data:image/svg+xml;utf8,&lt;svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='none' stroke='white' stroke-width='2.5' stroke-linecap='round' stroke-linejoin='round'&gt;&lt;line x1='18' y1='6' x2='6' y2='18'/&gt;&lt;line x1='6' y1='6' x2='18' y2='18'/&gt;&lt;/svg&gt;" />
        </div>

        <div class="page-container">
            <div class="reserve-card">
                <%-- ===== Very top: what to do ===== --%>
                <div class="mode-card">
                    <asp:RadioButtonList ID="rblMode" runat="server" CssClass="mode-options"
                        RepeatDirection="Horizontal" RepeatLayout="Flow"
                        AutoPostBack="true" CausesValidation="false">
                        <asp:ListItem Text="Add New Groups of Accounts" Value="New" Selected="True" />
                        <asp:ListItem Text="Edit Existing Group" Value="Edit" />
                        <asp:ListItem Text="Copy Existing Group" Value="Copy" />
                    </asp:RadioButtonList>
                </div>

                <h1 class="page-title"><asp:Literal ID="litPageTitle" runat="server" Text="New Accounts Group" /></h1>

                <%-- Edit / Copy: the existing group; picking one loads its accounts into the grid --%>
                <asp:Panel ID="pnlExistingGroup" runat="server" CssClass="form-row" Visible="false">
                    <asp:Label ID="lblExistingGroup" runat="server" Text="Group" AssociatedControlID="ddlExistingGroup" CssClass="form-label" />
                    <div class="form-control">
                        <asp:DropDownList ID="ddlExistingGroup" runat="server" CssClass="select-input" AutoPostBack="True" />
                    </div>
                </asp:Panel>

                <%-- Add New / Copy: the name of the new group --%>
                <asp:Panel ID="pnlNewGroupName" runat="server" CssClass="form-row">
                    <asp:Label ID="lblNewGroupName" runat="server" Text="New group name" AssociatedControlID="txtNewGroupName" CssClass="form-label" />
                    <div class="form-control">
                        <asp:TextBox ID="txtNewGroupName" runat="server" CssClass="select-input" MaxLength="200" placeholder="Name of the new accounts group" />
                    </div>
                </asp:Panel>

                <div class="lines-section">
                    <span class="lines-title">Accounts</span>

                    <%-- ===== Entry line: fill in, then "Add" puts it in the grid (Enter also adds) ===== --%>
                    <asp:Panel ID="pnlEntryLine" runat="server" CssClass="entry-line" DefaultButton="btnAddLine">
                        <div class="entry-field wide">
                            <asp:Label ID="lblAccount" runat="server" Text="Account" AssociatedControlID="txtAccount" />
                            <asp:TextBox ID="txtAccount" runat="server" CssClass="select-input" MaxLength="200" />
                        </div>
                        <div class="entry-field medium">
                            <asp:Label ID="lblAccountType" runat="server" Text="Account type" AssociatedControlID="txtAccountType" />
                            <asp:TextBox ID="txtAccountType" runat="server" CssClass="select-input" MaxLength="100" />
                        </div>
                        <div class="entry-field medium">
                            <asp:Label ID="lblEscrowType" runat="server" Text="Escrow type" AssociatedControlID="txtEscrowType" />
                            <asp:TextBox ID="txtEscrowType" runat="server" CssClass="select-input" MaxLength="100" />
                        </div>
                        <div class="entry-field medium">
                            <asp:Label ID="lblAccountNo" runat="server" Text="Account no." AssociatedControlID="txtAccountNo" />
                            <asp:TextBox ID="txtAccountNo" runat="server" CssClass="select-input" MaxLength="50" />
                        </div>
                        <div class="entry-field narrow">
                            <asp:Label ID="lblBranch" runat="server" Text="Branch" AssociatedControlID="txtBranch" />
                            <asp:TextBox ID="txtBranch" runat="server" CssClass="select-input" MaxLength="100" />
                        </div>
                        <asp:Button ID="btnAddLine" runat="server" Text="+ Add" CssClass="add-line-button" CausesValidation="false" />
                    </asp:Panel>

                    <div class="grid-wrap">
                        <asp:GridView ID="gvAccounts" runat="server"
                            CssClass="pay-grid"
                            GridLines="None"
                            AutoGenerateColumns="False"
                            ShowHeaderWhenEmpty="True"
                            EmptyDataText="No accounts yet. Fill in the line above and click Add.">
                            <EmptyDataRowStyle CssClass="empty" />
                            <Columns>
                                <asp:TemplateField HeaderText="" ItemStyle-CssClass="del">
                                    <ItemTemplate>
                                        <asp:ImageButton ID="imgDeleteRow" runat="server"
                                            CssClass="delete-row-btn"
                                            ImageUrl="data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='16' height='16' viewBox='0 0 24 24' fill='none' stroke='%23dc2626' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'%3E%3Cpolyline points='3 6 5 6 21 6'%3E%3C/polyline%3E%3Cpath d='M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6'%3E%3C/path%3E%3Cpath d='M10 11v6'%3E%3C/path%3E%3Cpath d='M14 11v6'%3E%3C/path%3E%3Cpath d='M9 6V4a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v2'%3E%3C/path%3E%3C/svg%3E"
                                            CommandName="DeleteRow"
                                            CommandArgument='<%# Container.DataItemIndex %>'
                                            CausesValidation="false"
                                            AlternateText="Delete row"
                                            ToolTip="Remove this account"
                                            OnClientClick="return confirm('Remove this account from the group?');" />
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField HeaderText="#" ItemStyle-CssClass="seq" HeaderStyle-CssClass="seq">
                                    <ItemTemplate><%# Container.DataItemIndex + 1 %></ItemTemplate>
                                </asp:TemplateField>
                                <asp:BoundField DataField="Account" HeaderText="Account" />
                                <asp:BoundField DataField="AccountType" HeaderText="Account type" />
                                <asp:BoundField DataField="EscrowType" HeaderText="Escrow type" />
                                <asp:BoundField DataField="AccountNo" HeaderText="Account no." ItemStyle-CssClass="code" />
                                <asp:BoundField DataField="Branch" HeaderText="Branch" />
                            </Columns>
                        </asp:GridView>
                    </div>
                </div>

                <asp:Label ID="lblMessage" runat="server" CssClass="form-message" Visible="false" />

                <div class="form-actions">
                    <asp:Button ID="btnSave" runat="server" Text="Save" CssClass="save-button" CausesValidation="false" />
                </div>
            </div>
        </div>
    </form>
</body>
</html>
