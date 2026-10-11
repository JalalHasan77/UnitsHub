<%@ Page Language="VB" AutoEventWireup="false" CodeFile="ManageRoles.aspx.vb" Inherits="ManageRoles" MaintainScrollPositionOnPostback="true" EnableEventValidation="false" %>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <link rel="stylesheet" href="styles.css" />
    <title>Manage Roles</title>
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

        /* ---------- Dropdowns ---------- */
        .select-bar {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
            gap: 12px 16px;
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

        /* Role row: dropdown and "+ Add New Role" side by side */
        .role-row { display: flex; flex-wrap: wrap; align-items: flex-end; gap: 12px; }
        .role-row .select-field { flex: 1 1 280px; }

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

        /* ---------- New role panel ---------- */
        .new-role {
            margin-top: 14px;
            padding: 14px;
            border: 1px dashed #a5b4fc;
            border-radius: 10px;
            background: #eef2ff;
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(240px, 1fr));
            gap: 12px 16px;
            align-items: end;
        }
        .new-role-actions { display: flex; gap: 8px; }

        /* ---------- Actions list ---------- */
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

        .actions-list {
            list-style: none;
            margin: 0;
            padding: 0;
            display: flex;
            flex-direction: column;   /* one action per line */
            gap: 6px;
        }
        .actions-list label { cursor: pointer; font-size: 14px; }
        .actions-list input[type="checkbox"] { width: 17px; height: 17px; margin-right: 8px; vertical-align: middle; accent-color: var(--navy); }
        .actions-list .aspNetDisabled { color: var(--muted); }

        .note { display: block; margin-top: 12px; font-size: 13px; font-weight: 600; }
        .note.ok { color: #047857; }
        .note.info { color: var(--muted); font-weight: 400; }
        .note.err { color: #b91c1c; }
    </style>
    <script type="text/javascript">
        // "Select all" / "Clear all" for the actions list (client side only; Save stores them)
        function setAllActions(checked) {
            var list = document.querySelector('.actions-list');
            if (!list) { return false; }
            var boxes = list.getElementsByTagName('input');
            for (var i = 0; i < boxes.length; i++) {
                if (boxes[i].type === 'checkbox' && !boxes[i].disabled) { boxes[i].checked = checked; }
            }
            return false;
        }
    </script>
</head>
<body>
    <form id="form1" runat="server">
        <div class="top-strip">
            <span>Manage Roles</span>
            <asp:ImageButton ID="imgClose"
                runat="server"
                CssClass="close-btn"
                CausesValidation="false"
                ToolTip="Close"
                AlternateText="Close"
                ImageUrl="data:image/svg+xml;utf8,&lt;svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='none' stroke='white' stroke-width='2.5' stroke-linecap='round' stroke-linejoin='round'&gt;&lt;line x1='18' y1='6' x2='6' y2='18'/&gt;&lt;line x1='6' y1='6' x2='18' y2='18'/&gt;&lt;/svg&gt;" />
        </div>

        <div class="page">

            <%-- ===== 1. Role: pick one, or add a new one ===== --%>
            <section class="card">
                <h2 class="card-title">Role</h2>
                <div class="role-row">
                    <div class="select-field">
                        <asp:Label ID="lblRoleCaption" runat="server" Text="Role" AssociatedControlID="ddlRole" CssClass="select-label" />
                        <asp:DropDownList ID="ddlRole" runat="server" CssClass="select-ddl" AutoPostBack="true" />
                    </div>
                    <asp:Button ID="btnShowAddRole" runat="server" Text="+ Add New Role" CssClass="btn" CausesValidation="false" />
                </div>

                <asp:Panel ID="pnlNewRole" runat="server" CssClass="new-role" Visible="false" DefaultButton="btnAddRole">
                    <div class="select-field">
                        <asp:Label ID="lblNewRoleName" runat="server" Text="Role name" AssociatedControlID="txtNewRoleName" CssClass="select-label" />
                        <asp:TextBox ID="txtNewRoleName" runat="server" CssClass="text-input" MaxLength="100" placeholder="e.g. Finance-Manager" />
                    </div>
                    <div class="select-field">
                        <asp:Label ID="lblNewRoleDesc" runat="server" Text="Description" AssociatedControlID="txtNewRoleDesc" CssClass="select-label" />
                        <asp:TextBox ID="txtNewRoleDesc" runat="server" CssClass="text-input" MaxLength="400" placeholder="What this role is for" />
                    </div>
                    <div class="new-role-actions">
                        <asp:Button ID="btnAddRole" runat="server" Text="Add Role" CssClass="btn primary" CausesValidation="false" />
                        <asp:Button ID="btnCancelAddRole" runat="server" Text="Cancel" CssClass="btn" CausesValidation="false" />
                    </div>
                </asp:Panel>
                <asp:Label ID="lblRoleNote" runat="server" CssClass="note" Visible="false" />
            </section>

            <%-- ===== 2. Where: Project -> Status -> Sub-status ===== --%>
            <section class="card">
                <h2 class="card-title">Where</h2>
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
                        <span class="select-hint">Narrows the actions listed; permissions are saved per status.</span>
                    </asp:Panel>
                </div>
            </section>

            <%-- ===== 3. Actions this role may perform here ===== --%>
            <section class="card">
                <div class="list-head">
                    <span class="list-title">Allowed actions: <asp:Literal ID="litWhere" runat="server" /></span>
                    <div class="list-tools">
                        <asp:Panel ID="pnlSelectTools" runat="server" CssClass="list-tools">
                            <button type="button" class="link-btn" onclick="return setAllActions(true);">Select all</button>
                            <button type="button" class="link-btn" onclick="return setAllActions(false);">Clear all</button>
                        </asp:Panel>
                        <asp:Button ID="btnSaveActions" runat="server" Text="Save" CssClass="btn primary" CausesValidation="false" />
                    </div>
                </div>
                <asp:CheckBoxList ID="cblActions" runat="server" CssClass="actions-list" RepeatLayout="UnorderedList" />
                <asp:Label ID="lblActionsNote" runat="server" CssClass="note" Visible="false" />
            </section>

        </div>
    </form>
</body>
</html>
