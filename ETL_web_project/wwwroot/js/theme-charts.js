/* =====================================================
   CHART THEME MANAGER
   Sets Chart.js defaults before charts are created,
   and live-updates all instances when the theme changes.
   Must be loaded in _Layout BEFORE @section Scripts
   (so its DOMContentLoaded fires first).
   ===================================================== */

(function () {

    var COLORS = {
        dark: {
            grid:         'rgba(55, 65, 81, 0.55)',
            ticks:        '#9ca3af',
            tooltipBg:    '#111827',
            tooltipTitle: '#e5e7eb',
            tooltipBody:  '#e5e7eb',
            tooltipBorder:'#4b5563',
        },
        light: {
            grid:         'rgba(203, 213, 225, 0.7)',
            ticks:        '#64748b',
            tooltipBg:    '#ffffff',
            tooltipTitle: '#1e293b',
            tooltipBody:  '#475569',
            tooltipBorder:'#e2e8f0',
        }
    };

    function currentMode() {
        return document.documentElement.classList.contains('light-theme') ? 'light' : 'dark';
    }

    function applyDefaults() {
        if (typeof Chart === 'undefined') return;
        var c = COLORS[currentMode()];

        /* Scales */
        Chart.defaults.color = c.ticks;
        if (Chart.defaults.scale) {
            if (Chart.defaults.scale.grid)  Chart.defaults.scale.grid.color  = c.grid;
            if (Chart.defaults.scale.ticks) Chart.defaults.scale.ticks.color = c.ticks;
        }

        /* Tooltip */
        var tt = Chart.defaults.plugins && Chart.defaults.plugins.tooltip;
        if (tt) {
            tt.backgroundColor = c.tooltipBg;
            tt.titleColor      = c.tooltipTitle;
            tt.bodyColor       = c.tooltipBody;
            tt.borderColor     = c.tooltipBorder;
            tt.borderWidth     = 1;
        }
    }

    function updateAllCharts() {
        if (typeof Chart === 'undefined') return;
        var c = COLORS[currentMode()];

        /* Chart.js 3/4 keeps all instances in Chart.instances */
        var instances = Chart.instances;
        if (!instances) return;

        Object.values(instances).forEach(function (chart) {
            var opts = chart.options;
            if (!opts) return;

            /* Update every scale */
            var scales = opts.scales || {};
            Object.values(scales).forEach(function (scale) {
                if (scale.grid)  scale.grid.color  = c.grid;
                if (scale.ticks) scale.ticks.color = c.ticks;
            });

            /* Update tooltip */
            var tt = opts.plugins && opts.plugins.tooltip;
            if (tt) {
                tt.backgroundColor = c.tooltipBg;
                tt.titleColor      = c.tooltipTitle;
                tt.bodyColor       = c.tooltipBody;
                tt.borderColor     = c.tooltipBorder;
            }

            chart.update('none'); /* instant, no animation */
        });
    }

    /* Expose for theme.js to call on toggle */
    window.__etlCharts = {
        applyDefaults:  applyDefaults,
        updateAllCharts: updateAllCharts
    };

    /* Apply defaults as soon as DOM + Chart.js are ready */
    document.addEventListener('DOMContentLoaded', applyDefaults);

})();
