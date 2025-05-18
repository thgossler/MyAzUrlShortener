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
