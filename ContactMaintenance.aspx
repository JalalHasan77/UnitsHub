<%@ Page Language="VB" AutoEventWireup="false" CodeFile="ContactMaintenance.aspx.vb" Inherits="ContactMaintenance"   MaintainScrollPositionOnPostback="true" EnableEventValidation = false  %>

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
            display: flex;
            align-items: center;
            justify-content: space-between;
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

        .close-btn:hover,
        .close-btn:focus-visible {
            background: rgba(255, 255, 255, 0.22);
            outline: none;
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
    <form id="form1" runat="server">
        <div class="top-strip">
            <span>Design Action</span>
            <asp:Label ID="lblID" runat="server" Text=""></asp:Label>
            <asp:Label ID="lblMode" runat="server" Text=""></asp:Label>
                        <asp:Label ID="lblIsDialogue" runat="server" Text=""></asp:Label>
            <asp:ImageButton ID="imgClose"
                runat="server"
                CssClass="close-btn"
                CausesValidation="false"
                ToolTip="Close"
                AlternateText="Close"
                ImageUrl="data:image/svg+xml;utf8,&lt;svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='none' stroke='white' stroke-width='2.5' stroke-linecap='round' stroke-linejoin='round'&gt;&lt;line x1='18' y1='6' x2='6' y2='18'/&gt;&lt;line x1='6' y1='6' x2='18' y2='18'/&gt;&lt;/svg&gt;" />
        </div>
        
        <table class="MainContainer"  cellpadding="0px" cellspacing="0px" >
            <tr>
                <td>
                       <div style="padding:10px">
                            <table cellpadding="0" class="nestedTable">
                                <tr>
                                    <td style="border-width: 0;">
                                            <asp:Label ID="lblPackageTitle" runat="server" Font-Names="Arial" Font-Size="20pt" ForeColor="Black" Text="[Package Title]" Font-Bold="True"></asp:Label>
                                        </td>
                                    </tr>
                            </table>
                        </div>
                
                
                </td>
            </tr>
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
                                            <asp:Menu ID="Menu3" runat="server" Font-Names="Arial" Height="50px" Orientation="Horizontal" Width="100%" CssClass="full-width-menu">
                                                <Items>
                                                    <asp:MenuItem Text="General" Value="General"></asp:MenuItem>
                                                    <asp:MenuItem Text="Contact Details" Value="Contact"></asp:MenuItem>
                                                    <asp:MenuItem Text="Address" Value="Address"></asp:MenuItem>
                                                    <asp:MenuItem Text="Documents" Value="Documents"></asp:MenuItem>
                                                </Items>
                                                <StaticHoverStyle BackColor="#000099" BorderColor="#000099" BorderWidth="1px" CssClass="itemCell" ForeColor="#F2F2F2" Height="50px" />
                                                <StaticMenuItemStyle BorderWidth="1px" CssClass="itemCell" Height="50px" HorizontalPadding="0px" ItemSpacing="0px" VerticalPadding="0px" BackColor="#F2F2F2"/>
                                                <StaticMenuStyle BorderWidth="0px" Height="50px" HorizontalPadding="0px" VerticalPadding="0px" Width="100%" />
                                                <StaticSelectedStyle BackColor="White" CssClass="itemCellSelecred itemCellSelecredBottmBorder" Height="50px" HorizontalPadding="0px" ItemSpacing="0px" VerticalPadding="0px" />
                                            </asp:Menu>
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
                                                    <asp:MultiView ID="MultiView3" runat="server" ActiveViewIndex="0">
                                                        <asp:View ID="View1" runat="server">
                                                            <asp:Panel ID="Panel1" runat="server" BackColor="White" Width="100%">
                                                                <div class="tab-body-inner">

                                                                    <div class="form-row">
                                                                        <div class="form-label">Full Name</div>
                                                                        <div class="form-control-cell">
                                                                            <asp:TextBox ID="txtFullName" runat="server" CssClass="txt-input" placeholder="e.g. Ahmed Ali" />
                                                                        </div>
                                                                    </div>

                                                                    <div class="form-row">
                                                                        <div class="form-label">Arabic Name</div>
                                                                        <div class="form-control-cell">
                                                                            <asp:TextBox ID="txtArabicName" runat="server" CssClass="txt-input" Style="direction:rtl;text-align:right;" />
                                                                        </div>
                                                                    </div>

                                                                    <div class="form-row">
                                                                        <div class="form-label">Gender</div>
                                                                        <div class="form-control-cell" style="max-width:220px;">
                                                                            <asp:DropDownList ID="ddlGender" runat="server" CssClass="ddl-input">
                                                                                <asp:ListItem Text="Male" Value="M"></asp:ListItem>
                                                                                <asp:ListItem Text="Female" Value="F"></asp:ListItem>
                                                                            </asp:DropDownList>
                                                                        </div>
                                                                    </div>

                                                                    <div class="form-row">
                                                                        <div class="form-group-inline">
                                                                            <div class="form-label">Date of Birth</div>
                                                                            <div class="form-control-cell">
                                                                                <div class="date-input-group">
                                                                                    <asp:TextBox ID="txtDOB" runat="server" CssClass="txt-input" ReadOnly="true" />
                                                                                    <asp:Button ID="btnToggleCalendar" runat="server" Text="Cal" CssClass="cal-btn" CausesValidation="false" OnClientClick="toggleCalendar(this); return false;" />
                                                                                    <asp:Panel ID="pnlCalendar" runat="server" CssClass="calendar-popup" Style="display:none;">
                                                                                        <asp:Calendar ID="calDOB" runat="server" OnSelectionChanged="calDOB_SelectionChanged" SelectionMode="Day"></asp:Calendar>
                                                                                    </asp:Panel>
                                                                                </div>
                                                                            </div>
                                                                        </div>
                                                                        <div class="form-group-inline" style="margin-left:40px;">
                                                                            <div class="form-label-sm">Age</div>
                                                                            <div class="form-control-cell-sm">
                                                                                <div class="date-input-group">
                                                                                    <asp:TextBox ID="txtAge" runat="server" CssClass="txt-input" Style="max-width:70px;" ReadOnly="true" />
                                                                                    <asp:Button ID="btnCalculateAge" runat="server" Text="Calculate" CssClass="cal-btn" CausesValidation="false" OnClick="btnCalculateAge_Click" />
                                                                                </div>
                                                                            </div>
                                                                        </div>
                                                                    </div>

                                                                    <div class="form-row">
                                                                        <div class="form-label">National ID/CPR</div>
                                                                        <div class="form-control-cell">
                                                                            <div class="date-input-group">
                                                                                <asp:TextBox ID="txtNationalID" runat="server" CssClass="txt-input" Style="max-width:150px;" MaxLength="9" />
                                                                                <asp:Button ID="btnCheckID" runat="server" Text="check" CssClass="cal-btn" CausesValidation="false" />
                                                                                <asp:Button ID="btnImportID" runat="server" Text="import" CssClass="cal-btn" CausesValidation="false" />
                                                                            </div>
                                                                        </div>
                                                                    </div>

                                                                    <div class="form-row">
                                                                        <div class="form-label">Nationality</div>
                                                                        <div class="form-control-cell" style="max-width:280px;">
                                                                            <asp:DropDownList ID="ddlNationality" runat="server" CssClass="ddl-input">
                                                                                <asp:ListItem Text="Bahraini" Value="Bahraini"></asp:ListItem>
                                                                            </asp:DropDownList>
                                                                        </div>
                                                                    </div>

                                                                    <div class="form-row">
                                                                        <div class="form-control-cell" style="margin-left:170px;">
                                                                            <asp:LinkButton ID="lnkContactDetails" runat="server" Text="Contact Details >>" CssClass="section-link" CausesValidation="false" OnClick="lnkContactDetails_Click" />
                                                                        </div>
                                                                    </div>

                                                                </div>
                                                            </asp:Panel>
                                                        </asp:View>
                                                        <asp:View ID="View2" runat="server">

                                                            <asp:Panel ID="Panel2" runat="server" BackColor="White" Width="100%">
                                                                <div class="tab-body-inner">
                                                                    <div class="section-box">
                                                                        <div class="section-title">Contact Details / Email</div>

                                                                        <div class="form-row">
                                                                            <div class="form-group-inline">
                                                                                <div class="form-label-sm">Mobile 1</div>
                                                                                <div class="form-control-cell-sm">
                                                                                    <asp:TextBox ID="txtMobile1" runat="server" CssClass="txt-input" />
                                                                                </div>
                                                                            </div>
                                                                            <div class="form-group-inline" style="margin-left:40px;">
                                                                                <div class="form-label-sm">Mobile 2</div>
                                                                                <div class="form-control-cell-sm">
                                                                                    <asp:TextBox ID="txtMobile2" runat="server" CssClass="txt-input" />
                                                                                </div>
                                                                            </div>
                                                                        </div>

                                                                        <div class="form-row">
                                                                            <div class="form-group-inline">
                                                                                <div class="form-label-sm">Home Phone</div>
                                                                                <div class="form-control-cell-sm">
                                                                                    <asp:TextBox ID="txtHomePhone" runat="server" CssClass="txt-input" />
                                                                                </div>
                                                                            </div>
                                                                            <div class="form-group-inline" style="margin-left:40px;">
                                                                                <div class="form-label-sm">Fax</div>
                                                                                <div class="form-control-cell-sm">
                                                                                    <asp:TextBox ID="txtFax" runat="server" CssClass="txt-input" />
                                                                                </div>
                                                                            </div>
                                                                        </div>

                                                                        <div class="form-row">
                                                                            <div class="form-label-sm">P.O.Box</div>
                                                                            <div class="form-control-cell-sm" style="max-width:100px;">
                                                                                <asp:TextBox ID="txtPOBox" runat="server" CssClass="txt-input" />
                                                                            </div>
                                                                        </div>

                                                                        <div class="form-row">
                                                                            <div class="chip-label">Email</div>
                                                                            <div class="form-control-cell">
                                                                                <asp:TextBox ID="txtEmail" runat="server" CssClass="txt-input" TextMode="Email" />
                                                                            </div>
                                                                        </div>

                                                                        <div class="form-row">
                                                                            <div class="form-group-inline">
                                                                                <div class="form-label-sm">Passport No.</div>
                                                                                <div class="form-control-cell-sm">
                                                                                    <asp:TextBox ID="txtPassportNo" runat="server" CssClass="txt-input" />
                                                                                </div>
                                                                            </div>
                                                                        </div>

                                                                        <div class="form-row">
                                                                            <div class="form-group-inline">
                                                                                <div class="form-label-sm">Issue Date</div>
                                                                                <div class="form-control-cell-sm">
                                                                                    <div class="date-input-group">
                                                                                        <asp:TextBox ID="txtIssueDate" runat="server" CssClass="txt-input" ReadOnly="true" />
                                                                                        <asp:Button ID="btnToggleCalendarIssue" runat="server" Text="Cal" CssClass="cal-btn" CausesValidation="false" OnClientClick="toggleCalendar(this); return false;" />
                                                                                        <asp:Panel ID="pnlCalendarIssue" runat="server" CssClass="calendar-popup" Style="display:none;">
                                                                                            <asp:Calendar ID="calIssueDate" runat="server" OnSelectionChanged="calIssueDate_SelectionChanged" SelectionMode="Day"></asp:Calendar>
                                                                                        </asp:Panel>
                                                                                    </div>
                                                                                </div>
                                                                            </div>
                                                                            <div class="form-group-inline" style="margin-left:40px;">
                                                                                <div class="form-label-sm">Expiry Date</div>
                                                                                <div class="form-control-cell-sm">
                                                                                    <div class="date-input-group">
                                                                                        <asp:TextBox ID="txtExpiryDate" runat="server" CssClass="txt-input" ReadOnly="true" />
                                                                                        <asp:Button ID="btnToggleCalendarExpiry" runat="server" Text="Cal" CssClass="cal-btn" CausesValidation="false" OnClientClick="toggleCalendar(this); return false;" />
                                                                                        <asp:Panel ID="pnlCalendarExpiry" runat="server" CssClass="calendar-popup" Style="display:none;">
                                                                                            <asp:Calendar ID="calExpiryDate" runat="server" OnSelectionChanged="calExpiryDate_SelectionChanged" SelectionMode="Day"></asp:Calendar>
                                                                                        </asp:Panel>
                                                                                    </div>
                                                                                </div>
                                                                            </div>
                                                                        </div>

                                                                    </div>
                                                                </div>
                                                            </asp:Panel>

                                                        </asp:View>
                                                        <asp:View ID="View3" runat="server">
                                                            <asp:Panel ID="Panel3" runat="server" BackColor="White" Width="100%">
                                                                <div class="tab-body-inner">
                                                                    <div class="section-box">
                                                                        <div class="section-title">Address Details</div>

                                                                        <div class="form-row">
                                                                            <div class="form-label">Country</div>
                                                                            <div class="form-control-cell" style="max-width:220px;">
                                                                                <asp:DropDownList ID="ddlCountry" runat="server" CssClass="ddl-input">
                                                                                    <asp:ListItem Text="Bahrain" Value="Bahrain"></asp:ListItem>
                                                                                </asp:DropDownList>
                                                                            </div>
                                                                        </div>

                                                                        <div class="form-row">
                                                                            <div class="form-group-inline">
                                                                                <div class="form-label-sm">Building #</div>
                                                                                <div class="form-control-cell-sm">
                                                                                    <asp:TextBox ID="txtBuilding" runat="server" CssClass="txt-input" />
                                                                                </div>
                                                                            </div>
                                                                            <div class="form-group-inline" style="margin-left:40px;">
                                                                                <div class="form-label-sm">Flat #</div>
                                                                                <div class="form-control-cell-sm">
                                                                                    <asp:TextBox ID="txtFlat" runat="server" CssClass="txt-input" />
                                                                                </div>
                                                                            </div>
                                                                        </div>

                                                                        <div class="form-row">
                                                                            <div class="form-group-inline">
                                                                                <div class="form-label-sm">Road</div>
                                                                                <div class="form-control-cell-sm">
                                                                                    <asp:TextBox ID="txtRoad" runat="server" CssClass="txt-input" />
                                                                                </div>
                                                                            </div>
                                                                            <div class="form-group-inline" style="margin-left:40px;">
                                                                                <div class="form-label-sm">Block</div>
                                                                                <div class="form-control-cell-sm">
                                                                                    <asp:TextBox ID="txtBlock" runat="server" CssClass="txt-input" />
                                                                                </div>
                                                                            </div>
                                                                        </div>

                                                                        <div class="form-row">
                                                                            <div class="form-label-sm">City</div>
                                                                            <div class="form-control-cell-sm">
                                                                                <asp:TextBox ID="txtCity" runat="server" CssClass="txt-input" />
                                                                            </div>
                                                                        </div>

                                                                    </div>
                                                                </div>
                                                            </asp:Panel>
                                                        </asp:View>
                                                        <asp:View ID="View4" runat="server">
                                                                <asp:Panel ID="Panel4" runat="server" BackColor="White" Height="300px" Width="100%">
                                                                View 3</asp:Panel>
                                                        </asp:View>
                                                    </asp:MultiView>

                                                    <div class="btn-row">
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
