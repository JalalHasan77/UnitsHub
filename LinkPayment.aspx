<%@ Page Language="VB" AutoEventWireup="false" CodeFile="LinkPayment.aspx.vb" Inherits="LinkPayment"   MaintainScrollPositionOnPostback="true" EnableEventValidation = false  %>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <link rel="stylesheet" href="styles.css" />

    <title></title>
    <style type="text/css">

        .bigCheckbox{
            width:30px;
            height:30px;

        }


        .diffBorderWidth {
            width: 100%;
            border-left-width:70px;
            border-right-width:20px;
            border-Top-width:10px;
            border-bottom-width:10px;
        }

        .MainContainer {
    width: 100%;
    border-collapse: collapse;
    border-style: solid;
    border-width: 0px;
}


        .nowrap {
            white-space: nowrap;
        }

        .itemCell, .itemCellSelecred, .itemCellSelecredBottmBorder {
            text-align: center;
            vertical-align: middle;
        }

        .itemCell a, .itemCellSelecred a, .itemCellSelecredBottmBorder a {
            display: block;
            text-align: center;
            line-height: 50px;
        }

        .full-width-menu {
            width: 100% !important;
            table-layout: fixed;
            border-collapse: collapse;
        }

        .itemCellSelecredBottmBorder,
        .itemCellSelecredBottmBorder a {
            border-bottom: none !important;
        }
        
        /*.Menu-rounded-corners {
            border: 3px solid darkgray;
            -webkit-border-radius: 8px;
            -moz-border-radius: 8px;
            border-radius: 8px;
            overflow: hidden;
            }*/
                
        .GarbageCell {
    border-collapse: collapse;
    border-style: solid;
    border-width: 0px;
   /* border-left-width: 2px;
    border-bottom-width: 0px;
    border-top-width: 0px;
    border-left-width: 1px;
    border-right-width: 0px;*/
    background-color: white;
}

        *,::after,::before{box-sizing:border-box}

        :root {
            --brand-1: #4f46e5;
            --brand-2: #3b82f6;
            --brand-soft: #eef2ff;
            --ink: #1e293b;
            --muted: #64748b;
            --border: #e2e8f0;
            --bg: #eef1f8;
            --success: #16a34a;
            --danger: #ef4444;
            --radius: 14px;
        }

        body {
            font-family: 'Inter', 'Segoe UI', Arial, Helvetica, sans-serif;
            margin: 0;
            padding: 0;
            color: var(--ink);
        }

        /* ---------- Tab-body content (matches ActionAddEdit form styling) ---------- */
        .tab-body-inner {
            padding: 22px 4px 6px;
        }

        .form-row {
            display: flex;
            flex-wrap: wrap;
            align-items: flex-start;
            gap: 6px 18px;
            margin-bottom: 18px;
        }
        .form-row:last-child { margin-bottom: 0; }

        .form-label {
            width: 170px;
            flex-shrink: 0;
            font-size: 13.5px;
            font-weight: 600;
            color: var(--ink);
            padding-top: 9px;
        }

        .form-control-cell { flex: 1; min-width: 220px; }
        .field-hint { font-size: 12px; color: var(--muted); margin-top: 6px; }

        /* ---------- Inputs ---------- */
        .txt-input, .ddl-input {
            width: 100%;
            max-width: 420px;
            padding: 10px 13px;
            border: 1.5px solid var(--border);
            border-radius: 10px;
            font-size: 14px;
            font-family: inherit;
            color: var(--ink);
            background-color: #f8fafc;
            transition: border-color .15s ease, box-shadow .15s ease, background-color .15s ease;
        }

        .txt-input::placeholder { color: #a0aec0; }

        .txt-input:focus, .ddl-input:focus {
            outline: none;
            border-color: var(--brand-2);
            background-color: #ffffff;
            box-shadow: 0 0 0 4px rgba(59, 130, 246, 0.14);
        }

        .ddl-input {
            appearance: none;
            -webkit-appearance: none;
            background-image: url("data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='16' height='16' fill='none' stroke='%2364748b' stroke-width='2'><path d='M4 6l4 4 4-4'/></svg>");
            background-repeat: no-repeat;
            background-position: right 12px center;
            padding-right: 34px;
            cursor: pointer;
        }

        /* ---------- Date field with inline calendar toggle ---------- */
        .date-input-group {
            display: flex;
            align-items: center;
            gap: 8px;
            position: relative;
        }

        .date-input-group .txt-input { max-width: 160px; }

        .cal-btn {
            display: inline-flex;
            align-items: center;
            gap: 6px;
            padding: 8px 15px;
            border-radius: 9px;
            background-color: #ffffff;
            border: 1.5px solid var(--border);
            font-size: 13px;
            font-weight: 600;
            color: var(--brand-1);
            cursor: pointer;
            transition: all .15s ease;
        }
        .cal-btn:hover { border-color: var(--brand-2); background-color: var(--brand-soft); }

        .age-display {
            font-size: 13.5px;
            font-weight: 600;
            color: var(--ink);
        }

        .calendar-popup {
            position: absolute;
            top: 44px;
            left: 0;
            z-index: 1000;
            background-color: #ffffff;
            border: 1.5px solid var(--border);
            border-radius: var(--radius);
            box-shadow: 0 14px 28px -10px rgba(30, 41, 59, 0.22);
            padding: 6px;
        }

        /* ---------- Paired fields on one row (e.g. Mobile 1 / Mobile 2) ---------- */
        .form-group-inline {
            display: flex;
            flex-wrap: wrap;
            align-items: flex-start;
            gap: 6px 14px;
            flex: 1;
            min-width: 220px;
        }

        .form-label-sm {
            width: 110px;
            flex-shrink: 0;
            font-size: 13.5px;
            font-weight: 600;
            color: var(--ink);
            padding-top: 9px;
        }

        .form-control-cell-sm { flex: 1; min-width: 120px; }

        /* ---------- Bordered section box with overlapping title (fieldset look) ---------- */
        .section-box {
            position: relative;
            border: 1.5px solid var(--border);
            border-radius: var(--radius);
            padding: 30px 20px 18px;
            margin-top: 14px;
            background-color: #fafbfc;
        }

        .section-title {
            position: absolute;
            top: -12px;
            left: 16px;
            background-color: #ffffff;
            padding: 0 8px;
            font-size: 14px;
            font-weight: 700;
            color: var(--ink);
        }

        /* ---------- Chip-styled label (e.g. "Email") ---------- */
        .chip-label {
            display: inline-flex;
            align-items: center;
            width: auto;
            padding: 8px 16px;
            border: 1.5px solid var(--brand-2);
            border-radius: 9px;
            color: var(--brand-1);
            font-size: 13.5px;
            font-weight: 600;
        }

        /* ---------- Section jump link (e.g. "Contact Details >>") ---------- */
        .section-link {
            display: inline-block;
            font-size: 14px;
            font-weight: 700;
            color: var(--brand-1);
            text-decoration: none;
            margin-top: 6px;
        }
        .section-link:hover { text-decoration: underline; color: var(--brand-2); }

        /* ---------- Buttons ---------- */
        .btn-row {
            display: flex;
            justify-content: flex-end;
            gap: 10px;
            margin: 12px -15px 0 -15px;
            padding: 20px 19px 18px;
            border-top: 1px solid #000000;
        }

        .btn-save, .btn-cancel {
            padding: 11px 26px;
            border-radius: 10px;
            font-size: 14px;
            font-weight: 600;
            font-family: inherit;
            border: none;
            cursor: pointer;
            transition: transform .12s ease, box-shadow .12s ease, background-color .12s ease;
        }

        .btn-save {
            background: linear-gradient(120deg, var(--brand-1), var(--brand-2));
            color: #ffffff;
            box-shadow: 0 10px 20px -8px rgba(79, 70, 229, 0.55);
        }
        .btn-save:hover { transform: translateY(-1px); box-shadow: 0 14px 24px -8px rgba(79, 70, 229, 0.6); }

        .btn-cancel {
            background-color: #f1f5f9;
            color: var(--muted);
            border: 1.5px solid var(--border);
        }
        .btn-cancel:hover { background-color: #e2e8f0; color: var(--ink); }

        .msg-success {
            display: block;
            margin: 0 4px 16px;
            padding: 10px 14px;
            border-radius: 10px;
            background-color: #ecfdf5;
            border: 1px solid #a7f3d0;
            color: #15803d;
            font-size: 13.5px;
            font-weight: 600;
        }

        .msg-success:empty { display: none; }

        @media (max-width: 560px) {
            .form-label { width: 100%; padding-top: 0; }
            .form-row { flex-direction: column; }
        }

        .top-strip {
            width: 100%;
            margin: 0;
            border: none;
            background: linear-gradient(120deg, #4f46e5, #3b82f6 75%);
            color: #ffffff;
            font-family: Arial, 'Segoe UI', sans-serif;
            font-size: 18px;
            font-weight: 700;
            padding: 14px 20px;
        }

        
                .tabs {
  display: flex;
}

.tab {
  flex: 1;
}

.tab input[type="radio"] {
  display: none;
}

.tab label {
  display: block;
  padding: 10px;
  background-color: #f0f0f0;
  cursor: pointer;
}

.tab-content {
  display: none;
  padding: 10px;
}

.tab input[type="radio"]:checked + label {
  background-color: #ccc;
}

.tab input[type="radio"]:checked ~ .tab-content {
  display: block;
}

#menu 
{
 width: 1024px;
 height: 25px;
 margin: 0 auto;
 text-align: right;
 background-color: Red;
}
.nestedTable {
    width: 100%;
    border-collapse: collapse;
    white-space: nowrap;
}

.nestedTable3 {
    width: 100%;
    border-collapse: collapse;
    white-space: nowrap;
}
.nestedTable2 {
    /*cellpadding: 25px;
    cell-spacing: 25px;*/
    /*border-width:unset 60px;*/
    width: 95%;
    /*max-width: 1000px;
    min-width: 900px;*/
    /*margin: 10px;*/
    border-collapse: collapse;
    margin: 5px auto;
}


#menuContainer{float: left;}


        .styled-dropdown {
        width: 250px;
        appearance: none;
        -webkit-appearance: none;
        -moz-appearance: none;
        font-size: 14px;
        padding: 8px 36px 8px 12px;
        border: 1px solid #ccc;
        border-radius: 5px;
        background-color: #ffffff;
        color: #333333;
        cursor: pointer;
        transition: border-color 0.2s ease, box-shadow 0.2s ease;

        /* custom dropdown arrow */
        background-image: url("data:image/svg+xml;charset=UTF-8,%3csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='none' stroke='%23666' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'%3e%3cpolyline points='6 9 12 15 18 9'%3e%3c/polyline%3e%3c/svg%3e");
        background-repeat: no-repeat;
        background-position: right 10px center;
        background-size: 16px;
    }

    
        </style>
    <script type="text/javascript">
        function changeHiddenFieldValue() {
            alert(document.getElementById("meow").value);
            //if (meow.value == true) {
            //    alert('true');

            //}
            //else {
            //    alert('false')

            //}

        }

        function hasClass(el, className) {
            return el && el.className && (' ' + el.className + ' ').indexOf(' ' + className + ' ') > -1;
        }

        function toggleCalendar(btn) {
            // Walk up to the containing .date-input-group, then find its .calendar-popup
            var group = btn;
            while (group && group.nodeType === 1 && !hasClass(group, 'date-input-group')) {
                group = group.parentNode;
            }
            if (!group) { return; }

            var candidates = group.getElementsByTagName('*');
            var pnl = null;
            for (var i = 0; i < candidates.length; i++) {
                if (hasClass(candidates[i], 'calendar-popup')) {
                    pnl = candidates[i];
                    break;
                }
            }
            if (!pnl) { return; }

            if (pnl.style.display === 'none' || pnl.style.display === '') {
                pnl.style.display = 'block';
            } else {
                pnl.style.display = 'none';
            }
        }
    </script>
</head>
<body>
    <div class="top-strip">Design Action</div>
    <form id="form1" runat="server">
        
        <table class="MainContainer"  cellpadding="0px" cellspacing="0px" >
            <tr>
                <td>
                    <div style="padding:10px">
<table cellpadding="0" class="nestedTable">
                                <tr>
                                    <td style="border-width: 0;">
                                        <table cellpadding="0" cellspacing="0" style="width:100%;border-width:0">
                                            <tr>
                                                <td style="width: 100%">
                                        <div style="width:100%;">
                                                <asp:DropDownList ID="DropDownList1" runat="server" CssClass="styled-dropdown" Font-Names="Arial" AutoPostBack="True" AppendDataBoundItems="True">
                                                </asp:DropDownList>
                                                <asp:CheckBox ID="chkShowUnlinkedOnly" runat="server" AutoPostBack="True" Checked="False" Font-Names="Arial" Text="Show only Unlinked" />
                                                <asp:Label ID="lblPROJID" runat="server"></asp:Label>
                                        </div>
                                     </td>
                                                <td style="width: 100%; border-bottom-style: solid; border-bottom-width: 0px; border-bottom-color: transparent; background-color:transparent">
                                                  <table cellpadding="0" cellspacing="0" style="height:50px;width:100%;border-width:0">
                                                      <tr><td style="width: 100%; border-bottom-style: solid; border-bottom-width: 1px; border-bottom-color: darkgray;">

                                                          </td></tr></table>

                                                </td>
                                            </tr>
                                        </table>




                                    </td>
                                </tr>
                                <!-- -->
                                <tr>
                                    <td>
                                        <table style="width:100%" cellpadding="0"  cellspacing="0">
                                            <tr>
                                                <td class="menuClass" style="background-color:white; padding: 0 15px 15px 15px; border-left: 1px solid black; border-right: 1px solid black; border-bottom: 1px solid black;">
                                                    <asp:GridView ID="GridView2" runat="server" AutoGenerateColumns="False" CellPadding="4" Font-Names="Arial" ForeColor="#333333" GridLines="Vertical" Width="100%">
                                                        <AlternatingRowStyle BackColor="White" />
                                                        <Columns>
                                                            <asp:TemplateField>
                                                                <ItemTemplate>
                                                                    &nbsp;<asp:LinkButton ID="LinkButton1" runat="server" OnClick="LinkButton1_Click">Link</asp:LinkButton>
                                                                </ItemTemplate>
                                                            </asp:TemplateField>
                                                            <asp:BoundField DataField="Date" HeaderText="Date" />
                                                            <asp:BoundField DataField="Transaction" HeaderText="Transaction" />
                                                            <asp:BoundField DataField="Amount" HeaderText="Amount" />
                                                            <asp:BoundField DataField="Comment 1" HeaderText="Comment 1" />
                                                            <asp:TemplateField HeaderText="Cust. Name">
                                                                <ItemTemplate>
                                                                    <asp:Label ID="lblNameAndCPR" runat="server"></asp:Label>
                                                <br />
                                                                    <asp:Label ID="lblUnit" runat="server"></asp:Label>
                                                                </ItemTemplate>
                                                            </asp:TemplateField>
                                                        </Columns>
                                                        <EditRowStyle BackColor="#2461BF" />
                                                        <FooterStyle BackColor="#507CD1" Font-Bold="True" ForeColor="White" />
                                                        <HeaderStyle BackColor="#507CD1" Font-Bold="True" ForeColor="White" />
                                                        <PagerStyle BackColor="#2461BF" ForeColor="White" HorizontalAlign="Center" />
                                                        <RowStyle BackColor="#EFF3FB" />
                                                        <SelectedRowStyle BackColor="#D1DDF1" Font-Bold="True" ForeColor="#333333" />
                                                        <SortedAscendingCellStyle BackColor="#F5F7FB" />
                                                        <SortedAscendingHeaderStyle BackColor="#6D95E1" />
                                                        <SortedDescendingCellStyle BackColor="#E9EBEF" />
                                                        <SortedDescendingHeaderStyle BackColor="#4870BE" />
                                                    </asp:GridView>
&nbsp;<div class="btn-row">
                                                        <asp:Button ID="btnLoad" runat="server" Text="Load" CssClass="btn-cancel" CausesValidation="false" OnClick="btnLoad_Click" />
                                                        <asp:Button ID="btnCancel" runat="server" Text="Cancel" CssClass="btn-cancel" CausesValidation="false" OnClick="btnCancel_Click" />
                                                        <asp:Button ID="btnSave" runat="server" Text="Save Action" CssClass="btn-save" OnClick="btnSave_Click" />
                                                    </div>

                                                    <asp:Label ID="lblMessage" runat="server" CssClass="msg-success" Visible="false" />

                                                </td>
                                            </tr>
                                        </table>
                                     </td>
                                </tr>
                            </table>
                        </div>
                </td>
            </tr>
        </table>
        
    </form>
</body>
</html>
