<%@ Page Language="VB" AutoEventWireup="false" CodeFile="ManageGroups.aspx.vb" Inherits="ManageGroups" MaintainScrollPositionOnPostback="true" EnableEventValidation="false" %>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <link rel="stylesheet" href="styles.css" />
    <title>Manage Groups</title>
    <style type="text/css">
        *, ::after, ::before { box-sizing: border-box; }

        :root {
            --ink: #1e293b;
            --muted: #64748b;
            --border: #e2e8f0;
            --navy: #1a1464;
        }

        body {
            font-family: 'Segoe UI', Arial, Helvetica, sans-serif;
            margin: 0;
            padding: 0;
            color: var(--ink);
            background: #eef1f8;
        }

        .top-strip {
            display: flex;
            align-items: center;
            justify-content: space-between;
            width: 100%;
            margin: 0;
            border: none;
            background: linear-gradient(120deg, #4f46e5, #3b82f6 75%);
            color: #ffffff;
            font-size: 18px;
            font-weight: 700;
            padding: 14px 20px;
        }

        .close-btn {
            width: 28px;
            height: 28px;
            padding: 4px;
            border: none;
            border-radius: 7px;
            background: transparent;
            cursor: pointer;
            transition: background .15s ease;
        }
        .close-btn:hover, .close-btn:focus-visible { background: rgba(255, 255, 255, 0.22); outline: none; }

        .page { max-width: 1100px; margin: 0 auto; padding: 18px 16px 32px; display: flex; flex-direction: column; gap: 16px; }

        /* ---------- Cards ---------- */
        .card {
            border: 1px solid #d5dbe5;
            border-radius: 12px;
            background: #ffffff;
            padding: 16px 18px;
        }
        .card-title {
            margin: 0 0 12px 0;
            font-size: 13px;
            font-weight: 700;
            letter-spacing: .05em;
            text-transform: uppercase;
            color: var(--navy);
        }

        /* ---------- Dropdowns / inputs ---------- */
        .select-bar {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
            gap: 12px 16px;
            margin-bottom: 14px;
        }
        .select-field { display: flex; flex-direction: column; gap: 5px; min-width: 0; }
        .select-label { font-size: 13px; font-weight: 700; color: var(--navy); }
        .select-ddl, .text-input {
            width: 100%;
            height: 38px;
            padding: 0 10px;
            border: 1.5px solid #cbd5e1;
            border-radius: 8px;
            background: #ffffff;
            color: var(--ink);
            font: inherit;
            font-size: 14px;
        }
        .select-ddl:focus, .text-input:focus { border-color: #4f46e5; outline: none; box-shadow: 0 0 0 3px rgba(79, 70, 229, 0.15); }
        .select-ddl:disabled { background: #f1f5f9; color: var(--muted); }
        .select-hint { font-size: 12px; color: var(--muted); }

        .group-row { display: flex; flex-wrap: wrap; align-items: flex-end; gap: 12px; }
        .group-row .select-field { flex: 1 1 280px; }
        .group-info { margin-top: 10px; font-size: 13px; color: var(--muted); }
        .group-info strong { color: var(--ink); }

        /* ---------- Buttons ---------- */
        .btn {
            height: 38px;
            padding: 0 18px;
            border: 1.5px solid var(--navy);
            border-radius: 8px;
            background: #ffffff;
            color: var(--navy);
            font: inherit;
            font-size: 14px;
            font-weight: 600;
            cursor: pointer;
        }
        .btn:hover { background: #f1f4fb; }
        .btn.primary { background: var(--navy); color: #ffffff; }
        .btn.primary:hover { background: #2a2390; }
        .btn.aspNetDisabled, .btn[disabled] { opacity: .45; cursor: not-allowed; }

        /* ---------- New group panel ---------- */
        .new-group {
            margin-top: 14px;
            padding: 14px;
            border: 1px dashed #a5b4fc;
            border-radius: 10px;
            background: #eef2ff;
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
            gap: 12px 16px;
            align-items: end;
        }
        .new-group-actions { display: flex; gap: 8px; }
        .check-field { display: flex; align-items: center; gap: 8px; height: 38px; font-size: 14px; }
        .check-field input { width: 17px; height: 17px; accent-color: var(--navy); }

        /* ---------- List heads ---------- */
        .list-head {
            display: flex;
            flex-wrap: wrap;
            align-items: center;
            justify-content: space-between;
            gap: 10px;
            margin-bottom: 12px;
        }
        .list-title { font-size: 14px; font-weight: 700; color: var(--navy); }
        .list-tools { display: flex; flex-wrap: wrap; gap: 8px; align-items: center; }
        .link-btn {
            border: none;
            background: none;
            padding: 0 4px;
            color: #2563eb;
            font: inherit;
            font-size: 13px;
            font-weight: 600;
            cursor: pointer;
        }
        .link-btn:hover { text-decoration: underline; }
        .filter-input { width: 220px; height: 34px; }

        /* ---------- Members ---------- */
        .members-list {
            list-style: none;
            margin: 0;
            padding: 0;
            display: flex;
            flex-direction: column;   /* one user per line */
            gap: 6px;
            max-height: 360px;
            overflow-y: auto;
        }
        .members-list label { cursor: pointer; font-size: 14px; }
        .members-list input[type="checkbox"] { width: 17px; height: 17px; margin-right: 8px; vertical-align: middle; accent-color: var(--navy); }
        .members-list .aspNetDisabled { color: var(--muted); }
        .members-list .u-id { color: var(--muted); font-size: 12.5px; }
        .members-list .u-tag {
            display: inline-block;
            margin-left: 6px;
            padding: 1px 7px;
            border-radius: 6px;
            background: #fee2e2;
            color: #991b1b;
            font-size: 11px;
            font-weight: 700;
            vertical-align: middle;
        }

        /* ---------- Roles ---------- */
        .roles-list {
            list-style: none;
            margin: 0;
            padding: 0;
            display: flex;
            flex-direction: column;   /* one role per line */
            max-height: 420px;
            overflow-y: auto;
            gap: 8px;
        }
        .roles-list label { cursor: pointer; font-size: 14px; font-weight: 600; }
        .roles-list input[type="radio"] { width: 17px; height: 17px; margin-right: 8px; vertical-align: middle; accent-color: var(--navy); }
        .roles-list .aspNetDisabled { color: var(--muted); }
        .roles-list .r-desc { display: block; margin-left: 25px; color: var(--muted); font-size: 12.5px; font-weight: 400; }
        .tab-hint { margin: 0 0 12px 0; font-size: 13px; color: var(--muted); }

        /* ---------- Actions table ---------- */
        .actions-wrap { overflow-x: auto; border: 1px solid var(--border); border-radius: 9px; }
        .actions-table { width: 100%; min-width: 620px; border-collapse: collapse; font-size: 14px; }
        .actions-table th {
            padding: 10px 14px;
            background: #f1f4fb;
            color: var(--navy);
            font-weight: 700;
            text-align: left;
            border-bottom: 1.5px solid #d5dbe5;
            white-space: nowrap;
        }
        .actions-table td { padding: 8px 14px; border-bottom: 1px solid var(--border); vertical-align: middle; }
        .actions-table tr:last-child td { border-bottom: none; }
        .actions-table .a-name { font-weight: 600; }
        .role-yes { color: #047857; font-size: 13px; }
        .role-no { color: var(--muted); font-size: 13px; }

        /* Role / Allow / Deny switch */
        .setting { position: relative; display: inline-flex; border: 1.5px solid #cbd5e1; border-radius: 8px; overflow: hidden; }
        .setting input[type="radio"] { position: absolute; opacity: 0; width: 0; height: 0; }
        .setting label {
            display: inline-block;
            padding: 6px 12px;
            font-size: 13px;
            font-weight: 600;
            color: #334155;
            background: #ffffff;
            cursor: pointer;
        }
        .setting label + input + label, .setting span + span label { border-left: 1.5px solid #cbd5e1; }
        .setting input[type="radio"]:focus-visible + label { outline: 2px solid var(--navy); outline-offset: -2px; }
        .setting input[value="R"]:checked + label { background: #e2e8f0; color: var(--ink); }
        .setting input[value="A"]:checked + label { background: #047857; color: #ffffff; }
        .setting input[value="D"]:checked + label { background: #b91c1c; color: #ffffff; }
        .setting.aspNetDisabled label, .setting input:disabled + label { opacity: .5; cursor: not-allowed; }

        /* ---------- Tabs: Members | Actions ---------- */
        .tabs-card { padding: 0; overflow: hidden; }
        .tabs { border-bottom: 1.5px solid #d5dbe5; background: #f1f4fb; }
        .tabs ul { list-style: none; margin: 0; padding: 0 10px; display: flex; gap: 2px; }
        .tabs li { float: none !important; }
        .tabs a.static {
            display: block;
            padding: 13px 22px 11px;
            border-bottom: 3px solid transparent;
            color: var(--muted);
            font-size: 14px;
            font-weight: 700;
            text-decoration: none;
        }
        .tabs a.static:hover { color: var(--navy); background: #e8ecf7; }
        .tabs a.static.selected { color: var(--navy); border-bottom-color: #4f46e5; background: #ffffff; }
        .tab-body { padding: 16px 18px 18px; }

        .note { display: block; margin-top: 12px; font-size: 13px; font-weight: 600; }
        .note.ok { color: #047857; }
        .note.info { color: var(--muted); font-weight: 400; }
        .note.err { color: #b91c1c; }
    </style>
    <script type="text/javascript">
        // Members: "Select all" / "Clear all" act on the users currently shown by the filter
        function setAllMembers(checked) {
            var list = document.querySelector('.members-list');
            if (!list) { return false; }
            var items = list.getElementsByTagName('li');
            for (var i = 0; i < items.length; i++) {
                if (items[i].style.display === 'none') { continue; }
                var box = items[i].querySelector('input[type="checkbox"]');
                if (box && !box.disabled) { box.checked = checked; }
            }
            return false;
        }

        // Members: show only the users whose name or ID contains the typed text
        function filterMembers(input) {
            var list = document.querySelector('.members-list');
            if (!list) { return; }
            var text = (input.value || '').toLowerCase();
            var items = list.getElementsByTagName('li');
            for (var i = 0; i < items.length; i++) {
                var match = items[i].textContent.toLowerCase().indexOf(text) >= 0;
                items[i].style.display = match ? '' : 'none';
            }
        }
    </script>
</head>
<body>
    <form id="form1" runat="server">
        <div class="top-strip">
            <span>Manage Groups</span>
            <asp:ImageButton ID="imgClose"
                runat="server"
                CssClass="close-btn"
                CausesValidation="false"
                ToolTip="Close"
                AlternateText="Close"
                ImageUrl="data:image/svg+xml;utf8,&lt;svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='none' stroke='white' stroke-width='2.5' stroke-linecap='round' stroke-linejoin='round'&gt;&lt;line x1='18' y1='6' x2='6' y2='18'/&gt;&lt;line x1='6' y1='6' x2='18' y2='18'/&gt;&lt;/svg&gt;" />
        </div>

        <div class="page">

            <%-- ===== 1. Group: pick one, or add a new one ===== --%>
            <section class="card">
                <h2 class="card-title">Group</h2>
                <div class="group-row">
                    <div class="select-field">
                        <asp:Label ID="lblGroupCaption" runat="server" Text="Group" AssociatedControlID="ddlGroup" CssClass="select-label" />
                        <asp:DropDownList ID="ddlGroup" runat="server" CssClass="select-ddl" AutoPostBack="true" />
                    </div>
                    <asp:Button ID="btnShowAddGroup" runat="server" Text="+ Add New Group" CssClass="btn" CausesValidation="false" />
                </div>
                <asp:Label ID="lblGroupInfo" runat="server" CssClass="group-info" Visible="false" />

                <asp:Panel ID="pnlNewGroup" runat="server" CssClass="new-group" Visible="false" DefaultButton="btnAddGroup">
                    <div class="select-field">
                        <asp:Label ID="lblNewGroupName" runat="server" Text="Group name" AssociatedControlID="txtNewGroupName" CssClass="select-label" />
                        <asp:TextBox ID="txtNewGroupName" runat="server" CssClass="text-input" MaxLength="100" placeholder="e.g. Bahrain System-Admin" />
                    </div>
                    <div class="select-field">
                        <asp:Label ID="lblNewGroupDesc" runat="server" Text="Description" AssociatedControlID="txtNewGroupDesc" CssClass="select-label" />
                        <asp:TextBox ID="txtNewGroupDesc" runat="server" CssClass="text-input" MaxLength="400" placeholder="What this group is for" />
                    </div>
                    <div class="check-field">
                        <asp:CheckBox ID="chkNewGroupSystem" runat="server" Text="System group" />
                    </div>
                    <div class="new-group-actions">
                        <asp:Button ID="btnAddGroup" runat="server" Text="Add Group" CssClass="btn primary" CausesValidation="false" />
                        <asp:Button ID="btnCancelAddGroup" runat="server" Text="Cancel" CssClass="btn" CausesValidation="false" />
                    </div>
                </asp:Panel>
                <asp:Label ID="lblGroupNote" runat="server" CssClass="note" Visible="false" />
            </section>

            <%-- ===== 2. Tabs: Members | Actions ===== --%>
            <section class="card tabs-card">
                <asp:Menu ID="menuTabs" runat="server" CssClass="tabs" Orientation="Horizontal"
                    RenderingMode="List" IncludeStyleBlock="false">
                    <Items>
                        <asp:MenuItem Text="Members" Value="0" Selected="true" />
                        <asp:MenuItem Text="Role" Value="1" />
                        <asp:MenuItem Text="Actions" Value="2" />
                    </Items>
                </asp:Menu>

                <asp:MultiView ID="mvTabs" runat="server" ActiveViewIndex="0">

                    <%-- Members: the users in this group --%>
                    <asp:View ID="viewMembers" runat="server">
                        <div class="tab-body">
                            <div class="list-head">
                                <span class="list-title">Users in this group</span>
                                <div class="list-tools">
                                    <asp:Panel ID="pnlMemberTools" runat="server" CssClass="list-tools">
                                        <input type="text" class="text-input filter-input" placeholder="Filter users" aria-label="Filter users" oninput="filterMembers(this);" />
                                        <button type="button" class="link-btn" onclick="return setAllMembers(true);">Select all</button>
                                        <button type="button" class="link-btn" onclick="return setAllMembers(false);">Clear all</button>
                                    </asp:Panel>
                                    <asp:Button ID="btnSaveMembers" runat="server" Text="Save Members" CssClass="btn primary" CausesValidation="false" />
                                </div>
                            </div>
                            <asp:CheckBoxList ID="cblMembers" runat="server" CssClass="members-list" RepeatLayout="UnorderedList" />
                            <asp:Label ID="lblMembersNote" runat="server" CssClass="note" Visible="false" />
                        </div>
                    </asp:View>

                    <%-- Role: the group's one role (UNITSHUB_SEC_GROUPS_ROLES) --%>
                    <asp:View ID="viewRoles" runat="server">
                        <div class="tab-body">
                            <div class="list-head">
                                <span class="list-title">Role of this group</span>
                                <asp:Button ID="btnSaveRoles" runat="server" Text="Save Role" CssClass="btn primary" CausesValidation="false" />
                            </div>
                            <p class="tab-hint">A group has one role. Its members get every action that role allows, unless the Actions tab denies it.</p>
                            <asp:RadioButtonList ID="rblRoles" runat="server" CssClass="roles-list" RepeatLayout="UnorderedList" />
                            <asp:Label ID="lblRolesNote" runat="server" CssClass="note" Visible="false" />
                        </div>
                    </asp:View>

                    <%-- Actions: allow / deny for this group at a project + status --%>
                    <asp:View ID="viewActions" runat="server">
                        <div class="tab-body">
                            <div class="select-bar">
                                <div class="select-field">
                                    <asp:Label ID="lblProjectCaption" runat="server" Text="Project" AssociatedControlID="ddlProject" CssClass="select-label" />
                                    <asp:DropDownList ID="ddlProject" runat="server" CssClass="select-ddl" AutoPostBack="true" />
                                </div>
                                <div class="select-field">
                                    <asp:Label ID="lblStatusCaption" runat="server" Text="Status" AssociatedControlID="ddlStatus" CssClass="select-label" />
                                    <asp:DropDownList ID="ddlStatus" runat="server" CssClass="select-ddl" AutoPostBack="true" />
                                </div>
                                <asp:Panel ID="pnlSubStatus" runat="server" CssClass="select-field">
                                    <asp:Label ID="lblSubStatusCaption" runat="server" Text="Sub-status" AssociatedControlID="ddlSubStatus" CssClass="select-label" />
                                    <asp:DropDownList ID="ddlSubStatus" runat="server" CssClass="select-ddl" AutoPostBack="true" />
                                    <span class="select-hint">Narrows the actions listed; settings are saved per status.</span>
                                </asp:Panel>
                            </div>

                            <div class="list-head">
                                <span class="list-title">Actions at: <asp:Literal ID="litWhere" runat="server" /></span>
                                <asp:Button ID="btnSaveActions" runat="server" Text="Save Actions" CssClass="btn primary" CausesValidation="false" />
                            </div>

                            <asp:Panel ID="pnlActions" runat="server" CssClass="actions-wrap">
                                <table class="actions-table">
                                    <thead>
                                        <tr>
                                            <th>Action</th>
                                            <th>From the group's role</th>
                                            <th>Group setting</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        <asp:Repeater ID="rptActions" runat="server">
                                            <ItemTemplate>
                                                <tr>
                                                    <td class="a-name">
                                                        <asp:HiddenField ID="hfActionId" runat="server" Value='<%# Eval("ACTION_ID") %>' />
                                                        <asp:Literal ID="litActionTitle" runat="server" Text='<%# Server.HtmlEncode(Convert.ToString(Eval("ACTION_TITLE"))) %>' />
                                                    </td>
                                                    <td><asp:Literal ID="litRoleGives" runat="server" Text='<%# Eval("ROLE_HTML") %>' /></td>
                                                    <td>
                                                        <asp:RadioButtonList ID="rblSetting" runat="server" CssClass="setting"
                                                            RepeatDirection="Horizontal" RepeatLayout="Flow">
                                                            <asp:ListItem Text="Inherit" Value="R" />
                                                            <asp:ListItem Text="Allow" Value="A" />
                                                            <asp:ListItem Text="Deny" Value="D" />
                                                        </asp:RadioButtonList>
                                                    </td>
                                                </tr>
                                            </ItemTemplate>
                                        </asp:Repeater>
                                    </tbody>
                                </table>
                            </asp:Panel>
                            <asp:Label ID="lblActionsNote" runat="server" CssClass="note" Visible="false" />
                        </div>
                    </asp:View>

                </asp:MultiView>
            </section>

        </div>
    </form>
</body>
</html>
