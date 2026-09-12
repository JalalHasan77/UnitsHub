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

    </style>
</head>
<body>
    <form id="form1" runat="server">
        <div class="page-header">
            <h1 class="page-title"><asp:Label ID="lblFormTitle" runat="server" Text="Add Auto-Transfer Plan" /></h1>
        </div>

        <div class="content-wrapper">

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
                document.addEventListener('dragover', onDragOver);
                document.addEventListener('drop', onDrop);
            })();
        </script>
    </form>
</body>
</html>
