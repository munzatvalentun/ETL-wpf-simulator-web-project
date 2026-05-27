/* =====================================================
   THEME MANAGER
   Reads/writes localStorage key "etl-theme" ('dark' | 'light').
   Called by the inline <script> in _Layout (applyStored)
   and by every toggle control at runtime.
   ===================================================== */

(function () {

    /* ── helpers ── */
    function applyStored() {
        var t = localStorage.getItem('etl-theme') || 'dark';
        document.documentElement.className = t + '-theme';
    }

    function toggle() {
        var isLight = document.documentElement.classList.contains('light-theme');
        setTheme(isLight ? 'dark' : 'light');
    }

    function setTheme(t) {
        document.documentElement.className = t + '-theme';
        localStorage.setItem('etl-theme', t);

        /* sync checkbox in Appearance tab */
        var cb = document.getElementById('themeToggleCheckbox');
        if (cb) cb.checked = (t === 'light');

        /* sync preview cards */
        syncPreviewCards(t);

        /* re-theme all Chart.js instances */
        if (window.__etlCharts) {
            window.__etlCharts.updateAllCharts();
        }
    }

    function syncPreviewCards(t) {
        document.querySelectorAll('.theme-preview-card').forEach(function (c) {
            if (t === 'light') {
                c.classList.toggle('selected', c.classList.contains('light-preview'));
            } else {
                c.classList.toggle('selected', c.classList.contains('dark-preview'));
            }
        });
    }

    /* ── wire up controls once DOM is ready ── */
    function init() {
        var current = localStorage.getItem('etl-theme') || 'dark';

        /* navbar toggle button */
        var navBtn = document.getElementById('themeToggleBtn');
        if (navBtn) navBtn.addEventListener('click', toggle);

        /* Appearance-tab checkbox */
        var cb = document.getElementById('themeToggleCheckbox');
        if (cb) {
            cb.checked = (current === 'light');
            cb.addEventListener('change', function () {
                setTheme(this.checked ? 'light' : 'dark');
            });
        }

        /* Appearance-tab preview cards */
        var darkCard  = document.querySelector('.theme-preview-card.dark-preview');
        var lightCard = document.querySelector('.theme-preview-card.light-preview');
        if (darkCard)  darkCard.addEventListener('click',  function () { setTheme('dark');  });
        if (lightCard) lightCard.addEventListener('click', function () { setTheme('light'); });

        syncPreviewCards(current);
    }

    /* Expose so the inline <head> script can call applyStored() immediately */
    window.__etlTheme = { applyStored: applyStored, setTheme: setTheme, toggle: toggle };

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

})();
