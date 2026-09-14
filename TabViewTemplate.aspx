<%@ Page Language="VB" AutoEventWireup="false" CodeFile="TabViewTemplate.aspx.vb" Inherits="TabViewTemplate"   MaintainScrollPositionOnPostback="true" EnableEventValidation = false  %>

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

        body {
            margin: 0;
            padding: 0;
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
                                                    <asp:MenuItem Text="Definition" Value="Definition"></asp:MenuItem>
                                                    <asp:MenuItem Text="Uploadees" Value="Uploadees"></asp:MenuItem>
                                                    <asp:MenuItem Text="Setting" Value="Setting"></asp:MenuItem>
                                                    <asp:MenuItem Text="Requester" Value="Ongoing/Requester"></asp:MenuItem>
                                                    <asp:MenuItem Text="Requestee" Value="Ongoing/Requestee"></asp:MenuItem>
                                                    <asp:MenuItem Text="Upcoming" Value="Upcoming"></asp:MenuItem>
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
                                                            <asp:Panel ID="Panel1" runat="server" BackColor="White" Height="300px" Width="100%">View 1</asp:Panel>
                                                        </asp:View>
                                                        <asp:View ID="View2" runat="server">

                                                            <asp:Panel ID="Panel2" runat="server" BackColor="White" Height="300px" Width="100%">
                                                                View 2</asp:Panel>

                                                        </asp:View>
                                                        <asp:View ID="View3" runat="server">
                                                            <asp:Panel ID="Panel3" runat="server" BackColor="White" Height="300px" Width="100%">
                                                                View 3</asp:Panel>
                                                        </asp:View>
                                                        <asp:View ID="View4" runat="server">
                                                                <!--####################################################################################################-->
                                                                <!--####################################################################################################-->
                                                                <!--####################################################################################################-->
                                                                <!--####################################################################################################-->
                                                                <!--####################################################################################################-->

                                                                <!--####################################################################################################-->
                                                                <!--####################################################################################################-->
                                                                <!--####################################################################################################-->
                                                                <!--####################################################################################################-->
                                                                <!--####################################################################################################-->

                                                        </asp:View>
                                                        <asp:View ID="View5" runat="server">
                                                                <!--####################################################################################################-->
                                                                <!--####################################################################################################-->
                                                                <!--####################################################################################################-->
                                                                <!--####################################################################################################-->
                                                                <!--####################################################################################################-->

                                                                <!--####################################################################################################-->
                                                                <!--####################################################################################################-->
                                                                <!--####################################################################################################-->
                                                                <!--####################################################################################################-->
                                                                <!--####################################################################################################-->

                                                        </asp:View>
                                                        <asp:View ID="View6" runat="server">
                                                                <!--####################################################################################################-->
                                                                <!--####################################################################################################-->
                                                                <!--####################################################################################################-->
                                                                <!--####################################################################################################-->
                                                                <!--####################################################################################################-->

                                                                <!--####################################################################################################-->
                                                                <!--####################################################################################################-->
                                                                <!--####################################################################################################-->
                                                                <!--####################################################################################################-->
                                                                <!--####################################################################################################-->

                                                        </asp:View>
                                                    </asp:MultiView>
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
