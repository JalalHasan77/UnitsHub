<%@ Page Language="VB" AutoEventWireup="false" CodeFile="LinkPaymentV2.aspx.vb" Inherits="LinkPaymentV2" MaintainScrollPositionOnPostback="true" EnableEventValidation="false" %>

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
    </style>
</head>

<body>
    <form id="form1" runat="server">
        <div class="top-strip">
            <span>Link Payment</span>
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

                <h1 class="page-title">Link Payment</h1>

                <%-- Context passed in from the caller's query string (NodeID/ProjectId/STATEID/ActionId)
                     plus the CONTACT_ID resolved server-side in LoadCustomerFromProperties(). Nothing here
                     is shown to the user - it's just the key payments are loaded/linked/saved against. --%>
                <div style="display:none">
                    <asp:Label ID="lblCID" runat="server" Text=""></asp:Label>
                    <asp:Label ID="lblPID" runat="server" Text=""></asp:Label>
                    <asp:Label ID="lblPRJID" runat="server" Text=""></asp:Label>
                    <asp:Label ID="lblSTATEID" runat="server" Text=""></asp:Label>
                    <asp:Label ID="lblActionID" runat="server" Text=""></asp:Label>
                </div>

                <%-- ===================== PAYMENTS ===================== --%>
                <%-- LOAD: populated by BindPayments() / GetPayments(), called from Page_Load
                           (LinkPaymentV2_aspx.vb) and again after every payment is linked.
                     POPUP: "Link a payment" opens LinkPayment.aspx (registered in
                           RegisterLinkPaymentPopup(), LinkPaymentV2_aspx.vb) which returns the
                           chosen transaction to lnkLinkPayment_Click, where it is written to
                           UNITSHUB_PAYMENTS.
                     SAVE: btnSave_Click reports back to this page's own opener (via
                           VendorPopupHelper.RegisterPopupSelectionAndClose) whether every
                           required payment line is now linked. --%>
                <asp:Panel ID="pnlPayments" runat="server" CssClass="form-row" Visible="False">
                    <asp:Label ID="lblPayments"
                        runat="server"
                        Text="Payments:"
                        CssClass="form-label" />
                    <div class="form-control">
                        <div class="grid-toolbar">
                            <asp:LinkButton ID="lnkLinkPayment"
                                runat="server"
                                CssClass="grid-link"
                                CausesValidation="False">Link a payment</asp:LinkButton>
                        </div>
                        <asp:Label ID="lblPaymentMessage"
                            runat="server"
                            CssClass="payment-message"
                            EnableViewState="False"
                            Text="" />
                        <div class="grid-wrap">
                            <asp:GridView ID="gvPayments"
                                runat="server"
                                AutoGenerateColumns="False"
                                CssClass="pay-grid"
                                GridLines="None"
                                CellSpacing="0"
                                UseAccessibleHeader="True"
                                EmptyDataText="No payments found."
                                EmptyDataRowStyle-CssClass="empty">
                                <Columns>
                                    <asp:BoundField DataField="SEQ" HeaderText="Seq" />
                                    <asp:BoundField DataField="DESCRIPTION" HeaderText="Description" />
                                    <asp:BoundField DataField="AMOUNT" HeaderText="Amount"
                                        DataFormatString="{0:N3}" HtmlEncode="False"
                                        HeaderStyle-CssClass="num" ItemStyle-CssClass="num" />
                                    <asp:BoundField DataField="DatePaid" HeaderText="Date Paid"
                                        DataFormatString="{0:yyyy-MM-dd}" HtmlEncode="False" />
                                    <asp:BoundField DataField="TimePaid" HeaderText="Time Paid" />
                                    <asp:BoundField DataField="Narrative" HeaderText="Narrative" />
                                </Columns>
                            </asp:GridView>
                        </div>
                    </div>
                </asp:Panel>

                <div class="form-actions">
                    <asp:Button ID="btnSave"
                        runat="server"
                        Text="Save"
                        CssClass="customer-button save-button" />

                    <asp:Button ID="btnCancel"
                            runat="server"
                            Text="Cancel"
                            CausesValidation="false"
                            CssClass="customer-button cancel-button" />
                </div>

            </div>
        </div>
    </form>
</body>
</html>
