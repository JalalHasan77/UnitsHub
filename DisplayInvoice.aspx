<%@ Page Language="VB" AutoEventWireup="false" CodeFile="DisplayInvoice.aspx.vb" Inherits="DisplayInvoice" MaintainScrollPositionOnPostback="true" EnableEventValidation="false" %>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>AutoTransfer</title>
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
            overflow: hidden;            /* the iframe scrolls, not the page */
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



/* Fills everything under the top strip: no margin left, right or bottom */
.page-container {
    width: 100%;
    max-width: none;
    height: calc(100vh - 58px);      /* 58px = .top-strip height */
    margin: 0 0 0 0;
    padding: 0;
    overflow: hidden;
}

/* Shown instead of the frame when no / a missing PDF is requested */
.pdf-message {
    display: block;
    margin: 24px;
    padding: 12px 16px;
    border: 1px solid #fca5a5;
    border-radius: 4px;
    background: #fef2f2;
    color: #b91c1c;
    font-size: 14px;
    font-weight: 600;
}

/* The invoice frame takes the whole container */
.invoice-frame {
    display: block;                  /* no gap under an inline iframe */
    width: 100%;
    height: 100%;
    margin: 0;
    padding: 0;
    border: 0;
    background: #ffffff;
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
            font-size: 26px;
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
            font-size: 14px;
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
            font-size: 12px;
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
            font-size: 17px;
        }

        .info-value {
            display: block;
            font-size: 17px;
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
            font-size: 17px;
            font-weight: 600;
            color: var(--ink);
        }

        .grid-section .grid-wrap {
            width: 100%;
        }

        .grid-section .pay-grid {
            font-size: 15px;
        }

        .grid-section .pay-grid th,
        .grid-section .pay-grid td {
            padding: 12px 14px;
        }

        .grid-section .grid-link {
            font-size: 15px;
        }

        /* ---------- Message bar above "Transactions": green = success, red = problem ---------- */
        .msg-bar {
            display: block;
            margin: 0 0 16px 0;
            padding: 12px 16px;
            border: 1px solid;
            border-radius: 4px;
            font-size: 14px;
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
            font-size: 14px;
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
            font-size: 14px;
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
            font-size: 14px;
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
            font-size: 14px;
            color: var(--ink);
        }

        .at-input {
            flex: 1;
            min-width: 0;
            padding: 8px 10px;
            border: 1px solid #d4d0d8;
            border-radius: 3px;
            background: #f5f3f7;
            font-size: 14px;
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

        .pay-grid tr.selected-row td {
            background: #eef2ff;
            font-weight: 600;
        }

        .selected-note {
            margin: 10px 0 0 0;
            font-size: 13px;
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
            <span>AutoTransfer</span>
            <asp:ImageButton ID="imgClose"
                runat="server"
                CssClass="close-btn"
                CausesValidation="false"
                ToolTip="Close"
                AlternateText="Close"
                ImageUrl="data:image/svg+xml;utf8,&lt;svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='none' stroke='white' stroke-width='2.5' stroke-linecap='round' stroke-linejoin='round'&gt;&lt;line x1='18' y1='6' x2='6' y2='18'/&gt;&lt;line x1='6' y1='6' x2='18' y2='18'/&gt;&lt;/svg&gt;" />
        </div>
        <div class="page-container">
            <iframe id="ifrInvoice" runat="server" class="invoice-frame" title="Invoice"></iframe>
            <asp:Label ID="lblPdfMessage" runat="server" CssClass="pdf-message" Visible="False" />
        </div>
    </form>
</body>
</html>
