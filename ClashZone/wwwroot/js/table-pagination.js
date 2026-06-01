// Reusable client-side search + pagination for admin tables.
// Opt-in via markup:
//   <input data-table-search="myTableId" ...>            (optional search box)
//   <table id="myTableId" data-paginate data-page-size="10"> ... </table>
// A pagination control is generated automatically right after the table.
(function () {
    'use strict';

    function initTable(table) {
        var pageSize = parseInt(table.getAttribute('data-page-size'), 10) || 10;
        var tbody = table.tBodies[0];
        if (!tbody) return;

        var allRows = Array.prototype.slice.call(tbody.rows);
        var columnCount = (table.tHead && table.tHead.rows[0]) ? table.tHead.rows[0].cells.length : 1;

        var state = { page: 1, query: '' };

        // "Brak wyników" row shown when the search matches nothing.
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

        function filteredRows() {
            if (!state.query) return allRows;
            var q = state.query;
            return allRows.filter(function (row) {
                return row.textContent.toLowerCase().indexOf(q) !== -1;
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

        var search = document.querySelector('[data-table-search="' + table.id + '"]');
        if (search) {
            search.addEventListener('input', function () {
                state.query = search.value.trim().toLowerCase();
                state.page = 1;
                render();
            });
        }

        render();
    }

    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('table[data-paginate]').forEach(initTable);
    });
})();
