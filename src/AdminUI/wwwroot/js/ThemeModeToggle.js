// Theme toggle functionality
(function () {
    const THEME_STORAGE_KEY = 'theme';
    const STYLE_OVERRIDE_ID = 'shui-fluent-runtime-overrides';

    function ensureRuntimeStyleOverrides() {
        if (document.getElementById(STYLE_OVERRIDE_ID)) {
            return;
        }

        const style = document.createElement('style');
        style.id = STYLE_OVERRIDE_ID;
        style.textContent = `
fluent-button[appearance="accent"]::part(control) {
    background: rgb(var(--ui-3)) !important;
    border: 1px solid rgb(var(--ui-3)) !important;
    color: #ffffff !important;
}
fluent-button[appearance="accent"]:hover::part(control) {
    background: rgb(var(--ui-2)) !important;
    border-color: rgb(var(--ui-2)) !important;
}
fluent-button[appearance="accent"]:active::part(control) {
    background: rgb(var(--ui-4)) !important;
    border-color: rgb(var(--ui-4)) !important;
}
fluent-button[appearance="accent"]::part(control):disabled {
    background: rgba(var(--ui-1), var(--opacity-200)) !important;
    border-color: rgba(var(--ui-1), var(--opacity-200)) !important;
    color: rgba(var(--ui-1), var(--opacity-500)) !important;
}
`;

        document.head.appendChild(style);
    }

    function getCurrentTheme() {
        return localStorage.getItem(THEME_STORAGE_KEY);
    }

    function getPreferredTheme() {
        return getCurrentTheme() || getSystemTheme();
    }

    function setTheme(theme, storeInLocalStorage = false) {
        const systemTheme = getSystemTheme();

        // If theme matches system theme, remove from localStorage
        if (theme === systemTheme) {
            localStorage.removeItem(THEME_STORAGE_KEY);
        }
        // Otherwise store in localStorage if explicitly requested
        else if (storeInLocalStorage) {
            localStorage.setItem(THEME_STORAGE_KEY, theme);
        }

        // Keep both attributes in sync so Fluent and SHUI-style tokens switch together.
        document.documentElement.setAttribute('data-theme', theme);
        document.documentElement.setAttribute('sh-color', theme);
        document.documentElement.style.colorScheme = theme;
        document.documentElement.style.backgroundColor = theme === 'dark' ? '#333333' : '#FFFFFF';

        // Find and update the fluent-design-theme element if it exists
        const fluentThemeElement = document.querySelector('fluent-design-theme');
        if (fluentThemeElement) {
            fluentThemeElement.setAttribute('mode', theme);
            if (typeof fluentThemeElement.dispatchEvent === 'function') {
                const event = new CustomEvent('themeChanged', {
                    detail: { theme: theme },
                    bubbles: true
                });
                fluentThemeElement.dispatchEvent(event);
            }
        }
    }

    function getSystemTheme() {
        return window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
    }

    function setupSystemThemeListener() {
        if (window.matchMedia) {
            const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
            if (mediaQuery.addEventListener) {
                mediaQuery.addEventListener('change', function (e) {
                    const newSystemTheme = e.matches ? 'dark' : 'light';
                    const savedTheme = getCurrentTheme();

                    if (savedTheme) {
                        // If saved theme now matches system theme, remove from localStorage
                        if (savedTheme === newSystemTheme) {
                            localStorage.removeItem(THEME_STORAGE_KEY);
                        }
                        // Apply saved theme without changing localStorage
                        setTheme(savedTheme, false);
                    } else {
                        // Apply system theme without storing in localStorage
                        setTheme(newSystemTheme, false);
                    }
                });
            }
        }
    }

    function setupToggleButton() {
        const themeToggleBtn = document.getElementById('theme-toggle-btn');
        if (!themeToggleBtn) return;

        themeToggleBtn.addEventListener('click', function () {
            // Get current theme from localStorage, document, or system
            const currentTheme = getCurrentTheme() ||
                document.documentElement.getAttribute('data-theme') ||
                getSystemTheme();
            const newTheme = currentTheme === 'dark' ? 'light' : 'dark';
            // User explicitly clicked, so check against system theme
            const systemTheme = getSystemTheme();

            if (newTheme === systemTheme) {
                // If new theme matches system, remove from localStorage and apply
                localStorage.removeItem(THEME_STORAGE_KEY);
                setTheme(newTheme, false);
            } else {
                // Otherwise persist in localStorage
                setTheme(newTheme, true);
            }
        });
    }

    // Main initialization function
    function initializeTheme() {
        ensureRuntimeStyleOverrides();

        // Check if user has a saved preference
        const savedTheme = getCurrentTheme();
        const systemTheme = getSystemTheme();

        if (savedTheme) {
            // If saved theme matches system theme, remove from localStorage
            if (savedTheme === systemTheme) {
                localStorage.removeItem(THEME_STORAGE_KEY);
            }
            setTheme(savedTheme, false);
        } else {
            setTheme(systemTheme, false);
        }

        setupToggleButton();
        setupSystemThemeListener();
    }

    // MutationObserver to watch for dynamic changes to the DOM
    function setupMutationObserver() {
        const htmlObserver = new MutationObserver(function () {
            const preferredTheme = getPreferredTheme();
            const currentDataTheme = document.documentElement.getAttribute('data-theme');
            const currentShColor = document.documentElement.getAttribute('sh-color');

            if (currentDataTheme !== preferredTheme || currentShColor !== preferredTheme) {
                setTheme(preferredTheme, false);
            }
        });

        htmlObserver.observe(document.documentElement, {
            attributes: true,
            attributeFilter: ['data-theme', 'sh-color']
        });

        // Watch for when the fluent-design-theme element appears or changes
        const observer = new MutationObserver(function (mutations) {
            mutations.forEach(function (mutation) {
                if (mutation.type === 'childList') {
                    const fluentThemeElement = document.querySelector('fluent-design-theme');
                    if (fluentThemeElement) {
                        const preferredTheme = getPreferredTheme();
                        if (fluentThemeElement.getAttribute('mode') !== preferredTheme) {
                            // Re-apply theme if necessary, without re-storing
                            setTheme(preferredTheme, false);
                        }
                    }
                }
            });
        });
        observer.observe(document.body, { childList: true, subtree: true });
    }

    // Setup for Blazor navigation
    function setupBlazorNavigation() {
        if (window.Blazor) {
            window.addEventListener('click', function (e) {
                const anchor = e.target.closest ? e.target.closest('a') : null;

                if (anchor && anchor.classList.contains('shui-brand-link')) {
                    const targetUrl = new URL(anchor.href, window.location.origin);
                    const targetPath = targetUrl.pathname.replace(/\/+$/, '') || '/';
                    const currentPath = window.location.pathname.replace(/\/+$/, '') || '/';

                    if (targetUrl.origin === window.location.origin && targetPath === currentPath) {
                        e.preventDefault();
                        e.stopPropagation();
                        setTheme(getPreferredTheme(), false);
                        return;
                    }
                }

                // Detect anchor clicks or navigation
                if ((e.target.tagName === 'A' || anchor || e.target.dataset.navLink)) {
                    const preferredTheme = getPreferredTheme();
                    setTheme(preferredTheme, false);
                    requestAnimationFrame(function () {
                        setTheme(preferredTheme, false);
                    });
                }
            }, true);
        }
    }

    // Initialize everything when the document is ready
    function documentReady(fn) {
        if (document.readyState === 'complete' || document.readyState === 'interactive') {
            setTimeout(fn, 1);
        } else {
            document.addEventListener('DOMContentLoaded', fn);
        }
    }

    // Start everything up
    documentReady(function () {
        initializeTheme();
        setupMutationObserver();
        setupBlazorNavigation();

        if (window.Blazor) {
            // After Blazor starts, ensure theme is applied
            window.Blazor.addEventListener('afterStarted', function () {
                setTimeout(function () {
                    setTheme(getPreferredTheme(), false);
                }, 50);
            });
        }
    });
})();
