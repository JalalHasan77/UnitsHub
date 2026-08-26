<%@ Page Language="VB" AutoEventWireup="true" Codefile="MainPage.aspx.vb" Inherits="MainPage"%>
<%@ Register assembly="ServerControl1" namespace="ServerControl1" tagprefix="cc1" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
    <style type="text/css">
        .auto-style2 {
            width: 100%;
            border-collapse: collapse;
        }
        .auto-style3 {
            width: 90%;
            border-collapse: collapse;
            table-layout: fixed;
        }
    .unit-filter {
        display: flex;
        flex-direction: column;
        gap: 6px;
        max-width: 320px;
        margin-bottom: 16px;
    }

    .unit-filter-label label {
        font-weight: 600;
        font-size: 14px;
        color: #333333;
    }

    .unit-filter-control {
        position: relative;
    }

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

    .styled-dropdown:hover {
        border-color: #999;
    }

    .styled-dropdown:focus {
        outline: none;
        border-color: #4a90d9;
        box-shadow: 0 0 0 3px rgba(74, 144, 217, 0.2);
    }

        .field-badge {
         display: inline-block;
        padding: 4px 10px;
        margin: 3px 0px 0px 0px;
        border-radius: 0px;
        border: 1.5px solid;
        font-size: 14px;
        font-family: 'Arial'; /*,'Franklin Gothic Medium', 'Arial Narrow',  'sans-serif'}*/
        /*font-weight: 600;*/
        background-color: transparent;
        white-space: nowrap;
        transition: background-color 0.15s ease, color 0.15s ease;
    }

    .badge-blue   { border-color: #4a90d9; color: #4a90d9; }
    .badge-green  { border-color: #4caf50; color: #4caf50; }
    .badge-orange { border-color: #f0932b; color: #f0932b; }
    .badge-purple { border-color: #9b59b6; color: #9b59b6; }
    .badge-teal   { border-color: #16a085; color: #16a085; }
    .badge-pink   { border-color: #e84393; color: #e84393; }

    .gridview-scroll-wrapper {
        width: 100%;
        overflow-x: auto;
        -webkit-overflow-scrolling: touch;
    }

    .gridview-scroll-top {
        width: 100%;
        overflow-x: auto;
        overflow-y: hidden;
        height: 17px;
    }

    .gridview-scroll-top-inner {
        height: 1px;
    }

    .dataItem1 {
    border-right: 1px solid #ddd;
    padding: 5px 25px 5px 5px;
}

    /* optional: fill in on hover, like a real button */
    /*.field-badge:hover {
        color: #ffffff;
    }
    .badge-blue:hover   { background-color: #4a90d9; }
    .badge-green:hover  { background-color: #4caf50; }
    .badge-orange:hover { background-color: #f0932b; }
    .badge-purple:hover { background-color: #9b59b6; }
    .badge-teal:hover   { background-color: #16a085; }
    .badge-pink:hover   { background-color: #e84393; }*/
.btn-bootstrap {
    display: inline-block;
    padding: 6px 12px;
    font-size: 14px;
    font-family: Arial, Helvetica, sans-serif;
    font-weight: 400;
    line-height: 1.5;
    color: #fff;
    text-align: center;
    text-decoration: none;
    vertical-align: middle;
    cursor: pointer;

    background-color: #0d6efd;
    border: 1px solid #0d6efd;
    border-radius: 4px;

    transition: all .15s ease-in-out;
}

.btn-bootstrap:hover {
    background-color: #0b5ed7;
    border-color: #0a58ca;
}

.btn-bootstrap:active {
    background-color: #0a58ca;
    border-color: #0a53be;
}

.btn-bootstrap:focus {
    outline: none;
    box-shadow: 0 0 0 0.2rem rgba(13,110,253,.25);
}

.btn-bootstrap:disabled {
    opacity: .65;
    cursor: not-allowed;
}

.actionMenu{
    position:relative;
    display:inline-block;
}

/* Hamburger button (opens right-side slide-in menu) */
.hamburgerBtn {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    width: 44px;
    height: 44px;
    border: 2px solid #ffffff;
    border-radius: 8px;
    background-color: transparent;
    cursor: pointer;
    padding: 0;
}

.hamburgerBtn:hover {
    background-color: rgba(255, 255, 255, 0.15);
}

.hamburgerBtn .hamburgerIcon,
.hamburgerBtn .hamburgerIcon::before,
.hamburgerBtn .hamburgerIcon::after {
    display: block;
    width: 22px;
    height: 2px;
    background-color: #ffffff;
    border-radius: 1px;
}

.hamburgerBtn .hamburgerIcon {
    position: relative;
}

.hamburgerBtn .hamburgerIcon::before,
.hamburgerBtn .hamburgerIcon::after {
    content: "";
    position: absolute;
    left: 0;
}

.hamburgerBtn .hamburgerIcon::before {
    top: -7px;
}

.hamburgerBtn .hamburgerIcon::after {
    top: 7px;
}

/* Right-side slide-in menu */
.sideMenuOverlay {
    display: none;
    position: fixed;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    background-color: rgba(0, 0, 0, 0.35);
    z-index: 1000;
}

.sideMenuOverlay.show {
    display: block;
}

.sideMenuPanel {
    position: fixed;
    top: 0;
    right: 0;
    height: 100%;
    width: 300px;
    max-width: 85%;
    background-color: #ffffff;
    box-shadow: -2px 0 10px rgba(0, 0, 0, 0.25);
    transform: translateX(100%);
    transition: transform 0.25s ease-in-out;
    z-index: 1001;
    display: flex;
    flex-direction: column;
}

.sideMenuOverlay.show .sideMenuPanel {
    transform: translateX(0);
}

.sideMenuHeader {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 16px 20px;
    background-color: #3366FF;
    color: #ffffff;
    font-family: Arial;
    font-size: 16pt;
    font-weight: bold;
}

.sideMenuClose {
    background: transparent;
    border: none;
    color: #ffffff;
    font-size: 22px;
    line-height: 1;
    cursor: pointer;
    padding: 0 4px;
}

.sideMenuClose:hover {
    opacity: 0.75;
}

.sideMenuBody {
    padding: 10px 0;
    overflow-y: auto;
    flex: 1 1 auto;
}

.sideMenuGroup {
    border-bottom: 1px solid #eeeeee;
}

.sideMenuGroupHeader {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 12px 20px;
    font-family: Arial;
    font-size: 12pt;
    font-weight: bold;
    color: #333333;
    cursor: pointer;
    user-select: none;
}

.sideMenuGroupHeader:hover {
    background-color: #f2f6ff;
}

.sideMenuGroupArrow {
    font-size: 10px;
    color: #888888;
    transition: transform 0.2s ease;
}

.sideMenuGroup.open .sideMenuGroupArrow {
    transform: rotate(180deg);
}

.sideMenuGroupItems {
    max-height: 0;
    overflow: hidden;
    background-color: #fafafa;
    transition: max-height 0.25s ease;
}

.sideMenuGroup.open .sideMenuGroupItems {
    max-height: 600px;
}

.sideMenuItem {
    display: block;
    padding: 10px 20px 10px 32px;
    font-family: Arial;
    font-size: 11pt;
    color: #333333;
    text-decoration: none;
    cursor: pointer;
    border-bottom: 1px solid #f0f0f0;
}

.sideMenuItem:last-child {
    border-bottom: none;
}

.sideMenuItem:hover {
    background-color: #e8f0ff;
}

.actionButton{
    display:inline-block;
    width:28px;
    height:28px;
    line-height:28px;
    text-align:center;
    font-size:22px;
    cursor:pointer;
    border-radius:4px;
    border-width:0px;
    user-select:none;
    background-color:transparent;
}

.actionButton:hover{
    background:#eeeeee;
}

.actionPopup{
    display:none;
    position:fixed;
    min-width:170px;
    background:white;
    border:1px solid #cccccc;
    border-radius:10px;
    overflow:hidden;
    box-shadow:0 3px 10px rgba(0,0,0,.25);
    z-index:9999;
}

.actionPopup.show{
    display:block;
}

.actionsHeaderHidden{
    font-size:0;
    line-height:0;
    color:transparent;
}

.actionItem{
    display:block;
    padding:8px 12px;
    color:#333;
    text-decoration:none;
    font-family:Arial;
    font-size:13px;
}

.actionItem:hover{
    background:#f5f5f5;
}

/* ---------- Menu Container ---------- */

.actionMenu {
    position: relative;
    display: inline-block;
}

/* ---------- Three Dots Button ---------- */

.menuButton {

    width:34px;
    height:26px;

    display:flex;
    align-items:center;
    justify-content:center;

    border-radius:50%;

    font-size:22px;
    color:#666;

    cursor:pointer;

    transition:.2s;
}

.menuButton:hover{

    background:#f3f5f7;

    color:#0078D4;

}

/* ---------- Popup ---------- */

.menuPopup{

    display:none;

    position:absolute;

    right:0;
    top:36px;

    width:220px;

    background:#fff;

    border-radius:10px;

    border:1px solid #E5E5E5;

    box-shadow:
        0 10px 25px rgba(0,0,0,.18);

    overflow:hidden;

    z-index:999999;
}

.menuPopup.show{

    display:block;

}

/* ---------- Each Row ---------- */

.menuRow{

    transition:.15s;

}

.menuRow:hover{

    background:#F7FAFD;

}

/* ---------- Link ---------- */
.menuItem{

    display:flex;
    align-items:center;

    padding:7px 14px;      /* was 12px 16px */

    text-decoration:none;

    color:#444;

    font-size:13px;        /* was 14px */

    font-family:'Segoe UI';

    transition:.15s;

    line-height:18px;      /* add this */
}

.menuItem:hover{

    color:#0078D4;

    padding-left:18px;     /* was 22px */
}

.menuIcon{

    width:22px;            /* was 26px */

    font-size:15px;        /* was 17px */

    color:#0078D4;
}

.menuDivider{

    border-top:1px solid #ECECEC;

    margin:2px 0;          /* was 4px */
}

.statusCard {
    background: #f8e08a;
    border: 1px solid #8c7b2a;
    border-radius: 8px;
    padding: 4px 5px;
    width: 100%;
    box-sizing: border-box;
    text-align: center
}

.statusTitle {
    font-family: Arial;
    font-size: 12pt;
    color: #000;
    text-decoration: underline;
    line-height: 1.2;
    margin-bottom: 4px;
    max-width: 100%;
    box-sizing: border-box;
    overflow-wrap: break-word;
    word-break: break-word;
    white-space: normal;
}

.statusSubtitle {
    font-family: Arial;
    font-size: 8pt;
    color: #333;
    background: #ffffff;
    border: 1px solid #999;
    border-radius: 6px;
    padding: 4px 8px;
    line-height: 1.3;
        display: inline-block;
    white-space: nowrap;
}
</style>
 

    <script type="text/javascript">
        function getSelectedElement(Select, ToTextBox) {
            //var e = document.getElementById("Select1");
            //var strUser = e.value;
            var e = document.getElementById(Select);
            var strUser = e.options[e.selectedIndex].text;

            // var txt = document.getElementById("TextBox2");
            if (strUser != 'From') {
                document.getElementById(ToTextBox).disabled = true;
            }
            else {
                document.getElementById(ToTextBox).disabled = false;
            }
            // alert(strUser);
        }

        function toggleMenu(btn, evt) {

            if (evt) { evt.stopPropagation(); }

            var popup = btn.nextElementSibling;
            var wasOpen = popup.classList.contains("show");

            document.querySelectorAll(".actionPopup").forEach(function (m) {
                m.classList.remove("show");
            });

            if (wasOpen) { return; }

            popup.style.visibility = "hidden";
            popup.classList.add("show");

            var rect = btn.getBoundingClientRect();
            var popupWidth = popup.offsetWidth;

            var top = rect.bottom + 2;
            var left = rect.left;

            var maxLeft = window.innerWidth - popupWidth - 4;
            if (left > maxLeft) { left = maxLeft; }

            popup.style.top = top + "px";
            popup.style.left = left + "px";
            popup.style.visibility = "visible";

        }

        function closeAllActionMenus() {
            document.querySelectorAll(".actionPopup").forEach(function (m) {
                m.classList.remove("show");
            });
        }

        document.addEventListener("click", closeAllActionMenus);
        window.addEventListener("scroll", closeAllActionMenus, true);
        window.addEventListener("resize", closeAllActionMenus);

        function openSideMenu(evt) {
            if (evt) { evt.stopPropagation(); }
            document.getElementById("sideMenuOverlay").classList.add("show");
        }

        function closeSideMenu(evt) {
            if (evt) { evt.stopPropagation(); }
            document.getElementById("sideMenuOverlay").classList.remove("show");
        }

        function toggleSideMenuGroup(header) {
            var group = header.parentElement;
            group.classList.toggle("open");
        }

        function openMenuLink(url, width, height) {
            if (!url) { return; }
            var w = width || 800;
            var h = height || 550;
            var left = (screen.width - w) / 2;
            var top = (screen.height - h) / 2;
            window.open(url, "_blank", "width=" + w + ",height=" + h + ",left=" + left + ",top=" + top + ",resizable=yes,scrollbars=yes");
            closeSideMenu();
        }


        function syncGridScrollbars() {
            var top = document.getElementById('gvScrollTop');
            var topInner = document.getElementById('gvScrollTopInner');
            var bottom = document.getElementById('gvScrollBottom');

            if (!top || !topInner || !bottom) { return; }

            // Make the dummy top strip exactly as wide as the actual scrollable content,
            // so its scrollbar behaves proportionally to the real one below.
            function resizeTopInner() {
                topInner.style.width = bottom.scrollWidth + 'px';
            }

            var isSyncing = false;

            top.addEventListener('scroll', function () {
                if (isSyncing) { return; }
                isSyncing = true;
                bottom.scrollLeft = top.scrollLeft;
                isSyncing = false;
            });

            bottom.addEventListener('scroll', function () {
                if (isSyncing) { return; }
                isSyncing = true;
                top.scrollLeft = bottom.scrollLeft;
                isSyncing = false;
            });

            resizeTopInner();
            window.addEventListener('resize', resizeTopInner);
        }

        window.addEventListener('load', syncGridScrollbars);
   </script>



</head>
<body style="margin: 0px;">
    <form id="form1" runat="server">

        <table cellpadding="0" class="auto-style2">
            <tr>
                <td style="background-color: #3366FF">
                    <table cellpadding="15" cellspacing="15" class="auto-style2">
                        <tr>
                            <td style="width: 50%">
                                <asp:Label ID="Label2" runat="server" Font-Names="Arial Black" Font-Size="36pt" ForeColor="White" Text="Label"></asp:Label>
                            </td>
                            <td style="vertical-align: top; width: 50%;" align="right">

                                <asp:LinkButton ID="lnkOpenAction" runat="server">Open Actions</asp:LinkButton>

                            </td>
                            <td style="vertical-align: top; width: 50%;" align="right">
                                                             <button type="button" class="hamburgerBtn" onclick="openSideMenu(event)" aria-label="Open menu">
                                    <span class="hamburgerIcon"></span>
                                </button></td>
                        </tr>
                    </table>
                </td>
            </tr>
            <tr>
                <td align="center" valign="top">
                    <table cellpadding="10" cellspacing="10" class="auto-style3">
                                <tr>
                                    <td style="width: 10%; vertical-align: top; text-align: center;" class="auto-style4">
                                        &nbsp;</td>
                                    <td style="width: 80%" align="left">
                                        <div class="unit-filter">
                                            <div class="unit-filter-label">
                                                <asp:Label ID="Label1" runat="server" Font-Names="Arial" Text="Show Units of Project:"></asp:Label>
                                            </div>
                                            <div class="unit-filter-control">
                                                <asp:DropDownList ID="DropDownList1" runat="server" CssClass="styled-dropdown" Font-Names="Arial" AutoPostBack="True">
                                                </asp:DropDownList>
                                            </div>
                                        </div>
                                        <table cellpadding="0" class="auto-style2">
                                            <tr>
                                                <td style="vertical-align: top;width:15%">
                                        <asp:GridView ID="GridView2" runat="server" CellPadding="4" Font-Names="Arial" ForeColor="#333333" BorderColor="Black" BorderStyle="Solid" BorderWidth="1px" Font-Size="13px" GridLines="Both">
                                            <AlternatingRowStyle BackColor="White" ForeColor="#284775" />
                                            <EditRowStyle BackColor="#999999" />
                                            <FooterStyle BackColor="#5D7B9D" Font-Bold="True" ForeColor="White" />
                                            <HeaderStyle BackColor="#5D7B9D" Font-Bold="True" ForeColor="White" />
                                            <PagerStyle BackColor="#284775" ForeColor="White" HorizontalAlign="Center" />
                                            <RowStyle BackColor="#F7F6F3" ForeColor="#333333" />
                                            <SelectedRowStyle BackColor="#E2DED6" Font-Bold="True" ForeColor="#333333" />
                                            <SortedAscendingCellStyle BackColor="#E9E7E2" />
                                            <SortedAscendingHeaderStyle BackColor="#506C8C" />
                                            <SortedDescendingCellStyle BackColor="#FFFDF8" />
                                            <SortedDescendingHeaderStyle BackColor="#6F8DAE" />
                                        </asp:GridView>
                                                </td>
                                                <td style="vertical-align: top;width:85%">
                                                    <asp:DataList ID="DataList2"     runat="server"
    RepeatColumns="3"
    Width="100%" CellPadding="4" Font-Names="Arial" ForeColor="#333333" GridLines="Both" Font-Size="13px">

                                                        <AlternatingItemStyle BackColor="White" ForeColor="#284775" CssClass="dataItem1" />
                                                        <FooterStyle BackColor="#5D7B9D" Font-Bold="True" ForeColor="White" />
                                                        <HeaderStyle BackColor="#5D7B9D" Font-Bold="True" ForeColor="White" />
                                                        <ItemStyle BackColor="#F7F6F3" ForeColor="#333333" CssClass="dataItem1" />

    <ItemTemplate>

        <table cellpadding="5">
            <tr>
                <td style="font-weight:bold; width:120px;">
                    <%# Eval("FIELD_NAME") %>
                </td>

                <td>
                    :
                </td>

                <td>
                    <%# Eval("FIELD_VALUE") %>
                </td>
            </tr>
        </table>

    </ItemTemplate>

                                                        <SelectedItemStyle BackColor="#E2DED6" Font-Bold="True" ForeColor="#333333" />

</asp:DataList>
                                                    </td>
                                            </tr>
                                        </table>
<asp:DataList ID="DataList1" runat="server"
    RepeatColumns="15">
    <ItemTemplate>
        <div class="unit-card">
            <asp:Literal ID="litFields" runat="server"></asp:Literal>
        </div>
    </ItemTemplate>
</asp:DataList>
                                        <br />

                                        <div style="gap: 8px;width:100%;align-content:flex-end">
                                            <asp:Button ID="btnColumns"
    runat="server"
    Text="Columns"
    CssClass="btn-bootstrap" />
                                            <asp:Button ID="btnFilter"
    runat="server"
    Text="Filter"
    CssClass="btn-bootstrap" />
                                        </div>
                                        <div class="gridview-scroll-top" id="gvScrollTop">
                                            <div class="gridview-scroll-top-inner" id="gvScrollTopInner">&nbsp;</div>
                                        </div>
                                        <div class="gridview-scroll-wrapper" id="gvScrollBottom">
                                        <asp:GridView ID="GridView1" runat="server" Width="100%" CellPadding="4" Font-Names="Arial" ForeColor="#333333" DataKeyNames="Reference,STATUS,NodeId">
                                            <AlternatingRowStyle BackColor="White" ForeColor="#284775" HorizontalAlign="Center" />
                                            <Columns>
                                                <asp:TemplateField HeaderText="Actions" HeaderStyle-CssClass="actionsHeaderHidden">
                                                    <ItemTemplate>

                                                        <div class="actionMenu">

    <button type="button" class="actionButton" onclick="toggleMenu(this, event)">
        &#8942;
    </button>

    <div class="actionPopup" onclick="event.stopPropagation();">
        <asp:PlaceHolder ID="phActions" runat="server" />
    </div>

</div>

                                                    </ItemTemplate>

<HeaderStyle CssClass="actionsHeaderHidden"></HeaderStyle>
                                                    <ItemStyle HorizontalAlign="Center" />
                                                </asp:TemplateField>
                                                <asp:TemplateField HeaderText="Status">
                                                    <ItemTemplate>
                                                        <div id="statusCard" runat="server" class="statusCard">
                                                            <div id="statusTitle" runat="server" class="statusTitle">
                                                                <%# Eval("STATUS") %>
                                                            </div>

                                                            <div id="statusSubtitle" runat="server" class="statusSubtitle">
                                                                <%# Eval("Status_Subtitle") %>
                                                            </div>
                                                        </div>
                                                    </ItemTemplate>
                                                    <ItemStyle Width="106px" HorizontalAlign="Center" />
                                                    <HeaderStyle Width="106px" HorizontalAlign="Center" />
                                                </asp:TemplateField>
                                            </Columns>
                                            <EditRowStyle BackColor="#999999" HorizontalAlign="Center" />
                                            <FooterStyle BackColor="#5D7B9D" Font-Bold="True" ForeColor="White" HorizontalAlign="Center" />
                                            <HeaderStyle BackColor="#5D7B9D" Font-Bold="True" ForeColor="White" HorizontalAlign="Center"/>
                                            <PagerStyle BackColor="#284775" ForeColor="White" HorizontalAlign="Center" />
                                            <RowStyle BackColor="#F7F6F3" ForeColor="#333333" HorizontalAlign="Center" />
                                            <SelectedRowStyle BackColor="#E2DED6" Font-Bold="True" ForeColor="#333333" />
                                            <SortedAscendingCellStyle BackColor="#E9E7E2" />
                                            <SortedAscendingHeaderStyle BackColor="#506C8C" />
                                            <SortedDescendingCellStyle BackColor="#FFFDF8" />
                                            <SortedDescendingHeaderStyle BackColor="#6F8DAE" />
                                        </asp:GridView>
                                        </div>
                                    </td>
                                    <td style="width: 10%; vertical-align: top;">
                                        &nbsp;</td>
                        </tr>
                                </table>
                        </td>
                     </tr>
                        </table>

        <div class="sideMenuOverlay" id="sideMenuOverlay" onclick="closeSideMenu(event)">
            <div class="sideMenuPanel" onclick="event.stopPropagation();">
                <div class="sideMenuHeader">
                    <span>Menu</span>
                    <button type="button" class="sideMenuClose" onclick="closeSideMenu(event)" aria-label="Close menu">&times;</button>
                </div>
                <div class="sideMenuBody">
                    <asp:PlaceHolder ID="phSideMenu" runat="server" />
                </div>
            </div>
        </div>

</form>
</body>
</html>