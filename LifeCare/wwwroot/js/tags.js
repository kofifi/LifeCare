(function () {
    class TagFilter {
        constructor(root) {
            this.root = root;
            this.display = root.querySelector('.tf-display');
            this.dropdown = root.querySelector('.tf-dropdown');
            this.list = root.querySelector('.tf-list');
            this.searchInput = root.querySelector('.tf-search input');
            this.resetBtn = root.querySelector('.tf-reset');
            this.placeholder = root.dataset.placeholder || 'Wszystkie tagi';
            this.queryKey = root.dataset.queryKey || 'tagIds';
            this.submitMode = (root.dataset.submitMode || 'manual').toLowerCase();
            this.section = root.dataset.section || null;
            this.form = root.closest('form');

            this.state = new Set(JSON.parse(root.dataset.selectedIds || '[]'));

            this.buildList();
            this.renderDisplay();
            this.bindEvents();
        }

        buildList() {
            const items = JSON.parse(this.root.dataset.items || '[]');
            this.list.innerHTML = '';
            for (const it of items) {
                const li = document.createElement('li');
                li.className = 'tf-item';
                li.innerHTML = `
                    <label>
                        <input type="checkbox" value="${it.id}">
                        <span>${this.escape(it.name)}</span>
                    </label>
                `;
                const cb = li.querySelector('input');
                cb.checked = this.state.has(it.id);

                cb.addEventListener('change', () => {
                    if (cb.checked) this.state.add(it.id);
                    else this.state.delete(it.id);
                    this.renderDisplay();
                });

                this.list.appendChild(li);
            }
        }

        renderDisplay() {
            const box = this.display.querySelector('.tf-chips');
            box.innerHTML = '';

            if (this.state.size === 0) {
                const span = document.createElement('span');
                span.className = 'tf-placeholder';
                span.textContent = this.placeholder;
                box.appendChild(span);
            } else {
                const nameById = {};
                const items = JSON.parse(this.root.dataset.items || '[]');
                for (const it of items) nameById[it.id] = it.name;

                for (const id of this.state) {
                    const chip = document.createElement('span');
                    chip.className = 'tf-chip';
                    chip.innerHTML = `
                        <span>${this.escape(nameById[id] || id)}</span>
                        <button type="button" aria-label="Usuń">×</button>
                    `;
                    chip.querySelector('button').addEventListener('click', (e) => {
                        e.stopPropagation();
                        this.state.delete(id);
                        const cb = this.list.querySelector(`input[value="${id}"]`);
                        if (cb) cb.checked = false;
                        this.renderDisplay();
                    });
                    box.appendChild(chip);
                }
            }

            if (this.resetBtn) this.resetBtn.disabled = this.state.size === 0;
        }

        bindEvents() {
            this.display.addEventListener('click', (e) => {
                if (e.target && e.target.classList.contains('tf-reset')) return;
                this.toggle();
            });

            if (this.resetBtn) {
                this.resetBtn.addEventListener('click', (e) => {
                    e.preventDefault();
                    if (this.state.size === 0) { this.close(); return; }
                    this.state.clear();
                    for (const cb of this.list.querySelectorAll('input[type="checkbox"]')) cb.checked = false;
                    this.renderDisplay();
                    this.submit(true);
                });
            }

            document.addEventListener('click', (e) => {
                if (!this.root.contains(e.target)) this.close();
            });

            let lastTerm = '';
            this.searchInput.addEventListener('input', () => {
                const term = this.searchInput.value.trim().toLowerCase();
                if (term === lastTerm) return;
                lastTerm = term;
                for (const li of this.list.children) {
                    const text = li.textContent.trim().toLowerCase();
                    li.style.display = text.includes(term) ? '' : 'none';
                }
            });

            const clearBtn = this.root.querySelector('.tf-clear');
            if (clearBtn) {
                clearBtn.addEventListener('click', (e) => {
                    e.preventDefault();
                    if (this.state.size === 0) { this.close(); return; }
                    this.state.clear();
                    for (const cb of this.list.querySelectorAll('input[type="checkbox"]')) cb.checked = false;
                    this.renderDisplay();
                    this.submit(true);
                });
            }

            const applyInline = this.root.querySelector('.tf-apply-inline');
            if (applyInline) {
                applyInline.addEventListener('click', (e) => {
                    e.preventDefault();
                    this.submit(true);
                });
            }

            const maybeOutside = this.root.nextElementSibling;
            if (maybeOutside && maybeOutside.matches('[data-tf-apply]')) {
                maybeOutside.addEventListener('click', (e) => {
                    e.preventDefault();
                    this.submit(true);
                });
            }

            const applyBtn = this.root.querySelector('.tf-apply');
            if (applyBtn) {
                applyBtn.addEventListener('click', (e) => {
                    e.preventDefault();
                    this.submit(true);
                });
            }
        }

        toggle() {
            this.root.classList.toggle('is-open');
            if (this.root.classList.contains('is-open')) {
                this.searchInput.value = '';
                this.searchInput.dispatchEvent(new Event('input'));
                setTimeout(() => this.searchInput.focus(), 10);
            }
        }

        close() {
            this.root.classList.remove('is-open');
        }

        submit(closeDropdown) {
            const selected = Array.from(this.state).map(String);
            if (closeDropdown) this.close();

            const evt = new CustomEvent('tagfilter:change', {
                bubbles: true,
                cancelable: true,
                detail: {
                    section: (this.section || 'today'),
                    queryKey: this.queryKey,
                    selectedIds: selected,
                    root: this.root
                }
            });
            this.root.dispatchEvent(evt);

            if (!evt.defaultPrevented && this.form) {
                this.form.querySelectorAll(`input[name="${this.queryKey}"]`).forEach(h => h.remove());
                for (const id of this.state) {
                    const h = document.createElement('input');
                    h.type = 'hidden';
                    h.name = this.queryKey;
                    h.value = String(id);
                    this.form.appendChild(h);
                }
                if (this.section && !this.form.querySelector('input[name="section"]')) {
                    const s = document.createElement('input');
                    s.type = 'hidden';
                    s.name = 'section';
                    s.value = this.section;
                    this.form.appendChild(s);
                }
                this.form.submit();
            }
        }

        escape(s) {
            return String(s)
                .replace(/&/g, '&amp;')
                .replace(/</g, '&lt;')
                .replace(/>/g, '&gt;');
        }
    }

    function initAll() {
        document.querySelectorAll('.tf[data-items]').forEach((el) => {
            if (!el.__tf) el.__tf = new TagFilter(el);
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initAll);
    } else {
        initAll();
    }
})();


(function () {
    const choicesInstances = new Map();

    function initChoicesFor(el) {
        if (!el || choicesInstances.has(el)) return;

        const inst = new Choices(el, {
            removeItemButton: true,
            shouldSort: true,
            placeholderValue: el.getAttribute('data-placeholder') || 'Wybierz tagi…',
            searchPlaceholderValue: 'Szukaj…',
            noResultsText: 'Brak wyników',
            noChoicesText: 'Brak tagów',
            itemSelectText: '',
            shouldCloseOnSelect: false
        });
        choicesInstances.set(el, inst);
    }

    function refreshAllChoicesWithNewTag(id, name) {
        choicesInstances.forEach((inst) => {
            inst.setChoices([{ value: String(id), label: name, selected: true }], 'value', 'label', false);
        });
    }

    function wireAddButtons() {
        document.addEventListener('click', (e) => {
            const btn = e.target.closest('#btnAddTag, button[data-role="tag-add"]');
            if (!btn) return;

            const modalEl = document.getElementById('tagModal');
            if (!modalEl) return;

            const bs = bootstrap.Modal.getOrCreateInstance(modalEl);
            bs.show();
        });
    }

    function wireModalSubmit() {
        const modalEl = document.getElementById('tagModal');
        if (!modalEl) return;

        const form = modalEl.querySelector('form#tagForm');
        if (!form) return;

        form.addEventListener('submit', async (e) => {
            e.preventDefault();
            const fd = new FormData(form);
            const token = form.querySelector('input[name="__RequestVerificationToken"]')?.value;
            const res = await fetch('/Tags/Create', {
                method: 'POST',
                headers: token ? { 'X-Requested-With': 'XMLHttpRequest', 'RequestVerificationToken': token } : { 'X-Requested-With': 'XMLHttpRequest' },
                body: fd
            });
            if (!res.ok) {
                alert('Nie udało się utworzyć tagu.');
                return;
            }
            const { id, name } = await res.json();
            refreshAllChoicesWithNewTag(id, name);
            form.reset();
            bootstrap.Modal.getInstance(modalEl)?.hide();
        });
    }

    function init() {
        document.querySelectorAll('select[name="SelectedTagIds"][multiple]').forEach(initChoicesFor);
        wireAddButtons();
        wireModalSubmit();

        document.addEventListener('tag:created', (e) => {
            const { id, name } = e.detail || {};
            if (id && name) refreshAllChoicesWithNewTag(id, name);
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
