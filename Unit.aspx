<%@ Page Language="VB" AutoEventWireup="false" CodeFile="Unit.aspx.vb" Inherits="Unit" MaintainScrollPositionOnPostback="true" EnableEventValidation="false" %>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Unit</title>
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

        .hint-empty { display: block; color: var(--muted); padding: 10px 0 2px; }
        .customer-box + .customer-box { margin-top: 4px; }


        /* ---------- Tabs under UNIT DETAILS (same structure as ContactMaintenance) ---------- */
        .unit-tabs { margin-top: 18px; }

        .itemCell, .itemCellSelecred, .itemCellSelecredBottmBorder {
            text-align: center;
            vertical-align: middle;
        }

        .itemCell a, .itemCellSelecred a, .itemCellSelecredBottmBorder a {
            display: block;
            text-align: center;
            line-height: 46px;
            font-size: 14px;
            font-weight: 600;
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

        .tab-panel {
            background-color: #ffffff;
            padding: 14px 15px 16px 15px;
            border-left: 1px solid #000000;
            border-right: 1px solid #000000;
            border-bottom: 1px solid #000000;
        }

        .tab-note { display: block; color: var(--muted); padding: 8px 2px; }

        /* Attachments toolbar: filter left, add button right */
        .tab-toolbar {
            display: flex;
            align-items: center;
            justify-content: space-between;
            gap: 12px;
            margin-bottom: 12px;
        }

        .tab-toolbar-left { display: flex; align-items: center; gap: 10px; }

        /* Comments toolbar: title and "add" button together on the far left */
        .tab-toolbar.tab-toolbar-start { justify-content: flex-start; gap: 10px; }

        /* ---------- Comments: one 3-row box per comment ---------- */
        .comment-list { display: flex; flex-direction: column; gap: 14px; }

        .comment-card {
            border: 1.5px solid var(--border);
            border-radius: 9px;
            overflow: hidden;
            background: #ffffff;
        }

        .comment-table {
            width: 100%;
            border-collapse: collapse;
            table-layout: fixed;
            font-size: 14px;
        }

        .comment-table td {
            padding: 10px 14px;
            border-bottom: 1px solid var(--border);
            text-align: left;
            vertical-align: top;
            color: var(--ink);
        }

        .comment-table tr:last-child td { border-bottom: none; }

        /* Row 1: who and when */
        .comment-table .comment-head td {
            background: #1a1464;
            color: #ffffff;
            font-size: 13px;
        }
        .comment-head .lbl { font-weight: 700; }

        /* Row 2: the comment itself, line breaks kept */
        .comment-table .comment-body td {
            white-space: pre-wrap;
            word-break: break-word;
            line-height: 1.5;
        }

        /* Row 3: attachments side by side, separated by commas */
        .comment-table .comment-files td {
            background: #f8fafc;
            font-size: 13px;
            word-break: break-word;
        }
        .comment-files .lbl { font-weight: 700; margin-right: 6px; }
        .comment-files .none { color: var(--muted); }
        .tab-toolbar-label { font-size: 13px; font-weight: 600; color: var(--ink); }

        .tab-ddl {
            min-width: 220px;
            height: 34px;
            padding: 0 10px;
            border: 1.5px solid #e2e8f0;
            border-radius: 8px;
            background: #ffffff;
            color: var(--ink);
            font: inherit;
            font-size: 13px;
        }

        /* List | Icons switch */
        .view-toggle {
            display: inline-flex;
            border: 1.5px solid #e2e8f0;
            border-radius: 8px;
            overflow: hidden;
            background: #ffffff;
        }

        .view-btn {
            display: inline-flex;
            align-items: center;
            gap: 6px;
            height: 31px;
            padding: 0 12px;
            color: var(--muted);
            font-size: 12px;
            font-weight: 600;
            text-decoration: none;
        }

        .view-btn + .view-btn { border-left: 1.5px solid #e2e8f0; }
        .view-btn svg { width: 15px; height: 15px; fill: none; stroke: currentColor; stroke-width: 2; stroke-linecap: round; }
        .view-btn:hover { color: #1a1464; background: #f1f4fb; }
        .view-btn.active { background: #1a1464; color: #ffffff; }

        /* Icons view: one tile per file */
        .file-tiles {
            display: grid;
            grid-template-columns: repeat(auto-fill, minmax(150px, 1fr));
            gap: 12px;
            margin-bottom: 4px;
        }

        .file-tile {
            display: flex;
            flex-direction: column;
            align-items: center;
            gap: 6px;
            padding: 14px 10px 12px;
            border: 1.5px solid #e2e8f0;
            border-radius: 10px;
            background: #ffffff;
            text-align: center;
        }

        .file-tile:hover { border-color: #1a1464; }
        a.file-tile { text-decoration: none; cursor: pointer; }

        .file-link {
            color: #1a1464;
            font-weight: 600;
            text-decoration: underline;
            text-underline-offset: 3px;
            cursor: pointer;
        }
        .file-link:hover { color: #3b82f6; }

        .file-icon {
            position: relative;
            width: 44px;
            height: 54px;
            border-radius: 4px 14px 4px 4px;
            background: #94a3b8;
            color: #ffffff;
            font-size: 10px;
            font-weight: 700;
            line-height: 54px;
            letter-spacing: .03em;
        }

        .file-icon.pdf { background: #dc2626; }
        .file-icon.img { background: #16a34a; }
        .file-icon.doc { background: #2563eb; }
        .file-icon.xls { background: #15803d; }

        .file-tile .name {
            max-width: 100%;
            font-size: 12px;
            font-weight: 600;
            color: var(--ink);
            word-break: break-word;
        }

        .file-tile .doc { font-size: 11px; color: var(--muted); }

        .add-btn {
            width: 34px;
            height: 34px;
            padding: 7px;
            border-radius: 50%;
            background: #1a1464;
            cursor: pointer;
        }

        .add-btn:hover,
        .add-btn:focus-visible { background: #2a2390; outline: none; }


        .doc-category {
            margin: 14px 0 6px 0;
            font-size: 13px;
            font-weight: 700;
            color: #1a1464;
            text-transform: uppercase;
            letter-spacing: .03em;
        }
        .doc-category:first-child { margin-top: 2px; }

        .file-chip {
            display: inline-block;
            margin: 2px 6px 2px 0;
            padding: 2px 8px;
            border-radius: 6px;
            background: #f1f5f9;
            color: var(--ink);
            font-size: 12px;
            white-space: nowrap;
        }

        .about-tag {
            display: inline-block;
            padding: 2px 8px;
            border-radius: 6px;
            font-size: 11px;
            white-space: nowrap;
            background: #eef2ff;
            color: #3c3489;
        }

        /* ---------- History grid (same look as ShowHistory) ---------- */
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
        .history-grid .seq { width: 50px; color: var(--muted); }
        .history-grid .when .d { display: block; color: var(--ink); font-weight: 600; }
        .history-grid .when .t { display: block; font-size: 11px; }
        .history-grid .action { min-width: 130px; }
        .history-grid .summary { min-width: 260px; }

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
            <span>Unit</span>
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

                <h1 class="page-title"><asp:Literal ID="litUnitTitle" runat="server" Text="Unit" /></h1>

                <fieldset class="customer-box">
                    <legend>PROJECT</legend>
                    <div class="kv-columns">
                        <ul class="kv-col">
                            <li><strong><span>Project</span><span>:</span></strong> <asp:Label ID="lblProjectName" runat="server" Text=""></asp:Label></li>
                        </ul>
                        <ul class="kv-col">
                            <li><strong><span>Project ID</span><span>:</span></strong> <asp:Label ID="lblProjectId" runat="server" Text=""></asp:Label></li>
                        </ul>
                    </div>
                </fieldset>

                <fieldset class="customer-box">
                    <legend>UNIT DETAILS</legend>
                    <%-- Attributes split over two columns: first half left, second half right --%>
                    <div class="kv-columns">
                        <ul class="kv-col">
                            <asp:Repeater ID="rptAttributesLeft" runat="server" OnItemDataBound="rptAttributes_ItemDataBound">
                                <ItemTemplate>
                                    <li><strong><span><asp:Literal ID="litName" runat="server" /></span><span>:</span></strong> <asp:Literal ID="litValue" runat="server" /></li>
                                </ItemTemplate>
                            </asp:Repeater>
                        </ul>
                        <ul class="kv-col">
                            <asp:Repeater ID="rptAttributesRight" runat="server" OnItemDataBound="rptAttributes_ItemDataBound">
                                <ItemTemplate>
                                    <li><strong><span><asp:Literal ID="litName" runat="server" /></span><span>:</span></strong> <asp:Literal ID="litValue" runat="server" /></li>
                                </ItemTemplate>
                            </asp:Repeater>
                        </ul>
                    </div>
                    <asp:Label ID="lblNoAttributes" runat="server" CssClass="hint-empty" Text="No details found for this unit." Visible="false" />
                </fieldset>

                <%-- ===================== Tabs: Attachments | Comments | History =====================
                     Same Menu + MultiView structure as ContactMaintenance (Menu3 / MultiView3) --%>
                <div class="unit-tabs">
                    <table cellpadding="0" cellspacing="0" style="width:100%;border-width:0">
                        <tr>
                            <td style="width:100%">
                                <asp:Menu ID="menuUnitTabs" runat="server" Font-Names="Arial" Height="46px" Orientation="Horizontal" Width="100%" CssClass="full-width-menu"
                                    OnMenuItemClick="menuUnitTabs_MenuItemClick">
                                    <Items>
                                        <asp:MenuItem Text="Attachments" Value="Attachments" Selected="true"></asp:MenuItem>
                                        <asp:MenuItem Text="Comments" Value="Comments"></asp:MenuItem>
                                        <asp:MenuItem Text="History" Value="History"></asp:MenuItem>
                                    </Items>
                                    <StaticHoverStyle BackColor="#000099" BorderColor="#000099" BorderWidth="1px" CssClass="itemCell" ForeColor="#F2F2F2" Height="46px" />
                                    <StaticMenuItemStyle BorderWidth="1px" CssClass="itemCell" Height="46px" HorizontalPadding="0px" ItemSpacing="0px" VerticalPadding="0px" BackColor="#F2F2F2" />
                                    <StaticMenuStyle BorderWidth="0px" Height="46px" HorizontalPadding="0px" VerticalPadding="0px" Width="100%" />
                                    <StaticSelectedStyle BackColor="White" CssClass="itemCellSelecred itemCellSelecredBottmBorder" Height="46px" HorizontalPadding="0px" ItemSpacing="0px" VerticalPadding="0px" />
                                </asp:Menu>
                            </td>
                        </tr>
                    </table>

                    <div class="tab-panel">
                        <asp:MultiView ID="mvUnitTabs" runat="server" ActiveViewIndex="0">

                            <%-- ---------- Attachments: documents grouped by category ---------- --%>
                            <asp:View ID="viewAttachments" runat="server">
                                <%-- Toolbar: category filter on the left, "add attachment" on the right --%>
                                <div class="tab-toolbar">
                                    <div class="tab-toolbar-left">
                                        <asp:Label ID="lblDisplayAttachments" runat="server" Text="Display Attachments" AssociatedControlID="ddlDocCategory" CssClass="tab-toolbar-label" />
                                        <asp:DropDownList ID="ddlDocCategory" runat="server" CssClass="tab-ddl" AutoPostBack="true"
                                            OnSelectedIndexChanged="ddlDocCategory_SelectedIndexChanged" />

                                        <%-- List / Icons switch for the attachments below --%>
                                        <div class="view-toggle" role="group" aria-label="Attachments view">
                                            <asp:LinkButton ID="lnkViewList" runat="server" CssClass="view-btn active" ToolTip="Show as list"
                                                CausesValidation="false" OnClick="lnkViewList_Click"><svg viewBox="0 0 24 24" aria-hidden="true"><line x1="8" y1="6" x2="21" y2="6"/><line x1="8" y1="12" x2="21" y2="12"/><line x1="8" y1="18" x2="21" y2="18"/><circle cx="4" cy="6" r="1"/><circle cx="4" cy="12" r="1"/><circle cx="4" cy="18" r="1"/></svg><span>List</span></asp:LinkButton>
                                            <asp:LinkButton ID="lnkViewIcons" runat="server" CssClass="view-btn" ToolTip="Show as icons"
                                                CausesValidation="false" OnClick="lnkViewIcons_Click"><svg viewBox="0 0 24 24" aria-hidden="true"><rect x="3" y="3" width="7" height="7" rx="1"/><rect x="14" y="3" width="7" height="7" rx="1"/><rect x="3" y="14" width="7" height="7" rx="1"/><rect x="14" y="14" width="7" height="7" rx="1"/></svg><span>Icons</span></asp:LinkButton>
                                        </div>
                                    </div>
                                    <asp:ImageButton ID="btnAddAttachment"
                                        runat="server"
                                        CssClass="add-btn"
                                        CausesValidation="false"
                                        ToolTip="Add a new attachment"
                                        AlternateText="Add attachment"
                                        OnClick="btnAddAttachment_Click"
                                        ImageUrl="data:image/svg+xml;utf8,&lt;svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='none' stroke='white' stroke-width='2.5' stroke-linecap='round' stroke-linejoin='round'&gt;&lt;line x1='12' y1='5' x2='12' y2='19'/&gt;&lt;line x1='5' y1='12' x2='19' y2='12'/&gt;&lt;/svg&gt;" />
                                </div>
                                <asp:Literal ID="litAttachments" runat="server" />

                                <%-- One block per category; inside it either the list (grid) or the icons (tiles).
                                     Each file is a link that opens it in the DisplayInvoice viewer. --%>
                                <asp:Repeater ID="rptAttachCategories" runat="server" OnItemDataBound="rptAttachCategories_ItemDataBound">
                                    <ItemTemplate>
                                        <div class="doc-category"><asp:Literal ID="litCategory" runat="server" /></div>

                                        <asp:Panel ID="pnlList" runat="server" CssClass="grid-wrap">
                                            <asp:GridView ID="gvAttachFiles" runat="server"
                                                AutoGenerateColumns="False"
                                                CssClass="pay-grid history-grid"
                                                GridLines="None"
                                                OnRowDataBound="gvAttachFiles_RowDataBound">
                                                <Columns>
                                                    <asp:BoundField DataField="SEQ_NO" HeaderText="Seq" ItemStyle-CssClass="seq" />
                                                    <asp:BoundField DataField="DATE_TEXT" HeaderText="Date" ItemStyle-CssClass="when" />
                                                    <asp:BoundField DataField="TIME_TEXT" HeaderText="Time" ItemStyle-CssClass="when" />
                                                    <asp:BoundField DataField="ADDED_BY" HeaderText="Added By" />
                                                    <asp:TemplateField HeaderText="Document">
                                                        <ItemTemplate>
                                                            <asp:LinkButton ID="lnkFile" runat="server" CssClass="file-link" CausesValidation="false" />
                                                        </ItemTemplate>
                                                    </asp:TemplateField>
                                                </Columns>
                                            </asp:GridView>
                                        </asp:Panel>

                                        <asp:Panel ID="pnlIcons" runat="server" CssClass="file-tiles">
                                            <asp:Repeater ID="rptTiles" runat="server" OnItemDataBound="rptTiles_ItemDataBound">
                                                <ItemTemplate>
                                                    <asp:LinkButton ID="lnkTile" runat="server" CssClass="file-tile" CausesValidation="false">
                                                        <asp:Literal ID="litTile" runat="server" />
                                                    </asp:LinkButton>
                                                </ItemTemplate>
                                            </asp:Repeater>
                                        </asp:Panel>
                                    </ItemTemplate>
                                </asp:Repeater>
                            </asp:View>

                            <%-- ---------- Comments ---------- --%>
                            <asp:View ID="viewComments" runat="server">
                                <%-- Toolbar: title and "Add Comment" button on the far left (button opens AddComment) --%>
                                <div class="tab-toolbar tab-toolbar-start">
                                    <span class="tab-toolbar-label">Comments</span>
                                    <asp:ImageButton ID="btnAddComment"
                                        runat="server"
                                        CssClass="add-btn"
                                        CausesValidation="false"
                                        ToolTip="Add Comment"
                                        AlternateText="Add Comment"
                                        OnClick="btnAddComment_Click"
                                        ImageUrl="data:image/svg+xml;utf8,&lt;svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='none' stroke='white' stroke-width='2.5' stroke-linecap='round' stroke-linejoin='round'&gt;&lt;line x1='12' y1='5' x2='12' y2='19'/&gt;&lt;line x1='5' y1='12' x2='19' y2='12'/&gt;&lt;/svg&gt;" />
                                </div>

                                <%-- One box per comment, three rows:
                                       1. Comment by: name, ID(user id), On: date and time
                                       2. Comment text
                                       3. Attachments as links side by side, separated by ", " --%>
                                <div class="comment-list">
                                    <asp:Repeater ID="rptComments" runat="server" OnItemDataBound="rptComments_ItemDataBound">
                                        <ItemTemplate>
                                            <div class="comment-card">
                                                <table class="comment-table">
                                                    <tr class="comment-head">
                                                        <td><asp:Literal ID="litCommentHead" runat="server" /></td>
                                                    </tr>
                                                    <tr class="comment-body">
                                                        <td><asp:Literal ID="litCommentText" runat="server" /></td>
                                                    </tr>
                                                    <tr class="comment-files">
                                                        <td>
                                                            <span class="lbl">Attachments:</span>
                                                            <asp:Repeater ID="rptCommentFiles" runat="server" OnItemDataBound="rptCommentFiles_ItemDataBound">
                                                                <ItemTemplate><asp:LinkButton ID="lnkCommentFile" runat="server" CssClass="file-link" CausesValidation="false" /></ItemTemplate>
                                                                <SeparatorTemplate>, </SeparatorTemplate>
                                                            </asp:Repeater>
                                                            <asp:Label ID="lblNoFiles" runat="server" CssClass="none" Text="None" Visible="false" />
                                                        </td>
                                                    </tr>
                                                </table>
                                            </div>
                                        </ItemTemplate>
                                    </asp:Repeater>
                                </div>
                                <asp:Label ID="lblNoComments" runat="server" CssClass="tab-note" Text="No comments for this unit or its customer yet." Visible="false" />
                                <asp:Label ID="lblCommentsNote" runat="server" CssClass="tab-note" Visible="false" />
                            </asp:View>

                            <%-- ---------- History (same as ShowHistory) ---------- --%>
                            <asp:View ID="viewHistory" runat="server">
                                <div class="grid-wrap">
                                    <asp:GridView ID="gvHistory"
                                        runat="server"
                                        AutoGenerateColumns="False"
                                        CssClass="pay-grid history-grid"
                                        GridLines="None"
                                        ShowHeaderWhenEmpty="True"
                                        EmptyDataText="No history recorded for this unit yet.">
                                        <EmptyDataRowStyle CssClass="empty" />
                                        <Columns>
                                            <asp:BoundField DataField="WHEN_TEXT" HeaderText="Date &amp; time" HtmlEncode="False" ItemStyle-CssClass="when" />
                                            <asp:BoundField DataField="ACTION_TITLE" HeaderText="Action" ItemStyle-CssClass="action" />
                                            <asp:BoundField DataField="FROM_HTML" HeaderText="From" HtmlEncode="False" />
                                            <asp:BoundField DataField="TO_HTML" HeaderText="To" HtmlEncode="False" />
                                            <asp:BoundField DataField="IMPLEMENTED_BY_NAME" HeaderText="Implemented by" />
                                            <asp:BoundField DataField="CUSTOMER_NAME" HeaderText="Customer" />
                                            <asp:BoundField DataField="SUMMARY" HeaderText="Summary" ItemStyle-CssClass="summary" />
                                        </Columns>
                                    </asp:GridView>
                                </div>
                            </asp:View>

                        </asp:MultiView>
                    </div>
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
