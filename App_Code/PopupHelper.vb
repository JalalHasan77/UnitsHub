Imports System.Text
Imports System.Web
Imports System.Web.UI
Imports System.Web.UI.WebControls

Public Module VendorPopupHelper

    Private Const StylesRegisteredKey As String = "VendorPopupStylesRegistered"
    Private Const MarkupRegisteredKey As String = "VendorPopupMarkupRegistered"
    Private Const ScriptRegisteredKey As String = "VendorPopupScriptRegistered"
    Private Const ReturnValueSessionKeyPrefix As String = "VendorPopupReturnValue:"

    Public Enum PopupPlacement
        Center
        RightSide
    End Enum

    Public Enum PopupDisplayMode
        Standard
        FrameOnly
    End Enum

    Public Sub RegisterVendorPopup(ByVal page As Page,
                                   ByVal triggerControl As WebControl,
                                   ByVal popupPageUrl As String,
                                   ByVal popupWidth As Integer,
                                   ByVal popupHeight As Integer,
                                   ByVal placement As PopupPlacement,
                                   ByVal selectedVendorValueField As HiddenField,
                                   ByVal selectedVendorTextField As HiddenField,
                                   ByVal displayTextBox As TextBox,
                                   Optional ByVal popupTitle As String = "",
                                   Optional ByVal displayMode As PopupDisplayMode = PopupDisplayMode.Standard)

        If page Is Nothing Then Throw New ArgumentNullException("page")
        If triggerControl Is Nothing Then Throw New ArgumentNullException("triggerControl")
        If selectedVendorValueField Is Nothing Then Throw New ArgumentNullException("selectedVendorValueField")
        If selectedVendorTextField Is Nothing Then Throw New ArgumentNullException("selectedVendorTextField")
        If displayTextBox Is Nothing Then Throw New ArgumentNullException("displayTextBox")

        RegisterVendorPopupStyles(page)
        RegisterVendorPopupMarkup(page)
        RegisterVendorPopupScript(page)

        Dim resolvedUrl As String = ResolvePopupUrl(page, popupPageUrl)

        Dim clientScript As String = BuildOpenDialogScript(
            resolvedUrl,
            popupTitle,
            popupWidth,
            popupHeight,
            placement,
            triggerControl.UniqueID,
            selectedVendorValueField.ClientID,
            selectedVendorTextField.ClientID,
            "",
            displayTextBox.ClientID,
            displayMode)

        triggerControl.Attributes("onclick") = clientScript
    End Sub

    Public Sub RegisterVendorPopup(ByVal page As Page,
                                   ByVal triggerControl As WebControl,
                                   ByVal popupPageUrl As String,
                                   ByVal popupWidth As Integer,
                                   ByVal popupHeight As Integer,
                                   ByVal placement As PopupPlacement,
                                   ByVal selectedVendorValueField As HiddenField,
                                   ByVal selectedVendorTextField As HiddenField,
                                   ByVal displayValueLabel As Label,
                                   ByVal displayTextLabel As Label,
                                   Optional ByVal popupTitle As String = "",
                                   Optional ByVal displayMode As PopupDisplayMode = PopupDisplayMode.Standard)

        If page Is Nothing Then Throw New ArgumentNullException("page")
        If triggerControl Is Nothing Then Throw New ArgumentNullException("triggerControl")
        If selectedVendorValueField Is Nothing Then Throw New ArgumentNullException("selectedVendorValueField")
        If selectedVendorTextField Is Nothing Then Throw New ArgumentNullException("selectedVendorTextField")
        If displayValueLabel Is Nothing Then Throw New ArgumentNullException("displayValueLabel")
        If displayTextLabel Is Nothing Then Throw New ArgumentNullException("displayTextLabel")

        RegisterVendorPopupStyles(page)
        RegisterVendorPopupMarkup(page)
        RegisterVendorPopupScript(page)

        Dim resolvedUrl As String = ResolvePopupUrl(page, popupPageUrl)

        Dim clientScript As String = BuildOpenDialogScript(
            resolvedUrl,
            popupTitle,
            popupWidth,
            popupHeight,
            placement,
            triggerControl.UniqueID,
            selectedVendorValueField.ClientID,
            selectedVendorTextField.ClientID,
            displayValueLabel.ClientID,
            displayTextLabel.ClientID,
            displayMode)

        triggerControl.Attributes("onclick") = clientScript
    End Sub

    Public Sub RegisterVendorPopup(ByVal page As Page,
                               ByVal triggerControl As WebControl,
                               ByVal popupPageUrl As String,
                               ByVal popupWidth As Integer,
                               ByVal popupHeight As Integer,
                               ByVal placement As PopupPlacement,
                               Optional ByVal popupTitle As String = "",
                               Optional ByVal displayMode As PopupDisplayMode = PopupDisplayMode.Standard)

        If page Is Nothing Then Throw New ArgumentNullException("page")
        If triggerControl Is Nothing Then Throw New ArgumentNullException("triggerControl")

        RegisterVendorPopupStyles(page)
        RegisterVendorPopupMarkup(page)
        RegisterVendorPopupScript(page)

        Dim resolvedUrl As String = ResolvePopupUrl(page, popupPageUrl)

        Dim clientScript As String = BuildOpenDialogScript(
        resolvedUrl,
        popupTitle,
        popupWidth,
        popupHeight,
        placement,
        triggerControl.UniqueID,
        "",
        "",
        "",
        "",
        displayMode)

        triggerControl.Attributes("onclick") = clientScript
    End Sub


    Private Sub RegisterVendorPopupStyles(ByVal page As Page)
        If page.Items(StylesRegisteredKey) IsNot Nothing Then Exit Sub

        Dim css As New StringBuilder()

        css.AppendLine("<style type=""text/css"">")
        css.AppendLine("#vendorModalOverlay {")
        css.AppendLine("    display: none;")
        css.AppendLine("    position: fixed;")
        css.AppendLine("    top: 0;")
        css.AppendLine("    left: 0;")
        css.AppendLine("    width: 100%;")
        css.AppendLine("    height: 100%;")
        css.AppendLine("    background-color: rgba(0, 0, 0, 0.55);")
        css.AppendLine("    z-index: 10000;")
        css.AppendLine("}")
        css.AppendLine("")
        css.AppendLine("#vendorModalDialog {")
        css.AppendLine("    position: fixed;")
        css.AppendLine("    top: 50%;")
        css.AppendLine("    left: 50%;")
        css.AppendLine("    transform: translate(-50%, -50%);")
        css.AppendLine("    width: 600px;")
        css.AppendLine("    height: 400px;")
        css.AppendLine("    max-width: 95vw;")
        css.AppendLine("    max-height: 95vh;")
        css.AppendLine("    background-color: #ffffff;")
        css.AppendLine("    border-radius: 8px;")
        css.AppendLine("    box-shadow: 0 8px 30px rgba(0,0,0,0.30);")
        css.AppendLine("    overflow: hidden;")
        css.AppendLine("    display: flex;")
        css.AppendLine("    flex-direction: column;")
        css.AppendLine("}")
        css.AppendLine("")
        css.AppendLine(".vendor-modal-header {")
        css.AppendLine("    display: flex;")
        css.AppendLine("    align-items: center;")
        css.AppendLine("    justify-content: space-between;")
        css.AppendLine("    padding: 15px 15px 10px 15px;")
        css.AppendLine("    border-bottom: 1px solid #e5e5e5;")
        css.AppendLine("    font-family: Arial, sans-serif;")
        css.AppendLine("}")
        css.AppendLine("")
        css.AppendLine(".vendor-modal-title {")
        css.AppendLine("    font-size: 20px;")
        css.AppendLine("    font-weight: bold;")
        css.AppendLine("    color: #333333;")
        css.AppendLine("}")
        css.AppendLine("")
        css.AppendLine(".btn-close-x {")
        css.AppendLine("    background: transparent;")
        css.AppendLine("    border: none;")
        css.AppendLine("    color: #666666;")
        css.AppendLine("    cursor: pointer;")
        css.AppendLine("    font-size: 24px;")
        css.AppendLine("    line-height: 1;")
        css.AppendLine("    padding: 0 4px;")
        css.AppendLine("}")
        css.AppendLine("")
        css.AppendLine(".btn-close-x:hover {")
        css.AppendLine("    color: #cc0000;")
        css.AppendLine("}")
        css.AppendLine("")
        css.AppendLine("#vendorPopupFrame {")
        css.AppendLine("    width: 100%;")
        css.AppendLine("    height: 100%;")
        css.AppendLine("    border: none;")
        css.AppendLine("    flex: 1 1 auto;")
        css.AppendLine("}")
        css.AppendLine("")
        css.AppendLine(".vendor-modal-footer {")
        css.AppendLine("    padding: 12px 15px;")
        css.AppendLine("    text-align: right;")
        css.AppendLine("    border-top: 1px solid #e5e5e5;")
        css.AppendLine("}")
        css.AppendLine("")
        css.AppendLine(".btn-close {")
        css.AppendLine("    padding: 8px 18px;")
        css.AppendLine("    background: #cc0000;")
        css.AppendLine("    color: #ffffff;")
        css.AppendLine("    border: none;")
        css.AppendLine("    border-radius: 4px;")
        css.AppendLine("    cursor: pointer;")
        css.AppendLine("    font-size: 14px;")
        css.AppendLine("}")
        css.AppendLine("")
        css.AppendLine(".btn-close:hover {")
        css.AppendLine("    background: #a80000;")
        css.AppendLine("}")
        css.AppendLine("")
        css.AppendLine("#vendorModalDialog.vendor-modal-frame-only {")
        css.AppendLine("    padding: 0;")
        css.AppendLine("}")
        css.AppendLine("")
        css.AppendLine("#vendorModalDialog.vendor-modal-frame-only .vendor-modal-header,")
        css.AppendLine("#vendorModalDialog.vendor-modal-frame-only .vendor-modal-footer {")
        css.AppendLine("    display: none;")
        css.AppendLine("}")
        css.AppendLine("")
        css.AppendLine("#vendorModalDialog.vendor-modal-frame-only #vendorPopupFrame {")
        css.AppendLine("    flex: 1 1 100%;")
        css.AppendLine("    height: 100%;")
        css.AppendLine("}")
        css.AppendLine("</style>")

        If page.Header IsNot Nothing Then
            page.Header.Controls.Add(New LiteralControl(css.ToString()))
        ElseIf page.Form IsNot Nothing Then
            page.Form.Controls.AddAt(0, New LiteralControl(css.ToString()))
        Else
            page.Controls.Add(New LiteralControl(css.ToString()))
        End If

        page.Items(StylesRegisteredKey) = True
    End Sub

    Private Sub RegisterVendorPopupMarkup(ByVal page As Page)
        If page.Items(MarkupRegisteredKey) IsNot Nothing Then Exit Sub

        Dim markup As New StringBuilder()

        markup.AppendLine("<div id=""vendorModalOverlay"" onclick=""return vendorPopupOverlayClick(event);"">")
        markup.AppendLine("    <div id=""vendorModalDialog"" role=""dialog"" aria-modal=""true"" aria-labelledby=""vendorModalTitle"" onclick=""if (event.stopPropagation) event.stopPropagation(); event.cancelBubble = true;"">")
        markup.AppendLine("        <div class=""vendor-modal-header"">")
        markup.AppendLine("            <span id=""vendorModalTitle"" class=""vendor-modal-title"">Select Vendor</span>")
        markup.AppendLine("            <button type=""button"" class=""btn-close-x"" onclick=""return closeVendorDialog();"" aria-label=""Close popup"">&#10005;</button>")
        markup.AppendLine("        </div>")
        markup.AppendLine("        <iframe id=""vendorPopupFrame"" src=""about:blank""></iframe>")
        markup.AppendLine("        <div class=""vendor-modal-footer"">")
        markup.AppendLine("            <button type=""button"" class=""btn-close"" onclick=""return closeVendorDialog();"">Cancel</button>")
        markup.AppendLine("        </div>")
        markup.AppendLine("    </div>")
        markup.AppendLine("</div>")

        If page.Form IsNot Nothing Then
            page.Form.Controls.Add(New LiteralControl(markup.ToString()))
        Else
            page.Controls.Add(New LiteralControl(markup.ToString()))
        End If

        page.Items(MarkupRegisteredKey) = True
    End Sub

    Private Sub RegisterVendorPopupScript(ByVal page As Page)
        If page.Items(ScriptRegisteredKey) IsNot Nothing Then Exit Sub

        Dim js As New StringBuilder()

        js.AppendLine("var vendorPopupContext = {")
        js.AppendLine("    postBackId: '',")
        js.AppendLine("    valueFieldId: '',")
        js.AppendLine("    textFieldId: '',")
        js.AppendLine("    displayValueControlId: '',")
        js.AppendLine("    displayTextControlId: '',")
        js.AppendLine("    displayMode: 'Standard'")
        js.AppendLine("};")
        js.AppendLine("")
        js.AppendLine("function vendorPopupGet(id) {")
        js.AppendLine("    if (!id) return null;")
        js.AppendLine("    return document.getElementById(id);")
        js.AppendLine("}")
        js.AppendLine("")
        js.AppendLine("function vendorPopupGetStateStorageKey() {")
        js.AppendLine("    return 'VendorPopupPersistentState:' + ((window.location && window.location.pathname) ? window.location.pathname : 'default');")
        js.AppendLine("}")
        js.AppendLine("")
        js.AppendLine("function vendorPopupLoadPersistentState() {")
        js.AppendLine("    try {")
        js.AppendLine("        if (window.sessionStorage) {")
        js.AppendLine("            var raw = window.sessionStorage.getItem(vendorPopupGetStateStorageKey());")
        js.AppendLine("            return raw ? JSON.parse(raw) : {};")
        js.AppendLine("        }")
        js.AppendLine("    } catch (ex) { }")
        js.AppendLine("    return {};")
        js.AppendLine("}")
        js.AppendLine("")
        js.AppendLine("function vendorPopupSavePersistentState(state) {")
        js.AppendLine("    try {")
        js.AppendLine("        if (window.sessionStorage) {")
        js.AppendLine("            window.sessionStorage.setItem(vendorPopupGetStateStorageKey(), JSON.stringify(state || {}));")
        js.AppendLine("        }")
        js.AppendLine("    } catch (ex) { }")
        js.AppendLine("}")
        js.AppendLine("")
        js.AppendLine("function vendorPopupFindField(fieldKey) {")
        js.AppendLine("    if (!fieldKey || !document) return null;")
        js.AppendLine("")
        js.AppendLine("    var target = document.getElementById(fieldKey);")
        js.AppendLine("    var i;")
        js.AppendLine("")
        js.AppendLine("    if (target) return target;")
        js.AppendLine("")
        js.AppendLine("    var inputs = document.getElementsByTagName('input');")
        js.AppendLine("    for (i = 0; i < inputs.length; i++) {")
        js.AppendLine("        if ((inputs[i].id && new RegExp(fieldKey + '$', 'i').test(inputs[i].id)) ||")
        js.AppendLine("            (inputs[i].name && new RegExp(fieldKey + '$', 'i').test(inputs[i].name))) {")
        js.AppendLine("            return inputs[i];")
        js.AppendLine("        }")
        js.AppendLine("    }")
        js.AppendLine("")
        js.AppendLine("    var selects = document.getElementsByTagName('select');")
        js.AppendLine("    for (i = 0; i < selects.length; i++) {")
        js.AppendLine("        if ((selects[i].id && new RegExp(fieldKey + '$', 'i').test(selects[i].id)) ||")
        js.AppendLine("            (selects[i].name && new RegExp(fieldKey + '$', 'i').test(selects[i].name))) {")
        js.AppendLine("            return selects[i];")
        js.AppendLine("        }")
        js.AppendLine("    }")
        js.AppendLine("")
        js.AppendLine("    var textareas = document.getElementsByTagName('textarea');")
        js.AppendLine("    for (i = 0; i < textareas.length; i++) {")
        js.AppendLine("        if ((textareas[i].id && new RegExp(fieldKey + '$', 'i').test(textareas[i].id)) ||")
        js.AppendLine("            (textareas[i].name && new RegExp(fieldKey + '$', 'i').test(textareas[i].name))) {")
        js.AppendLine("            return textareas[i];")
        js.AppendLine("        }")
        js.AppendLine("    }")
        js.AppendLine("")
        js.AppendLine("    return null;")
        js.AppendLine("}")
        js.AppendLine("")
        js.AppendLine("function vendorPopupAssignFieldValue(target, value) {")
        js.AppendLine("    if (!target) return;")
        js.AppendLine("")
        js.AppendLine("    if (typeof target.value !== 'undefined') {")
        js.AppendLine("        target.value = value;")
        js.AppendLine("        if (target.setAttribute) target.setAttribute('value', value);")
        js.AppendLine("    } else {")
        js.AppendLine("        target.innerHTML = value;")
        js.AppendLine("    }")
        js.AppendLine("")
        js.AppendLine("    if (document.createEvent) {")
        js.AppendLine("        var inputEvent = document.createEvent('HTMLEvents');")
        js.AppendLine("        inputEvent.initEvent('input', true, true);")
        js.AppendLine("        target.dispatchEvent(inputEvent);")
        js.AppendLine("        var changeEvent = document.createEvent('HTMLEvents');")
        js.AppendLine("        changeEvent.initEvent('change', true, true);")
        js.AppendLine("        target.dispatchEvent(changeEvent);")
        js.AppendLine("    } else if (target.fireEvent) {")
        js.AppendLine("        target.fireEvent('onchange');")
        js.AppendLine("    }")
        js.AppendLine("}")
        js.AppendLine("")
        js.AppendLine("function vendorPopupPersistFieldValue(fieldKey, value) {")
        js.AppendLine("    if (!fieldKey) return;")
        js.AppendLine("    var state = vendorPopupLoadPersistentState();")
        js.AppendLine("    state[fieldKey] = value || '';")
        js.AppendLine("    vendorPopupSavePersistentState(state);")
        js.AppendLine("    vendorPopupAssignFieldValue(vendorPopupFindField(fieldKey), value || '');")
        js.AppendLine("}")
        js.AppendLine("")
        js.AppendLine("function vendorPopupRestorePersistentValues() {")
        js.AppendLine("    var state = vendorPopupLoadPersistentState();")
        js.AppendLine("    var key;")
        js.AppendLine("    for (key in state) {")
        js.AppendLine("        if (state.hasOwnProperty(key)) {")
        js.AppendLine("            vendorPopupAssignFieldValue(vendorPopupFindField(key), state[key] || '');")
        js.AppendLine("        }")
        js.AppendLine("    }")
        js.AppendLine("}")
        js.AppendLine("")
        js.AppendLine("function vendorPopupSetPlacement(placement) {")
        js.AppendLine("    var dialog = document.getElementById('vendorModalDialog');")
        js.AppendLine("    if (!dialog) return;")
        js.AppendLine("")
        js.AppendLine("    if (placement === 'RightSide') {")
        js.AppendLine("        dialog.style.top = '20px';")
        js.AppendLine("        dialog.style.left = 'auto';")
        js.AppendLine("        dialog.style.right = '20px';")
        js.AppendLine("        dialog.style.transform = 'none';")
        js.AppendLine("    } else {")
        js.AppendLine("        dialog.style.top = '50%';")
        js.AppendLine("        dialog.style.left = '50%';")
        js.AppendLine("        dialog.style.right = 'auto';")
        js.AppendLine("        dialog.style.transform = 'translate(-50%, -50%)';")
        js.AppendLine("    }")
        js.AppendLine("}")
        js.AppendLine("")
        js.AppendLine("function vendorPopupApplyDisplayMode(displayMode) {")
        js.AppendLine("    var dialog = document.getElementById('vendorModalDialog');")
        js.AppendLine("    if (!dialog) return;")
        js.AppendLine("")
        js.AppendLine("    if (displayMode === 'FrameOnly') {")
        js.AppendLine("        dialog.className = 'vendor-modal-frame-only';")
        js.AppendLine("    } else {")
        js.AppendLine("        dialog.className = '';")
        js.AppendLine("    }")
        js.AppendLine("}")
        js.AppendLine("")
        js.AppendLine("function vendorPopupOverlayClick(e) {")
        js.AppendLine("    if ((vendorPopupContext.displayMode || 'Standard') === 'FrameOnly') {")
        js.AppendLine("        return false;")
        js.AppendLine("    }")
        js.AppendLine("    return closeVendorDialog();")
        js.AppendLine("}")
        js.AppendLine("")
        js.AppendLine("function openVendorDialog(popupUrl, popupTitle, popupWidth, popupHeight, placement, postBackId, valueFieldId, textFieldId, displayValueControlId, displayTextControlId, displayMode) {")
        js.AppendLine("    var overlay = document.getElementById('vendorModalOverlay');")
        js.AppendLine("    var dialog = document.getElementById('vendorModalDialog');")
        js.AppendLine("    var title = document.getElementById('vendorModalTitle');")
        js.AppendLine("    var frame = document.getElementById('vendorPopupFrame');")
        js.AppendLine("")
        js.AppendLine("    if (!overlay || !dialog || !title || !frame) return false;")
        js.AppendLine("")
        js.AppendLine("    vendorPopupContext.postBackId = postBackId || '';")
        js.AppendLine("    vendorPopupContext.valueFieldId = valueFieldId || '';")
        js.AppendLine("    vendorPopupContext.textFieldId = textFieldId || '';")
        js.AppendLine("    vendorPopupContext.displayValueControlId = displayValueControlId || '';")
        js.AppendLine("    vendorPopupContext.displayTextControlId = displayTextControlId || '';")
        js.AppendLine("    vendorPopupContext.displayMode = displayMode || 'Standard';")
        js.AppendLine("")
        js.AppendLine("    title.innerHTML = popupTitle || 'Select Vendor';")
        js.AppendLine("")
        js.AppendLine("    if (popupWidth && popupWidth > 0) {")
        js.AppendLine("        dialog.style.width = popupWidth + 'px';")
        js.AppendLine("    } else {")
        js.AppendLine("        dialog.style.width = '95vw';")
        js.AppendLine("    }")
        js.AppendLine("")
        js.AppendLine("    if (popupHeight && popupHeight > 0) {")
        js.AppendLine("        dialog.style.height = popupHeight + 'px';")
        js.AppendLine("    } else {")
        js.AppendLine("        dialog.style.height = '95vh';")
        js.AppendLine("    }")
        js.AppendLine("")
        js.AppendLine("    vendorPopupSetPlacement(placement || 'Center');")
        js.AppendLine("    vendorPopupApplyDisplayMode(vendorPopupContext.displayMode);")
        js.AppendLine("")
        js.AppendLine("    frame.src = popupUrl || 'about:blank';")
        js.AppendLine("    overlay.style.display = 'block';")
        js.AppendLine("")
        js.AppendLine("    if (document.body) {")
        js.AppendLine("        document.body.style.overflow = 'hidden';")
        js.AppendLine("    }")
        js.AppendLine("")
        js.AppendLine("    return false;")
        js.AppendLine("}")
        js.AppendLine("")
        js.AppendLine("function closeVendorDialog() {")
        js.AppendLine("    var overlay = document.getElementById('vendorModalOverlay');")
        js.AppendLine("    var frame = document.getElementById('vendorPopupFrame');")
        js.AppendLine("")
        js.AppendLine("    if (overlay) overlay.style.display = 'none';")
        js.AppendLine("    if (frame) frame.src = 'about:blank';")
        js.AppendLine("")
        js.AppendLine("    if (document.body) {")
        js.AppendLine("        document.body.style.overflow = '';")
        js.AppendLine("    }")
        js.AppendLine("")
        js.AppendLine("    return false;")
        js.AppendLine("}")
        js.AppendLine("")
        js.AppendLine("function receiveVendorValue(selectedValue, displayText, skipPostBack) {")
        js.AppendLine("    var selected = selectedValue || '';")
        js.AppendLine("    var text = displayText || selected;")
        js.AppendLine("")
        js.AppendLine("    var valueField = vendorPopupGet(vendorPopupContext.valueFieldId);")
        js.AppendLine("    var textField = vendorPopupGet(vendorPopupContext.textFieldId);")
        js.AppendLine("    var displayValueControl = vendorPopupGet(vendorPopupContext.displayValueControlId);")
        js.AppendLine("    var displayTextControl = vendorPopupGet(vendorPopupContext.displayTextControlId);")
        js.AppendLine("")
        js.AppendLine("    if (vendorPopupContext.valueFieldId) {")
        js.AppendLine("        vendorPopupPersistFieldValue(vendorPopupContext.valueFieldId, selected);")
        js.AppendLine("    } else if (valueField) {")
        js.AppendLine("        valueField.value = selected;")
        js.AppendLine("        if (valueField.setAttribute) valueField.setAttribute('value', selected);")
        js.AppendLine("    }")
        js.AppendLine("    if (vendorPopupContext.textFieldId) {")
        js.AppendLine("        vendorPopupPersistFieldValue(vendorPopupContext.textFieldId, text);")
        js.AppendLine("    } else if (textField) {")
        js.AppendLine("        textField.value = text;")
        js.AppendLine("        if (textField.setAttribute) textField.setAttribute('value', text);")
        js.AppendLine("    }")
        js.AppendLine("    if (vendorPopupContext.displayValueControlId) {")
        js.AppendLine("        vendorPopupPersistFieldValue(vendorPopupContext.displayValueControlId, selected);")
        js.AppendLine("    } else if (displayValueControl) {")
        js.AppendLine("        vendorPopupAssignFieldValue(displayValueControl, selected);")
        js.AppendLine("    }")
        js.AppendLine("    if (vendorPopupContext.displayTextControlId) {")
        js.AppendLine("        vendorPopupPersistFieldValue(vendorPopupContext.displayTextControlId, text);")
        js.AppendLine("    } else if (displayTextControl) {")
        js.AppendLine("        vendorPopupAssignFieldValue(displayTextControl, text);")
        js.AppendLine("    }")
        js.AppendLine("")
        js.AppendLine("    closeVendorDialog();")
        js.AppendLine("")
        js.AppendLine("    if (skipPostBack === true) {")
        js.AppendLine("        return;")
        js.AppendLine("    }")
        js.AppendLine("")
        js.AppendLine("    if (typeof __doPostBack === 'function' && vendorPopupContext.postBackId) {")
        js.AppendLine("        __doPostBack(vendorPopupContext.postBackId, '');")
        js.AppendLine("    }")
        js.AppendLine("}")
        js.AppendLine("")
        js.AppendLine("if (!window.vendorPopupRestoreHandlerRegistered) {")
        js.AppendLine("    window.vendorPopupRestoreHandlerRegistered = true;")
        js.AppendLine("    if (document.addEventListener) {")
        js.AppendLine("        document.addEventListener('DOMContentLoaded', vendorPopupRestorePersistentValues);")
        js.AppendLine("    }")
        js.AppendLine("    if (window.addEventListener) {")
        js.AppendLine("        window.addEventListener('load', vendorPopupRestorePersistentValues);")
        js.AppendLine("    }")
        js.AppendLine("}")
        js.AppendLine("")
        js.AppendLine("vendorPopupRestorePersistentValues();")
        js.AppendLine("")
        js.AppendLine("if (!window.vendorPopupEscapeHandlerRegistered) {")
        js.AppendLine("    window.vendorPopupEscapeHandlerRegistered = true;")
        js.AppendLine("    if (document.addEventListener) {")
        js.AppendLine("        document.addEventListener('keydown', function (e) {")
        js.AppendLine("            e = e || window.event;")
        js.AppendLine("            var key = e.key || e.keyCode;")
        js.AppendLine("            var overlay = document.getElementById('vendorModalOverlay');")
        js.AppendLine("            if (overlay && overlay.style.display === 'block' && (key === 'Escape' || key === 'Esc' || key === 27)) {")
        js.AppendLine("                if ((vendorPopupContext.displayMode || 'Standard') !== 'FrameOnly') {")
        js.AppendLine("                    closeVendorDialog();")
        js.AppendLine("                }")
        js.AppendLine("            }")
        js.AppendLine("        });")
        js.AppendLine("    }")
        js.AppendLine("}")

        If ScriptManager.GetCurrent(page) IsNot Nothing Then
            ScriptManager.RegisterClientScriptBlock(page, page.GetType(), "VendorPopupScript", js.ToString(), True)
        Else
            page.ClientScript.RegisterClientScriptBlock(page.GetType(), "VendorPopupScript", js.ToString(), True)
        End If

        page.Items(ScriptRegisteredKey) = True
    End Sub

    Private Function BuildOpenDialogScript(ByVal popupUrl As String,
                                           ByVal popupTitle As String,
                                           ByVal popupWidth As Integer,
                                           ByVal popupHeight As Integer,
                                           ByVal placement As PopupPlacement,
                                           ByVal postBackUniqueId As String,
                                           ByVal selectedVendorValueClientId As String,
                                           ByVal selectedVendorTextClientId As String,
                                           ByVal displayValueControlClientId As String,
                                           ByVal displayTextControlClientId As String,
                                           ByVal displayMode As PopupDisplayMode) As String

        ' A width/height of 0 (or less) means "not specified" — the client script
        ' will expand that dimension to the maximum available size (95vw / 95vh)
        ' instead of falling back to a fixed pixel default.
        If popupWidth < 0 Then popupWidth = 0
        If popupHeight < 0 Then popupHeight = 0

        Dim script As New StringBuilder()

        script.Append("return openVendorDialog('")
        script.Append(JsEncode(popupUrl))
        script.Append("','")
        script.Append(JsEncode(popupTitle))
        script.Append("',")
        script.Append(popupWidth.ToString())
        script.Append(",")
        script.Append(popupHeight.ToString())
        script.Append(",'")
        script.Append(placement.ToString())
        script.Append("','")
        script.Append(JsEncode(postBackUniqueId))
        script.Append("','")
        script.Append(JsEncode(selectedVendorValueClientId))
        script.Append("','")
        script.Append(JsEncode(selectedVendorTextClientId))
        script.Append("','")
        script.Append(JsEncode(displayValueControlClientId))
        script.Append("','")
        script.Append(JsEncode(displayTextControlClientId))
        script.Append("','")
        script.Append(displayMode.ToString())
        script.Append("');")

        Return script.ToString()
    End Function

    Public Sub RegisterPopupSelectionAndClose(ByVal page As Page,
                                              ByVal selectedValue As String,
                                              ByVal selectedText As String,
                                              Optional ByVal additionalFieldKey As String = "",
                                              Optional ByVal additionalFieldValue As String = "",
                                              Optional ByVal startupScriptKey As String = "VendorPopupSelectionAndClose",
                                              Optional ByVal skipPostBack As Boolean = True)

        If page Is Nothing Then Throw New ArgumentNullException("page")

        Dim script As String = BuildPopupSelectionAndCloseScript(selectedValue,
                                                                 selectedText,
                                                                 additionalFieldKey,
                                                                 additionalFieldValue,
                                                                 skipPostBack)

        If ScriptManager.GetCurrent(page) IsNot Nothing Then
            ScriptManager.RegisterStartupScript(page, page.GetType(), startupScriptKey, script, True)
        Else
            page.ClientScript.RegisterStartupScript(page.GetType(), startupScriptKey, script, True)
        End If
    End Sub

    Public Sub RegisterPopupSelectionAndClose(ByVal page As Page,
                                              ByVal returnValue As Object,
                                              Optional ByVal startupScriptKey As String = "VendorPopupSelectionAndClose",
                                              Optional ByVal skipPostBack As Boolean = True)

        If page Is Nothing Then Throw New ArgumentNullException("page")

        SavePopupReturnValue(page, startupScriptKey, returnValue)

        Dim script As String = BuildPopupCloseAndPostBackScript(skipPostBack)

        If ScriptManager.GetCurrent(page) IsNot Nothing Then
            ScriptManager.RegisterStartupScript(page, page.GetType(), startupScriptKey, script, True)
        Else
            page.ClientScript.RegisterStartupScript(page.GetType(), startupScriptKey, script, True)
        End If
    End Sub

    Public Function GetPopupReturnValue(ByVal page As Page,
                                        Optional ByVal startupScriptKey As String = "VendorPopupSelectionAndClose",
                                        Optional ByVal clearAfterRead As Boolean = True) As Object

        If page Is Nothing Then Throw New ArgumentNullException("page")

        Dim sessionKey As String = BuildPopupReturnValueSessionKey(startupScriptKey)
        Dim value As Object = Nothing

        If page.Session IsNot Nothing Then
            value = page.Session(sessionKey)
            If clearAfterRead Then
                page.Session.Remove(sessionKey)
            End If
        End If

        Return value
    End Function

    Private Sub SavePopupReturnValue(ByVal page As Page,
                                     ByVal startupScriptKey As String,
                                     ByVal returnValue As Object)
        If page Is Nothing OrElse page.Session Is Nothing Then Exit Sub

        page.Session(BuildPopupReturnValueSessionKey(startupScriptKey)) = returnValue
    End Sub

    Private Function BuildPopupReturnValueSessionKey(ByVal startupScriptKey As String) As String
        If String.IsNullOrWhiteSpace(startupScriptKey) Then
            startupScriptKey = "VendorPopupSelectionAndClose"
        End If

        Return ReturnValueSessionKeyPrefix & startupScriptKey.Trim()
    End Function

    Private Function BuildPopupCloseAndPostBackScript(ByVal skipPostBack As Boolean) As String
        Dim sb As New StringBuilder()

        sb.AppendLine("(function () {")
        sb.AppendLine("    if (!window.parent) return;")
        sb.AppendLine("")
        sb.AppendLine("    if (typeof window.parent.closeVendorDialog === 'function') {")
        sb.AppendLine("        window.parent.closeVendorDialog();")
        sb.AppendLine("    }")
        sb.AppendLine("")
        sb.AppendLine("    if (" & LCase(skipPostBack.ToString()) & ") {")
        sb.AppendLine("        return;")
        sb.AppendLine("    }")
        sb.AppendLine("")
        sb.AppendLine("    if (typeof window.parent.__doPostBack === 'function' &&")
        sb.AppendLine("        window.parent.vendorPopupContext &&")
        sb.AppendLine("        window.parent.vendorPopupContext.postBackId) {")
        sb.AppendLine("        window.parent.__doPostBack(window.parent.vendorPopupContext.postBackId, '');")
        sb.AppendLine("    }")
        sb.AppendLine("})();")

        Return sb.ToString()
    End Function

    Private Function BuildPopupSelectionAndCloseScript(ByVal selectedValue As String,
                                                       ByVal selectedText As String,
                                                       ByVal additionalFieldKey As String,
                                                       ByVal additionalFieldValue As String,
                                                       ByVal skipPostBack As Boolean) As String
        Dim sb As New StringBuilder()

        sb.AppendLine("(function () {")
        sb.AppendLine("    if (!window.parent) return;")
        sb.AppendLine("")
        sb.AppendLine("    if ('" & JsEncode(additionalFieldKey) & "' !== '' && typeof window.parent.vendorPopupPersistFieldValue === 'function') {")
        sb.AppendLine("        window.parent.vendorPopupPersistFieldValue('" & JsEncode(additionalFieldKey) & "', '" & JsEncode(additionalFieldValue) & "');")
        sb.AppendLine("    }")
        sb.AppendLine("")
        sb.AppendLine("    if (typeof window.parent.receiveVendorValue === 'function') {")
        sb.AppendLine("        window.parent.receiveVendorValue('" & JsEncode(selectedValue) & "', '" & JsEncode(selectedText) & "', " & LCase(skipPostBack.ToString()) & ");")
        sb.AppendLine("        return;")
        sb.AppendLine("    }")
        sb.AppendLine("")
        sb.AppendLine("    if (typeof window.parent.closeVendorDialog === 'function') {")
        sb.AppendLine("        window.parent.closeVendorDialog();")
        sb.AppendLine("    }")
        sb.AppendLine("})();")

        Return sb.ToString()
    End Function

    Private Function ResolvePopupUrl(ByVal page As Page, ByVal popupPageUrl As String) As String
        If popupPageUrl Is Nothing Then Return "about:blank"

        popupPageUrl = popupPageUrl.Trim()
        If popupPageUrl = String.Empty Then Return "about:blank"

        Return page.ResolveClientUrl(popupPageUrl)
    End Function

    Private Function JsEncode(ByVal value As String) As String
        If value Is Nothing Then Return String.Empty

        Return value.Replace("\", "\\") _
                    .Replace("'", "\'") _
                    .Replace(vbCrLf, "\n") _
                    .Replace(vbCr, "\n") _
                    .Replace(vbLf, "\n")
    End Function

End Module
