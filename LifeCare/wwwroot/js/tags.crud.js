(function () {
    const q = (s, r = document) => r.querySelector(s);
    const qa = (s, r = document) => Array.from(r.querySelectorAll(s));

    function token(form) {
        return form.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
    }

    function appendRow(tag) {
        const tr = document.createElement('tr');
        tr.setAttribute('data-id', tag.id);
        tr.innerHTML = `
      <td>${tag.id}</td>
      <td class="tag-name">${escapeHtml(tag.name)}</td>
      <td class="text-end">
        <button class="btn btn-sm btn-outline-primary me-2" data-role="tag-open-edit" data-id="${tag.id}" data-name="${escapeHtml(tag.name)}">
          <i class="fa fa-edit"></i>
        </button>
        <button class="btn btn-sm btn-outline-danger" data-role="tag-open-delete" data-id="${tag.id}" data-name="${escapeHtml(tag.name)}">
          <i class="fa fa-trash"></i>
        </button>
      </td>`;
        q('#tagsTableBody')?.appendChild(tr);
    }

    function updateRow(tag) {
        const tr = q(`tr[data-id="${tag.id}"]`);
        if (!tr) return;
        tr.querySelector('.tag-name').textContent = tag.name;
        const editBtn = tr.querySelector('[data-role="tag-open-edit"]');
        const delBtn = tr.querySelector('[data-role="tag-open-delete"]');
        if (editBtn) { editBtn.dataset.name = tag.name; }
        if (delBtn) { delBtn.dataset.name = tag.name; }
    }

    function removeRow(id) {
        q(`tr[data-id="${id}"]`)?.remove();
    }

    function escapeHtml(s) {
        return String(s).replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;');
    }

    function openCreate() {
        const modalEl = q('#tagCreateModal');
        q('#tagCreateName', modalEl).value = '';
        bootstrap.Modal.getOrCreateInstance(modalEl).show();
    }

    function openEdit(id, name) {
        const modalEl = q('#tagEditModal');
        q('#tagEditId', modalEl).value = id;
        q('#tagEditName', modalEl).value = name || '';
        bootstrap.Modal.getOrCreateInstance(modalEl).show();
    }

    function openDelete(id, name) {
        const modalEl = q('#tagDeleteModal');
        q('#tagDeleteId', modalEl).value = id;
        q('#tagDeleteName', modalEl).textContent = name || '';
        bootstrap.Modal.getOrCreateInstance(modalEl).show();
    }

    function bindOpeners() {
        document.addEventListener('click', (e) => {
            const createBtn = e.target.closest('[data-role="tag-open-create"]');
            if (createBtn) { e.preventDefault(); openCreate(); return; }

            const editBtn = e.target.closest('[data-role="tag-open-edit"]');
            if (editBtn) { e.preventDefault(); openEdit(editBtn.dataset.id, editBtn.dataset.name); return; }

            const delBtn = e.target.closest('[data-role="tag-open-delete"]');
            if (delBtn) { e.preventDefault(); openDelete(delBtn.dataset.id, delBtn.dataset.name); return; }
        });
    }

    function bindForms() {
        const createForm = q('#tagCreateForm');
        if (createForm && !createForm.dataset.bound) {
            createForm.dataset.bound = '1';
            createForm.addEventListener('submit', async (e) => {
                e.preventDefault();
                const fd = new FormData(createForm);
                try {
                    const res = await fetch('/Tags/Create', {
                        method: 'POST',
                        body: fd,
                        credentials: 'same-origin'
                    });
                    if (!res.ok) {
                        const t = await res.text().catch(()=> '');
                        console.error('Create failed', res.status, t);
                        alert('Nie udało się utworzyć tagu.');
                        return;
                    }
                    const data = await res.json();
                    appendRow(data);
                    createForm.reset();
                    bootstrap.Modal.getInstance(q('#tagCreateModal'))?.hide();

                    document.dispatchEvent(new CustomEvent('tag:created', { detail: data }));
                } catch (err) {
                    console.error(err);
                    alert('Nie udało się utworzyć tagu.');
                }
            });
        }

        const editForm = q('#tagEditForm');
        if (editForm && !editForm.dataset.bound) {
            editForm.dataset.bound = '1';
            editForm.addEventListener('submit', async (e) => {
                e.preventDefault();
                const fd = new FormData(editForm);
                try {
                    const res = await fetch('/Tags/Edit', {
                        method: 'POST',
                        body: fd,
                        credentials: 'same-origin'
                    });
                    if (!res.ok) {
                        const t = await res.text().catch(()=> '');
                        console.error('Edit failed', res.status, t);
                        alert('Nie udało się zapisać.');
                        return;
                    }
                    const data = await res.json();
                    updateRow(data);
                    bootstrap.Modal.getInstance(q('#tagEditModal'))?.hide();

                    document.dispatchEvent(new CustomEvent('tag:updated', { detail: data }));
                } catch (err) {
                    console.error(err);
                    alert('Nie udało się zapisać.');
                }
            });
        }

        const deleteForm = q('#tagDeleteForm');
        if (deleteForm && !deleteForm.dataset.bound) {
            deleteForm.dataset.bound = '1';
            deleteForm.addEventListener('submit', async (e) => {
                e.preventDefault();
                const fd = new FormData(deleteForm);
                try {
                    const res = await fetch('/Tags/Delete', {
                        method: 'POST',
                        body: fd,
                        credentials: 'same-origin'
                    });
                    if (!res.ok) {
                        const t = await res.text().catch(()=> '');
                        console.error('Delete failed', res.status, t);
                        alert('Nie udało się usunąć tagu.');
                        return;
                    }
                    const data = await res.json();
                    removeRow(data.id);
                    bootstrap.Modal.getInstance(q('#tagDeleteModal'))?.hide();

                    document.dispatchEvent(new CustomEvent('tag:deleted', { detail: data }));
                } catch (err) {
                    console.error(err);
                    alert('Nie udało się usunąć tagu.');
                }
            });
        }
    }

    function init() {
        bindOpeners();
        bindForms();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
