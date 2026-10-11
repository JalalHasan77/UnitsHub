<%@ Page Language="VB" AutoEventWireup="false" CodeFile="ManageUsers.aspx.vb" Inherits="ManageUsers" MaintainScrollPositionOnPostback="true" EnableEventValidation="false" %>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <link rel="stylesheet" href="styles.css" />
    <title>Manage Users</title>
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

        .user-row { display: flex; flex-wrap: wrap; align-items: flex-end; gap: 12px; }
        .user-row .select-field { flex: 1 1 280px; }
        .user-info { display: block; margin-top: 10px; font-size: 13px; color: var(--muted); line-height: 1.6; }
        .user-info strong { color: var(--ink); }
        .user-info .off { color: #b91c1c; font-weight: 700; }

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

        /* ---------- Tabs: Groups | Actions ---------- */
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
        .tab-hint { margin: 0 0 12px 0; font-size: 13px; color: var(--muted); }

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

        /* ---------- Groups list ---------- */
        .groups-list {
            list-style: none;
            margin: 0;
            padding: 0;
            display: flex;
            flex-direction: column;   /* one group per line */
            gap: 8px;
            max-height: 420px;
            overflow-y: auto;
        }
        .groups-list label { cursor: pointer; font-size: 14px; font-weight: 600; }
        .groups-list input[type="checkbox"] { width: 17px; height: 17px; margin-right: 8px; vertical-align: middle; accent-color: var(--navy); }
        .groups-list .aspNetDisabled { color: var(--muted); }
        .groups-list .g-tag {
            display: inline-block;
            margin-left: 6px;
            padding: 1px 7px;
            border-radius: 6px;
            background: #e0e7ff;
            color: #3730a3;
            font-size: 11px;
            font-weight: 700;
            vertical-align: middle;
        }
        .groups-list .g-desc { display: block; margin-left: 25px; color: var(--muted); font-size: 12.5px; font-weight: 400; }

        /* ---------- Actions table ---------- */
        .actions-wrap { overflow-x: auto; border: 1px solid var(--border); border-radius: 9px; }
        .actions-table { width: 100%; min-width: 680px; border-collapse: collapse; font-size: 14px; }
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
        .via-yes { display: block; color: #047857; font-size: 13px; }
        .via-deny { display: block; color: #b91c1c; font-size: 13px; }
        .via-no { display: block; color: var(--muted); font-size: 13px; }
        .result { display: inline-block; padding: 2px 9px; border-radius: 6px; font-size: 12px; font-weight: 700; }
        .result.yes { background: #d1fae5; color: #065f46; }
        .result.no { background: #f1f5f9; color: var(--muted); }

        /* Inherit / Allow / Deny switch */
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
        .setting label + input + label { border-left: 1.5px solid #cbd5e1; }
        .setting input[type="radio"]:focus-visible + label { outline: 2px solid var(--navy); outline-offset: -2px; }
        .setting input[value="R"]:checked + label { background: #e2e8f0; color: var(--ink); }
        .setting input[value="A"]:checked + label { background: #047857; color: #ffffff; }
        .setting input[value="D"]:checked + label { background: #b91c1c; color: #ffffff; }
        .setting.aspNetDisabled label, .setting input:disabled + label { opacity: .5; cursor: not-allowed; }

        .note { display: block; margin-top: 12px; font-size: 13px; font-weight: 600; }
        .note.ok { color: #047857; }
        .note.info { color: var(--muted); font-weight: 400; }
        .note.err { color: #b91c1c; }
    </style>
    <script type="text/javascript">
        // Groups: "Select all" / "Clear all" act on the groups currently shown by the filter
        function setAllGroups(checked) {
            var list = document.querySelector('.groups-list');
            if (!list) { return false; }
            var items = list.getElementsByTagName('li');
            for (var i = 0; i < items.length; i++) {
                if (items[i].style.display === 'none') { continue; }
                var box = items[i].querySelector('input[type="checkbox"]');
                if (box && !box.disabled) { box.checked = checked; }
            }
            return false;
        }

        // Groups: show only the groups whose text contains the typed text
        function filterGroups(input) {
            var list = document.querySelector('.groups-list');
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
            <span>Manage Users</span>
            <asp:ImageButton ID="imgClose"
                runat="server"
                CssClass="close-btn"
                CausesValidation="false"
                ToolTip="Close"
                AlternateText="Close"
                ImageUrl="data:image/svg+xml;utf8,&lt;svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='none' stroke='white' stroke-width='2.5' stroke-linecap='round' stroke-linejoin='round'&gt;&lt;line x1='18' y1='6' x2='6' y2='18'/&gt;&lt;line x1='6' y1='6' x2='18' y2='18'/&gt;&lt;/svg&gt;" />
        </div>

        <div class="page">

            <%-- ===== 1. User ===== --%>
            <section class="card">
                <h2 class="card-title">User</h2>
                <div class="user-row">
                    <div class="select-field">
                        <asp:Label ID="lblUserCaption" runat="server" Text="User" AssociatedControlID="ddlUser" CssClass="select-label" />
                        <asp:DropDownList ID="ddlUser" runat="server" CssClass="select-ddl" AutoPostBack="true" />
                    </div>
                </div>
                <asp:Label ID="lblUserInfo" runat="server" CssClass="user-info" Visible="false" />
                <asp:Label ID="lblUserNote" runat="server" CssClass="note" Visible="false" />
            </section>

            <%-- ===== 2. Tabs: Groups | Actions ===== --%>
            <section class="card tabs-card">
                <asp:Menu ID="menuTabs" runat="server" CssClass="tabs" Orientation="Horizontal"
                    RenderingMode="List" IncludeStyleBlock="false">
                    <Items>
                        <asp:MenuItem Text="Groups" Value="0" Selected="true" />
                        <asp:MenuItem Text="Actions" Value="1" />
                    </Items>
                </asp:Menu>

                <asp:MultiView ID="mvTabs" runat="server" ActiveViewIndex="0">

                    <%-- Groups: the groups this user belongs to (UNITSHUB_SEC_USER_GROUPS) --%>
                    <asp:View ID="viewGroups" runat="server">
                        <div class="tab-body">
                            <div class="list-head">
                                <span class="list-title">Groups this user belongs to</span>
                                <div class="list-tools">
                                    <asp:Panel ID="pnlGroupTools" runat="server" CssClass="list-tools">
                                        <input type="text" class="text-input filter-input" placeholder="Filter groups" aria-label="Filter groups" oninput="filterGroups(this);" />
                                        <button type="button" class="link-btn" onclick="return setAllGroups(true);">Select all</button>
                                        <button type="button" class="link-btn" onclick="return setAllGroups(false);">Clear all</button>
                                    </asp:Panel>
                                    <asp:Button ID="btnSaveGroups" runat="server" Text="Save Groups" CssClass="btn primary" CausesValidation="false" />
                                </div>
                            </div>
                            <p class="tab-hint">The user gets the actions of every ticked group (through its role or its own Allow), unless a group or the Actions tab denies them.</p>
                            <asp:CheckBoxList ID="cblGroups" runat="server" CssClass="groups-list" RepeatLayout="UnorderedList" />
                            <asp:Label ID="lblGroupsNote" runat="server" CssClass="note" Visible="false" />
                        </div>
                    </asp:View>

                    <%-- Actions: allow / deny for this user at a project + status --%>
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
                                            <th>From the user's groups</th>
                                            <th>User setting</th>
                                            <th>Result (saved)</th>
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
                                                    <td><asp:Literal ID="litGroupsGive" runat="server" Text='<%# Eval("GROUPS_HTML") %>' /></td>
                                                    <td>
                                                        <asp:RadioButtonList ID="rblSetting" runat="server" CssClass="setting"
                                                            RepeatDirection="Horizontal" RepeatLayout="Flow">
                                                            <asp:ListItem Text="Inherit" Value="R" />
                                                            <asp:ListItem Text="Allow" Value="A" />
                                                            <asp:ListItem Text="Deny" Value="D" />
                                                        </asp:RadioButtonList>
                                                    </td>
                                                    <td><asp:Literal ID="litResult" runat="server" Text='<%# Eval("RESULT_HTML") %>' /></td>
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
