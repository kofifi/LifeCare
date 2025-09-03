(function () {
    if (window.__categoryModalBound) return;
    window.__categoryModalBound = true;

    const modalSelector = '#categoryModal';
    const formSelector  = '#categoryForm';
    const inputSelector = '#categoryName';
    let targetSelectId = null;

    function getRequestVerificationToken() {
        const el = document.querySelector('input[name="__RequestVerificationToken"]');
        return el?.value || null;
    }

    function addCategoryToSelects(category, options = { scope: document, selectName: 'CategoryId', selectId: null }) {
        const { scope, selectName, selectId } = options;
        const selects = selectId
            ? [document.getElementById(selectId)].filter(Boolean)
            : Array.from(scope.querySelectorAll(`select[name='${selectName}']`));

        selects.forEach(select => {
            if (!select) return;
            const exists = Array.from(select.options).some(o => o.value == category.id);
            if (!exists) {
                const opt = new Option(category.name, category.id, true, true);
                select.add(opt);
            } else {
                select.value = category.id;
            }
            select.dispatchEvent(new Event('change', { bubbles: true }));
        });
    }

    function cleanupModal() {
        const modalEl = document.querySelector(modalSelector);
        const input   = document.querySelector(inputSelector);
        if (input) input.value = '';
        if (modalEl && window.bootstrap) {
            const modal = bootstrap.Modal.getInstance(modalEl) || new bootstrap.Modal(modalEl);
            modal.hide();
        }
    }

    document.addEventListener('show.bs.modal', function (ev) {
        const modal = ev.target;
        if (!modal.matches(modalSelector)) return;
        const btn = ev.relatedTarget;
        targetSelectId = btn?.getAttribute('data-target-select') || null;

        const input = document.querySelector(inputSelector);
        if (input) { input.value = ''; setTimeout(() => input.focus(), 50); }
    });

    document.addEventListener('submit', async function (e) {
        const form = e.target;
        if (!form.matches(formSelector)) return;

        e.preventDefault();

        const nameInput = document.querySelector(inputSelector);
        const name = (nameInput?.value || '').trim();
        if (!name) return;

        const token = getRequestVerificationToken();

        try {
            const res = await fetch('/Category/CreateAjax', {
                method: 'POST',
                credentials: 'same-origin',
                headers: Object.assign(
                    { 'Content-Type': 'application/json' },
                    token ? { 'RequestVerificationToken': token } : {}
                ),
                body: JSON.stringify({ name })
            });

            if (!res.ok) {
                const text = await res.text().catch(() => '');
                throw new Error(`Błąd dodawania kategorii (HTTP ${res.status}). ${text || ''}`);
            }

            const category = await res.json();
            addCategoryToSelects(category, { selectId: targetSelectId });

            window.dispatchEvent(new CustomEvent('category:created', { detail: category }));
            cleanupModal();
        } catch (err) {
            console.error(err);
            alert(err.message || 'Wystąpił błąd.');
        }
    });
})();
