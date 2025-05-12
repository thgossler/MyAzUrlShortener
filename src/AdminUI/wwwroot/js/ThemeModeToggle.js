// Theme toggle functionality
(function () {
    const THEME_STORAGE_KEY = 'theme';

    function getCurrentTheme() {
        return localStorage.getItem(THEME_STORAGE_KEY);
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

        // Set data-theme attribute on document for CSS selectors
        document.documentElement.setAttribute('data-theme', theme);

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
        // Watch for when the fluent-design-theme element appears or changes
        const observer = new MutationObserver(function (mutations) {
            mutations.forEach(function (mutation) {
                if (mutation.type === 'childList') {
                    const fluentThemeElement = document.querySelector('fluent-design-theme');
                    if (fluentThemeElement) {
                        const savedTheme = getCurrentTheme();
                        if (savedTheme && fluentThemeElement.getAttribute('mode') !== savedTheme) {
                            // Re-apply theme if necessary, without re-storing
                            setTheme(savedTheme, false);
                        } else if (!savedTheme) {
                            // If no saved theme, apply system theme
                            setTheme(getSystemTheme(), false);
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
                // Detect anchor clicks or navigation
                if (e.target.tagName === 'A' ||
                    e.target.closest('a') ||
                    e.target.dataset.navLink) {
                    setTimeout(function () {
                        const savedTheme = getCurrentTheme();
                        if (savedTheme) {
                            setTheme(savedTheme, false);
                        } else {
                            setTheme(getSystemTheme(), false);
                        }
                    }, 100);
                }
            });
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
                    const savedTheme = getCurrentTheme();
                    const systemTheme = getSystemTheme();

                    if (savedTheme) {
                        // Check if saved theme matches system theme
                        if (savedTheme === systemTheme) {
                            localStorage.removeItem(THEME_STORAGE_KEY);
                        }
                        setTheme(savedTheme, false);
                    } else {
                        setTheme(systemTheme, false);
                    }
                }, 50);
            });
        }
    });
})();
