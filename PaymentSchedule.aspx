<%@ Page Language="VB" AutoEventWireup="false" CodeFile="PaymentSchedule.aspx.vb" Inherits="PaymentSchedule" MaintainScrollPositionOnPostback="true" EnableEventValidation="false" %>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Link Payment</title>
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
            margin: 0;
            padding: 0;
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
            color: #ffffff;
            cursor: pointer;
            transition: background .15s ease;
        }

        .close-btn:hover,
        .close-btn:focus-visible {
            background: rgba(255, 255, 255, 0.22);
            outline: none;
        }

        .close-btn svg {
            width: 20px;
            height: 20px;
        }



.page-container {
    width: 100%;
    max-width: none;
    margin: 0;
    padding: 0;
}

.reserve-card {
    width: 100%;
    min-height: calc(100vh - 58px);
    margin: 0;
    border: none;
    border-radius: 0;
    box-shadow: none;
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

        .form-control {
            flex: 1;
            min-width: 0;
        }

        .grid-toolbar {
            display: flex;
            justify-content: flex-end;
            margin-bottom: 8px;
        }

        /* Payments: "Payments:" (left) and "Link a payment" (right) on one line,
           the grid underneath using the full width - no label column on the left */
        .payments-section {
            margin-bottom: 18px;
        }

        .payments-head {
            display: flex;
            align-items: center;
            justify-content: space-between;
            gap: 12px;
            margin-bottom: 8px;
        }

        .payments-title {
            font-size: 14px;
            font-weight: 600;
            color: var(--ink);
        }

        /* Disabled links (Enabled = False renders class "aspNetDisabled"): grey, and
           not clickable at all, so no script attached to them can run */
        .grid-link.aspNetDisabled,
        .grid-link[disabled] {
            color: var(--muted);
            opacity: .55;
            cursor: not-allowed;
            pointer-events: none;
            text-decoration: none;
        }

        .payment-message {
            display: block;
            margin-bottom: 8px;
            font-size: 13px;
            font-weight: 600;
            color: #b91c1c;
        }

        .payment-message:empty {
            display: none;
        }

        .grid-link {
            font-size: 13px;
            font-weight: 600;
            color: var(--brand-1);
            text-decoration: none;
            cursor: pointer;
        }

        .grid-link:hover,
        .grid-link:focus-visible {
            color: var(--brand-2);
            text-decoration: underline;
            outline: none;
        }

        .grid-wrap {
            overflow-x: auto;
            border: 1.5px solid var(--border);
            border-radius: 9px;
            background: #ffffff;
        }

        .pay-grid {
            width: 100%;
            border-collapse: collapse;
            font-size: 13px;
        }

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

        .pay-grid tr:last-child td {
            border-bottom: none;
        }

        .pay-grid .num {
            text-align: right;
        }

        .pay-grid .empty {
            padding: 18px;
            text-align: center;
            color: var(--muted);
        }

        .form-actions {
            display: flex;
            gap: 12px;
            margin: 28px 0 0 0;
            padding-top: 22px;
            border-top: 1px solid var(--border);
        }

        .customer-button {
            padding: 10px 20px;
            border-radius: 9px;
            border: 1px solid var(--brand-1);
            background: #ffffff;
            color: var(--brand-1);
            font-size: 14px;
            font-weight: 600;
            font-family: inherit;
            cursor: pointer;
            transition: all .15s ease;
        }

        .customer-button:hover {
            background: #eef2ff;
            border-color: var(--brand-2);
            color: var(--brand-2);
        }

        .customer-button.save-button {
            border-color: transparent;
            background: linear-gradient(120deg, var(--brand-1), var(--brand-2) 75%);
            color: #ffffff;
            min-width: 100px;
        }

        .customer-button.save-button:hover {
            background: linear-gradient(120deg, var(--brand-1), var(--brand-2) 75%);
            color: #ffffff;
            filter: brightness(1.08);
        }

        .customer-button.cancel-button {
            min-width: 100px;
        }

        @media (max-width: 600px) {
            .reserve-card { padding: 22px 18px; }

            .form-row {
                flex-direction: column;
                gap: 6px;
            }

            .form-label {
                width: 100%;
                padding-top: 0;
            }
        }


        /* ---------- Payment schedule (controls from SetupPaymentSchedule) ---------- */
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

        .pay-grid .seq { width: 40px; text-align: center; color: var(--muted); }
        .pay-grid .del { width: 44px; text-align: center; }

        .delete-row-btn {
            opacity: 0.75;
            cursor: pointer;
            transition: opacity 0.15s ease, transform 0.1s ease;
        }
        .delete-row-btn:hover { opacity: 1; transform: scale(1.1); }

        .schedule-message {
            display: block;
            margin-top: 14px;
            font-size: 13px;
            font-weight: 600;
        }
        .schedule-message.ok { color: #047857; }
        .schedule-message.err { color: #b91c1c; }

        /* ---------- Top panel: Add New / Edit Existing / Copy Existing Schedule ---------- */
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
    </style>
</head>

<body>
    <form id="form1" runat="server">
        <div class="top-strip">
            <span>Payment Schedule</span>
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
                        <asp:ListItem Text="Add New" Value="New" Selected="True" />
                        <asp:ListItem Text="Edit Existing" Value="Edit" />
                        <asp:ListItem Text="Copy Existing Schedule" Value="Copy" />
                    </asp:RadioButtonList>
                </div>

                <h1 class="page-title"><asp:Literal ID="litPageTitle" runat="server" Text="New Payment Schedule" /></h1>

                <%-- Edit / Copy: the existing plan; picking one loads its lines into the grid --%>
                <asp:Panel ID="pnlExistingPlan" runat="server" CssClass="form-row" Visible="false">
                    <asp:Label ID="lblPaymentPlan" runat="server" Text="Payment Plan" AssociatedControlID="ddlExistingPlan" CssClass="form-label" />
                    <div class="form-control">
                        <asp:DropDownList ID="ddlExistingPlan" runat="server" CssClass="select-input"
                            AutoPostBack="True" />
                    </div>
                </asp:Panel>

                <%-- Add New / Copy: the name of the new plan --%>
                <asp:Panel ID="pnlNewPlanName" runat="server" CssClass="form-row">
                    <asp:Label ID="lblNewPlanName" runat="server" Text="New plan name" AssociatedControlID="txtNewPlanName" CssClass="form-label" />
                    <div class="form-control">
                        <asp:TextBox ID="txtNewPlanName" runat="server" CssClass="select-input" MaxLength="200" placeholder="Name of the new payment plan" />
                    </div>
                </asp:Panel>

                <%-- Schedule rows: title on the left, "Add Other Expenses/Reduction" (opens AddFormula) on the right --%>
                <div class="payments-section">
                    <div class="payments-head">
                        <span class="payments-title">Schedule</span>
                        <asp:LinkButton ID="btnAddExpRdc" runat="server" CssClass="grid-link" CausesValidation="false">
                            + Add Other Expenses/Reduction
                        </asp:LinkButton>
                    </div>

                    <div class="grid-wrap">
                        <asp:GridView ID="GridView2" runat="server"
                            CssClass="pay-grid"
                            GridLines="None"
                            AutoGenerateColumns="False"
                            ShowHeaderWhenEmpty="True"
                            EmptyDataText="No lines yet. Choose a payment plan, or add an expense / reduction.">
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
                                            ToolTip="Delete this row"
                                            OnClientClick="return confirm('Delete this row?');" />
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField HeaderText="#" ItemStyle-CssClass="seq" HeaderStyle-CssClass="seq">
                                    <ItemTemplate><%# Container.DataItemIndex + 1 %></ItemTemplate>
                                </asp:TemplateField>
                                <asp:BoundField DataField="Description" HeaderText="Description" />
                                <asp:BoundField DataField="Formula" HeaderText="Formula" />
                                <asp:BoundField DataField="Comment" HeaderText="Comment" />
                            </Columns>
                        </asp:GridView>
                    </div>
                </div>

                <asp:Label ID="lblScheduleMessage" runat="server" CssClass="schedule-message" Visible="false" />

                <div class="form-actions">
                    <asp:Button ID="btnSave" runat="server" Text="Save" CssClass="customer-button save-button" CausesValidation="false" />
                </div>
            </div>
        </div>
    </form>
</body>
</html>
