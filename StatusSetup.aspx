<%@ Page Language="VB" AutoEventWireup="false" CodeFile="StatusSetup.aspx.vb" Inherits="StatusSetup" MaintainScrollPositionOnPostback="true" EnableEventValidation="false" %>

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
            grid-template-columns: minmax(0, 1.6fr) minmax(0, 0.9fr) minmax(0, 1.2fr) auto;
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
            <asp:DropDownList ID="ddlProject" runat="server" AutoPostBack="true" OnSelectedIndexChanged="ddlProject_SelectedIndexChanged" />
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
                            <asp:Button ID="btnUp" runat="server" Text="▲" CssClass="btn small" ToolTip="Move up" CausesValidation="false" OnClick="btnUp_Click" />
                            <asp:Button ID="btnDown" runat="server" Text="▼" CssClass="btn small" ToolTip="Move down" CausesValidation="false" OnClick="btnDown_Click" />
                            <asp:Button ID="btnEdit" runat="server" Text="Edit" CssClass="btn small" CausesValidation="false" OnClick="btnEdit_Click" />
                        </div>
                    </div>

                    <!-- Edit status / sub-status -->
                    <asp:Panel ID="pnlEdit" runat="server" CssClass="panel" Visible="false">
                        <div class="form-grid">
                            <asp:Label ID="lblEditStatusName" runat="server" Text="Status name" AssociatedControlID="txtEditStatus" />
                            <asp:TextBox ID="txtEditStatus" runat="server" CssClass="inp" />
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
                            <span>Users</span>
                            <asp:DropDownList ID="ddlCopyUsersFrom" runat="server" CssClass="inp" />
                        </div>
                        <div class="panel-actions">
                            <asp:Button ID="btnPlaceCancel" runat="server" Text="Cancel" CssClass="btn small" CausesValidation="false" OnClick="btnPlaceCancel_Click" />
                            <asp:Button ID="btnPlaceSave" runat="server" Text="Place action" CssClass="btn small primary" OnClick="btnPlaceSave_Click" />
                        </div>
                    </asp:Panel>

                    <div class="arow headrow"><span>Action</span><span>Comes from</span><span>Users</span><span></span></div>

                    <asp:Repeater ID="rptActions" runat="server" OnItemCommand="rptActions_ItemCommand" OnItemDataBound="rptActions_ItemDataBound">
                        <ItemTemplate>
                            <div class="arow">
                                <span>
                                    <%# Server.HtmlEncode(Eval("ACTION_TITLE").ToString()) %>
                                    <span class="target"><%# Eval("TargetText") %></span>
                                </span>
                                <span><span class='<%# "tag " & Eval("TagClass") %>'><%# Server.HtmlEncode(Eval("TagText").ToString()) %></span></span>
                                <span><%# Eval("UsersHtml") %></span>
                                <span class="row-tools">
                                    <asp:Button ID="btnUsers" runat="server" Text="Users" CssClass="btn small" CommandName="Users"
                                        CommandArgument='<%# Eval("PlacementKey") %>' CausesValidation="false" />
                                    <asp:Button ID="btnEditAction" runat="server" Text="Edit" CssClass="btn small" CausesValidation="false"
                                        CommandName="EditAction" CommandArgument='<%# Eval("PlacementKey") %>' />
                                    <asp:Button ID="btnRemove" runat="server" Text="Remove from here" CssClass="btn small danger" CommandName="Remove"
                                        CommandArgument='<%# Eval("PlacementKey") %>' CausesValidation="false"
                                        OnClientClick="return confirm('Remove this action from here? Its users for this place are removed too.');" />
                                </span>
                            </div>
                        </ItemTemplate>
                    </asp:Repeater>
                    <asp:Label ID="lblNoActions" runat="server" CssClass="empty" Text="No actions here yet. Place an existing action or create a new one." Visible="false" />

                    <!-- Users of one placement -->
                    <asp:Panel ID="pnlUsers" runat="server" CssClass="panel" Visible="false">
                        <p class="sec-label"><asp:Literal ID="litUsersFor" runat="server" /></p>
                        <asp:Repeater ID="rptUsers" runat="server" OnItemCommand="rptUsers_ItemCommand">
                            <ItemTemplate>
                                <div style="display:flex;justify-content:space-between;align-items:center;padding:6px 0;border-top:1px solid var(--line)">
                                    <span><%# Server.HtmlEncode(Eval("USER_ID").ToString()) %></span>
                                    <asp:Button ID="btnRemoveUser" runat="server" Text="Remove" CssClass="btn small danger"
                                        CommandName="RemoveUser" CommandArgument='<%# Eval("USER_ID") %>' CausesValidation="false" />
                                </div>
                            </ItemTemplate>
                        </asp:Repeater>
                        <p class="hint"><asp:Literal ID="litInheritedUsers" runat="server" /></p>
                        <div style="display:flex;gap:8px;align-items:center">
                            <asp:TextBox ID="txtAddUser" runat="server" CssClass="inp" placeholder="User ID, e.g. 2271" />
                            <asp:Button ID="btnAddUser" runat="server" Text="Add user" CssClass="btn small primary" OnClick="btnAddUser_Click" />
                        </div>
                        <div class="panel-actions">
                            <asp:Button ID="btnUsersClose" runat="server" Text="Done" CssClass="btn small" CausesValidation="false" OnClick="btnUsersClose_Click" />
                        </div>
                    </asp:Panel>
                </div>
            </div>
        </div>
    </form>
</body>
</html>
