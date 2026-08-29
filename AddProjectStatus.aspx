<%@ Page Language="VB" AutoEventWireup="false" CodeFile="AddProjectStatus.aspx.vb" Inherits="AddProjectStatus" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Add Project Status</title>
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
            box-shadow: 0 2px 10px rgba(55, 48, 163, 0.25);
        }

        .page-title {
            font-size: 24px;
            font-weight: 700;
            color: #ffffff;
            letter-spacing: 0.3px;
            margin: 0;
        }

        .page-subtitle {
            font-size: 13.5px;
            color: #e0e7ff;
            margin-top: 4px;
        }

        .content-wrapper {
            max-width: 900px;
            margin: 28px auto 48px auto;
            padding: 0 20px;
        }

        .content-card {
            background-color: #ffffff;
            border: 1px solid var(--border);
            border-radius: 12px;
            box-shadow: 0 1px 4px rgba(30, 41, 59, 0.06);
            padding: 24px 28px;
            margin-bottom: 20px;
        }

        .section-title {
            font-size: 15px;
            font-weight: 700;
            color: var(--ink);
            margin: 0 0 16px 0;
            padding-bottom: 10px;
            border-bottom: 1px solid var(--border);
        }

        .form-row {
            display: flex;
            flex-wrap: wrap;
            align-items: center;
            gap: 14px;
            padding: 10px 0;
        }

        .form-label {
            flex: 0 0 200px;
            font-size: 13.5px;
            font-weight: 600;
            color: var(--muted);
        }

        .form-control-cell {
            flex: 1;
            min-width: 220px;
        }

        .txt-input, .ddl-input {
            width: 100%;
            padding: 9px 12px;
            border: 1.5px solid var(--border);
            border-radius: 8px;
            font-size: 14px;
            font-family: 'Inter', 'Segoe UI', Arial, sans-serif;
            color: var(--ink);
            background-color: #ffffff;
        }

        .txt-input:focus, .ddl-input:focus {
            outline: none;
            border-color: var(--brand-1);
            box-shadow: 0 0 0 3px rgba(79, 70, 229, 0.12);
        }

        .color-field {
            display: flex;
            align-items: center;
            gap: 10px;
        }

        .color-field input[type="color"] {
            width: 48px;
            height: 38px;
            padding: 2px;
            border: 1.5px solid var(--border);
            border-radius: 8px;
            cursor: pointer;
            background: none;
        }

        .color-hex {
            font-family: 'Consolas', 'Courier New', monospace;
            font-size: 13.5px;
            color: var(--muted);
            min-width: 70px;
        }

        .required-mark {
            color: #dc2626;
            margin-left: 3px;
        }

        .btn-row {
            display: flex;
            justify-content: flex-end;
            gap: 12px;
            margin-top: 4px;
        }

        .btn-save, .btn-cancel {
            padding: 10px 22px;
            border-radius: 8px;
            font-size: 14px;
            font-weight: 600;
            cursor: pointer;
            border: none;
        }

        .btn-save {
            background-color: var(--brand-1);
            color: #ffffff;
        }

        .btn-save:hover { background-color: #4338ca; }

        .btn-cancel {
            background-color: #ffffff;
            color: var(--ink);
            border: 1.5px solid var(--border);
        }

        .btn-cancel:hover { border-color: #94a3b8; }

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

        .msg-error {
            display: block;
            margin-top: 14px;
            padding: 10px 14px;
            border-radius: 8px;
            background-color: #fef2f2;
            color: #b91c1c;
            border: 1px solid #fecaca;
            font-size: 13.5px;
        }

        /* --- Live preview, matching the status-box / subtitle-box look used on
             ProjectStatusAndAction.aspx --- */
        .preview-shell {
            background-color: #1a1464;
            padding: 20px;
            border-radius: 8px;
            display: flex;
            justify-content: flex-start;
        }

        .preview-status-box {
            display: inline-block;
            box-sizing: border-box;
            width: 260px;
            max-width: 100%;
            padding: 14px 20px;
            border-radius: 12px;
            box-shadow: 0 2px 5px rgba(0, 0, 0, 0.25);
            text-align: center;
            background-color: #ffffff;
            color: #1e293b;
        }

        .preview-status-title {
            font-size: 18px;
            font-weight: 700;
            text-decoration: underline;
            display: block;
            word-wrap: break-word;
        }

        .preview-subtitle-box {
            display: inline-block;
            margin-top: 10px;
            padding: 6px 14px;
            border-radius: 8px;
            font-size: 13px;
            font-weight: 500;
            box-shadow: 0 1px 3px rgba(0, 0, 0, 0.15);
        }

    </style>
</head>
<body>
    <form id="form1" runat="server">
        <div class="page-header">
            <h1 class="page-title">Add Project Status</h1>
            <div class="page-subtitle">Create a new status entry for a project's workflow</div>
            <div class="page-subtitle">
                ProjectID: <asp:Label ID="lblProjectID" runat="server" Text="" />
                &nbsp;|&nbsp; StatusID: <asp:Label ID="lblStatusID" runat="server" Text="" />
                &nbsp;|&nbsp; Mode: <asp:Label ID="lblMode" runat="server" Text="" />
                &nbsp;|&nbsp; Dir: <asp:Label ID="lblDir" runat="server" Text="" />
            </div>
        </div>

        <div class="content-wrapper">

            <div class="content-card">
                <div class="section-title">Details</div>

                <div class="form-row">
                    <div class="form-label">Project<span class="required-mark">*</span></div>
                    <div class="form-control-cell">
                        <asp:DropDownList ID="ddlProjectName" runat="server" CssClass="ddl-input" />
                        <asp:RequiredFieldValidator ID="rfvProject" runat="server" ControlToValidate="ddlProjectName"
                            InitialValue="" ErrorMessage="Project is required." Display="Dynamic" ForeColor="#dc2626" Font-Size="12.5px" />
                    </div>
                </div>

                <div class="form-row">
                    <div class="form-label">Status Title<span class="required-mark">*</span></div>
                    <div class="form-control-cell">
                        <asp:TextBox ID="txtStatusTitle" runat="server" CssClass="txt-input" placeholder="e.g. In Repayment" onkeyup="updatePreview();" onchange="updatePreview();" />
                        <asp:RequiredFieldValidator ID="rfvStatusTitle" runat="server" ControlToValidate="txtStatusTitle"
                            ErrorMessage="Status Title is required." Display="Dynamic" ForeColor="#dc2626" Font-Size="12.5px" />
                    </div>
                </div>

                <div class="form-row">
                    <div class="form-label">Status Subtitle</div>
                    <div class="form-control-cell">
                        <asp:TextBox ID="txtStatusSubtitle" runat="server" CssClass="txt-input" placeholder="e.g. Waiting for TR Manager Approval" onkeyup="updatePreview();" onchange="updatePreview();" />
                    </div>
                </div>
            </div>

            <div class="content-card">
                <div class="section-title">Colors</div>

                <div class="form-row">
                    <div class="form-label">Status Background Color</div>
                    <div class="form-control-cell">
                        <div class="color-field">
                            <input type="color" id="colorStatusBg" runat="server" value="#4f46e5" onchange="updatePreview();" oninput="updatePreview();" />
                            <span class="color-hex" id="hexStatusBg">#4f46e5</span>
                        </div>
                    </div>
                </div>

                <div class="form-row">
                    <div class="form-label">Status Font Color</div>
                    <div class="form-control-cell">
                        <div class="color-field">
                            <input type="color" id="colorStatusFg" runat="server" value="#ffffff" onchange="updatePreview();" oninput="updatePreview();" />
                            <span class="color-hex" id="hexStatusFg">#ffffff</span>
                        </div>
                    </div>
                </div>

                <div class="form-row">
                    <div class="form-label">Subtitle Background Color</div>
                    <div class="form-control-cell">
                        <div class="color-field">
                            <input type="color" id="colorSubtitleBg" runat="server" value="#ffffff" onchange="updatePreview();" oninput="updatePreview();" />
                            <span class="color-hex" id="hexSubtitleBg">#ffffff</span>
                        </div>
                    </div>
                </div>

                <div class="form-row">
                    <div class="form-label">Subtitle Font Color</div>
                    <div class="form-control-cell">
                        <div class="color-field">
                            <input type="color" id="colorSubtitleFg" runat="server" value="#1e293b" onchange="updatePreview();" oninput="updatePreview();" />
                            <span class="color-hex" id="hexSubtitleFg">#1e293b</span>
                        </div>
                    </div>
                </div>
            </div>

            <div class="content-card">
                <div class="section-title">Preview</div>
                <div class="preview-shell">
                    <div class="preview-status-box" id="previewStatusBox">
                        <span class="preview-status-title" id="previewStatusTitle">Status Title</span>
                        <div class="preview-subtitle-box" id="previewSubtitleBox">Status Subtitle</div>
                    </div>
                </div>
            </div>

            <div class="content-card">
                <div class="btn-row">
                    <asp:Button ID="btnCancel" runat="server" Text="Cancel" CssClass="btn-cancel" CausesValidation="false" OnClick="btnCancel_Click" />
                    <asp:Button ID="btnSave" runat="server" Text="Save Status" CssClass="btn-save" OnClick="btnSave_Click" />
                </div>
                <asp:Label ID="lblMessage" runat="server" CssClass="msg-success" />
            </div>

        </div>

        <script type="text/javascript">
            function updatePreview() {
                var title = document.getElementById('<%= txtStatusTitle.ClientID %>').value || 'Status Title';
                var subtitle = document.getElementById('<%= txtStatusSubtitle.ClientID %>').value;

                var statusBg = document.getElementById('colorStatusBg').value;
                var statusFg = document.getElementById('colorStatusFg').value;
                var subtitleBg = document.getElementById('colorSubtitleBg').value;
                var subtitleFg = document.getElementById('colorSubtitleFg').value;

                document.getElementById('hexStatusBg').textContent = statusBg;
                document.getElementById('hexStatusFg').textContent = statusFg;
                document.getElementById('hexSubtitleBg').textContent = subtitleBg;
                document.getElementById('hexSubtitleFg').textContent = subtitleFg;

                var box = document.getElementById('previewStatusBox');
                box.style.backgroundColor = statusBg;
                box.style.color = statusFg;

                document.getElementById('previewStatusTitle').textContent = title;

                var subtitleBox = document.getElementById('previewSubtitleBox');
                if (subtitle && subtitle.length > 0) {
                    subtitleBox.style.display = 'inline-block';
                    subtitleBox.style.backgroundColor = subtitleBg;
                    subtitleBox.style.color = subtitleFg;
                    subtitleBox.textContent = subtitle;
                } else {
                    subtitleBox.style.display = 'none';
                }
            }

            if (document.addEventListener) {
                document.addEventListener('DOMContentLoaded', updatePreview);
            }
        </script>
    </form>
</body>
</html>
