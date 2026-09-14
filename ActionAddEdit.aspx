<%@ Page Language="VB" AutoEventWireup="false" CodeFile="ActionAddEdit.aspx.vb" Inherits="ActionAddEdit"   MaintainScrollPositionOnPostback="true" EnableEventValidation = false  %>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <link rel="stylesheet" href="styles.css" />

    <title></title>
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

        .bigCheckbox{
            width:30px;
            height:30px;

        }


        .diffBorderWidth {
            width: 100%;
            border-left-width:70px;
            border-right-width:20px;
            border-Top-width:10px;
            border-bottom-width:10px;
        }

        .MainContainer {
    width: 100%;
    border-collapse: collapse;
    border-style: solid;
    border-width: 0px;
}


        .nowrap {
            white-space: nowrap;
        }

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
        
        /*.Menu-rounded-corners {
            border: 3px solid darkgray;
            -webkit-border-radius: 8px;
            -moz-border-radius: 8px;
            border-radius: 8px;
            overflow: hidden;
            }*/
                
        .GarbageCell {
    border-collapse: collapse;
    border-style: solid;
    border-width: 0px;
   /* border-left-width: 2px;
    border-bottom-width: 0px;
    border-top-width: 0px;
    border-left-width: 1px;
    border-right-width: 0px;*/
    background-color: white;
}

        *,::after,::before{box-sizing:border-box}

        body {
            font-family: 'Inter', 'Segoe UI', Arial, Helvetica, sans-serif;
            margin: 0;
            padding: 0;
            color: var(--ink);
        }

        .top-strip {
            width: 100%;
            margin: 0;
            border: none;
            background: linear-gradient(120deg, #4f46e5, #3b82f6 75%);
            color: #ffffff;
            font-family: Arial, 'Segoe UI', sans-serif;
            font-size: 18px;
            font-weight: 700;
            padding: 14px 20px;
        }

        
                .tabs {
  display: flex;
}

.tab {
  flex: 1;
}

.tab input[type="radio"] {
  display: none;
}

.tab label {
  display: block;
  padding: 10px;
  background-color: #f0f0f0;
  cursor: pointer;
}

.tab-content {
  display: none;
  padding: 10px;
}

.tab input[type="radio"]:checked + label {
  background-color: #ccc;
}

.tab input[type="radio"]:checked ~ .tab-content {
  display: block;
}

#menu 
{
 width: 1024px;
 height: 25px;
 margin: 0 auto;
 text-align: right;
 background-color: Red;
}
.nestedTable {
    width: 100%;
    border-collapse: collapse;
    white-space: nowrap;
}

.nestedTable3 {
    width: 100%;
    border-collapse: collapse;
    white-space: nowrap;
}
.nestedTable2 {
    /*cellpadding: 25px;
    cell-spacing: 25px;*/
    /*border-width:unset 60px;*/
    width: 95%;
    /*max-width: 1000px;
    min-width: 900px;*/
    /*margin: 10px;*/
    border-collapse: collapse;
    margin: 5px auto;
}


#menuContainer{float: left;}

        /* ---------- Tab-body content (migrated from DesignAction) ---------- */
        .tab-body-inner {
            padding: 22px 4px 6px;
            height: 500px;
            overflow-y: auto;
        }

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

        .plan-block {
            border: 1px solid var(--border);
            border-radius: 10px;
            padding: 14px 16px;
            margin-bottom: 14px;
            background-color: #f8fafc;
        }

        .plan-block:last-child { margin-bottom: 0; }

        .plan-block-title {
            font-size: 13.5px;
            font-weight: 700;
            color: var(--ink);
            margin-bottom: 10px;
        }

        .plan-chip {
            display: inline-flex;
            align-items: center;
            gap: 6px;
            background-color: var(--brand-soft);
            color: var(--brand-1);
            border-radius: 999px;
            padding: 6px 10px 6px 14px;
            margin: 0 8px 8px 0;
            font-size: 13px;
            font-weight: 600;
        }

        .plan-chip a { color: inherit; text-decoration: none; }

        .plan-chip-remove {
            color: var(--brand-1);
            opacity: 0.6;
            font-size: 11px;
            cursor: pointer;
        }

        .plan-chip-remove:hover { opacity: 1; color: #dc2626; }

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
            margin: 12px -15px 0 -15px;
            padding: 20px 19px 18px;
            border-top: 1px solid #000000;
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
            margin: 0 4px 16px;
            padding: 10px 14px;
            border-radius: 10px;
            background-color: #ecfdf5;
            border: 1px solid #a7f3d0;
            color: #15803d;
            font-size: 13.5px;
            font-weight: 600;
        }

        .msg-success:empty { display: none; }

        @media (max-width: 560px) {
            .form-label { width: 100%; padding-top: 0; }
            .form-row { flex-direction: column; }
        }

        </style>
    <script type="text/javascript">
        function changeHiddenFieldValue() {
            alert(document.getElementById("meow").value);
        }

        // ---------- Payments tab: Need Payment checkbox -> Add Payment Plan row ----------
        // Self-contained within the Payments tab now that Need Payment no longer shares a
        // tab with Action Type - visibility depends only on the checkbox itself.
        function togglePaymentPlanVisibility() {
            var chk = document.getElementById('<%= chkNeedPayment.ClientID %>');
            var planRow = document.getElementById('<%= rowPaymentPlan.ClientID %>');
            var addedPlansRow = document.getElementById('<%= rowAddedPlans.ClientID %>');
            var planDetailsRow = document.getElementById('<%= rowPlanDetails.ClientID %>');

            if (!chk || !planRow || !addedPlansRow || !planDetailsRow) { return; }

            var planRowVisible = chk.checked;
            planRow.style.display = planRowVisible ? '' : 'none';

            // Whether the chips / plan-details rows should actually be SHOWN depends on
            // which plans have been added and which one is current, which the server only
            // knows after a postback. So JS only ever force-hides these rows; showing
            // them is left to whatever the server most recently rendered.
            if (!planRowVisible) {
                addedPlansRow.style.display = 'none';
                planDetailsRow.style.display = 'none';
            }
        }

        // ---------- Pre-Execution tab: fully self-contained cascade ----------
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
                togglePaymentPlanVisibility();
                togglePreExecutionVisibility();

                var preExecutionRadios = document.getElementsByName('<%= rblPreExecution.UniqueID %>');
                for (var i = 0; i < preExecutionRadios.length; i++) {
                    preExecutionRadios[i].addEventListener('change', togglePreExecutionVisibility);
                }
            });
        }
    </script>
