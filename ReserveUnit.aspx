<%@ Page Language="VB" AutoEventWireup="false" CodeFile="ReserveUnit.aspx.vb" Inherits="ReserveUnit" MaintainScrollPositionOnPostback="true" EnableEventValidation="false" %>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Reserve A Unit</title>
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
    <div class="top-strip">
        <span>Reserve A Unit</span>
        <button type="button" class="close-btn" title="Close" aria-label="Close" onclick="closeForm();">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"
                stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                <line x1="18" y1="6" x2="6" y2="18"></line>
                <line x1="6" y1="6" x2="18" y2="18"></line>
            </svg>
        </button>
    </div>

    <form id="form1" runat="server">
        <div class="page-container">
            <div class="reserve-card">

                <h1 class="page-title">Reserve A Unit</h1>

                <div class="customer-actions">
                    <asp:Button ID="btnAddNewCustomer"
                        runat="server"
                        Text="Add New Customer"
                        CssClass="customer-button" />

                    <asp:Button ID="btnSelectExistingCustomer"
                        runat="server"
                        Text="Select Existing Customer"
                        CssClass="customer-button" />
                    <asp:Label ID="lblCID" runat="server" Text=""></asp:Label>
                    <asp:Label ID="lblPID" runat="server" Text=""></asp:Label>
                    <asp:Label ID="lblPRJID" runat="server" Text=""></asp:Label>
                    <asp:Label ID="lblSTATEID" runat="server" Text=""></asp:Label>
                    <asp:Label ID="lblActionID" runat="server" Text=""></asp:Label>

                </div>

                <div class="form-row">
                    <asp:Label ID="lblCustomerName"
                        runat="server"
                        Text="Customer Name:"
                        CssClass="form-label" />
                    <div class="form-control">
                        <asp:TextBox ID="txtCustomerName"
                            readonly="true"
                            runat="server"
                            CssClass="txt-input"
                            placeholder="Enter real customer name" />
                    </div>
                </div>

                <div class="form-row">
                    <asp:Label ID="lblCustomerCPR"
                        runat="server"
                        Text="Customer CPR:"
                        CssClass="form-label" />
                    <div class="form-control">
                        <asp:TextBox ID="txtCustomerCPR"
                            readonly="true"
                            runat="server"
                            CssClass="txt-input"
                            placeholder="Enter customer CPR" />
                    </div>
                </div>

                <div class="form-row">
                    <asp:Label ID="lblReservedBy"
                        runat="server"
                        Text="Reserved by:"
                        CssClass="form-label" />
                    <div class="form-control">
                        <asp:DropDownList ID="ddlReservedBy"
                            runat="server"
                            CssClass="ddl-input">
                            <asp:ListItem Text="-- Select --" Value="" />
                        </asp:DropDownList>
                    </div>
                </div>

                <asp:Panel ID="pnlPayments" runat="server" CssClass="form-row">
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

                <div class="form-row">
                    <asp:Label ID="lblDate"
                        runat="server"
                        Text="Date:"
                        CssClass="form-label" />
                    <div class="form-control">
                        <div class="date-picker">
                            <asp:TextBox ID="txtReservationDate"
                                runat="server"
                                CssClass="txt-input"
                                autocomplete="off"
                                placeholder="yyyy-mm-dd" />
                            <button type="button" id="btnReservationDate" class="date-picker-btn"
                                title="Pick a date" aria-label="Pick a date">
                                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"
                                    stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                                    <rect x="3" y="4" width="18" height="18" rx="2" ry="2"></rect>
                                    <line x1="16" y1="2" x2="16" y2="6"></line>
                                    <line x1="8" y1="2" x2="8" y2="6"></line>
                                    <line x1="3" y1="10" x2="21" y2="10"></line>
                                </svg>
                            </button>
                            <input type="date" id="dtpReservationDate" class="date-picker-native"
                                tabindex="-1" aria-hidden="true" />
                        </div>
                    </div>
                </div>

                <div class="form-row">
                    <asp:Label ID="lblComments"
                        runat="server"
                        Text="Comments:"
                        CssClass="form-label" />
                    <div class="form-control">
                        <asp:TextBox ID="txtComments"
                            runat="server"
                            CssClass="comments-input"
                            TextMode="MultiLine"
                            Rows="5"
                            placeholder="Enter comments..." />
                    </div>
                </div>

                <div class="form-actions">
                    <asp:Button ID="btnSave"
                        runat="server"
                        Text="Save"
                        CssClass="customer-button save-button" />

                    <asp:Button ID="btnCancel"
                        runat="server"
                        Text="Cancel"
                        CssClass="customer-button cancel-button"
                        CausesValidation="false"
                        OnClientClick="closeForm(); return false;" />
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
