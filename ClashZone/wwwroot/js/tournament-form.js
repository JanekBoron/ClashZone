// Dynamicznie ogranicza dostępne formaty drużyn w zależności od wybranej gry.
// Wymaga w markupie:
//   <select id="gameTitleSelect"> ... </select>
//   <select id="formatSelect"> ... </select>
//   <script id="allowedFormatsData" type="application/json">{ "Gra": ["1v1", ...] }</script>
(function () {
    'use strict';

    function init() {
        var gameSelect = document.getElementById('gameTitleSelect');
        var formatSelect = document.getElementById('formatSelect');
        var dataEl = document.getElementById('allowedFormatsData');
        if (!gameSelect || !formatSelect || !dataEl) return;

        var allowed = {};
        try { allowed = JSON.parse(dataEl.textContent || '{}'); } catch (e) { return; }

        // Opcjonalny prefill domyślnych reguł zależnie od gry.
        var rulesTextarea = document.getElementById('rulesTextarea');
        var rulesData = {};
        var rulesEl = document.getElementById('defaultRulesData');
        if (rulesEl) {
            try { rulesData = JSON.parse(rulesEl.textContent || '{}'); } catch (e) { rulesData = {}; }
        }
        // Inicjalizujemy domyślnymi regułami aktualnie wybranej gry, aby przy
        // edycji nie nadpisać własnych reguł organizatora.
        var lastDefaultRules = (rulesData[gameSelect.value] || '').trim();

        function applyFormats() {
            var list = allowed[gameSelect.value] || [];
            Array.prototype.forEach.call(formatSelect.options, function (o) {
                var ok = list.indexOf(o.value) !== -1;
                o.hidden = !ok;
                o.disabled = !ok;
            });
            // Jeśli aktualnie wybrany format jest niedozwolony dla gry — przełącz na pierwszy dozwolony.
            if (list.indexOf(formatSelect.value) === -1) {
                var first = Array.prototype.find.call(formatSelect.options, function (o) { return !o.disabled; });
                if (first) formatSelect.value = first.value;
            }
        }

        function applyRules() {
            if (!rulesTextarea) return;
            var nextDefault = (rulesData[gameSelect.value] || '').trim();
            var current = rulesTextarea.value.trim();
            // Podmieniamy treść tylko, gdy pole jest puste albo nadal zawiera
            // niezmodyfikowane domyślne reguły poprzedniej gry.
            if (current === '' || current === lastDefaultRules) {
                rulesTextarea.value = nextDefault;
            }
            lastDefaultRules = nextDefault;
        }

        gameSelect.addEventListener('change', function () {
            applyFormats();
            applyRules();
        });
        applyFormats();
        applyRules();
    }

    document.addEventListener('DOMContentLoaded', init);
})();