</head>
<body>
    <div class="top-strip">Design Action</div>
    <form id="form1" runat="server">
        
        <table class="MainContainer"  cellpadding="0px" cellspacing="0px" >
            <tr>
                <td>
                       <div style="padding:10px">
                            <table cellpadding="0" class="nestedTable">
                                <tr>
                                    <td style="border-width: 0;">
                                            <asp:Label ID="lblPackageTitle" runat="server" Font-Names="Arial" Font-Size="20pt" ForeColor="Black" Text="Action Control" Font-Bold="True"></asp:Label>
                                            <asp:Label ID="lblProjectID" runat="server" CssClass="field-hint" >001</asp:Label>
                                            <asp:Label ID="lblSTATUSID" runat="server" CssClass="field-hint" >0000</asp:Label>
                                            <asp:Label ID="lblActionID" runat="server" Text="Label" Visible="false"></asp:Label>
                                            <asp:Label ID="lblMode" runat="server" Text="Label" Visible="false"></asp:Label>
                                        </td>
                                    </tr>
                            </table>
                        </div>
                
                
                </td>
            </tr>
            <tr>
                <td>
                    <div style="padding:10px">
<table cellpadding="0" class="nestedTable">
                                <tr>
                                    <td style="border-width: 0;">
                                        <table cellpadding="0" cellspacing="0" style="width:100%;border-width:0">
                                            <tr>
                                                <td style="width: 100%">
                                        <div style="width:100%;">
                                            <asp:Menu ID="Menu3" runat="server" Font-Names="Arial" Height="50px" Orientation="Horizontal" Width="100%" CssClass="full-width-menu">
                                                <Items>
                                                    <asp:MenuItem Text="General" Value="General"></asp:MenuItem>
                                                    <asp:MenuItem Text="Payments" Value="Payments"></asp:MenuItem>
                                                    <asp:MenuItem Text="AutoTransfer" Value="AutoTransfer"></asp:MenuItem>
                                                    <asp:MenuItem Text="Parameters and Script" Value="Parameters and Script"></asp:MenuItem>
                                                    <asp:MenuItem Text="Pre-Execution" Value="Pre-Execution"></asp:MenuItem>
                                                </Items>
                                                <StaticHoverStyle BackColor="#000099" BorderColor="#000099" BorderWidth="1px" CssClass="itemCell" ForeColor="#F2F2F2" Height="50px" />
                                                <StaticMenuItemStyle BorderWidth="1px" CssClass="itemCell" Height="50px" HorizontalPadding="0px" ItemSpacing="0px" VerticalPadding="0px" BackColor="#F2F2F2"/>
                                                <StaticMenuStyle BorderWidth="0px" Height="50px" HorizontalPadding="0px" VerticalPadding="0px" Width="100%" />
                                                <StaticSelectedStyle BackColor="White" CssClass="itemCellSelecred itemCellSelecredBottmBorder" Height="50px" HorizontalPadding="0px" ItemSpacing="0px" VerticalPadding="0px" />
                                            </asp:Menu>
                                        </div>
                                     </td>
                                                <td style="width: 100%; border-bottom-style: solid; border-bottom-width: 0px; border-bottom-color: transparent; background-color:transparent">
                                                  <table cellpadding="0" cellspacing="0" style="height:50px;width:100%;border-width:0">
                                                      <tr><td style="width: 100%; border-bottom-style: solid; border-bottom-width: 1px; border-bottom-color: darkgray;">

                                                          </td></tr></table>

                                                </td>
                                            </tr>
                                        </table>




                                    </td>
                                </tr>
                                <!-- -->
                                <tr>
                                    <td>
                                        <table style="width:100%" cellpadding="0"  cellspacing="0">
                                            <tr>
                                                <td class="menuClass" style="background-color:white; padding: 0 15px 15px 15px; border-left: 1px solid black; border-right: 1px solid black; border-bottom: 1px solid black;">

                                                    <asp:MultiView ID="MultiView3" runat="server" ActiveViewIndex="0">

                                                        <%-- ===================== General ===================== --%>
                                                        <asp:View ID="View1" runat="server">
                                                            <div class="tab-body-inner">

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
                                                                    <div class="form-label">Show in</div>
                                                                    <div class="form-control-cell">
                                                                        <asp:CheckBoxList ID="cblShowIn" runat="server" RepeatDirection="Horizontal" RepeatLayout="Flow" CssClass="chip-list">
                                                                            <asp:ListItem Text="Show In Default" Value="Default" Selected="True" />
                                                                            <asp:ListItem Text="Show In Preview" Value="Preview" />
                                                                        </asp:CheckBoxList>
                                                                    </div>
                                                                </div>

                                                                <div class="form-row">
                                                                    <div class="form-label">Action Title</div>
                                                                    <div class="form-control-cell">
                                                                        <asp:TextBox ID="txtActionTitle" runat="server" CssClass="txt-input" placeholder="e.g. Delete Record" />
                                                                        <div class="field-hint">Not in the original tab list you gave me - kept here since Save/Update needs a title for the action. Remove if that was intentional.</div>
                                                                    </div>
                                                                </div>

                                                                <div class="form-row">
                                                                    <div class="form-label">Action Type</div>
                                                                    <div class="form-control-cell">
                                                                        <asp:DropDownList ID="ddlActionType" runat="server" CssClass="ddl-input">
                                                                            <asp:ListItem>Select Action</asp:ListItem>
                                                                            <asp:ListItem Value="CHANGE">Change Status</asp:ListItem>
                                                                            <asp:ListItem Value="DLET">Delete</asp:ListItem>
                                                                            <asp:ListItem Value="ACTV">Activate</asp:ListItem>
                                                                            <asp:ListItem Value="DACTV">De-Activate</asp:ListItem>
                                                                            <asp:ListItem>Edit</asp:ListItem>
                                                                        </asp:DropDownList>
                                                                    </div>
                                                                </div>

                                                                <div class="form-row">
                                                                    <div class="form-label">To Status</div>
                                                                    <div class="form-control-cell">
                                                                        <asp:DropDownList ID="ddlToStatus" runat="server" CssClass="ddl-input" />
                                                                    </div>
                                                                </div>

                                                                <div class="form-row">
                                                                    <div class="form-label">Implementer Title</div>
                                                                    <div class="form-control-cell">
                                                                        <asp:TextBox ID="txtImplementerTitle" runat="server" CssClass="txt-input" placeholder="Maker, Checker, etc" />
                                                                    </div>
                                                                </div>

                                                            </div>
                                                        </asp:View>

                                                        <%-- ===================== Payments ===================== --%>
                                                        <asp:View ID="View2" runat="server">
                                                            <div class="tab-body-inner">

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
                                                                    <div class="form-label">Add Payment Plan</div>
                                                                    <div class="form-control-cell">
                                                                        <asp:DropDownList ID="ddlPaymentPlan" runat="server" CssClass="ddl-input" AutoPostBack="true" OnSelectedIndexChanged="ddlPaymentPlan_SelectedIndexChanged" />
                                                                    </div>
                                                                </div>

                                                                <div class="form-row" id="rowAddedPlans" runat="server" style="flex-basis:100%;">
                                                                    <div class="form-control-cell" style="flex-basis:100%;">
                                                                        <asp:Repeater ID="rptAddedPlans" runat="server" OnItemCommand="rptAddedPlans_ItemCommand">
                                                                            <ItemTemplate>
                                                                                <span class="plan-chip">
                                                                                    <asp:LinkButton ID="lnkViewPlan" runat="server" CommandName="View" CommandArgument='<%# Eval("PLAN_ID") %>' CausesValidation="false" Text='<%# Eval("NAME") %>' />
                                                                                    <asp:LinkButton ID="lnkRemovePlan" runat="server" CommandName="Remove" CommandArgument='<%# Eval("PLAN_ID") %>' CssClass="plan-chip-remove" CausesValidation="false" ToolTip="Remove this plan">&#10005;</asp:LinkButton>
                                                                                </span>
                                                                            </ItemTemplate>
                                                                        </asp:Repeater>
                                                                    </div>
                                                                </div>

                                                                <div class="form-row" id="rowPlanDetails" runat="server" style="flex-basis:100%;">
                                                                    <div class="form-control-cell" style="flex-basis:100%;">
                                                                        <div class="plan-block">
                                                                            <div class="plan-block-title">
                                                                                <asp:Label ID="lblCurrentPlanName" runat="server" />
                                                                            </div>
                                                                            <asp:GridView ID="gvPlanDetails" runat="server" AutoGenerateColumns="True"
                                                                                CssClass="plan-details-grid" GridLines="None"
                                                                                DataKeyNames="DETAIL_ID"
                                                                                EmptyDataText="No plan details found for this payment plan.">
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
                                                                </div>

                                                            </div>
                                                        </asp:View>

                                                        <%-- ===================== AutoTransfer (intentionally empty) ===================== --%>
                                                        <asp:View ID="View3" runat="server">
                                                            <div class="tab-body-inner"></div>
                                                        </asp:View>

                                                        <%-- ===================== Parameters and Script ===================== --%>
                                                        <asp:View ID="View4" runat="server">
                                                            <div class="tab-body-inner">

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
                                                        </asp:View>

                                                        <%-- ===================== Pre-Execution ===================== --%>
                                                        <asp:View ID="View5" runat="server">
                                                            <div class="tab-body-inner">

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
                                                        </asp:View>

                                                    </asp:MultiView>

                                                    <div class="btn-row">
                                                        <asp:Button ID="btnLoad" runat="server" Text="Load" CssClass="btn-cancel" CausesValidation="false" OnClick="btnLoad_Click" />
                                                        <asp:Button ID="btnCancel" runat="server" Text="Cancel" CssClass="btn-cancel" CausesValidation="false" OnClick="btnCancel_Click" />
                                                        <asp:Button ID="btnSave" runat="server" Text="Save Action" CssClass="btn-save" OnClick="btnSave_Click" />
                                                    </div>

                                                    <asp:Label ID="lblMessage" runat="server" CssClass="msg-success" Visible="false" />

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
