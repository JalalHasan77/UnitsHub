<%@ Page Language="VB" AutoEventWireup="false" CodeFile="AutoTransferPlanForm.aspx.vb" Inherits="AutoTransferPlanForm" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Auto-Transfer Plan</title>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <link rel="preconnect" href="https://fonts.googleapis.com" />
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700;800&display=swap" rel="stylesheet" />
    <style type="text/css">

        :root {
            --brand-1: #4f46e5;
            --brand-soft: #eef2ff;
            --ink: #1e293b;
            --muted: #64748b;
            --border: #e2e8f0;
            --bg: #eef1f8;
        }

        * { box-sizing: border-box; }

        body {
            font-family: 'Inter', 'Segoe UI', Arial, sans-serif;
            background-color: var(--bg);
            margin: 0;
            color: var(--ink);
        }

        .page-header {
            background: linear-gradient(135deg, #4f46e5 0%, #3730a3 100%);
            padding: 22px 32px;
            display: flex;
            align-items: center;
            justify-content: space-between;
            gap: 16px;
        }

        /* [X] in the upper right corner of the header */
        .close-btn {
            flex: 0 0 auto;
            width: 34px;
            height: 34px;
            padding: 7px;
            border: none;
            border-radius: 8px;
            background: transparent;
            cursor: pointer;
            transition: background .15s ease;
        }
        .close-btn:hover,
        .close-btn:focus-visible {
            background: rgba(255, 255, 255, 0.22);
            outline: none;
        }

        .page-title {
            font-size: 24px;
            font-weight: 700;
            color: #ffffff;
            margin: 0;
        }

        .content-wrapper {
            max-width: 1200px;
            margin: 28px auto 48px auto;
            padding: 0 20px;
        }

        .content-card {
            background-color: #ffffff;
            border: 1px solid var(--border);
            border-radius: 12px;
            padding: 20px 24px;
            margin-bottom: 20px;
        }

        .section-label {
            font-size: 12px;
            font-weight: 700;
            text-transform: uppercase;
            letter-spacing: 0.4px;
            color: var(--muted);
            margin-bottom: 12px;
        }

        .txt-input {
            width: 100%;
            padding: 9px 12px;
            border: 1.5px solid var(--border);
            border-radius: 8px;
            font-size: 14px;
            font-family: inherit;
            color: var(--ink);
        }

        .txt-input:focus { outline: none; border-color: var(--brand-1); }

        .field-label { font-size: 12px; color: var(--muted); display: block; margin-bottom: 4px; }

        .mini-form-row {
            display: grid;
            grid-template-columns: 90px 1fr auto;
            gap: 10px;
            align-items: end;
        }

        .detail-mini-form-row {
            display: grid;
            grid-template-columns: 1.4fr 1fr 1fr 0.7fr 1fr 0.7fr 0.7fr auto;
            gap: 8px;
            align-items: end;
            padding: 12px 0 4px 0;
        }

        .btn {
            padding: 9px 16px;
            border-radius: 8px;
            font-size: 13.5px;
            font-weight: 600;
            font-family: inherit;
            cursor: pointer;
            border: 1.5px solid var(--border);
            background: #ffffff;
            color: var(--ink);
        }

        .btn-primary { background: var(--brand-1); border-color: var(--brand-1); color: #ffffff; }
        .btn-icon { width: 34px; height: 34px; padding: 0; border-radius: 8px; }

        .group-card {
            border: 1px solid var(--border);
            border-radius: 12px;
            overflow: hidden;
            margin-bottom: 16px;
        }

        .group-card:last-child { margin-bottom: 0; }

        .group-header {
            background: var(--brand-soft);
            padding: 10px 16px;
            display: flex;
            align-items: center;
            gap: 10px;
            cursor: grab;
        }

        .group-header.dragging { opacity: 0.4; }

        .group-chip {
            background: var(--brand-1);
            color: #ffffff;
            font-size: 12px;
            font-weight: 700;
            padding: 3px 10px;
            border-radius: 999px;
        }

        .group-title { flex: 1; font-weight: 600; font-size: 14px; color: var(--ink); }

        /* Dropdown + textbox on the right of each group header */
        .group-header-controls {
            display: flex;
            align-items: center;
            gap: 8px;
            margin-left: auto;
        }

        .group-header-controls .hdr-input {
            height: 28px;
            padding: 2px 8px;
            border: 1.5px solid var(--border);
            border-radius: 7px;
            background: #ffffff;
            font-size: 12px;
            font-family: inherit;
            color: var(--ink);
            cursor: auto;
        }

        .group-header-controls select.hdr-input { width: 150px; cursor: pointer; }
        .group-header-controls input.hdr-input  { width: 190px; }
        .group-header-controls .hdr-input:focus { outline: none; border-color: var(--brand-1); }

        .group-remove {
            border: none;
            background: none;
            color: var(--muted);
            font-size: 16px;
            cursor: pointer;
            padding: 2px 6px;
        }
        .group-remove:hover { color: #dc2626; }

        .detail-table { width: 100%; border-collapse: collapse; font-size: 13px; }
        .detail-table th {
            text-align: left;
            font-size: 11px;
            text-transform: uppercase;
            color: var(--muted);
            padding: 8px 16px 4px 16px;
            font-weight: 700;
        }
        .detail-table td { padding: 7px 16px; border-top: 1px solid var(--border); }
        .detail-table td.num { text-align: right; }

        .row-actions { white-space: nowrap; }
        .row-actions button {
            border: none; background: none; cursor: pointer; color: var(--muted);
            font-size: 13px; padding: 0 3px;
        }
        .row-actions button:hover { color: var(--brand-1); }
        .row-actions button.danger:hover { color: #dc2626; }

        .detail-mini-form { padding: 4px 16px 14px 16px; }

        .btn-row {
            display: flex;
            justify-content: flex-end;
            gap: 10px;
            margin-top: 4px;
        }

        .msg-success {
            display: block;
            margin-top: 14px;
            padding: 10px 14px;
            border-radius: 8px;
            background-color: #ecfdf5;
            color: #047857;
            border: 1px solid #a7f3d0;
            font-size: 13.5px;
        }

        .empty-hint { color: var(--muted); font-size: 13px; padding: 6px 0; }

        /* ---------- Top panel: what to do (Add / Edit / Copy) ---------- */
        /* Tinted indigo so it stands apart from the white boxes below */
        .mode-card {
            padding-bottom: 16px;
            background: linear-gradient(135deg, #eef2ff 0%, #e0e7ff 100%);
            border: 1px solid #c7d2fe;
            border-left: 5px solid var(--brand-1);
            box-shadow: 0 2px 8px rgba(79, 70, 229, 0.10);
        }
        .mode-card .section-label { color: #3730a3; }
        .mode-card .field-label { color: #4338ca; font-weight: 600; }

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
        .mode-options input[type="radio"]:checked + label {
            background: var(--brand-1);
            border-color: var(--brand-1);
            color: #ffffff;
        }
        .mode-options input[type="radio"]:focus-visible + label { outline: 2px solid var(--brand-1); outline-offset: 2px; }

        .mode-row {
            display: grid;
            grid-template-columns: minmax(220px, 1fr) minmax(220px, 1fr) auto;
            gap: 10px;
            align-items: end;
            margin-top: 16px;
        }
        .mode-row > div { min-width: 0; }

        .msg-error { display: block; margin-top: 10px; color: #dc2626; font-size: 13px; }

    </style>
</head>
<body>
    <form id="form1" runat="server">
        <div class="page-header">
            <h1 class="page-title"><asp:Label ID="lblFormTitle" runat="server" Text="Add Auto-Transfer Plan" /></h1>
            <asp:ImageButton ID="imgClose"
                runat="server"
                CssClass="close-btn"
                CausesValidation="false"
                ToolTip="Close"
                AlternateText="Close"
                OnClick="imgClose_Click"
                ImageUrl="data:image/svg+xml;utf8,&lt;svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='none' stroke='white' stroke-width='2.5' stroke-linecap='round' stroke-linejoin='round'&gt;&lt;line x1='18' y1='6' x2='6' y2='18'/&gt;&lt;line x1='6' y1='6' x2='18' y2='18'/&gt;&lt;/svg&gt;" />
        </div>

        <div class="content-wrapper">

            <%-- ===== Top panel: Add new / Edit existing / Copy existing ===== --%>
            <div class="content-card mode-card">
                <div class="section-label">What do you want to do?</div>
                <asp:RadioButtonList ID="rblMode" runat="server" CssClass="mode-options"
                    RepeatDirection="Horizontal" RepeatLayout="Flow"
                    AutoPostBack="true" CausesValidation="false"
                    OnSelectedIndexChanged="rblMode_SelectedIndexChanged">
                    <asp:ListItem Text="Add New AutoTransfer Plan" Value="New" Selected="True" />
                    <asp:ListItem Text="Edit Existing Plan" Value="Edit" />
                    <asp:ListItem Text="Copy Existing Plan" Value="Copy" />
                </asp:RadioButtonList>

                <%-- Edit / Copy: pick a plan; Copy also: new title + Copy button --%>
                <asp:Panel ID="pnlPlanPicker" runat="server" CssClass="mode-row" Visible="false">
                    <div>
                        <span class="field-label">Existing plan</span>
                        <asp:DropDownList ID="ddlExistingPlan" runat="server" CssClass="txt-input"
                            AutoPostBack="true" CausesValidation="false"
                            OnSelectedIndexChanged="ddlExistingPlan_SelectedIndexChanged" />
                    </div>
                    <asp:PlaceHolder ID="phCopy" runat="server" Visible="false">
                        <div>
                            <span class="field-label">Plan Title</span>
                            <asp:TextBox ID="txtCopyTitle" runat="server" CssClass="txt-input" placeholder="Title of the new plan" />
                        </div>
                        <asp:Button ID="btnCopy" runat="server" Text="Copy" CssClass="btn btn-primary"
                            CausesValidation="false" OnClick="btnCopy_Click" />
                    </asp:PlaceHolder>
                </asp:Panel>
                <asp:Label ID="lblModeMessage" runat="server" CssClass="msg-error" Visible="false" />
            </div>

            <%-- The plan editor (hidden in Copy mode, and in Edit mode until a plan is picked) --%>
            <asp:Panel ID="pnlEditor" runat="server">

            <div class="content-card">
                <div class="section-label">Plan (entered once)</div>
                <span class="field-label">Plan name</span>
                <asp:TextBox ID="txtPlanName" runat="server" CssClass="txt-input" placeholder="e.g. Sales team to process the entries in dedicated screens in ICBS" />
                <asp:RequiredFieldValidator ID="rfvPlanName" runat="server" ControlToValidate="txtPlanName"
                    ErrorMessage="Plan name is required." Display="Dynamic" ForeColor="#dc2626" Font-Size="12.5px" />
            </div>

            <div class="content-card">
                <div class="section-label">Add group</div>
                <div class="mini-form-row">
                    <div>
                        <span class="field-label">Label</span>
                        <asp:TextBox ID="txtGroupLabel" runat="server" CssClass="txt-input" placeholder="1.1" />
                    </div>
                    <div>
                        <span class="field-label">Group title</span>
                        <asp:TextBox ID="txtGroupTitle" runat="server" CssClass="txt-input" placeholder="Auto-transfer" />
                    </div>
                    <asp:Button ID="btnAddGroup" runat="server" Text="Add group" CssClass="btn" OnClick="btnAddGroup_Click" CausesValidation="false" />
                </div>
            </div>

            <div class="content-card">
                <div class="section-label">Groups &amp; detail rows</div>

                <asp:HiddenField ID="hdnGroupOrder" runat="server" />

                <asp:Repeater ID="rptGroups" runat="server" OnItemCommand="rptGroups_ItemCommand" OnItemDataBound="rptGroups_ItemDataBound">
                    <ItemTemplate>
                        <div class="group-card" data-group-id='<%# Eval("GroupId") %>'>
                            <div class="group-header" draggable="true">
                                <span class="group-chip"><%# Eval("Label") %></span>
                                <span class="group-title"><%# Eval("Title") %></span>
                                <span class="group-header-controls">
                                    <asp:HiddenField ID="hdnHeaderGroupId" runat="server" Value='<%# Eval("GroupId") %>' />
                                    <asp:DropDownList ID="ddlGroupOption" runat="server" CssClass="hdr-input">
                                        <asp:ListItem Text="implement on.." Value="" />
                                    </asp:DropDownList>
                                    <asp:TextBox ID="txtGroupValue" runat="server" CssClass="hdr-input" placeholder="Enter the Reference Phrase" />
                                </span>
                                <asp:LinkButton ID="lnkRemoveGroup" runat="server" CssClass="group-remove"
                                    CommandName="RemoveGroup" CommandArgument='<%# Eval("GroupId") %>'
                                    CausesValidation="false" ToolTip="Remove group">&#10005;</asp:LinkButton>
                            </div>

                            <asp:Repeater ID="rptDetails" runat="server" OnItemCommand="rptDetails_ItemCommand">
                                <HeaderTemplate>
                                    <table class="detail-table">
                                        <tr>
                                            <th>Account</th><th>Escrow type</th><th>Account no.</th><th>Branch</th>
                                            <th>System</th><th class="num">Debit</th><th class="num">Credit</th><th></th>
                                        </tr>
                                </HeaderTemplate>
                                <ItemTemplate>
                                        <tr>
                                            <td><%# Eval("AccountDescription") %></td>
                                            <td><%# Eval("TypeOfEscrow") %></td>
                                            <td><%# Eval("AccountNumber") %></td>
                                            <td><%# Eval("Branch") %></td>
                                            <td><%# Eval("SystemName") %></td>
                                            <td class="num"><%# Eval("Debit") %></td>
                                            <td class="num"><%# Eval("Credit") %></td>
                                            <td class="row-actions">
                                                <asp:LinkButton ID="lnkMoveUp" runat="server" CommandName="MoveDetailUp" CommandArgument='<%# Eval("DetailId") %>' CausesValidation="false" ToolTip="Move up">&#9650;</asp:LinkButton>
                                                <asp:LinkButton ID="lnkMoveDown" runat="server" CommandName="MoveDetailDown" CommandArgument='<%# Eval("DetailId") %>' CausesValidation="false" ToolTip="Move down">&#9660;</asp:LinkButton>
                                                <asp:LinkButton ID="lnkRemoveDetail" runat="server" CssClass="danger" CommandName="RemoveDetail" CommandArgument='<%# Eval("DetailId") %>' CausesValidation="false" ToolTip="Remove row">&#10005;</asp:LinkButton>
                                            </td>
                                        </tr>
                                </ItemTemplate>
                                <FooterTemplate>
                                    </table>
                                </FooterTemplate>
                            </asp:Repeater>

                            <div class="detail-mini-form">
                                <div class="detail-mini-form-row">
                                    <div><span class="field-label">Account</span><asp:TextBox ID="txtAccountDescription" runat="server" CssClass="txt-input" /></div>
                                    <div><span class="field-label">Escrow type</span><asp:TextBox ID="txtTypeOfEscrow" runat="server" CssClass="txt-input" /></div>
                                    <div><span class="field-label">Account no.</span><asp:TextBox ID="txtAccountNumber" runat="server" CssClass="txt-input" /></div>
                                    <div><span class="field-label">Branch</span><asp:TextBox ID="txtBranch" runat="server" CssClass="txt-input" /></div>
                                    <div><span class="field-label">System</span><asp:TextBox ID="txtSystemName" runat="server" CssClass="txt-input" /></div>
                                    <div><span class="field-label">Debit</span><asp:TextBox ID="txtDebit" runat="server" CssClass="txt-input" /></div>
                                    <div><span class="field-label">Credit</span><asp:TextBox ID="txtCredit" runat="server" CssClass="txt-input" /></div>
                                    <asp:LinkButton ID="lnkAddDetail" runat="server" CssClass="btn btn-icon" CommandName="AddDetail"
                                        CommandArgument='<%# Eval("GroupId") %>' CausesValidation="false" ToolTip="Add row">+</asp:LinkButton>
                                </div>
                            </div>
                        </div>
                    </ItemTemplate>
                </asp:Repeater>

                <asp:Label ID="lblNoGroups" runat="server" CssClass="empty-hint" Text="No groups added yet — use the form above." />
            </div>

            <div class="content-card">
                <div class="btn-row">
                    <asp:Button ID="btnCancel" runat="server" Text="Cancel" CssClass="btn" CausesValidation="false" OnClick="btnCancel_Click" />
                    <asp:Button ID="btnSave" runat="server" Text="Save plan" CssClass="btn btn-primary" OnClick="btnSave_Click" />
                </div>
                <asp:Label ID="lblMessage" runat="server" CssClass="msg-success" />
            </div>

            </asp:Panel>

        </div>

        <script type="text/javascript">
            (function () {
                var dragSource = null;

                function onDragStart(e) {
                    dragSource = e.target.closest('.group-card');
                    e.target.classList.add('dragging');
                    e.dataTransfer.effectAllowed = 'move';
                }

                function onDragEnd(e) {
                    e.target.classList.remove('dragging');
                }

                function onDragOver(e) {
                    e.preventDefault();
                    var card = e.target.closest('.group-card');
                    if (!card || card === dragSource) { return; }

                    var container = card.parentNode;
                    var rect = card.getBoundingClientRect();
                    var before = (e.clientY - rect.top) < (rect.height / 2);
                    container.insertBefore(dragSource, before ? card : card.nextSibling);
                }

                function onDrop(e) {
                    e.preventDefault();
                    var order = [];
                    document.querySelectorAll('.group-card').forEach(function (card) {
                        order.push(card.getAttribute('data-group-id'));
                    });

                    var hidden = document.getElementById('<%= hdnGroupOrder.ClientID %>');
                    if (hidden && hidden.value !== order.join(',')) {
                        hidden.value = order.join(',');
                        __doPostBack('<%= hdnGroupOrder.UniqueID %>', 'reorder');
                    }
                }

                document.addEventListener('dragstart', function (e) {
                    if (e.target.classList && e.target.classList.contains('group-header')) { onDragStart(e); }
                });
                document.addEventListener('dragend', function (e) {
                    if (e.target.classList && e.target.classList.contains('group-header')) { onDragEnd(e); }
                });
                // Header is draggable; don't start a drag when the user is clicking into
                // the header's dropdown / textbox (otherwise typing/selecting drags the card)
                document.addEventListener('mousedown', function (e) {
                    var header = e.target.closest ? e.target.closest('.group-header') : null;
                    if (!header) { return; }
                    header.draggable = !e.target.closest('input, select, textarea');
                });

                document.addEventListener('dragover', onDragOver);
                document.addEventListener('drop', onDrop);
            })();
        </script>
    </form>
</body>
</html>
