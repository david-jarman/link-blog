// Theme & Style toggle with localStorage persistence.
// Theme: light / dark (follows system preference if not set).
// Style: modern → classic → warm (cycles on button click).
(function () {
    'use strict';

    const THEME_KEY = 'theme-preference';
    const STYLE_KEY = 'style-preference';
    const STYLES = ['modern', 'classic', 'warm'];

    // ── Theme ────────────────────────────────────────────────

    function getThemePreference() {
        return localStorage.getItem(THEME_KEY) ||
            (window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light');
    }

    function setTheme(theme) {
        document.documentElement.setAttribute('data-theme', theme);
        localStorage.setItem(THEME_KEY, theme);
        updateThemeIcon(theme);
    }

    function updateThemeIcon(theme) {
        const sunIcon  = document.querySelector('.sun-icon');
        const moonIcon = document.querySelector('.moon-icon');
        if (!sunIcon || !moonIcon) return;
        if (theme === 'dark') {
            sunIcon.style.display  = 'block';
            moonIcon.style.display = 'none';
        } else {
            sunIcon.style.display  = 'none';
            moonIcon.style.display = 'block';
        }
    }

    function toggleTheme() {
        const current = document.documentElement.getAttribute('data-theme') || getThemePreference();
        setTheme(current === 'dark' ? 'light' : 'dark');
    }

    // ── Style ────────────────────────────────────────────────

    function getStylePreference() {
        return localStorage.getItem(STYLE_KEY) || 'modern';
    }

    function setStyle(style) {
        document.documentElement.setAttribute('data-style', style);
        localStorage.setItem(STYLE_KEY, style);
        updateStyleLabel(style);
    }

    function updateStyleLabel(style) {
        const label = document.querySelector('.style-label');
        if (label) {
            const names = { modern: 'Modern', classic: 'Classic', warm: 'Warm' };
            label.textContent = names[style] || style;
        }
    }

    function cycleStyle() {
        const current = document.documentElement.getAttribute('data-style') || getStylePreference();
        const idx     = STYLES.indexOf(current);
        const next    = STYLES[(idx + 1) % STYLES.length];
        setStyle(next);
    }

    // ── Init ─────────────────────────────────────────────────

    function init() {
        setTheme(getThemePreference());
        setStyle(getStylePreference());

        const themeBtn = document.getElementById('theme-toggle');
        if (themeBtn) themeBtn.addEventListener('click', toggleTheme);

        const styleBtn = document.getElementById('style-toggle');
        if (styleBtn) styleBtn.addEventListener('click', cycleStyle);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

    // Track system theme changes (only when user hasn't set a preference)
    window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', (e) => {
        if (!localStorage.getItem(THEME_KEY)) {
            setTheme(e.matches ? 'dark' : 'light');
        }
    });
})();
