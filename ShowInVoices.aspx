<%@ Page Language="VB" AutoEventWireup="false" CodeFile="ShowInVoices.aspx.vb" Inherits="ShowInVoices" MaintainScrollPositionOnPostback="true" EnableEventValidation="false" %>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Invoices</title>
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
            font-size: 19px;
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
            margin: 0 0 28px 0;
            font-size: 25px;
            font-weight: 700;
            color: var(--ink);
        }

        .customer-actions {
            display: flex;
            gap: 12px;
            margin-bottom: 28px;
            padding-bottom: 22px;
            border-bottom: 1px solid var(--border);
        }

        .customer-button {
            padding: 10px 20px;
            border-radius: 9px;
            border: 1px solid var(--brand-1);
            background: #ffffff;
            color: var(--brand-1);
            font-size: 13px;
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
            font-size: 13px;
            font-weight: 600;
            color: var(--ink);
        }

        .form-control {
            flex: 1;
            min-width: 0;
        }

        .txt-input,
        .ddl-input,
        .comments-input {
            width: 100%;
            max-width: 520px;
            padding: 10px 13px;
            border: 1.5px solid var(--border);
            border-radius: 9px;
            background: #f8fafc;
            color: var(--ink);
            font-size: 13px;
            font-family: inherit;
        }

        .txt-input:focus,
        .ddl-input:focus,
        .comments-input:focus {
            outline: none;
            border-color: var(--brand-2);
            background: #ffffff;
            box-shadow: 0 0 0 4px rgba(59, 130, 246, 0.14);
        }

        .ddl-input {
            max-width: 300px;
            cursor: pointer;
        }

        .date-picker {
            position: relative;
            display: inline-block;
            width: 100%;
            max-width: 200px;
        }

        .date-picker .txt-input {
            padding-right: 42px;
            cursor: pointer;
        }

        .date-picker-btn {
            position: absolute;
            top: 50%;
            right: 6px;
            transform: translateY(-50%);
            display: flex;
            align-items: center;
            justify-content: center;
            width: 30px;
            height: 30px;
            padding: 0;
            border: none;
            border-radius: 7px;
            background: transparent;
            color: var(--brand-1);
            cursor: pointer;
            transition: background .15s ease;
        }

        .date-picker-btn:hover,
        .date-picker-btn:focus-visible {
            background: #eef2ff;
            outline: none;
        }

        .date-picker-btn svg {
            width: 18px;
            height: 18px;
        }

        /* Native date input: kept in the layout (so the calendar anchors to the textbox) but invisible */
        .date-picker-native {
            position: absolute;
            left: 0;
            bottom: 0;
            width: 100%;
            height: 100%;
            opacity: 0;
            pointer-events: none;
        }

        .grid-toolbar {
            display: flex;
            justify-content: flex-end;
            margin-bottom: 8px;
        }

        .grid-link {
            font-size: 12px;
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
            font-size: 12px;
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

        .comments-input {
            max-width: 100%;
            min-height: 120px;
            resize: vertical;
        }

        .form-actions {
            display: flex;
            gap: 12px;
            margin: 28px 0 0 168px;
            padding-top: 22px;
            border-top: 1px solid var(--border);
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

        .required-note {
            margin: -6px 0 18px 168px;
            font-size: 11px;
            color: var(--muted);
        }

        /* Title / value rows (Customer Name, CPR, Project, Unit Reference) */
        .info-row {
            display: flex;
            align-items: baseline;
            gap: 32px;
            margin-bottom: 20px;
        }

        .info-row .form-label {
            width: 190px;
            padding-top: 0;
            font-size: 16px;
        }

        .info-value {
            display: block;
            font-size: 16px;
            color: var(--ink);
            min-height: 22px;
        }

        /* Accounts: title on its own line, grid full width underneath */
        .grid-section {
            margin-top: 28px;
        }

        .grid-section-title {
            display: block;
            margin-bottom: 12px;
            font-size: 16px;
            font-weight: 600;
            color: var(--ink);
        }

        .grid-section .grid-wrap {
            width: 100%;
        }

        .grid-section .pay-grid {
            font-size: 14px;
        }

        .grid-section .pay-grid th,
        .grid-section .pay-grid td {
            padding: 12px 14px;
        }

        .grid-section .grid-link {
            font-size: 14px;
        }

        /* ---------- Message bar above "Transactions": green = success, red = problem ---------- */
        .msg-bar {
            display: block;
            margin: 0 0 16px 0;
            padding: 12px 16px;
            border: 1px solid;
            border-radius: 4px;
            font-size: 13px;
            font-weight: 600;
        }

        .msg-bar.msg-success {
            background: #ecfdf3;
            border-color: #86efac;
            color: #15803d;
        }

        .msg-bar.msg-error {
            background: #fef2f2;
            border-color: #fca5a5;
            color: #b91c1c;
        }

        /* ---------- Transactions: action buttons under the title ---------- */
        .tx-actions {
            display: flex;
            flex-wrap: wrap;
            gap: 10px;
            margin-bottom: 12px;
        }

        .tx-btn {
            padding: 8px 18px;
            border: 1.5px solid var(--brand-1);
            border-radius: 8px;
            background: #ffffff;
            color: var(--brand-1);
            font-size: 13px;
            font-weight: 600;
            font-family: inherit;
            cursor: pointer;
        }

        .tx-btn:hover {
            border-color: var(--brand-2);
            color: var(--brand-2);
        }

        .tx-btn-primary {
            background: linear-gradient(120deg, var(--brand-1), var(--brand-2));
            border-color: transparent;
            color: #ffffff;
        }

        .tx-btn-primary:hover {
            color: #ffffff;
            filter: brightness(1.08);
        }

        /* Disabled buttons: greyed out and not clickable */
        .tx-btn[disabled],
        .tx-btn.aspNetDisabled {
            background: #eef0f3;
            border-color: #d4d7dd;
            color: #9aa1ab;
            cursor: not-allowed;
            filter: none;
        }

        /* ---------- Customer box ---------- */
        .customer-box {
            margin: 0 0 20px 0;
            padding: 8px 16px 14px 16px;
            border: 1px solid #a0a0a0;
            border-radius: 2px;
        }

        .customer-box legend {
            padding: 0 4px;
            font-size: 13px;
            color: var(--ink);
        }

        /* Two columns per box. Each column is its own grid: the title column is as
           wide as the longest title in THAT column, so its colons line up right after
           that longest title, and all values in the column start at the same point. */
        .kv-columns {
            display: grid;
            grid-template-columns: repeat(2, minmax(0, 1fr));
            column-gap: 40px;
            margin-top: 14px;
            font-size: 13px;
            line-height: 1.6;
        }

        .kv-col {
            display: grid;
            grid-template-columns: max-content 1fr;
            column-gap: 10px;
            row-gap: 2px;
            align-items: center;
            align-content: start;
            margin: 0;
            padding: 0;
            list-style: none;
        }

        .kv-col li {
            display: contents;
        }

        .kv-col li strong {
            display: flex;
            justify-content: space-between;
            gap: 4px;
            font-weight: 700;
        }

        /* bullet in front of each title */
        .kv-col li strong > span:first-child::before {
            content: "\2022";
            margin-right: 8px;
            font-weight: 400;
        }

        @media (max-width: 700px) {
            .kv-columns { grid-template-columns: 1fr; row-gap: 2px; }
        }

        /* ---------- Unit / amounts: two columns of label + box ---------- */
        .at-grid {
            display: grid;
            grid-template-columns: repeat(2, minmax(0, 1fr));
            column-gap: 12px;
            row-gap: 12px;
        }

        .at-field {
            display: flex;
            align-items: center;
            gap: 8px;
        }

        .at-label {
            flex: 0 0 105px;
            font-size: 13px;
            color: var(--ink);
        }

        .at-input {
            flex: 1;
            min-width: 0;
            padding: 8px 10px;
            border: 1px solid #d4d0d8;
            border-radius: 3px;
            background: #f5f3f7;
            font-size: 13px;
            font-family: inherit;
            color: var(--ink);
        }

        .at-input.at-disabled {
            background: #d3d3d3;
            border-color: #c4c4c4;
        }

        .at-input.at-editable {
            background: #ffffff;
        }

        .at-input.at-editable:focus {
            outline: none;
            border-color: var(--brand-2);
        }

        @media (max-width: 700px) {
            .at-grid { grid-template-columns: 1fr; }
        }

        .hidden-fields {
            display: none;
        }

        /* ---------- Unit History ---------- */
        .history-sub {
            margin: -18px 0 18px 0;
            color: var(--muted);
            font-size: 13px;
        }

        .history-grid td { vertical-align: top; }

        /* Header: dark blue with white text */
        .history-grid th {
            background: #1a1464;
            color: #ffffff;
            border-bottom: none;
        }

        /* Alternating rows (row 1 is the header, so the first data row is row 2 = white) */
        .history-grid tr:nth-child(even) td { background: #ffffff; }
        .history-grid tr:nth-child(odd) td { background: #f1f4fb; }
        .history-grid .when { white-space: nowrap; color: var(--muted); }
        .history-grid .when .d { display: block; color: var(--ink); font-weight: 600; }
        .history-grid .when .t { display: block; font-size: 11px; }
        .history-grid .action { min-width: 130px; }
        .history-grid .summary { min-width: 260px; }

        /* Invoices grid */
        .history-grid .seq { width: 50px; color: var(--muted); }
        .history-grid .amount { white-space: nowrap; font-weight: 600; }
        .invoice-link {
            color: #1a1464;
            font-weight: 600;
            text-decoration: underline;
            text-underline-offset: 3px;
            cursor: pointer;
        }
        .invoice-link:hover { color: #3b82f6; }
        .no-invoice { color: var(--ink); }

        /* Status card, same look as the cards on MainPage: status name on the card colour,
           sub-status in a rounded box underneath */
        .st-card {
            display: inline-block;
            min-width: 150px;
            max-width: 220px;
            padding: 8px 10px;
            border-radius: 12px;
            background: #e2e8f0;
            color: #1e293b;
            text-align: center;
        }

        .st-card .st-title {
            display: block;
            font-size: 14px;
            font-weight: 700;
            text-decoration: underline;
            text-underline-offset: 3px;
        }

        .st-card .st-sub {
            display: block;
            margin-top: 6px;
            padding: 5px 8px;
            border-radius: 8px;
            background: #ffffff;
            color: #1e293b;
            font-size: 11px;
            line-height: 1.3;
        }

        .pay-grid tr.selected-row td {
            background: #eef2ff;
            font-weight: 600;
        }

        .selected-note {
            margin: 10px 0 0 0;
            font-size: 12px;
            color: var(--brand-1);
            font-weight: 600;
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

            .required-note {
                margin-left: 0;
            }

            .info-row {
                flex-direction: column;
                gap: 6px;
            }

            .info-row .form-label {
                width: 100%;
            }

            .form-actions {
                margin-left: 0;
            }

            .customer-actions {
                flex-direction: column;
            }

            .customer-button {
                width: 100%;
            }
        }
    </style>
</head>

<body>
    <form id="form1" runat="server">
        <div class="top-strip">
            <span>Invoices</span>
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

                <!-- Values passed in from MainPage - kept on the page, not shown -->
                <div class="hidden-fields">
                    <asp:Label ID="lblPID" runat="server" Text=""></asp:Label>
                    <asp:Label ID="lblPRJID" runat="server" Text=""></asp:Label>
                    <asp:Label ID="lblSTATEID" runat="server" Text=""></asp:Label>
                    <asp:Label ID="lblActionID" runat="server" Text=""></asp:Label>
                </div>

                <h1 class="page-title">Invoices</h1>
                <div class="history-sub"><asp:Literal ID="litInvoicesFor" runat="server" /></div>

                <div class="grid-wrap">
                    <asp:GridView ID="gvInvoices"
                        runat="server"
                        AutoGenerateColumns="False"
                        CssClass="pay-grid history-grid"
                        GridLines="None"
                        ShowHeaderWhenEmpty="True"
                        DataKeyNames="INVOICENUM"
                        EmptyDataText="No payments recorded for this unit yet.">
                        <EmptyDataRowStyle CssClass="empty" />
                        <Columns>
                            <asp:BoundField DataField="SEQ_NO" HeaderText="Seq" ItemStyle-CssClass="seq" />
                            <asp:BoundField DataField="DATE_TEXT" HeaderText="Date" ItemStyle-CssClass="when" />
                            <asp:TemplateField HeaderText="Description">
                                <ItemTemplate>
                                    <asp:LinkButton ID="lnkInvoice" runat="server" CssClass="invoice-link" CausesValidation="false" />
                                    <asp:Label ID="lblNoInvoice" runat="server" CssClass="no-invoice" Visible="false" />
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:BoundField DataField="INVOICENUM" HeaderText="Invoice No." />
                            <asp:BoundField DataField="AMOUNT_TEXT" HeaderText="Amount" HeaderStyle-CssClass="num" ItemStyle-CssClass="num amount" />
                        </Columns>
                    </asp:GridView>
                </div>

            </div>
        </div>

        <script type="text/javascript">
            (function () {
                // Keep server code blocks out of this page: they stop server code from adding controls to the form.
                // Each .date-picker wrapper is self-contained, so its parts are found relative to it.

                // The textbox uses yyyy-MM-dd, the same format <input type="date"> uses,
                // so the value only needs validating, not converting.
                function toPickerValue(text) {
                    return /^\d{4}-\d{2}-\d{2}$/.test(text) ? text : '';
                }

                function initDatePicker(wrap) {
                    var txt = wrap.querySelector('.txt-input');
                    var btn = wrap.querySelector('.date-picker-btn');
                    var dtp = wrap.querySelector('.date-picker-native');
                    if (!txt || !btn || !dtp) return;

                    function openPicker() {
                        dtp.value = toPickerValue(txt.value);
                        if (typeof dtp.showPicker === 'function') {
                            dtp.showPicker();
                        } else {
                            // older browsers: fall back to focusing the native control
                            dtp.style.pointerEvents = 'auto';
                            dtp.focus();
                            dtp.click();
                        }
                    }

                    dtp.addEventListener('change', function () {
                        if (dtp.value) {
                            txt.value = dtp.value;
                        }
                    });

                    btn.addEventListener('click', openPicker);
                    txt.addEventListener('click', openPicker);
                }

                // Used by the X button and Cancel. window.close() only works for windows opened by script;
                // otherwise fall back to going back one page.
                window.closeForm = function () {
                    window.close();
                    setTimeout(function () {
                        if (!window.closed && window.history.length > 1) {
                            window.history.back();
                        }
                    }, 150);
                };

                var wraps = document.querySelectorAll('.date-picker');
                for (var i = 0; i < wraps.length; i++) {
                    initDatePicker(wraps[i]);
                }
            })();
        </script>
    </form>
</body>
</html>
