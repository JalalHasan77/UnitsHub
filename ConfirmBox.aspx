<%@ Page Language="VB" AutoEventWireup="false" CodeFile="ConfirmBox.aspx.vb" Inherits="ConfirmBox" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Confirm Box</title>

    <!-- Bootstrap CSS -->
    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/twitter-bootstrap/5.3.3/css/bootstrap.min.css" />

    <style type="text/css">
        html, body {
            height: 100%;
            margin: 0;
            background-color: #eaf4fb;
        }

        #form1 {
            width: 100%;
            height: 100%;
        }

        .confirm-wrapper {
            display: flex;
            justify-content: center;
            align-items: center;
            width: 100%;
            height: 100%;
            padding: 7px;
            box-sizing: border-box;
        }

        /* Collapsed state of the expandable div */
        #pnlConfirmBox {
            width: 100%;
            height: 100%;
            max-height: 0;
            overflow: hidden;
            opacity: 0;
            box-sizing: border-box;
            padding: 0 25px;
            border: 1px solid #cfe3ee;
            border-radius: 8px;
            background-color: #ffffff;
            box-shadow: 0 2px 10px rgba(0,0,0,0.08);
            transition: max-height 0.4s ease, opacity 0.4s ease, padding 0.4s ease;
        }

        /* Expanded state - toggled via CSS class */
        #pnlConfirmBox.expanded {
            max-height: 100%;
            opacity: 1;
            padding: 25px;
            display: flex;
            flex-direction: column;
            justify-content: center;
            align-items: center;
        }

        .confirm-message {
            text-align: center;
            margin-bottom: 25px;
        }

        .confirm-textbox {
            display: block;
            margin: 0 auto 20px auto;
            text-align: center;
        }

        .confirm-buttons {
            display: flex;
            justify-content: center;
            gap: 10px;
            margin-top: 10px;
        }
    </style>

    <script type="text/javascript">
        function toggleConfirmBox() {
            var panel = document.getElementById('<%= pnlConfirmBox.ClientID %>');
            if (panel) {
                panel.classList.toggle('expanded');
            }
        }
    </script>
</head>
<body>
    <form id="form1" runat="server">
        <div class="confirm-wrapper">

                <asp:Panel ID="pnlConfirmBox" runat="server" CssClass="expanded">
                    <div class="confirm-message">
                        <asp:Label ID="Label1" runat="server" Text="Are you sure you want to change the status of the Unit?" Font-Names="Arial" Font-Size="20pt"></asp:Label>
                    </div>
                    <div class="confirm-buttons">
                        <asp:Button ID="btnOK" runat="server" Text="OK" CssClass="btn btn-primary" />
                        <asp:Button ID="btnCancel" runat="server" Text="Cancel" CssClass="btn btn-outline-secondary" />
                    </div>

                </asp:Panel>
            </div>

    </form>
</body>
</html>
