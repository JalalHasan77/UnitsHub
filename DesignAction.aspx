<%@ Page Language="VB" AutoEventWireup="false" CodeFile="DesignAction.aspx.vb" Inherits="DesignAction" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Action Control</title>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <link rel="preconnect" href="https://fonts.googleapis.com" />
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700;800&display=swap" rel="stylesheet" />
    <style type="text/css">

        :root {
            --brand-1: #4f46e5;
            --brand-2: #3b82f6;
            --brand-soft: #eef2ff;
            --ink: #1e293b;
            --muted: #64748b;
            --border: #e2e8f0;
            --bg: #eef1f8;
            --success: #16a34a;
            --danger: #ef4444;
            --radius: 14px;
        }

        * { box-sizing: border-box; }

        body {
            font-family: 'Inter', 'Segoe UI', Arial, Helvetica, sans-serif;
            background: radial-gradient(circle at top left, #eef2ff, var(--bg) 55%);
            margin: 0;
            padding: 36px 20px;
            color: var(--ink);
        }

        .action-panel {
            max-width: 760px;
            margin: 0 auto;
            background-color: #ffffff;
            border-radius: 22px;
            overflow: hidden;
            box-shadow: 0 25px 60px -20px rgba(30, 41, 59, 0.35);
        }

        /* ---------- Header ---------- */
        .action-header {
            background: linear-gradient(120deg, var(--brand-1), var(--brand-2) 75%);
            color: #ffffff;
            padding: 30px 34px;
            position: relative;
            overflow: hidden;
        }

        .action-header::after {
            content: "";
            position: absolute;
            right: -60px;
            top: -60px;
            width: 220px;
            height: 220px;
            background: rgba(255,255,255,0.08);
            border-radius: 50%;
        }

        .header-row {
            display: flex;
            align-items: center;
            gap: 16px;
            position: relative;
            z-index: 1;
        }

        .header-icon {
            width: 46px;
            height: 46px;
            border-radius: 12px;
            background: rgba(255,255,255,0.16);
            display: flex;
            align-items: center;
            justify-content: center;
            flex-shrink: 0;
        }

        .header-icon svg { width: 24px; height: 24px; stroke: #ffffff; }

        .action-title { font-size: 24px; font-weight: 700; letter-spacing: 0.2px; }
        .action-subtitle { font-size: 13.5px; opacity: 0.85; margin-top: 2px; }

        /* ---------- Body ---------- */
        .action-body { padding: 30px 34px 34px; }

        .section {
            padding: 22px 0;
            border-bottom: 1px solid var(--border);
        }
        .section:last-of-type { border-bottom: none; padding-bottom: 6px; }
        .section:first-of-type { padding-top: 4px; }

        .section-kicker {
            display: flex;
            align-items: center;
            gap: 8px;
            font-size: 12.5px;
            font-weight: 700;
            text-transform: uppercase;
            letter-spacing: 0.6px;
            color: var(--brand-1);
            margin-bottom: 16px;
        }

        .section-kicker svg { width: 15px; height: 15px; stroke: var(--brand-1); }

        .form-row {
            display: flex;
            flex-wrap: wrap;
            align-items: flex-start;
            gap: 6px 18px;
            margin-bottom: 18px;
        }
        .form-row:last-child { margin-bottom: 0; }

        .form-label {
            width: 170px;
            flex-shrink: 0;
            font-size: 13.5px;
            font-weight: 600;
            color: var(--ink);
            padding-top: 9px;
        }

        .form-control-cell { flex: 1; min-width: 220px; }
        .field-hint { font-size: 12px; color: var(--muted); margin-top: 6px; }

        /* ---------- Inputs ---------- */
        .txt-input, .ddl-input {
            width: 100%;
            max-width: 420px;
            padding: 10px 13px;
            border: 1.5px solid var(--border);
            border-radius: 10px;
            font-size: 14px;
            font-family: inherit;
            color: var(--ink);
            background-color: #f8fafc;
            transition: border-color .15s ease, box-shadow .15s ease, background-color .15s ease;
        }

        .txt-input::placeholder { color: #a0aec0; }

        .txt-input:focus, .ddl-input:focus {
            outline: none;
            border-color: var(--brand-2);
            background-color: #ffffff;
            box-shadow: 0 0 0 4px rgba(59, 130, 246, 0.14);
        }

        .ddl-input {
            appearance: none;
            -webkit-appearance: none;
            background-image: url("data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='16' height='16' fill='none' stroke='%2364748b' stroke-width='2'><path d='M4 6l4 4 4-4'/></svg>");
            background-repeat: no-repeat;
            background-position: right 12px center;
            padding-right: 34px;
            cursor: pointer;
        }

        /* ---------- Segmented / chip controls ---------- */
        .seg-list, .chip-list {
            display: flex;
            flex-wrap: wrap;
            gap: 10px;
        }

        .seg-list input[type="radio"],
        .chip-list input[type="checkbox"] {
            position: absolute;
            opacity: 0;
            width: 0;
            height: 0;
        }

        .seg-list label, .chip-list label {
            display: inline-flex;
            align-items: center;
            gap: 7px;
            padding: 8px 16px;
            border-radius: 999px;
            border: 1.5px solid var(--border);
            background-color: #f8fafc;
            font-size: 13.5px;
            font-weight: 500;
            color: var(--ink);
            cursor: pointer;
            transition: all .15s ease;
        }

        .seg-list label:hover, .chip-list label:hover {
            border-color: var(--brand-2);
            background-color: var(--brand-soft);
        }

        .seg-list input:checked + label {
            background: linear-gradient(120deg, var(--brand-1), var(--brand-2));
            border-color: transparent;
            color: #ffffff;
            box-shadow: 0 6px 14px -6px rgba(79, 70, 229, 0.6);
        }

        .chip-list input:checked + label {
            background-color: #ecfdf5;
            border-color: var(--success);
            color: #15803d;
        }

        .chip-list label::before {
            content: "";
            width: 15px;
            height: 15px;
            border-radius: 4px;
            border: 1.5px solid #cbd5e1;
            background-color: #ffffff;
            flex-shrink: 0;
        }

        .chip-list input:checked + label::before {
            background-color: var(--success);
            border-color: var(--success);
            background-image: url("data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='12' height='12' fill='none' stroke='white' stroke-width='3'><path d='M2 6l3 3 5-6'/></svg>");
            background-repeat: no-repeat;
            background-position: center;
        }

        /* Status pills get semantic colors */
        #rblActionStatus_0:checked + label {
            background: linear-gradient(120deg, #16a34a, #22c55e);
            box-shadow: 0 6px 14px -6px rgba(22, 163, 74, 0.55);
        }
        #rblActionStatus_1:checked + label {
            background: linear-gradient(120deg, #64748b, #94a3b8);
            box-shadow: 0 6px 14px -6px rgba(100, 116, 139, 0.5);
        }
        .seg-list label::before {
            content: "";
            width: 8px;
            height: 8px;
            border-radius: 50%;
            background-color: #cbd5e1;
            flex-shrink: 0;
        }
        .seg-list input:checked + label::before { background-color: #ffffff; }

        /* ---------- Receive parameters card ---------- */
        .params-card {
            background: linear-gradient(180deg, #f5f8ff, #eef2ff);
            border: 1px solid #dfe6fb;
            border-radius: 14px;
            padding: 16px 18px;
        }

        .params-card-top {
            display: flex;
            align-items: center;
            justify-content: space-between;
            flex-wrap: wrap;
            gap: 10px;
            margin-bottom: 14px;
        }

        .toggle-inline { display: flex; align-items: center; gap: 8px; font-size: 13.5px; font-weight: 600; color: var(--ink); }
        .toggle-inline input { width: 17px; height: 17px; accent-color: var(--brand-1); cursor: pointer; }

        .plan-details-grid {
            width: 100%;
            border-collapse: separate;
            border-spacing: 0;
            font-size: 13.5px;
            border: 1.5px solid #94a3b8;
            border-radius: 10px;
            overflow: hidden;
        }

        .plan-details-grid th {
            background-color: var(--brand-soft);
            color: var(--brand-1);
            text-align: left;
            font-weight: 700;
            font-size: 12px;
            text-transform: uppercase;
            letter-spacing: 0.4px;
            padding: 10px 12px;
            border-bottom: 1px solid var(--border);
        }

        .plan-details-grid td {
            padding: 9px 12px;
            border-bottom: 1px solid var(--border);
            color: var(--ink);
        }

        .plan-details-grid tr:last-child td { border-bottom: none; }
        .plan-details-grid tr:hover td { background-color: #f8fafc; }
        .plan-details-grid input[type="checkbox"] { width: 16px; height: 16px; accent-color: var(--brand-1); cursor: pointer; }
        .plan-details-empty { font-size: 13px; color: var(--muted); padding: 10px 2px; }

        .select-users-btn {
            display: inline-flex;
            align-items: center;
            gap: 6px;
            margin-top: 14px;
            padding: 8px 15px;
            border-radius: 9px;
            background-color: #ffffff;
            border: 1.5px solid var(--border);
            font-size: 13px;
            font-weight: 600;
            color: var(--brand-1);
            text-decoration: none;
            transition: all .15s ease;
        }
        .select-users-btn:hover { border-color: var(--brand-2); background-color: var(--brand-soft); }

        /* ---------- Script editor look ---------- */
        .code-editor {
            border-radius: 12px;
            overflow: hidden;
            border: 1px solid #1e293b;
            max-width: 100%;
        }

        .code-editor-bar {
            background-color: #1e293b;
            padding: 9px 14px;
            display: flex;
            align-items: center;
            gap: 7px;
        }

        .code-dot { width: 10px; height: 10px; border-radius: 50%; }
        .code-dot.red { background-color: #ef4444; }
        .code-dot.yellow { background-color: #f59e0b; }
        .code-dot.green { background-color: #22c55e; }
        .code-editor-label { margin-left: 8px; font-size: 12px; color: #94a3b8; font-family: Consolas, monospace; }

        .script-box {
            width: 100%;
            height: 150px;
            border: none;
            padding: 14px;
            font-family: Consolas, 'Courier New', monospace;
            font-size: 13px;
            line-height: 1.6;
            resize: vertical;
            background-color: #0f172a;
            color: #e2e8f0;
            display: block;
        }
        .script-box:focus { outline: none; }
        .script-box::placeholder { color: #64748b; }

        /* ---------- Buttons ---------- */
        .btn-row {
            display: flex;
            justify-content: flex-end;
            gap: 10px;
            margin-top: 28px;
        }

        .btn-save, .btn-cancel {
            padding: 11px 26px;
            border-radius: 10px;
            font-size: 14px;
            font-weight: 600;
            font-family: inherit;
            border: none;
            cursor: pointer;
            transition: transform .12s ease, box-shadow .12s ease, background-color .12s ease;
        }

        .btn-save {
            background: linear-gradient(120deg, var(--brand-1), var(--brand-2));
            color: #ffffff;
            box-shadow: 0 10px 20px -8px rgba(79, 70, 229, 0.55);
        }
        .btn-save:hover { transform: translateY(-1px); box-shadow: 0 14px 24px -8px rgba(79, 70, 229, 0.6); }

        .btn-cancel {
            background-color: #f1f5f9;
            color: var(--muted);
            border: 1.5px solid var(--border);
        }
        .btn-cancel:hover { background-color: #e2e8f0; color: var(--ink); }

        .msg-success {
            display: block;
            margin-top: 16px;
            padding: 10px 14px;
            border-radius: 10px;
            background-color: #ecfdf5;
            border: 1px solid #a7f3d0;
            color: #15803d;
            font-size: 13.5px;
            font-weight: 600;
        }

        @media (max-width: 560px) {
            .form-label { width: 100%; padding-top: 0; }
            .form-row { flex-direction: column; }
        }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <div class="action-panel">

            <div class="action-header">
                <div class="header-row">
                    <div class="header-icon">
                        <svg viewBox="0 0 24 24" fill="none" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M13 2 3 14h7l-1 8 10-12h-7l1-8z"/></svg>
                    </div>
                    <div>
                        <div class="action-title">Action Control<asp:Label ID="lblProjectID" runat="server" CssClass="field-hint" >001</asp:Label>
                            <asp:Label ID="lblSTATUSID" runat="server" CssClass="field-hint" >0000</asp:Label>
                            <asp:Label ID="lblActionID" runat="server" Text="Label"></asp:Label>
                            <asp:Label ID="lblMode" runat="server" Text="Label"></asp:Label>
                        </div>
                        <div class="action-subtitle">Configure how this action behaves, who can trigger it, and what it runs</div>
                    </div>
                </div>
            </div>

            <div class="action-body">

                <%-- ===================== General ===================== --%>
                <div class="section">
                    <div class="section-kicker">
                        <svg viewBox="0 0 24 24" fill="none" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="9"/><path d="M12 8v4l3 2"/></svg>
                        General
                    </div>

                    <div class="form-row">
                        <div class="form-label">Action Status</div>
                        <div class="form-control-cell">
                            <asp:RadioButtonList ID="rblActionStatus" runat="server" RepeatDirection="Horizontal" RepeatLayout="Flow" CssClass="seg-list">
                                <asp:ListItem Text="Active" Value="Active" Selected="True" />
                                <asp:ListItem Text="InActive" Value="InActive" />
                            </asp:RadioButtonList>
                        </div>
                    </div>

                    <div class="form-row">
                        <div class="form-label">Action Title</div>
                        <div class="form-control-cell">
                            <asp:TextBox ID="txtActionTitle" runat="server" CssClass="txt-input" placeholder="e.g. Delete Record" />
                        </div>
                    </div>

                    <div class="form-row">
                        <div class="form-label">Action Type</div>
                        <div class="form-control-cell">
                            <asp:DropDownList ID="ddlActionType" runat="server" CssClass="ddl-input" onchange="toggleNeedPaymentVisibility();">
                                <asp:ListItem>Select Action</asp:ListItem>
                                <asp:ListItem Value="CHANGE">Change Status</asp:ListItem>
                                <asp:ListItem Value="DLET">Delete</asp:ListItem>
                                <asp:ListItem Value="ACTV">Activate</asp:ListItem>
                                <asp:ListItem Value="DACTV">De-Activate</asp:ListItem>
                                <asp:ListItem>Edit</asp:ListItem>
                            </asp:DropDownList>
                        </div>
                        <div class="form-control-cell" id="cellToStatus" runat="server">
                            <asp:DropDownList ID="ddlToStatus" runat="server" CssClass="ddl-input" />
                        </div>
                    </div>

                    <div class="form-row" id="rowNeedPayment" runat="server">
                        <div class="form-label"></div>
                        <div class="form-control-cell">
                            <label class="toggle-inline">
                                <asp:CheckBox ID="chkNeedPayment" runat="server" onchange="togglePaymentPlanVisibility();" />
                                Need Payment
                            </label>
                        </div>
                    </div>

                    <div class="form-row" id="rowPaymentPlan" runat="server">
                        <div class="form-label">Payment Plan</div>
                        <div class="form-control-cell">
                            <asp:DropDownList ID="ddlPaymentPlan" runat="server" CssClass="ddl-input" AutoPostBack="true" OnSelectedIndexChanged="ddlPaymentPlan_SelectedIndexChanged" />
                        </div>
                    </div>

                    <div class="form-row" id="rowPlanDetails" runat="server" style="flex-basis:100%;">
                        <div class="form-control-cell" style="flex-basis:100%;">
                            <asp:GridView ID="gvPlanDetails" runat="server" AutoGenerateColumns="True"
                                CssClass="plan-details-grid" GridLines="None"
                                DataKeyNames="DETAIL_ID"
                                EmptyDataText="No plan details found for the selected payment plan.">
                                <Columns>
                                    <asp:TemplateField HeaderText="Select">
                                        <ItemTemplate>
                                            <asp:CheckBox ID="chkSelectDetail" runat="server" />
                                        </ItemTemplate>
                                    </asp:TemplateField>
                                </Columns>
                            </asp:GridView>
                        </div>
                    </div>

                    <div class="form-row">
                        <div class="form-label">Implementer Title</div>
                        <div class="form-control-cell">
                            <asp:TextBox ID="txtImplementerTitle" runat="server" CssClass="txt-input" placeholder="Maker, Checker, etc" />
                        </div>
                    </div>
                </div>

                <%-- ===================== Visibility & Parameters ===================== --%>
                <div class="section">
                    <div class="section-kicker">
                        <svg viewBox="0 0 24 24" fill="none" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M1 12s4-7 11-7 11 7 11 7-4 7-11 7-11-7-11-7z"/><circle cx="12" cy="12" r="3"/></svg>
                        Visibility &amp; Parameters
                    </div>

                    <div class="form-row">
                        <div class="form-label">Show in</div>
                        <div class="form-control-cell">
                            <asp:CheckBoxList ID="cblShowIn" runat="server" RepeatDirection="Horizontal" RepeatLayout="Flow" CssClass="chip-list">
                                <asp:ListItem Text="Show In Default" Value="Default" Selected="True" />
                                <asp:ListItem Text="Show In Preview" Value="Preview" />
                            </asp:CheckBoxList>
                        </div>
                    </div>

                    <div class="form-row">
                        <div class="form-label">Receive Parameters</div>
                        <div class="form-control-cell">
                            <div class="params-card">
                                <div class="params-card-top">
                                    <label class="toggle-inline">
                                        <asp:CheckBox ID="chkReceiveParameters" runat="server" />
                                        Enable parameter passing
                                    </label>
                                </div>
                                <asp:RadioButtonList ID="rblReceiveParameters" runat="server" RepeatDirection="Horizontal" RepeatLayout="Flow" CssClass="seg-list">
                                    <asp:ListItem Text="No" Value="No" Selected="True" />
                                    <asp:ListItem Text="For Enable" Value="ForEnable" />
                                    <asp:ListItem Text="For Disable" Value="ForDisable" />
                                    <asp:ListItem Text="To Hide" Value="ToHide" />
                                </asp:RadioButtonList>

                                <asp:LinkButton ID="lnkSelectUsers" runat="server" Text="  Select Users" CssClass="select-users-btn" />
                            </div>
                        </div>
                    </div>
                </div>

                <%-- ===================== Script ===================== --%>
                <div class="section">
                    <div class="section-kicker">
                        <svg viewBox="0 0 24 24" fill="none" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M8 4 2 12l6 8"/><path d="M16 4l6 8-6 8"/></svg>
                        Script
                    </div>

                    <div class="form-row">
                        <div class="form-control-cell" style="flex-basis:100%;">
                            <div class="code-editor">
                                <div class="code-editor-bar">
                                    <span class="code-dot red"></span>
                                    <span class="code-dot yellow"></span>
                                    <span class="code-dot green"></span>
                                    <span class="code-editor-label">action-script.vb</span>
                                </div>
                                <asp:TextBox ID="txtScript" runat="server" TextMode="MultiLine" CssClass="script-box" placeholder="' Write the script that runs for this action..." />
                            </div>
                        </div>
                    </div>
                </div>

                <%-- ===================== Execution ===================== --%>
                <div class="section">
                    <div class="section-kicker">
                        <svg viewBox="0 0 24 24" fill="none" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polygon points="6 3 20 12 6 21 6 3"/></svg>
                        Pre-Execution
                    </div>

                    <div class="form-row">
                        <div class="form-control-cell" style="flex-basis:100%;">
                            <asp:RadioButtonList ID="rblPreExecution" runat="server" RepeatDirection="Horizontal" RepeatLayout="Flow" CssClass="seg-list">
                                <asp:ListItem Text="None" Value="None" Selected="True" />
                                <asp:ListItem Text="Confirmation" Value="Confirmation" />
                                <asp:ListItem Text="Parameter" Value="Parameter" />
                            </asp:RadioButtonList>
                        </div>
                    </div>

                    <div class="form-row" id="rowConfirmationText" runat="server">
                        <div class="form-label">Confirmation Text</div>
                        <div class="form-control-cell">
                            <asp:TextBox ID="txtConfirmationText" runat="server" CssClass="txt-input" placeholder="e.g. Are you sure you want to proceed?" />
                        </div>
                    </div>

                    <div class="form-row" id="rowParameterType" runat="server">
                        <div class="form-label">Parameter Type</div>
                        <div class="form-control-cell">
                            <asp:DropDownList ID="ddlParameterType" runat="server" CssClass="ddl-input" onchange="toggleParameterTypeVisibility();">
                                <asp:ListItem Text="Justification" Value="JUSTIFICATION" />
                                <asp:ListItem Text="Restriction (Window)" Value="RESTRICTION_WINDOW" />
                                <asp:ListItem Text="Restriction (No Window)" Value="RESTRICTION_NO_WINDOW" />
                                <asp:ListItem Text="Restriction and Justification" Value="RESTRICTION_JUSTIFICATION" />
                                <asp:ListItem Text="Modify" Value="MODIFY" />
                            </asp:DropDownList>
                        </div>
                    </div>

                    <div class="form-row" id="rowFormTitle" runat="server">
                        <div class="form-label">Form Title</div>
                        <div class="form-control-cell">
                            <asp:TextBox ID="txtFormTitle" runat="server" CssClass="txt-input" placeholder="e.g. Restriction Request" />
                        </div>
                    </div>

                    <div class="form-row" id="rowSelectSQL" runat="server">
                        <div class="form-label">Select SQL</div>
                        <div class="form-control-cell" style="flex-basis:100%;">
                            <asp:TextBox ID="txtSelectSQL" runat="server" TextMode="MultiLine" CssClass="script-box" placeholder="Select ... from ... where ..." />
                        </div>
                    </div>
                </div>

                <div class="btn-row">
                    <asp:Button ID="btnLoad" runat="server" Text="Load" CssClass="btn-cancel" CausesValidation="false" OnClick="btnLoad_Click" />
                    <asp:Button ID="btnCancel" runat="server" Text="Cancel" CssClass="btn-cancel" CausesValidation="false" OnClick="btnCancel_Click" />
                    <asp:Button ID="btnSave" runat="server" Text="Save Action" CssClass="btn-save" OnClick="btnSave_Click" />
                </div>

                <asp:Label ID="lblMessage" runat="server" CssClass="msg-success" />

            </div>
        </div>

        <script type="text/javascript">
            function toggleNeedPaymentVisibility() {
                var ddl = document.getElementById('<%= ddlActionType.ClientID %>');
                var row = document.getElementById('<%= rowNeedPayment.ClientID %>');
                var toStatusCell = document.getElementById('<%= cellToStatus.ClientID %>');

                if (!ddl || !row || !toStatusCell) { return; }

                var selectedValue = ddl.options[ddl.selectedIndex] ? ddl.options[ddl.selectedIndex].value : '';
                var isChangeStatus = (selectedValue === 'CHANGE');

                row.style.display = isChangeStatus ? '' : 'none';
                toStatusCell.style.display = isChangeStatus ? '' : 'none';

                togglePaymentPlanVisibility();
            }

            function togglePaymentPlanVisibility() {
                var chk = document.getElementById('<%= chkNeedPayment.ClientID %>');
                var needPaymentRow = document.getElementById('<%= rowNeedPayment.ClientID %>');
                var planRow = document.getElementById('<%= rowPaymentPlan.ClientID %>');
                var planDetailsRow = document.getElementById('<%= rowPlanDetails.ClientID %>');
                var planDdl = document.getElementById('<%= ddlPaymentPlan.ClientID %>');

                if (!chk || !needPaymentRow || !planRow || !planDetailsRow || !planDdl) { return; }

                var needPaymentRowVisible = (needPaymentRow.style.display !== 'none');
                var planRowVisible = (needPaymentRowVisible && chk.checked);
                var selectedPlanValue = planDdl.options[planDdl.selectedIndex] ? planDdl.options[planDdl.selectedIndex].value : '';
                var planDetailsRowVisible = (planRowVisible && selectedPlanValue !== '');

                planRow.style.display = planRowVisible ? '' : 'none';
                planDetailsRow.style.display = planDetailsRowVisible ? '' : 'none';
            }

            function togglePreExecutionVisibility() {
                var radios = document.getElementsByName('<%= rblPreExecution.UniqueID %>');
                var confirmationRow = document.getElementById('<%= rowConfirmationText.ClientID %>');
                var parameterTypeRow = document.getElementById('<%= rowParameterType.ClientID %>');

                if (!radios || !confirmationRow || !parameterTypeRow) { return; }

                var selectedValue = '';
                for (var i = 0; i < radios.length; i++) {
                    if (radios[i].checked) {
                        selectedValue = radios[i].value;
                        break;
                    }
                }

                confirmationRow.style.display = (selectedValue === 'Confirmation') ? '' : 'none';
                parameterTypeRow.style.display = (selectedValue === 'Parameter') ? '' : 'none';

                toggleParameterTypeVisibility();
            }

            function toggleParameterTypeVisibility() {
                var parameterTypeRow = document.getElementById('<%= rowParameterType.ClientID %>');
                var ddl = document.getElementById('<%= ddlParameterType.ClientID %>');
                var formTitleRow = document.getElementById('<%= rowFormTitle.ClientID %>');
                var selectSqlRow = document.getElementById('<%= rowSelectSQL.ClientID %>');

                if (!parameterTypeRow || !ddl || !formTitleRow || !selectSqlRow) { return; }

                var parameterRowVisible = (parameterTypeRow.style.display !== 'none');
                var selectedValue = ddl.options[ddl.selectedIndex] ? ddl.options[ddl.selectedIndex].value : '';

                var showFormTitle = parameterRowVisible && (
                    selectedValue === 'JUSTIFICATION' ||
                    selectedValue === 'RESTRICTION_WINDOW' ||
                    selectedValue === 'RESTRICTION_NO_WINDOW' ||
                    selectedValue === 'RESTRICTION_JUSTIFICATION');

                var showSelectSql = parameterRowVisible && (
                    selectedValue === 'RESTRICTION_WINDOW' ||
                    selectedValue === 'RESTRICTION_NO_WINDOW' ||
                    selectedValue === 'RESTRICTION_JUSTIFICATION');

                formTitleRow.style.display = showFormTitle ? '' : 'none';
                selectSqlRow.style.display = showSelectSql ? '' : 'none';
            }

            if (document.addEventListener) {
                document.addEventListener('DOMContentLoaded', function () {
                    toggleNeedPaymentVisibility();
                    togglePreExecutionVisibility();

                    var preExecutionRadios = document.getElementsByName('<%= rblPreExecution.UniqueID %>');
                    for (var i = 0; i < preExecutionRadios.length; i++) {
                        preExecutionRadios[i].addEventListener('change', togglePreExecutionVisibility);
                    }
                });
            }
        </script>
    </form>
</body>
</html>
