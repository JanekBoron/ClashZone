// Reusable client-side search + filter + sort + pagination for admin tables.
// Opt-in via markup:
//   <input data-table-search="myTableId" ...>                         (optional search box)
//   <select data-table-filter="myTableId" data-filter-col="4">...      (optional column filter; option value matches cell text, empty = all)
//   <table id="myTableId" data-paginate data-page-size="10">
//     <thead><tr><th data-sort-col="0">...</th> ...                    (optional sortable headers)
//   </table>
// A pagination control is generated automatically right after the table.
(function () {
    'use strict';

    function initTable(table) {
        var pageSize = parseInt(table.getAttribute('data-page-size'), 10) || 10;
        var tbody = table.tBodies[0];
        if (!tbody) return;

        var allRows = Array.prototype.slice.call(tbody.rows);
        var columnCount = (table.tHead && table.tHead.rows[0]) ? table.tHead.rows[0].cells.length : 1;

        var state = { page: 1, query: '', filterValue: '', filterCol: -1, sortCol: -1, sortDir: 'asc' };

        // "Brak wyników" row shown when the search/filter matches nothing.
        var emptyRow = document.createElement('tr');
        var emptyCell = document.createElement('td');
        emptyCell.colSpan = columnCount;
        emptyCell.className = 'text-center text-muted py-3';
        emptyCell.textContent = 'Brak wyników.';
        emptyRow.appendChild(emptyCell);
        emptyRow.style.display = 'none';
        tbody.appendChild(emptyRow);

        // Pagination container right after the table.
        var pager = document.createElement('nav');
        pager.className = 'cc-pagination';
        table.parentNode.insertBefore(pager, table.nextSibling);

        function cellText(row, idx) {
            return (idx >= 0 && row.cells[idx]) ? row.cells[idx].textContent.trim() : '';
        }

        function filteredRows() {
            return allRows.filter(function (row) {
                if (state.query && row.textContent.toLowerCase().indexOf(state.query) === -1) return false;
                if (state.filterValue && state.filterCol >= 0) {
                    if (cellText(row, state.filterCol).toLowerCase() !== state.filterValue.toLowerCase()) return false;
                }
                return true;
            });
        }

        function render() {
            var rows = filteredRows();
            var pageCount = Math.max(1, Math.ceil(rows.length / pageSize));
            if (state.page > pageCount) state.page = pageCount;

            var start = (state.page - 1) * pageSize;
            var end = start + pageSize;

            allRows.forEach(function (row) { row.style.display = 'none'; });
            rows.slice(start, end).forEach(function (row) { row.style.display = ''; });

            emptyRow.style.display = rows.length === 0 ? '' : 'none';

            renderPager(pageCount);
        }

        function renderPager(pageCount) {
            pager.innerHTML = '';
            if (pageCount <= 1) return;

            var ul = document.createElement('ul');
            ul.className = 'pagination justify-content-center mb-0';

            function pageItem(label, page, disabled, active) {
                var li = document.createElement('li');
                li.className = 'page-item' + (disabled ? ' disabled' : '') + (active ? ' active' : '');
                var a = document.createElement('a');
                a.className = 'page-link';
                a.href = '#';
                a.innerHTML = label;
                a.addEventListener('click', function (e) {
                    e.preventDefault();
                    if (disabled || active) return;
                    state.page = page;
                    render();
                });
                li.appendChild(a);
                return li;
            }

            ul.appendChild(pageItem('&laquo;', state.page - 1, state.page === 1, false));
            for (var i = 1; i <= pageCount; i++) {
                ul.appendChild(pageItem(String(i), i, false, i === state.page));
            }
            ul.appendChild(pageItem('&raquo;', state.page + 1, state.page === pageCount, false));

            pager.appendChild(ul);
        }

        function sortRows(headers) {
            var col = state.sortCol;
            var dir = state.sortDir === 'desc' ? -1 : 1;
            allRows.sort(function (a, b) {
                var ta = cellText(a, col).toLowerCase();
                var tb = cellText(b, col).toLowerCase();
                var na = parseFloat(ta.replace(',', '.'));
                var nb = parseFloat(tb.replace(',', '.'));
                var cmp;
                if (!isNaN(na) && !isNaN(nb) && ta !== '' && tb !== '') {
                    cmp = na - nb;
                } else {
                    cmp = ta < tb ? -1 : (ta > tb ? 1 : 0);
                }
                return cmp * dir;
            });
            // Reorder DOM to match the sorted array; keep the empty row last.
            allRows.forEach(function (r) { tbody.appendChild(r); });
            tbody.appendChild(emptyRow);

            Array.prototype.forEach.call(headers, function (th) {
                th.classList.remove('cc-sort-asc', 'cc-sort-desc');
                if (parseInt(th.getAttribute('data-sort-col'), 10) === col) {
                    th.classList.add(state.sortDir === 'desc' ? 'cc-sort-desc' : 'cc-sort-asc');
                }
            });
        }

        // Search box.
        var search = document.querySelector('[data-table-search="' + table.id + '"]');
        if (search) {
            search.addEventListener('input', function () {
                state.query = search.value.trim().toLowerCase();
                state.page = 1;
                render();
            });
        }

        // Column filter (e.g. by game).
        var filterSelect = document.querySelector('select[data-table-filter="' + table.id + '"]');
        if (filterSelect) {
            state.filterCol = parseInt(filterSelect.getAttribute('data-filter-col'), 10);
            state.filterValue = filterSelect.value;
            filterSelect.addEventListener('change', function () {
                state.filterValue = filterSelect.value;
                state.page = 1;
                render();
            });
        }

        // Sortable headers.
        var headers = table.tHead ? table.tHead.querySelectorAll('th[data-sort-col]') : [];
        Array.prototype.forEach.call(headers, function (th) {
            th.classList.add('cc-sortable');
            th.addEventListener('click', function () {
                var col = parseInt(th.getAttribute('data-sort-col'), 10);
                if (state.sortCol === col) {
                    state.sortDir = state.sortDir === 'asc' ? 'desc' : 'asc';
                } else {
                    state.sortCol = col;
                    state.sortDir = 'asc';
                }
                sortRows(headers);
                state.page = 1;
                render();
            });
        });

        render();
    }

    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('table[data-paginate]').forEach(initTable);
    });
})();
