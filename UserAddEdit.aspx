<%@ Page Language="VB" AutoEventWireup="false" CodeFile="UserAddEdit.aspx.vb" Inherits="UserAddEdit" MaintainScrollPositionOnPostback="true" EnableEventValidation="false" %>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <link rel="stylesheet" href="styles.css" />
    <title>Users</title>
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

        .page { max-width: 760px; margin: 0 auto; padding: 18px 16px 32px; display: flex; flex-direction: column; gap: 16px; }

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

        /* ---------- Fields ---------- */
        .pick-row { display: flex; flex-wrap: wrap; align-items: flex-end; gap: 12px; }
        .pick-row .field { flex: 1 1 280px; }

        .form-grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(240px, 1fr));
            gap: 14px 18px;
        }
        .field { display: flex; flex-direction: column; gap: 5px; min-width: 0; }
        .field.full { grid-column: 1 / -1; }
        .label { font-size: 13px; font-weight: 700; color: var(--navy); }
        .label .req { color: #b91c1c; margin-left: 2px; }
        .hint { font-size: 12px; color: var(--muted); }

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
        .text-input[readonly], .text-input:disabled, .text-input.aspNetDisabled { background: #f1f5f9; color: var(--muted); }

        .check-field { display: flex; align-items: center; gap: 8px; height: 38px; font-size: 14px; font-weight: 600; }
        .check-field input { width: 18px; height: 18px; accent-color: var(--navy); }

        .facts { display: flex; flex-wrap: wrap; gap: 8px 22px; font-size: 13px; color: var(--muted); }
        .facts strong { color: var(--ink); font-weight: 600; }

        /* ---------- Buttons ---------- */
        .actions { display: flex; flex-wrap: wrap; gap: 8px; justify-content: flex-end; margin-top: 18px; }
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

        .mode-tag {
            display: inline-block;
            margin-left: 8px;
            padding: 2px 9px;
            border-radius: 6px;
            font-size: 11px;
            font-weight: 700;
            letter-spacing: .04em;
            vertical-align: middle;
        }
        .mode-tag.new { background: #dcfce7; color: #166534; }
        .mode-tag.edit { background: #e0e7ff; color: #3730a3; }

        .note { display: block; margin-top: 12px; font-size: 13px; font-weight: 600; }
        .note.ok { color: #047857; }
        .note.info { color: var(--muted); font-weight: 400; }
        .note.err { color: #b91c1c; }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <div class="top-strip">
            <span>Users</span>
            <asp:ImageButton ID="imgClose"
                runat="server"
                CssClass="close-btn"
                CausesValidation="false"
                ToolTip="Close"
                AlternateText="Close"
                ImageUrl="data:image/svg+xml;utf8,&lt;svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='none' stroke='white' stroke-width='2.5' stroke-linecap='round' stroke-linejoin='round'&gt;&lt;line x1='18' y1='6' x2='6' y2='18'/&gt;&lt;line x1='6' y1='6' x2='18' y2='18'/&gt;&lt;/svg&gt;" />
        </div>

        <div class="page">

            <%-- ===== 1. Pick a user to edit, or start a new one ===== --%>
            <section class="card">
                <h2 class="card-title">User</h2>
                <div class="pick-row">
                    <div class="field">
                        <asp:Label ID="lblPickCaption" runat="server" Text="Edit an existing user" AssociatedControlID="ddlUser" CssClass="label" />
                        <asp:DropDownList ID="ddlUser" runat="server" CssClass="select-ddl" AutoPostBack="true" />
                    </div>
                    <asp:Button ID="btnNew" runat="server" Text="+ Add New User" CssClass="btn" CausesValidation="false" />
                </div>
            </section>

            <%-- ===== 2. The user's details ===== --%>
            <asp:Panel ID="pnlDetails" runat="server" CssClass="card" DefaultButton="btnSave">
                <h2 class="card-title">
                    <asp:Literal ID="litDetailsTitle" runat="server" Text="New user" />
                    <asp:Label ID="lblMode" runat="server" CssClass="mode-tag new" Text="NEW" />
                </h2>

                <div class="form-grid">
                    <div class="field">
                        <asp:Label ID="lblUserIdCaption" runat="server" AssociatedControlID="txtUserId" CssClass="label" Text="User ID" />
                        <asp:TextBox ID="txtUserId" runat="server" CssClass="text-input" MaxLength="50" placeholder="Leave empty for the next number" />
                        <asp:Label ID="lblUserIdHint" runat="server" CssClass="hint" Text="Used to sign in and in every rights table; it can't be changed later." />
                    </div>
                    <div class="field">
                        <asp:Label ID="lblFullNameCaption" runat="server" AssociatedControlID="txtFullName" CssClass="label">Full name<span class="req">*</span></asp:Label>
                        <asp:TextBox ID="txtFullName" runat="server" CssClass="text-input" MaxLength="200" placeholder="e.g. Ahmed Ali" />
                    </div>
                    <div class="field full">
                        <asp:Label ID="lblEmailCaption" runat="server" AssociatedControlID="txtEmail" CssClass="label" Text="Email" />
                        <asp:TextBox ID="txtEmail" runat="server" CssClass="text-input" MaxLength="200" TextMode="Email" placeholder="name@company.com" />
                    </div>
                    <div class="field full">
                        <div class="check-field">
                            <asp:CheckBox ID="chkEnabled" runat="server" Text="Account enabled" Checked="true" />
                        </div>
                        <span class="hint">A disabled user keeps their groups and rights but sees no actions.</span>
                    </div>
                </div>

                <asp:Panel ID="pnlFacts" runat="server" CssClass="facts" Visible="false" style="margin-top:14px">
                    <span>Created on: <strong><asp:Literal ID="litCreatedOn" runat="server" /></strong></span>
                    <span>Last login: <strong><asp:Literal ID="litLastLogin" runat="server" /></strong></span>
                </asp:Panel>

                <div class="actions">
                    <asp:Button ID="btnCancel" runat="server" Text="Discard changes" CssClass="btn" CausesValidation="false" />
                    <asp:Button ID="btnSave" runat="server" Text="Add User" CssClass="btn primary" CausesValidation="false" />
                </div>
                <asp:Label ID="lblNote" runat="server" CssClass="note" Visible="false" />
            </asp:Panel>

        </div>
    </form>
</body>
</html>
