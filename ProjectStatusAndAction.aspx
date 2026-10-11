<%@ Page Language="VB" AutoEventWireup="false" CodeFile="ProjectStatusAndAction.aspx.vb" Inherits="ProjectStatusAndAction" MaintainScrollPositionOnPostback="true" EnableEventValidation="false" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Statuses and actions</title>
    <style>
        :root {
            --ink: #1e293b;
            --muted: #64748b;
            --line: #e2e8f0;
            --pane: #ffffff;
            --page: #f4f6fb;
            --navy: #1a1464;
            --sel-bg: #dbe7fb;
            --sel-ink: #1d3f8a;
            --tag-here-bg: #eeedfe; --tag-here-ink: #3c3489;
            --tag-status-bg: #e1f5ee; --tag-status-ink: #085041;
            --tag-all-bg: #f1efe8; --tag-all-ink: #444441;
        }

        * { box-sizing: border-box; }

        body {
            margin: 0;
            background: var(--page);
            color: var(--ink);
            font-family: "Segoe UI", Tahoma, Arial, sans-serif;
            font-size: 14px;
        }

        .topbar {
            display: flex;
            align-items: center;
            justify-content: space-between;
            gap: 16px;
            padding: 14px 20px;
            background: var(--navy);
            color: #ffffff;
        }

        .topbar h1 { margin: 0; font-size: 18px; font-weight: 600; }
        .topbar select { min-width: 220px; height: 34px; border-radius: 8px; border: 0; padding: 0 10px; }

        .layout {
            display: grid;
            grid-template-columns: 260px minmax(0, 1fr);
            gap: 14px;
            padding: 16px 20px 24px;
        }

        .pane { background: var(--pane); border: 1px solid var(--line); border-radius: 12px; }

        /* ---------- Tree ---------- */
        .tree { padding: 10px 8px; }

        .node {
            display: flex;
            align-items: center;
            gap: 8px;
            padding: 6px 10px;
            border-radius: 8px;
            color: var(--ink);
            text-decoration: none;
            line-height: 1.3;
        }

        .node:hover { background: #f1f5f9; }
        .node.sub { padding-left: 30px; color: var(--muted); }
        .node.sel { background: var(--sel-bg); color: var(--sel-ink); }
        .node .dot { width: 11px; height: 11px; border-radius: 3px; flex: none; }

        .tree-actions { display: flex; gap: 8px; padding: 12px 6px 4px; border-top: 1px solid var(--line); margin-top: 10px; }

        /* ---------- Buttons / inputs ---------- */
        .btn {
            display: inline-flex;
            align-items: center;
            gap: 6px;
            height: 34px;
            padding: 0 14px;
            border: 1px solid #cbd5e1;
            border-radius: 8px;
            background: #ffffff;
            color: var(--ink);
            font: inherit;
            cursor: pointer;
            text-decoration: none;
        }

        .btn:hover { border-color: var(--navy); }
        .btn.primary { background: var(--navy); border-color: var(--navy); color: #ffffff; }
        .btn.small { height: 30px; padding: 0 10px; font-size: 13px; }
        .btn.danger { color: #b91c1c; }
        .btn[disabled], .btn.aspNetDisabled { opacity: .45; cursor: not-allowed; }

        /* ---------- Icon buttons (Edit / Delete / Move / Users / Groups / Insert / Close) ---------- */
        .icon-btn {
            display: inline-flex;
            align-items: center;
            justify-content: center;
            width: 34px;
            height: 34px;
            padding: 0;
            border: 1.5px solid #cbd5e1;
            border-radius: 7px;
            background: #ffffff;
            color: var(--navy);
            cursor: pointer;
            text-decoration: none;
            transition: background .15s ease, border-color .15s ease, transform .1s ease;
        }
        .icon-btn svg { width: 18px; height: 18px; }
        .icon-btn:hover { border-color: var(--navy); background: #f1f4fb; }
        .icon-btn:active { transform: scale(0.95); }
        .icon-btn:focus-visible { outline: 2px solid var(--navy); outline-offset: 2px; }

        /* Edit: blue outline, blue pen (as in the sample) */
        .icon-btn.edit { border-color: #2563eb; color: #2563eb; }
        .icon-btn.edit:hover { background: #eff6ff; }

        /* Delete: red outline, light red fill, red bin (as in the sample) */
        .icon-btn.del { border-color: #dc2626; color: #dc2626; background: #fef2f2; }
        .icon-btn.del:hover { background: #fee2e2; }

        .icon-btn.aspNetDisabled { opacity: .45; cursor: not-allowed; }

        .inp, select.inp {
            height: 34px;
            padding: 0 10px;
            border: 1px solid #cbd5e1;
            border-radius: 8px;
            font: inherit;
            color: var(--ink);
            background: #ffffff;
        }

        .inp.color { width: 52px; padding: 2px; }

        /* ---------- Detail pane ---------- */
        .sec { padding: 14px 18px; border-bottom: 1px solid var(--line); }
        .sec:last-child { border-bottom: 0; }
        .sec-label { font-size: 12px; color: var(--muted); margin: 0 0 8px; }

        .head { display: flex; justify-content: space-between; align-items: center; gap: 12px; }
        .crumb { font-size: 12px; color: var(--muted); }
        .title { font-size: 18px; font-weight: 600; margin-top: 2px; }
        .head-tools { display: flex; gap: 8px; }

        .preview { display: flex; align-items: center; gap: 10px; flex-wrap: wrap; }
        .card-status { border-radius: 8px; padding: 6px 16px; font-weight: 600; }
        .card-sub { border-radius: 8px; padding: 4px 12px; font-size: 13px; }
        .hint { color: var(--muted); font-size: 13px; }

        .form-grid { display: grid; grid-template-columns: 140px minmax(0, 1fr); gap: 10px 14px; align-items: center; }
        .form-grid .full { grid-column: 1 / -1; }
        .colors { display: flex; gap: 14px; flex-wrap: wrap; align-items: center; font-size: 13px; color: var(--muted); }
        .colors label { display: inline-flex; align-items: center; gap: 6px; }
        .panel { background: #f8fafc; border: 1px solid var(--line); border-radius: 10px; padding: 14px; margin-top: 10px; }
        .panel-actions { display: flex; gap: 8px; justify-content: flex-end; margin-top: 12px; }

        /* ---------- Actions table ---------- */
        .actions-head { display: flex; justify-content: space-between; align-items: center; gap: 10px; margin-bottom: 6px; }
        .arow {
            display: grid;
            grid-template-columns: minmax(0, 1.5fr) minmax(0, 0.8fr) minmax(0, 1.1fr) minmax(0, 1.1fr) auto;
            gap: 12px;
            align-items: center;
            padding: 9px 0;
            border-top: 1px solid var(--line);
        }

        .arow.headrow { border-top: 0; color: var(--muted); font-size: 12px; padding-top: 4px; }
        .target { color: var(--muted); }
        .tag { display: inline-block; font-size: 12px; padding: 2px 9px; border-radius: 6px; white-space: nowrap; }
        .tag.here { background: var(--tag-here-bg); color: var(--tag-here-ink); }
        .tag.status { background: var(--tag-status-bg); color: var(--tag-status-ink); }
        .tag.all { background: var(--tag-all-bg); color: var(--tag-all-ink); }
        .chip { display: inline-block; font-size: 12px; padding: 2px 8px; margin: 2px 4px 2px 0; border-radius: 6px; background: #f1f5f9; color: var(--muted); }
        .row-tools { display: flex; gap: 6px; }
        .empty { color: var(--muted); padding: 14px 0 4px; }

        .msg { display: block; margin: 0 20px; padding: 10px 14px; border-radius: 8px; font-weight: 600; }
        .msg.ok { background: #ecfdf3; color: #15803d; border: 1px solid #86efac; }
        .msg.err { background: #fef2f2; color: #b91c1c; border: 1px solid #fca5a5; }

        .btn:focus-visible, .node:focus-visible, .inp:focus-visible { outline: 2px solid var(--navy); outline-offset: 2px; }

        @media (max-width: 860px) {
            .layout { grid-template-columns: 1fr; }
            .arow { grid-template-columns: 1fr; }
            .form-grid { grid-template-columns: 1fr; }
        }
    </style>
</head>
<body>
    <form id="form1" runat="server">

        <div class="topbar">
            <h1>Statuses and actions</h1>
            <div style="display:flex;gap:10px;align-items:center">
                <asp:DropDownList ID="ddlProjectName" runat="server" AutoPostBack="true" OnSelectedIndexChanged="ddlProjectName_SelectedIndexChanged" />
                <asp:LinkButton ID="btnClose" runat="server" OnClick="btnClose_Click" CausesValidation="false" CssClass="icon-btn" ToolTip="Close" aria-label="Close"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.4" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg></asp:LinkButton>
            </div>
        </div>

        <div style="padding-top:12px">
            <asp:Label ID="lblMsg" runat="server" CssClass="msg" EnableViewState="false" Visible="false" />
        </div>

        <div class="layout">

            <!-- ================= Left: status tree ================= -->
            <div class="pane tree">
                <asp:Repeater ID="rptTree" runat="server" OnItemCommand="rptTree_ItemCommand">
                    <ItemTemplate>
                        <asp:LinkButton ID="lnkNode" runat="server" CommandName="Select"
                            CommandArgument='<%# Eval("NodeKey") %>' CausesValidation="false"
                            CssClass='<%# "node" & If(CInt(Eval("NodeLevel")) = 2, " sub", "") & If(CBool(Eval("IsSelected")), " sel", "") %>'>
                            <%# Eval("Marker") %><span><%# Server.HtmlEncode(Eval("NodeText").ToString()) %></span>
                        </asp:LinkButton>
                    </ItemTemplate>
                </asp:Repeater>

                <div class="tree-actions">
                    <asp:Button ID="btnAddStatus" runat="server" Text="+ Status" CssClass="btn small" CausesValidation="false" />
                    <asp:Button ID="btnShowAddSub" runat="server" Text="+ Sub-status" CssClass="btn small" CausesValidation="false" OnClick="btnShowAddSub_Click" />
                </div>
            </div>

            <!-- ================= Right: selected node ================= -->
            <div class="pane">

                <div class="sec">
                    <div class="head">
                        <div>
                            <div class="crumb"><asp:Literal ID="litCrumb" runat="server" /></div>
                            <div class="title"><asp:Literal ID="litTitle" runat="server" /></div>
                        </div>
                        <div class="head-tools">
                            <asp:LinkButton ID="btnAddStateUp" runat="server" CausesValidation="false" CssClass="icon-btn" ToolTip="Add a state above" aria-label="Add a state above"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><line x1="12" y1="3" x2="12" y2="11"/><line x1="8" y1="7" x2="16" y2="7"/><rect x="4" y="15" width="16" height="5" rx="1.2"/></svg></asp:LinkButton>
                            <asp:LinkButton ID="btnAddStateDown" runat="server" CausesValidation="false" CssClass="icon-btn" ToolTip="Add a state below" aria-label="Add a state below"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect x="4" y="4" width="16" height="5" rx="1.2"/><line x1="12" y1="13" x2="12" y2="21"/><line x1="8" y1="17" x2="16" y2="17"/></svg></asp:LinkButton>
                            <asp:LinkButton ID="btnEditStatus" runat="server" CausesValidation="false" CssClass="icon-btn edit" ToolTip="Edit status" aria-label="Edit status"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M11 4H6a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2v-5"/><path d="M18.4 2.6a2 2 0 0 1 2.8 2.8L12 14.6l-3.6.8.8-3.6z"/></svg></asp:LinkButton>
                            <asp:LinkButton ID="btnUp" runat="server" CausesValidation="false" OnClick="btnUp_Click" CssClass="icon-btn" ToolTip="Move up" aria-label="Move up"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.4" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><polyline points="6 15 12 9 18 15"/></svg></asp:LinkButton>
                            <asp:LinkButton ID="btnDown" runat="server" CausesValidation="false" OnClick="btnDown_Click" CssClass="icon-btn" ToolTip="Move down" aria-label="Move down"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.4" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><polyline points="6 9 12 15 18 9"/></svg></asp:LinkButton>
                            <asp:LinkButton ID="btnEdit" runat="server" CausesValidation="false" OnClick="btnEdit_Click" CssClass="icon-btn edit" ToolTip="Edit sub-status" aria-label="Edit sub-status"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M11 4H6a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2v-5"/><path d="M18.4 2.6a2 2 0 0 1 2.8 2.8L12 14.6l-3.6.8.8-3.6z"/></svg></asp:LinkButton>
                        </div>
                    </div>

                    <!-- Edit status / sub-status -->
                    <asp:Panel ID="pnlEdit" runat="server" CssClass="panel" Visible="false">
                        <div class="form-grid">
                            <span>Subtitle</span>
                            <asp:TextBox ID="txtEditSubtitle" runat="server" CssClass="inp" />
                            <span>Colours</span>
                            <div class="colors">
                                <label>Card <asp:TextBox ID="txtEditStatusBg" runat="server" TextMode="Color" CssClass="inp color" /></label>
                                <label>Title <asp:TextBox ID="txtEditStatusFg" runat="server" TextMode="Color" CssClass="inp color" /></label>
                                <label>Subtitle box <asp:TextBox ID="txtEditSubBg" runat="server" TextMode="Color" CssClass="inp color" /></label>
                                <label>Subtitle text <asp:TextBox ID="txtEditSubFg" runat="server" TextMode="Color" CssClass="inp color" /></label>
                            </div>
                        </div>
                        <div class="panel-actions">
                            <asp:Button ID="btnEditCancel" runat="server" Text="Cancel" CssClass="btn small" CausesValidation="false" OnClick="btnEditCancel_Click" />
                            <asp:Button ID="btnEditSave" runat="server" Text="Save changes" CssClass="btn small primary" OnClick="btnEditSave_Click" />
                        </div>
                    </asp:Panel>

                    <!-- Add sub-status -->
                    <asp:Panel ID="pnlAddSub" runat="server" CssClass="panel" Visible="false">
                        <div class="form-grid">
                            <span>Status</span>
                            <strong><asp:Literal ID="litAddSubStatus" runat="server" /></strong>
                            <span>Subtitle</span>
                            <asp:TextBox ID="txtNewSubtitle" runat="server" CssClass="inp" placeholder="Waiting 3rd payment" />
                            <span>Colours</span>
                            <div class="colors">
                                <label>Card <asp:TextBox ID="txtNewStatusBg" runat="server" TextMode="Color" CssClass="inp color" /></label>
                                <label>Title <asp:TextBox ID="txtNewStatusFg" runat="server" TextMode="Color" CssClass="inp color" /></label>
                                <label>Subtitle box <asp:TextBox ID="txtNewSubBg" runat="server" TextMode="Color" CssClass="inp color" /></label>
                                <label>Subtitle text <asp:TextBox ID="txtNewSubFg" runat="server" TextMode="Color" CssClass="inp color" /></label>
                            </div>
                        </div>
                        <div class="panel-actions">
                            <asp:Button ID="btnAddSubCancel" runat="server" Text="Cancel" CssClass="btn small" CausesValidation="false" OnClick="btnAddSubCancel_Click" />
                            <asp:Button ID="btnAddSubSave" runat="server" Text="Add sub-status" CssClass="btn small primary" OnClick="btnAddSubSave_Click" />
                        </div>
                    </asp:Panel>
                </div>

                <asp:Panel ID="pnlPreview" runat="server" CssClass="sec">
                    <p class="sec-label">Card colours</p>
                    <div class="preview">
                        <div id="divPreviewStatus" runat="server" class="card-status"></div>
                        <div id="divPreviewSub" runat="server" class="card-sub"></div>
                        <span class="hint">Preview as on the main page</span>
                    </div>
                </asp:Panel>

                <div class="sec">
                    <div class="actions-head">
                        <p class="sec-label" style="margin:0">Actions available here</p>
                        <div class="row-tools">
                            <asp:Button ID="btnShowPlace" runat="server" Text="Place existing action" CssClass="btn small" CausesValidation="false" OnClick="btnShowPlace_Click" />
                            <asp:Button ID="btnNewAction" runat="server" Text="+ New action" CssClass="btn small" CausesValidation="false" OnClick="btnNewAction_Click" />
                        </div>
                    </div>

                    <!-- Place an existing action here -->
                    <asp:Panel ID="pnlPlace" runat="server" CssClass="panel" Visible="false">
                        <div class="form-grid">
                            <span>Action</span>
                            <asp:DropDownList ID="ddlPlaceAction" runat="server" CssClass="inp" AutoPostBack="true" OnSelectedIndexChanged="ddlPlaceAction_SelectedIndexChanged" />
                            <asp:Label ID="lblPlaceScope" runat="server" Text="Valid in" />
                            <asp:RadioButtonList ID="rblPlaceScope" runat="server" RepeatDirection="Horizontal" />
                            <span>Moves the unit to</span>
                            <div style="display:flex;gap:8px;flex-wrap:wrap">
                                <asp:DropDownList ID="ddlPlaceToStatus" runat="server" CssClass="inp" AutoPostBack="true" OnSelectedIndexChanged="ddlPlaceToStatus_SelectedIndexChanged" />
                                <asp:DropDownList ID="ddlPlaceToSub" runat="server" CssClass="inp" />
                            </div>
                            <span>Rights</span>
                            <asp:DropDownList ID="ddlCopyUsersFrom" runat="server" CssClass="inp" />
                        </div>
                        <div class="panel-actions">
                            <asp:Button ID="btnPlaceCancel" runat="server" Text="Cancel" CssClass="btn small" CausesValidation="false" OnClick="btnPlaceCancel_Click" />
                            <asp:Button ID="btnPlaceSave" runat="server" Text="Place action" CssClass="btn small primary" OnClick="btnPlaceSave_Click" />
                        </div>
                    </asp:Panel>

                    <div class="arow headrow"><span>Action</span><span>Comes from</span><span>Groups allowed</span><span>Users allowed</span><span></span></div>

                    <asp:Repeater ID="rptActions" runat="server" OnItemCommand="rptActions_ItemCommand" OnItemDataBound="rptActions_ItemDataBound">
                        <ItemTemplate>
                            <div class="arow">
                                <span>
                                    <%# Server.HtmlEncode(Eval("ACTION_TITLE").ToString()) %>
                                    <span class="target"><%# Eval("TargetText") %></span>
                                </span>
                                <span><span class='<%# "tag " & Eval("TagClass") %>'><%# Server.HtmlEncode(Eval("TagText").ToString()) %></span></span>
                                <span><%# Eval("GroupsHtml") %></span>
                                <span><%# Eval("UsersHtml") %></span>
                                <span class="row-tools">
                                    <asp:LinkButton ID="btnGroups" runat="server"
                                        CommandArgument='<%# Eval("PlacementKey") %>' CausesValidation="false" OnClick="btnGroups_Click" CssClass="icon-btn" ToolTip="Groups" aria-label="Groups"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><circle cx="12" cy="7" r="3"/><path d="M6.5 20a5.5 5.5 0 0 1 11 0"/><circle cx="4.8" cy="10" r="2.2"/><path d="M1.5 19a3.6 3.6 0 0 1 4.2-3.5"/><circle cx="19.2" cy="10" r="2.2"/><path d="M22.5 19a3.6 3.6 0 0 0-4.2-3.5"/></svg></asp:LinkButton>
                                    <asp:LinkButton ID="btnUsers" runat="server"
                                        CommandArgument='<%# Eval("PlacementKey") %>' CausesValidation="false" OnClick="btnUsers_Click" CssClass="icon-btn" ToolTip="Users" aria-label="Users"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.1" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><circle cx="9" cy="8" r="3.5"/><path d="M2.5 20a6.5 6.5 0 0 1 13 0"/><path d="M16 4.6a3.5 3.5 0 0 1 0 6.8"/><path d="M18.5 14.2A6.5 6.5 0 0 1 21.5 20"/></svg></asp:LinkButton>
                                    <asp:LinkButton ID="btnEditAction" runat="server" CausesValidation="false"
                                        CommandName="EditAction" CommandArgument='<%# Eval("PlacementKey") %>' CssClass="icon-btn edit" ToolTip="Edit action" aria-label="Edit action"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M11 4H6a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2v-5"/><path d="M18.4 2.6a2 2 0 0 1 2.8 2.8L12 14.6l-3.6.8.8-3.6z"/></svg></asp:LinkButton>
                                    <asp:LinkButton ID="btnRemove" runat="server" CommandName="Remove"
                                        CommandArgument='<%# Eval("PlacementKey") %>' CausesValidation="false"
                                        OnClientClick="return confirm('Remove this action from here? Its group and user rights are kept (they are per status).');" CssClass="icon-btn del" ToolTip="Remove from here" aria-label="Remove from here"><svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true"><path d="M9 2.5h6l1 1.5h4.5v2.5h-17V4H8z"/><path d="M5.5 8h13l-1 12.2A2 2 0 0 1 15.5 22h-7a2 2 0 0 1-2-1.8z"/></svg></asp:LinkButton>
                                </span>
                            </div>
                        </ItemTemplate>
                    </asp:Repeater>
                    <asp:Label ID="lblNoActions" runat="server" CssClass="empty" Text="No actions here yet. Place an existing action or create a new one." Visible="false" />

                </div>
            </div>
        </div>
    </form>
</body>
</html>
