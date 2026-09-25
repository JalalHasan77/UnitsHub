<%@ Page Language="VB" AutoEventWireup="false" CodeFile="AssignAccount.aspx.vb" Inherits="AssignAccount" MaintainScrollPositionOnPostback="true" EnableEventValidation="false" %>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Link Account</title>
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
            <span>Link Account</span>
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

                <h1 class="page-title">Link Account</h1>

                <!-- IDs passed in / looked up; kept on the page (hidden) so they survive postbacks -->
                <div class="hidden-fields">
                    <asp:Label ID="lblCID" runat="server" Text=""></asp:Label>
                    <asp:Label ID="lblPID" runat="server" Text=""></asp:Label>
                    <asp:Label ID="lblPRJID" runat="server" Text=""></asp:Label>
                    <asp:Label ID="lblSTATEID" runat="server" Text=""></asp:Label>
                    <asp:Label ID="lblActionID" runat="server" Text=""></asp:Label>
                    <asp:Label ID="lblSelectedAccount" runat="server" Text=""></asp:Label>
                    <asp:Label ID="lblSelectedBranch" runat="server" Text=""></asp:Label>
                    <asp:Label ID="lblSelectedCustId" runat="server" Text=""></asp:Label>
                </div>

                <div class="info-row">
                    <span class="form-label">Customer Name:</span>
                    <div class="form-control">
                        <asp:Label ID="lblCustomerName" runat="server" CssClass="info-value" Text=""></asp:Label>
                    </div>
                </div>

                <div class="info-row">
                    <span class="form-label">CPR:</span>
                    <div class="form-control">
                        <asp:Label ID="lblCPR" runat="server" CssClass="info-value" Text=""></asp:Label>
                    </div>
                </div>

                <div class="info-row">
                    <span class="form-label">Project:</span>
                    <div class="form-control">
                        <asp:Label ID="lblProject" runat="server" CssClass="info-value" Text=""></asp:Label>
                    </div>
                </div>

                <div class="info-row">
                    <span class="form-label">Unit Reference:</span>
                    <div class="form-control">
                        <asp:Label ID="lblUnitRef" runat="server" CssClass="info-value" Text=""></asp:Label>
                    </div>
                </div>

                <div class="grid-section">
                    <span class="grid-section-title">Accounts:</span>
                    <div class="grid-wrap">
                        <asp:GridView ID="gvAccounts"
                            runat="server"
                            AutoGenerateColumns="False"
                            CssClass="pay-grid"
                            GridLines="None"
                            ShowHeaderWhenEmpty="True"
                            DataKeyNames="ACCOUNT,BRANCH,CUST_ID"
                            EmptyDataText="No accounts found for this CPR.">
                            <EmptyDataRowStyle CssClass="empty" />
                            <SelectedRowStyle CssClass="selected-row" />
                            <Columns>
                                <asp:ButtonField ButtonType="Link" Text="Select" CommandName="SelectAccount"
                                    ControlStyle-CssClass="grid-link" />
                                <asp:BoundField DataField="ACCOUNT" HeaderText="Account" />
                                <asp:BoundField DataField="BRANCH" HeaderText="Branch" />
                                <asp:BoundField DataField="PROPERTYREF" HeaderText="Property Ref" />
                                <asp:BoundField DataField="CUST_ID" HeaderText="Customer ID" />
                            </Columns>
                        </asp:GridView>
                    </div>
                    <asp:Label ID="lblSelectedNote" runat="server" CssClass="selected-note" Text=""></asp:Label>
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
