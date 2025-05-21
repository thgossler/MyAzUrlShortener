window.getViewportWidth = () => {
    return window.innerWidth;
};

window.disableFluentDialogFocusTrap = function (dialogId) {
    const dialog = document.getElementById(dialogId) || document.querySelector(dialogId);
    if (!dialog) return;
    dialog.trapFocus = false;
    dialog.isTrappingFocus = false;
    dialog.updateTrapFocus(false);
};

window.enableFluentDialogFocusTrap = function (dialogId) {
    const dialog = document.getElementById(dialogId) || document.querySelector(dialogId);
    if (!dialog) return;
    dialog.trapFocus = true;
    dialog.isTrappingFocus = true;
    dialog.updateTrapFocus(true);
};

// Get the scrollTop of a DataGrid by id
window.getDataGridScrollTop = function (gridId) {
    var grid = document.getElementById(gridId);
    if (!grid) return 0;
    var tbody = grid.querySelector('tbody');
    return tbody ? tbody.scrollTop : 0;
};

// Set the scrollTop of a DataGrid by id
window.setDataGridScrollTop = function (gridId, scrollTop) {
    var grid = document.getElementById(gridId);
    if (!grid) return;
    var tbody = grid.querySelector('tbody');
    if (tbody) tbody.scrollTop = scrollTop;
};
