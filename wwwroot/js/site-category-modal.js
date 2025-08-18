// wwwroot/js/site-category-modal.js
(function () {
    // Guard: nie rejestruj podwójnie
    if (window.__categoryModalBound) return;
    window.__categoryModalBound = true;

    // Ustal selektory (ID z partiala)
    const modalSelector = '#categoryModal';
    const formSelector  = '#categoryForm';
    const inputSelector = '#categoryName';

    // Pobranie tokena antyforgery, jeśli istnieje w DOM
    function getRequestVerificationToken() {
        const el = document.querySelector('input[name="__RequestVerificationToken"]');
        return el?.value || null;
    }

    // Dodaj nową opcję do selectów CategoryId
    function addCategoryToSelects(category, options = { scope: document, selectName: 'CategoryId', selectId: null }) {
        const { scope, selectName, selectId } = options;

        // Jeśli podano konkretny select po id — użyj tylko jego
        const selects = selectId
            ? [document.getElementById(selectId)].filter(Boolean)
            : Array.from(scope.querySelectorAll(`select[name='${selectName}']`));

        selects.forEach(select => {
            if (!select) return;

            // Sprawdź, czy już jest
            const exists = Array.from(select.options).some(o => o.value == category.id);
            if (!exists) {
                const opt = new Option(category.name, category.id, true, true);
                select.add(opt);
            } else {
                select.value = category.id;
            }

            // Dla BS Select/Select2 itp. można triggerować refresh tutaj
            const evt = new Event('change', { bubbles: true });
            select.dispatchEvent(evt);
        });
    }

    // Zamknij modal i wyczyść input
    function cleanupModal() {
        const modalEl = document.querySelector(modalSelector);
        const input   = document.querySelector(inputSelector);
        if (input) input.value = '';
        if (modalEl && window.bootstrap) {
            const modal = bootstrap.Modal.getInstance(modalEl) || new bootstrap.Modal(modalEl);
            modal.hide();
        }
    }

    // Globalny listener SUBMIT dla formularza w modalu
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
                headers: Object.assign(
                    { 'Content-Type': 'application/json' },
                    token ? { 'RequestVerificationToken': token } : {}
                ),
                body: JSON.stringify({ name })
            });

            if (!res.ok) throw new Error('Błąd dodawania kategorii.');

            const category = await res.json();

            // Domyślnie: zaktualizuj WSZYSTKIE selecty CategoryId na stronie
            addCategoryToSelects(category);

            // Wyemituj event, żeby inne miejsca mogły zareagować specyficznie
            window.dispatchEvent(new CustomEvent('category:created', { detail: category }));

            cleanupModal();
        } catch (err) {
            alert(err.message || 'Wystąpił błąd.');
        }
    });

    // (Opcjonalnie) Obsługa klawisza Enter w input — forma już to robi, więc nie trzeba nic dodawać.
})();
