<%@ Page Language="VB" AutoEventWireup="true" CodeFile="SelectOneItemFromListMultiColumns.aspx.vb" Inherits="SelectOneItemFromListMultiColumns" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Select One Item From List</title>
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

        .items-card {
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

        .items-title {
            font-size: 18px;
            font-weight: 700;
            color: #1f2937;
            margin-bottom: 14px;
            flex: 0 0 auto;
        }

        .items-search {
            flex: 0 0 auto;
            margin-bottom: 12px;
        }

        .items-textbox {
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

        .items-scroll {
            flex: 1 1 auto;
            overflow-y: auto;
            overflow-x: auto;
            border: 1px solid #e5e7eb;
            border-radius: 8px;
            background: #f8fafc;
            box-sizing: border-box;
        }

        .items-table {
            width: 100%;
            min-width: 100%;
            border-collapse: collapse;
            table-layout: fixed;
            font-size: 14px;
            color: #111827;
        }

        .items-table th {
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

        .items-table td {
            padding: 10px 12px;
            border-bottom: 1px solid #e5e7eb;
            vertical-align: middle;
            word-break: break-word;
            background: #ffffff;
        }

        .items-table tbody tr:hover td {
            background: #eef4ff;
        }

        .items-table tbody tr.selected-row td {
            background: #dbeafe;
        }

        .items-selector,
        .items-selector-cell,
        .items-selector-column {
            width: 42px;
            text-align: center;
            white-space: nowrap;
        }

        .items-selector-cell input[type="radio"] {
            margin: 0;
        }

        .hoverable-mode .items-selector,
        .hoverable-mode .items-selector-cell,
        .hoverable-mode .items-selector-column {
            display: none;
            width: 0 !important;
            min-width: 0 !important;
            padding: 0 !important;
            border: none !important;
        }

        .hoverable-mode .items-selector-cell input[type="radio"] {
            display: none;
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

        .item-highlight {
            background-color: #fef08a;
            color: #111827;
            font-weight: 700;
            padding: 0 1px;
            border-radius: 3px;
        }

        .items-empty {
            display: none;
            padding: 18px;
            text-align: center;
            color: #6b7280;
            font-size: 14px;
        }

        .items-buttons {
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
        function isHoverableListMode() {
            return <%= If(IsHoverableList, "true", "false") %>;
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
                    html += '<span class="item-highlight">' + htmlEncode(part) + '</span>';
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

        function updateSelectedRowStyles() {
            var rows = document.querySelectorAll('#itemsTable tbody tr');
            for (var i = 0; i < rows.length; i++) {
                var radio = rows[i].querySelector('input[type="radio"][name="selectedItem"]');
                if (radio && radio.checked) {
                    rows[i].classList.add('selected-row');
                } else {
                    rows[i].classList.remove('selected-row');
                }
            }
        }

        function selectTableRow(row) {
            var radio = row.querySelector('input[type="radio"][name="selectedItem"]');
            if (!radio) {
                return;
            }

            radio.checked = true;
            updateSelectedRowStyles();
        }

        function filterItems() {
            var txt = document.getElementById('<%= TextBoxSearch.ClientID %>');
            var table = document.getElementById('itemsTable');
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

            updateSelectedRowStyles();
        }

        function initializeEditableCells() {
            var table = document.getElementById('itemsTable');
            if (!table || !table.tBodies.length) {
                return;
            }

            var rows = table.tBodies[0].rows;
            for (var r = 0; r < rows.length; r++) {
                rows[r].addEventListener('click', function (e) {
                    var tagName = e.target && e.target.tagName ? e.target.tagName.toLowerCase() : '';
                    if (tagName === 'input' || tagName === 'textarea' || tagName === 'select' || tagName === 'button') {
                        return;
                    }
                    selectTableRow(this);
                });
            }

            var inputs = table.querySelectorAll('input[data-search-input="1"]');
            for (var i = 0; i < inputs.length; i++) {
                inputs[i].addEventListener('input', filterItems);
                inputs[i].addEventListener('click', function (e) {
                    this.focus();
                    this.select();
                    if (e) {
                        e.stopPropagation();
                    }
                });
            }

            var radios = table.querySelectorAll('input[type="radio"][name="selectedItem"]');
            for (var j = 0; j < radios.length; j++) {
                radios[j].addEventListener('change', updateSelectedRowStyles);
            }

            updateSelectedRowStyles();
        }

        function validateSelection() {
            var selected = document.querySelector('input[type="radio"][name="selectedItem"]:checked');
            if (!selected) {
                alert('Please select an item first.');
                return false;
            }
            return true;
        }

        function initializePage() {
            initializeEditableCells();
            filterItems();
        }
    </script>
</head>
<body class="<%= If(IsHoverableList, "hoverable-mode", "radio-mode") %>" onload="initializePage();">
    <form id="form1" runat="server">
        <div class="items-card">
            <asp:Label ID="Label1" runat="server" CssClass="items-title" Text="Select Item"></asp:Label>

            <div class="items-search">
                <asp:TextBox ID="TextBoxSearch" runat="server" CssClass="items-textbox" autocomplete="off"
                    placeholder="Search..." onkeyup="filterItems();"></asp:TextBox>
            </div>

            <div class="items-scroll">
                <asp:Literal ID="litItemsTable" runat="server"></asp:Literal>
                <div id="emptyState" class="items-empty">No matching records found.</div>
            </div>

            <div class="items-buttons">
                <asp:Button ID="Button1" runat="server" Text="Add" CssClass="btn-modern btn-add" OnClientClick="return validateSelection();" />
                <asp:Button ID="Button2" runat="server" Text="Cancel" CssClass="btn-modern btn-cancel" CausesValidation="false" UseSubmitBehavior="false" />
            </div>
        </div>
    </form>
</body>
</html>
