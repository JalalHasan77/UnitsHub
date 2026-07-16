<%@ Page Language="VB" AutoEventWireup="false" CodeFile="AddMultipleItemsFromList.aspx.vb" Inherits="AddMultipleItemsFromList" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Members</title>

    <style type="text/css">
        html, body, form {
            width: 100%;
            height: 100%;
            margin: 0;
        }

        body {
            background: transparent;
            overflow: hidden;
            font-family: Arial, sans-serif;
        }

        .adj-card {
            width: 100%;
            height: 100%;
            box-sizing: border-box;
            padding: 20px 22px;
            border: none;
            border-radius: 0;
            background: #ffffff;
            box-shadow: none;
            display: flex;
            flex-direction: column;
        }

        .adj-title {
            font-size: 18px;
            font-weight: 700;
            color: #1f2937;
            margin-bottom: 14px;
            flex: 0 0 auto;
        }

        .members-search {
            flex: 0 0 auto;
            margin-bottom: 12px;
        }

        .adj-textbox {
            width: 100%;
            height: 34px;
            padding: 6px 10px;
            border: 1px solid #cbd5e1;
            border-radius: 8px;
            background: #f8fafc;
            font-size: 14px;
            color: #111827;
            box-sizing: border-box;
        }

        .members-scroll {
            flex: 1 1 auto;
            overflow-y: auto;
            overflow-x: auto;
            border: 1px solid #e5e7eb;
            border-radius: 8px;
            background: #f8fafc;
            box-sizing: border-box;
        }

        .members-table {
            width: 100%;
            min-width: 100%;
            border-collapse: collapse;
            table-layout: fixed;
            font-size: 14px;
            color: #111827;
        }

        .members-table th {
            position: sticky;
            top: 0;
            z-index: 1;
            background: #e5eefc;
            color: #1f2937;
            text-align: left;
            font-weight: 700;
            padding: 10px 12px;
            border-bottom: 1px solid #cbd5e1;
            white-space: nowrap;
        }

        .members-table td {
            padding: 10px 12px;
            border-bottom: 1px solid #e5e7eb;
            vertical-align: middle;
            word-break: break-word;
            background: #ffffff;
        }

        .members-table tbody tr:hover td {
            background: #eef4ff;
        }

        .members-selector,
        .members-selector-cell,
        .members-selector-column {
            width: 42px;
            text-align: center;
            white-space: nowrap;
        }

        .members-selector-cell input[type="checkbox"] {
            margin: 0;
        }

        .editable-cell {
            padding: 4px 6px !important;
        }

        .editable-cell input[type="text"] {
            width: 100%;
            min-width: 0;
            padding: 6px 8px;
            border: 1px solid transparent;
            border-radius: 6px;
            background: transparent;
            box-sizing: border-box;
            font-size: 14px;
            color: #111827;
            outline: none;
        }

        .editable-cell input[type="text"]:focus {
            border-color: #2563eb;
            background: #ffffff;
            box-shadow: 0 0 0 3px rgba(37, 99, 235, 0.12);
        }

        .member-highlight {
            background-color: #fef08a;
            color: #111827;
            font-weight: 700;
            padding: 0 1px;
            border-radius: 3px;
        }

        .members-empty {
            display: none;
            padding: 18px;
            text-align: center;
            color: #6b7280;
            font-size: 14px;
        }

        .adj-buttons {
            display: flex;
            justify-content: flex-end;
            gap: 10px;
            margin-top: 18px;
            padding-top: 14px;
            border-top: 1px solid #e5e7eb;
            flex: 0 0 auto;
        }

        .btn-modern {
            min-width: 90px;
            height: 36px;
            padding: 0 16px;
            border: none;
            border-radius: 8px;
            font-size: 14px;
            font-weight: 700;
            cursor: pointer;
        }

        .btn-add {
            background: #2563eb;
            color: white;
        }

        .btn-add:hover {
            background: #1d4ed8;
        }

        .btn-cancel {
            background: #e5e7eb;
            color: #374151;
        }

        .btn-cancel:hover {
            background: #d1d5db;
        }
    </style>

    <script type="text/javascript">
        function closeParentVendorPopup() {
            if (window.parent && typeof window.parent.closeVendorDialog === 'function') {
                window.parent.closeVendorDialog();
            }
            return false;
        }

        function htmlEncode(value) {
            var div = document.createElement('div');
            div.textContent = value || '';
            return div.innerHTML;
        }

        function escapeRegExp(text) {
            return (text || '').replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
        }

        function buildHighlightedHtml(text, keyword) {
            text = text || '';
            if (!keyword) {
                return htmlEncode(text);
            }

            var regex = new RegExp('(' + escapeRegExp(keyword) + ')', 'ig');
            var parts = text.split(regex);
            var html = '';

            for (var i = 0; i < parts.length; i++) {
                var part = parts[i];
                if (part.toLowerCase() === keyword.toLowerCase()) {
                    html += '<span class="member-highlight">' + htmlEncode(part) + '</span>';
                } else {
                    html += htmlEncode(part);
                }
            }

            return html;
        }

        function getRowSearchText(row) {
            var parts = [];
            var textCells = row.querySelectorAll('td[data-original-text]');
            for (var i = 0; i < textCells.length; i++) {
                parts.push(textCells[i].getAttribute('data-original-text') || '');
            }

            var inputs = row.querySelectorAll('input[data-search-input="1"]');
            for (var j = 0; j < inputs.length; j++) {
                parts.push(inputs[j].value || '');
            }

            return parts.join(' ').toLowerCase();
        }

        function filterMembers() {
            var txt = document.getElementById('<%= TextBoxSearch.ClientID %>');
            var table = document.getElementById('membersTable');
            var empty = document.getElementById('emptyState');

            if (!txt || !table || !table.tBodies.length) {
                if (empty) {
                    empty.style.display = 'block';
                }
                return;
            }

            var keyword = txt.value.toLowerCase().trim();
            var rows = table.tBodies[0].rows;
            var visibleCount = 0;

            for (var i = 0; i < rows.length; i++) {
                var row = rows[i];
                var searchText = getRowSearchText(row);
                var isMatch = keyword === '' || searchText.indexOf(keyword) > -1;
                row.style.display = isMatch ? '' : 'none';

                var cells = row.querySelectorAll('td[data-original-text]');
                for (var c = 0; c < cells.length; c++) {
                    var original = cells[c].getAttribute('data-original-text') || '';
                    cells[c].innerHTML = buildHighlightedHtml(original, isMatch ? keyword : '');
                }

                if (isMatch) {
                    visibleCount++;
                }
            }

            if (empty) {
                empty.style.display = visibleCount === 0 ? 'block' : 'none';
            }
        }

        function initializeEditableCells() {
            var table = document.getElementById('membersTable');
            if (!table || !table.tBodies.length) {
                return;
            }

            var inputs = table.querySelectorAll('input[data-search-input="1"]');
            for (var i = 0; i < inputs.length; i++) {
                inputs[i].addEventListener('input', filterMembers);
                inputs[i].addEventListener('click', function (e) {
                    this.focus();
                    this.select();
                    if (e) {
                        e.stopPropagation();
                    }
                });
            }
        }

        function initializeMembersForm() {
            initializeEditableCells();
            filterMembers();
        }
    </script>
</head>
<body onload="initializeMembersForm();">
    <form id="form1" runat="server">
        <div class="adj-card">
            <div class="adj-title">
                <asp:Label ID="Label1" runat="server"></asp:Label>
            </div>

            <div class="members-search">
                <asp:TextBox ID="TextBoxSearch"
                             runat="server"
                             CssClass="adj-textbox"
                             onkeyup="filterMembers()"
                             placeholder="Search members..."></asp:TextBox>
            </div>

            <div class="members-scroll">
                <asp:Literal ID="litMembersTable" runat="server"></asp:Literal>
                <div id="emptyState" class="members-empty">No items found.</div>
            </div>

            <div class="adj-buttons">
                <asp:Button ID="Button1"
                            runat="server"
                            Text="Add"
                            CssClass="btn-modern btn-add"
                            OnClick="Button1_Click" />

                <asp:Button ID="Button2"
                            runat="server"
                            Text="Cancel"
                            CssClass="btn-modern btn-cancel"
                            OnClientClick="return closeParentVendorPopup();"
                            UseSubmitBehavior="false" />
            </div>
        </div>
    </form>
</body>
</html>
