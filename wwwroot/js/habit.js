﻿(function () {
    const state = {
        selectedDate: new Date(),
        weekOffset: 0
    };

    const qs = (sel, root = document) => root.querySelector(sel);
    const qsa = (sel, root = document) => Array.from(root.querySelectorAll(sel));

    function fmtYmd(d) {
        const y = d.getFullYear();
        const m = String(d.getMonth() + 1).padStart(2, '0');
        const day = String(d.getDate()).padStart(2, '0');
        return `${y}-${m}-${day}`;
    }

    function isSameDay(a, b) {
        return a.getFullYear() === b.getFullYear()
            && a.getMonth() === b.getMonth()
            && a.getDate() === b.getDate();
    }

    function renderCalendar() {
        const host = qs('#calendar-scroll');
        if (!host) return;

        host.innerHTML = '';
        const base = new Date();
        base.setDate(base.getDate() + state.weekOffset * 7);

        const monday = new Date(base);
        const delta = (monday.getDay() + 6) % 7;
        monday.setDate(monday.getDate() - delta);

        for (let i = 0; i < 7; i++) {
            const d = new Date(monday);
            d.setDate(d.getDate() + i);

            const btn = document.createElement('button');
            btn.className = 'btn btn-outline-primary mx-1 day-button';
            btn.dataset.date = fmtYmd(d);
            btn.textContent = d.toLocaleDateString('pl-PL', {weekday: 'short', day: '2-digit', month: '2-digit'});
            if (isSameDay(d, state.selectedDate)) btn.classList.add('active');

            btn.addEventListener('click', () => {
                qsa('.day-button', host).forEach(b => b.classList.remove('active'));
                btn.classList.add('active');
                state.selectedDate = new Date(`${btn.dataset.date}T00:00:00`);
                loadEntries(fmtYmd(state.selectedDate));
            });

            host.appendChild(btn);
        }

        loadEntries(fmtYmd(state.selectedDate));
    }

    function bindWeekButtons() {
        qs('#prevWeek')?.addEventListener('click', () => {
            state.weekOffset--;
            renderCalendar();
        });
        qs('#nextWeek')?.addEventListener('click', () => {
            state.weekOffset++;
            renderCalendar();
        });
    }

    async function loadEntries(dateStr) {
        const res = await fetch(`/Habits/GetEntries?date=${encodeURIComponent(dateStr + 'T12:00:00')}`, {cache: 'no-store'});
        if (!res.ok) return;
        const entries = await res.json();

        resetUiToDefaults();

        const byHabit = new Map();
        entries.forEach(e => {
            const hid = e.habitId ?? e.HabitId ?? e.habitID;
            if (hid != null) byHabit.set(Number(hid), e);
        });

        qsa('.habit-card').forEach(card => {
            const id = Number(card.dataset.id);
            const type = (card.dataset.type || '').toLowerCase();
            const target = Number(card.dataset.target || 0);

            const entry = byHabit.get(id);

            if (type === 'checkbox') {
                const cb = qs('[data-habit-checkbox]', card);
                if (!cb) return;
                const done = !!(entry && (entry.completed ?? entry.Completed));
                cb.checked = done;
            } else {
                const prog = qs('[data-habit-progress]', card);
                if (!prog) return;
                const q = Number(entry?.quantity ?? entry?.Quantity ?? 0);
                prog.textContent = `${q}/${isNaN(target) ? 0 : target}`;
                if (target > 0 && q >= target) {
                    prog.style.color = 'var(--bs-success, #28a745)';
                    prog.classList.add('fw-semibold');
                } else if (q > 0) {
                    prog.style.color = 'var(--bs-warning, #ffc107)';
                    prog.classList.remove('fw-semibold');
                } else {
                    prog.style.color = 'gray';
                    prog.classList.remove('fw-semibold');
                }
            }
        });
    }

    function resetUiToDefaults() {
        qsa('.habit-card').forEach(card => {
            const type = (card.dataset.type || '').toLowerCase();
            const target = Number(card.dataset.target || 0);
            if (type === 'checkbox') {
                const cb = qs('[data-habit-checkbox]', card);
                if (cb) cb.checked = false;
            } else {
                const prog = qs('[data-habit-progress]', card);
                if (prog) {
                    prog.textContent = `0/${isNaN(target) ? 0 : target}`;
                    prog.style.color = 'gray';
                    prog.classList.remove('fw-semibold');
                }
            }
        });
    }

    function bindInteractions() {
        const list = qs('#habit-list');
        if (!list) return;

        list.addEventListener('change', async (e) => {
            const cb = e.target.closest('[data-habit-checkbox]');
            if (!cb) return;

            const card = e.target.closest('.habit-card');
            if (!card) return;

            const habitId = Number(card.dataset.id);
            const completed = !!cb.checked;

            const d = state.selectedDate;
            const safeDate = new Date(d.getFullYear(), d.getMonth(), d.getDate(), 12, 0, 0);

            const payload = {
                habitId,
                date: safeDate,
                completed,
                quantity: null
            };

            const ok = await fetch('/Habits/SaveEntry', {
                method: 'POST',
                headers: {'Content-Type': 'application/json'},
                body: JSON.stringify(payload)
            }).then(r => r.ok);

            if (!ok) cb.checked = !completed;
            else await loadEntries(fmtYmd(state.selectedDate));
        });

        list.addEventListener('click', (e) => {
            const btn = e.target.closest('[data-qty-plus]');
            if (!btn) return;

            const card = e.target.closest('.habit-card');
            if (!card) return;

            const name = card.dataset.name || 'Nawyk';
            const unit = card.dataset.unit || '';
            const hid = Number(card.dataset.id);

            const modalEl = qs('#quantityModal');
            if (!modalEl) return;

            modalEl.dataset.habitId = String(hid);
            modalEl.querySelector('#modalHabitName').textContent = name;
            modalEl.querySelector('#modalUnit').textContent = unit;
            modalEl.querySelector('#modalQuantityInput').value = '';

            const m = bootstrap.Modal.getOrCreateInstance(modalEl);
            m.show();
        });

        qs('#confirmQuantityBtn')?.addEventListener('click', async () => {
            const modalEl = qs('#quantityModal');
            if (!modalEl) return;

            const hid = Number(modalEl.dataset.habitId);
            const val = Number(qs('#modalQuantityInput').value || 0);
            const card = qs(`.habit-card[data-id="${hid}"]`);
            const target = Number(card?.dataset.target || 0);

            const completed = target > 0 ? (val >= target) : (val > 0);

            const d = state.selectedDate;
            const safeDate = new Date(d.getFullYear(), d.getMonth(), d.getDate(), 12, 0, 0);

            const payload = {
                habitId: hid,
                date: safeDate,
                completed,
                quantity: isNaN(val) ? 0 : val
            };

            const ok = await fetch('/Habits/SaveEntry', {
                method: 'POST',
                headers: {'Content-Type': 'application/json'},
                body: JSON.stringify(payload)
            }).then(r => r.ok);

            if (ok) {
                bootstrap.Modal.getOrCreateInstance(modalEl).hide();
                await loadEntries(fmtYmd(state.selectedDate));
            }
        });
    }

    function initFromUrlTagFilter() {
        const tfRoot = document.querySelector('#habitTagFilterWrap .tf');
        if (!tfRoot) return;
        const form = tfRoot.closest('form');
        if (!form) return;

        const origSubmit = form.submit ? form.submit.bind(form) : null;
        form.submit = function () {
            try {
                const selected = Array.from(tfRoot.querySelectorAll('.tf-list input[type="checkbox"]:checked'))
                    .map(i => i.value);

                const url = new URL(window.location.href);
                url.searchParams.delete('tagIds');
                selected.forEach(id => url.searchParams.append('tagIds', id));

                window.history.replaceState(null, '', url.toString());
                if (typeof origSubmit === 'function') origSubmit();
            } catch {
            }
            return false;
        };

        form.addEventListener('submit', function (e) {
            e.preventDefault();
        }, true);

        (function seed() {
            const fromUrl = new URLSearchParams(location.search).getAll('tagIds').map(String);
            if (!fromUrl.length) return;
            tfRoot.querySelectorAll('.tf-list input[type="checkbox"]').forEach(cb => {
                cb.checked = fromUrl.includes(cb.value);
            });
            const evt = new CustomEvent('tagfilter:change', {
                bubbles: true, cancelable: true,
                detail: {
                    section: (tfRoot.dataset.section || 'habits'),
                    queryKey: (tfRoot.dataset.queryKey || 'tagIds'),
                    selectedIds: fromUrl,
                    root: tfRoot
                }
            });
            tfRoot.dispatchEvent(evt);
        })();
    }

    function init() {
        bindWeekButtons();
        bindInteractions();
        initFromUrlTagFilter();
        renderCalendar();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
