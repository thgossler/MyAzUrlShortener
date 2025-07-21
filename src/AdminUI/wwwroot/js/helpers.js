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

// Listen for ESC keydown anywhere on the page and call a .NET method
window.registerEscKeyHandler = function (dotNetHelper) {
    function handler(e) {
        if (e.key === 'Escape') {
            dotNetHelper.invokeMethodAsync('OnEscPressed');
        }
    }
    window.__escKeyHandler = handler;
    document.addEventListener('keydown', handler);
};

window.unregisterEscKeyHandler = function () {
    if (window.__escKeyHandler) {
        document.removeEventListener('keydown', window.__escKeyHandler);
        window.__escKeyHandler = null;
    }
};

window.getViewportWidth = () => {
    return window.innerWidth;
};

window.getViewportHeight = () => {
    return window.innerHeight;
};

window.getNavMenuWidth = () => {
    try {
        return document.getElementsByClassName('body-content')[0].getBoundingClientRect().x;
    } catch (e) {
        return 200;
    }
};

window.registerViewportResizeHandler = function(dotNetHelper) {
    if (window.__viewportResizeHandler) {
        window.removeEventListener('resize', window.__viewportResizeHandler);
        window.removeEventListener('orientationchange', window.__viewportResizeHandler);
    }
    window.__viewportResizeHandler = function() {
        dotNetHelper.invokeMethodAsync('OnViewportResize', window.innerWidth, window.innerHeight);
    };
    window.addEventListener('resize', window.__viewportResizeHandler);
    window.addEventListener('orientationchange', window.__viewportResizeHandler);
};
