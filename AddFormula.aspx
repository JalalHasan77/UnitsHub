<%@ Page Title="Add Formula" Language="VB" AutoEventWireup="false" CodeFile="AddFormula.aspx.vb" Inherits="AddFormula" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Add Formula</title>
    <style type="text/css">
        body {
            font-family: Segoe UI, Arial, sans-serif;
            background: #f3f4f6;
            color: #1f2937;
            margin: 0;
            padding: 24px;
        }
        .page {
            max-width: 600px;
            margin: 0 auto;
        }
        h1 {
            font-size: 22px;
            margin-bottom: 4px;
        }
        .subtitle {
            color: #6b7280;
            margin-bottom: 24px;
            font-size: 14px;
        }
        .card {
            background: #ffffff;
            border: 1px solid #e5e7eb;
            border-radius: 8px;
            padding: 24px;
        }
        label.formfield {
            display: block;
            font-size: 13px;
            font-weight: 600;
            color: #374151;
            margin-bottom: 6px;
        }
        .name-input {
            width: 100%;
            padding: 8px 10px;
            border: 1px solid #d1d5db;
            border-radius: 6px;
            margin-bottom: 6px;
            box-sizing: border-box;
            font-size: 14px;
        }
        .formula-box {
            width: 100%;
            padding: 10px;
            border: 1px solid #d1d5db;
            border-radius: 6px;
            font-family: Consolas, "Courier New", monospace;
            font-size: 14px;
            box-sizing: border-box;
            resize: vertical;
            margin-bottom: 6px;
        }
        .field-block {
            margin-bottom: 18px;
        }
        .field-error {
            color: #dc2626;
            font-size: 12px;
        }
        .actions {
            margin-top: 8px;
        }
        .btn {
            border: none;
            border-radius: 6px;
            padding: 9px 18px;
            font-size: 13px;
            font-weight: 600;
            cursor: pointer;
            transition: background-color 0.15s ease, transform 0.1s ease, box-shadow 0.15s ease;
        }
        .btn-primary {
            background: #4f46e5;
            color: #fff;
        }
        .btn-primary:hover {
            background: #4338ca;
            box-shadow: 0 1px 3px rgba(0, 0, 0, 0.15);
        }
        .btn-primary:active {
            transform: scale(0.97);
        }
        .btn-primary:focus {
            outline: none;
            box-shadow: 0 0 0 3px rgba(79, 70, 229, 0.25);
        }
        .validation-ok {
            display: inline-block;
            margin-top: 14px;
            color: #065f46;
            background: #d1fae5;
            border: 1px solid #a7f3d0;
            border-radius: 6px;
            padding: 8px 12px;
            font-size: 13px;
        }
        .validation-error {
            display: inline-block;
            margin-top: 14px;
            color: #991b1b;
            background: #fee2e2;
            border: 1px solid #fecaca;
            border-radius: 6px;
            padding: 8px 12px;
            font-size: 13px;
        }

        .pill-btn {
    border: 1px solid #d1d5db;
    border-radius: 5px;
    padding: 6px 14px;
    background: #fff;
    color: #374151;
    cursor: pointer;
    transition: background-color 0.15s ease, transform 0.1s ease, box-shadow 0.15s ease;
}

/* Mouse hovering over the button */
.pill-btn:hover {
    background: #eef2ff;
    border-color: #c7d2fe;
    color: #3730a3;
    box-shadow: 0 1px 3px rgba(0, 0, 0, 0.08);
}

/* The moment it's clicked / pressed down */
.pill-btn:active {
    background: #c7d2fe;
    transform: scale(0.97);
    box-shadow: inset 0 1px 2px rgba(0, 0, 0, 0.1);
}

/* Keyboard / tab focus — don't skip this, it matters for accessibility */
.pill-btn:focus {
    outline: none;
    box-shadow: 0 0 0 3px rgba(79, 70, 229, 0.25);
}
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <div class="page">
            <h1>Add Payment</h1>
            <div class="subtitle">Give the formula a title and enter the expression to save it.</div>

            <div class="card">
                <div class="field-block">
                    <label class="formfield">Title</label>
                    <asp:TextBox ID="txtTitle" runat="server" CssClass="name-input" MaxLength="200" placeholder="e.g. Standard Price With Tax" />
                    <asp:RequiredFieldValidator ID="valTitle" runat="server"
                        ControlToValidate="txtTitle"
                        ErrorMessage="Title is required."
                        CssClass="field-error"
                        Display="Dynamic" />
                </div>

                <div class="field-block">
                    
                </div>

                <div class="field-block">
                    <label class="formfield">Formula</label>
                    
                    <asp:Repeater ID="rptButtons" runat="server" OnItemCommand="rptButtons_ItemCommand">
                    <HeaderTemplate><div class="btn-row"></HeaderTemplate>
                    <ItemTemplate>
                        <asp:Button runat="server" CssClass="pill-btn"
                            Text='<%# Eval("Label") %>'
                            CommandName="Select"
                            CommandArgument='<%# Eval("Label") %>' />
                    </ItemTemplate>
                    <FooterTemplate></div></FooterTemplate>
                </asp:Repeater>
                    <asp:TextBox ID="txtFormulaText" runat="server" CssClass="formula-box" Rows="3" placeholder="e.g. [BasePrice] + ([BasePrice] * [TaxPercent] / 100)" />
                    <asp:RequiredFieldValidator ID="valFormula" runat="server"
                        ControlToValidate="txtFormulaText"
                        ErrorMessage="Formula is required."
                        CssClass="field-error"
                        Display="Dynamic" />
                </div>
                <div class="field-block">
                    <label class="formfield">Comments</label>
                    <asp:TextBox ID="txtComments" runat="server" CssClass="name-input" MaxLength="200" placeholder="e.g. this is final paymentt" />
<%--                    <asp:RequiredFieldValidator ID="RequiredFieldValidator1" runat="server"
                        ControlToValidate="txtTitle"
                        ErrorMessage="Title is required."
                        CssClass="field-error"
                        Display="Dynamic" />--%>
                </div>

                <div class="actions">
                    <asp:Button ID="btnInsert" runat="server" CssClass="btn btn-primary" Text="Save" />
                </div>

                <asp:Label ID="lblMessage" runat="server" />
            </div>
        </div>
    </form>
</body>
</html>
