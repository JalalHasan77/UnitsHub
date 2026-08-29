<%@ Page Language="VB" AutoEventWireup="false" CodeFile="Try.aspx.vb" Inherits="Try" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
        <div>
            <asp:ListBox ID="ListBox1" runat="server" SelectionMode="Multiple" Height="324px" Width="172px"></asp:ListBox>
            <asp:Button ID="Button1" runat="server" Text="Button" OnClick="Button1_Click" />
        </div>

        <script type="text/javascript">
            var selectedOrder = [];

            function enforceMaxTwoSelections() {
                var select = document.getElementById('<%= ListBox1.ClientID %>');
                if (!select) { return; }

                var currentlySelected = [];
                for (var i = 0; i < select.options.length; i++) {
                    if (select.options[i].selected) {
                        currentlySelected.push(select.options[i].value);
                    }
                }

                // Drop anything we were tracking that's no longer selected.
                selectedOrder = selectedOrder.filter(function (v) {
                    return currentlySelected.indexOf(v) !== -1;
                });

                // Append anything newly selected, in the order the browser reports it.
                for (var j = 0; j < currentlySelected.length; j++) {
                    if (selectedOrder.indexOf(currentlySelected[j]) === -1) {
                        selectedOrder.push(currentlySelected[j]);
                    }
                }

                // Keep only the two most recently selected — drop the oldest first.
                while (selectedOrder.length > 2) {
                    selectedOrder.shift();
                }

                for (var k = 0; k < select.options.length; k++) {
                    select.options[k].selected = (selectedOrder.indexOf(select.options[k].value) !== -1);
                }
            }

            if (document.addEventListener) {
                document.addEventListener('DOMContentLoaded', function () {
                    var select = document.getElementById('<%= ListBox1.ClientID %>');
                    if (select) {
                        select.addEventListener('change', enforceMaxTwoSelections);
                    }
                });
            }
        </script>
    </form>
</body>
</html>
