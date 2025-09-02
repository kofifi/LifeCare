if (!window.__RoutinesStepsInit) {
    window.__RoutinesStepsInit = true;

    (function () {
        const $  = (sel, root = document) => root.querySelector(sel);
        const $$ = (sel, root = document) => Array.from(root.querySelectorAll(sel));

        const form         = $('#routineForm') || document.querySelector('form[asp-action="Create"]') || document.querySelector('form[action*="Create"]');
        const stepsRoot    = document.getElementById('stepsContainer') || document.getElementById('stepsList');
        const addStepBtn   = document.getElementById('addStepBtn');
        const stepsJsonEl  = document.getElementById('StepsJson');

        if (!form || !stepsRoot || !stepsJsonEl) return;

        const productModalEl  = document.getElementById('productModal');
        const productModal    = productModalEl ? new bootstrap.Modal(productModalEl) : null;
        const prodName        = document.getElementById('prodName');
        const prodNote        = document.getElementById('prodNote');
        const prodUrl         = document.getElementById('prodUrl');
        const productSaveBtn  = document.getElementById('productSaveBtn');

        const state = { steps: [] };

        function syncHidden() {
            stepsJsonEl.value = JSON.stringify(state.steps || []);
        }

        function renderProductRow(p, idx) {
            return `
        <div class="product-row d-flex align-items-start justify-content-between border rounded p-2" data-pidx="${idx}">
          <div class="d-flex align-items-start gap-2">
            <i class="fa fa-grip-vertical product-drag mt-1" title="Przeciągnij"></i>
            ${p.imageUrl ? `<img src="${p.imageUrl}" alt="" style="width:44px;height:44px;object-fit:cover;border-radius:6px;">` : ``}
            <div>
              <div class="fw-semibold">${p.name || ''}</div>
              ${p.note ? `<div class="small text-muted">${p.note}</div>` : ``}
              ${p.url ? `<div class="small"><a href="${p.url}" target="_blank" rel="noopener">link</a></div>` : ``}
            </div>
          </div>
          <div class="btn-group btn-group-sm">
            <button type="button" class="btn btn-outline-secondary edit" title="Edytuj">
                <i class="fa fa-edit"></i>
            </button>
            <button type="button" class="btn btn-outline-danger remove" title="Usuń">
                <i class="fa fa-trash"></i>
            </button>
          </div>
        </div>
      `;
        }

        function renderStepCard(step, idx) {
            const hasProducts = (step.products?.length || 0) > 0;
            const canRotate   = (step.products?.length || 0) > 1;
            const rotEnabled  = !!step.rotation?.enabled && canRotate;
            const rotMode     = step.rotation?.mode || 'ALL';

            return `
        <div class="step-card soft-card p-3" data-index="${idx}">
          <div class="d-flex align-items-center justify-content-between">
            <span class="drag-handle" title="Przeciągnij"><i class="fa fa-grip-lines"></i></span>
            <div class="d-flex align-items-center gap-2 flex-grow-1">
              <div class="flex-grow-1">
                <label class="form-label mb-1">Nazwa kroku</label>
                <input class="form-control step-name" value="${step.name || ''}" placeholder="np. Oczyszczanie" />
              </div>
            </div>
            <button type="button" class="btn btn-sm btn-outline-danger remove-step ms-2" title="Usuń">
              <i class="fa fa-trash"></i>
            </button>
          </div>

          <div class="mt-2">
            <button type="button" class="btn btn-sm btn-outline-secondary toggle-desc">
                ${step.desc ? 'Ukryj opis' : 'Dodaj opis'}
            </button>
          </div>
          <div class="mt-2 step-desc" style="display:${step.desc ? 'block' : 'none'};">
            <label class="form-label mb-1">Opis (opcjonalnie)</label>
            <textarea rows="2" class="form-control step-desc-input">${step.desc || ''}</textarea>
          </div>
          
          <div class="row g-2 mt-2">
            <div class="col-sm-6">
                <label class="form-label mb-1">Czas (min)</label>
                <input type="number" min="0" class="form-control step-minutes" value="${step.minutes ?? 0}" />
            </div>
          </div>

          <div class="product-list mt-3 d-flex flex-column gap-2">
            ${(step.products || []).map((p, i) => renderProductRow(p, i)).join('')}
          </div>

          <div class="mt-2">
            <button type="button" class="btn btn-sm btn-outline-primary add-product">
              <i class="fa fa-plus"></i> Dodaj produkt / działanie
            </button>
          </div>

          <div class="rotation-section mt-3" ${hasProducts ? '' : 'style="display:none;"'}>
            <div class="form-check form-switch">
              <input class="form-check-input rotation-toggle" type="checkbox" ${rotEnabled ? 'checked' : ''} ${canRotate ? '' : 'disabled'} />
              <label class="form-check-label">Czy chcesz użyć rotacji?</label>
            </div>
            <div class="mt-2 rotation-modes" style="display:${rotEnabled ? 'block' : 'none'};">
              <div class="btn-group" role="group">
                <button type="button" class="btn btn-sm ${rotMode === 'ALL' ? 'btn-primary' : 'btn-outline-primary'} rot-all">
                  Użyj wszystkiego w rotacji
                </button>
                <button type="button" class="btn btn-sm ${rotMode === 'ANY' ? 'btn-primary' : 'btn-outline-primary'} rot-any">
                  Wybierz z wielu
                </button>
              </div>
              ${!canRotate ? '<div class="form-text">Dodaj co najmniej 2 produkty, aby włączyć rotację.</div>' : ''}
            </div>
          </div>
        </div>
      `;
        }

        function render() {
            stepsRoot.innerHTML = state.steps.map(renderStepCard).join('');

            new Sortable(stepsRoot, {
                handle: '.drag-handle',
                animation: 150,
                onEnd: (evt) => {
                    const moved = state.steps.splice(evt.oldIndex, 1)[0];
                    state.steps.splice(evt.newIndex, 0, moved);
                    syncHidden();
                    render();
                }
            });

            $$('.step-card', stepsRoot).forEach((card, idx) => {
                const list = card.querySelector('.product-list');
                if (!list) return;
                new Sortable(list, {
                    handle: '.product-drag',
                    animation: 150,
                    onEnd: (evt) => {
                        const s = state.steps[idx];
                        const moved = s.products.splice(evt.oldIndex, 1)[0];
                        s.products.splice(evt.newIndex, 0, moved);
                        syncHidden();
                        render();
                    }
                });
            });

            const addBelow = document.createElement('div');
            addBelow.className = 'mt-3 d-flex justify-content-end';
            addBelow.innerHTML = `
        <button type="button" class="btn btn-outline-primary btn-sm" id="addStepBtnBottom">
          <i class="fa fa-plus me-1"></i>Dodaj krok
        </button>`;
            stepsRoot.appendChild(addBelow);

            $('#addStepBtnBottom')?.addEventListener('click', () => {
                state.steps.push({ name: '', minutes: 0, desc: '', products: [], rotation: { enabled: false, mode: 'ALL' } });
                render();
            });

            syncHidden();
        }

        if (addStepBtn) {
            addStepBtn.addEventListener('click', () => {
                state.steps.push({ id: 0, name: '', minutes: 0, desc: '', products: [], rotation: { enabled: false, mode: 'ALL' } });
                render();
            });
        }

        stepsRoot.addEventListener('click', (e) => {
            const card = e.target.closest('.step-card');
            if (!card) return;
            const idx = parseInt(card.dataset.index, 10);
            const step = state.steps[idx];

            if (e.target.closest('.remove-step')) {
                state.steps.splice(idx, 1);
                render();
                return;
            }

            if (e.target.closest('.toggle-desc')) {
                const box = card.querySelector('.step-desc');
                const btn = e.target.closest('button');
                const show = box.style.display !== 'block';
                box.style.display = show ? 'block' : 'none';
                btn.textContent = show ? 'Ukryj opis' : 'Dodaj opis';
                return;
            }

            if (e.target.closest('.add-product')) {
                openProductModal(idx);
                return;
            }

            if (e.target.closest('.product-row .edit')) {
                const row = e.target.closest('.product-row');
                const pidx = parseInt(row.dataset.pidx, 10);
                openProductModal(idx, pidx);
                return;
            }

            if (e.target.closest('.product-row .remove')) {
                const row = e.target.closest('.product-row');
                const pidx = parseInt(row.dataset.pidx, 10);
                step.products.splice(pidx, 1);
                if ((step.products?.length || 0) < 2) {
                    step.rotation = { enabled: false, mode: 'ALL' };
                }
                render();
                return;
            }

            if (e.target.closest('.rot-all')) {
                step.rotation = step.rotation || {};
                step.rotation.enabled = true;
                step.rotation.mode = 'ALL';
                render();
                return;
            }
            if (e.target.closest('.rot-any')) {
                step.rotation = step.rotation || {};
                step.rotation.enabled = true;
                step.rotation.mode = 'ANY';
                render();
                return;
            }
        });

        stepsRoot.addEventListener('change', (e) => {
            const card = e.target.closest('.step-card');
            if (!card) return;
            const idx = parseInt(card.dataset.index, 10);
            const step = state.steps[idx];

            if (e.target.classList.contains('step-name')) {
                step.name = e.target.value;
                if (!step.name.trim()) e.target.classList.add('is-invalid'); else e.target.classList.remove('is-invalid');
            }
            if (e.target.classList.contains('step-minutes')) {
                const n = parseInt(e.target.value || '0', 10);
                step.minutes = isNaN(n) || n < 0 ? 0 : n;
            }
            if (e.target.classList.contains('step-desc-input')) {
                step.desc = e.target.value || '';
            }
            if (e.target.classList.contains('rotation-toggle')) {
                const canRotate = (step.products?.length || 0) > 1;
                step.rotation = step.rotation || {};
                step.rotation.enabled = canRotate ? e.target.checked : false;
                render();
            }

            syncHidden();
        });

        let productTargetIdx = null;
        let productEditIdx = null;

        function openProductModal(stepIdx, prodIdx = null) {
            if (!productModal) return;
            productTargetIdx = stepIdx;
            productEditIdx   = prodIdx;

            const isEdit = prodIdx !== null;
            const existing = isEdit ? (state.steps[stepIdx].products[prodIdx] || {}) : {};

            prodName.value = existing.name || '';
            prodNote.value = existing.note || '';
            prodUrl.value  = existing.url  || '';
            const hidden = $('#prodImageUrl');
            if (hidden) hidden.value = existing.imageUrl || '';

            const hasImg = !!existing.imageUrl;
            const prevBox = $('#prodImgPreview');
            const prevImg = $('#prodImg');
            if (prevBox) prevBox.style.display = hasImg ? 'block' : 'none';
            if (prevImg && hasImg) prevImg.src = existing.imageUrl;

            prodName.classList.remove('is-invalid');
            productModal.show();
        }

        productSaveBtn?.addEventListener('click', () => {
            const name = (prodName.value || '').trim();
            if (!name) { prodName.classList.add('is-invalid'); return; }

            const step = state.steps[productTargetIdx];
            step.products = step.products || [];

            const imageUrl = document.getElementById('prodImageUrl')?.value || null;
            const payload = {
                name,
                note: prodNote.value || '',
                url:  prodUrl.value  || '',
                imageUrl
            };

            if (productEditIdx !== null) {
                const current = step.products[productEditIdx] || {};
                const keepId = (typeof current.id !== 'undefined') ? current.id : 0;
                step.products[productEditIdx] = { ...current, ...payload, id: keepId };
            } else {
                step.products.push({ id: 0, ...payload });
            }

            if ((step.products?.length || 0) < 2) {
                step.rotation = { enabled: false, mode: 'ALL' };
            } else {
                step.rotation = step.rotation || { enabled: false, mode: 'ALL' };
            }

            productEditIdx = null;
            productTargetIdx = null;
            productModal.hide();
            render();
        });

        try {
            if (stepsJsonEl.value) {
                const parsed = JSON.parse(stepsJsonEl.value);
                if (Array.isArray(parsed)) state.steps = parsed;
            }
        } catch {}

        render();

        form.addEventListener('submit', (e) => {
            let ok = true;
            state.steps.forEach((s) => { if (!s.name || !s.name.trim()) ok = false; });
            if (!ok) {
                e.preventDefault();
                e.stopPropagation();
                $$('.step-card .step-name', stepsRoot).forEach((inp) => {
                    if (!inp.value.trim()) inp.classList.add('is-invalid'); else inp.classList.remove('is-invalid');
                });
            } else {
                syncHidden();
            }
        });

        const drop = document.getElementById('prodDropzone');
        const file = document.getElementById('prodFile');
        const imgPrev = document.getElementById('prodImgPreview');
        const imgEl   = document.getElementById('prodImg');
        const imgUrlHidden = document.getElementById('prodImageUrl');
        const imgRemoveBtn = document.getElementById('prodImgRemove');

        function setImageUrl(url){
            if (imgUrlHidden) imgUrlHidden.value = url || '';
            if (url) {
                if (imgEl) imgEl.src = url;
                if (imgPrev) imgPrev.style.display = 'block';
            } else {
                if (imgPrev) imgPrev.style.display = 'none';
            }
        }

        async function uploadImage(f){
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
            const fd = new FormData();
            fd.append('file', f);
            const res = await fetch('/Files/Upload', {
                method: 'POST',
                headers: { 'RequestVerificationToken': token },
                body: fd
            });
            if (!res.ok) throw new Error('Upload failed');
            const data = await res.json();
            return data.url;
        }

        drop?.addEventListener('dragover', (e)=>{ e.preventDefault(); drop.classList.add('border-primary'); });
        drop?.addEventListener('dragleave', ()=> drop.classList.remove('border-primary'));
        drop?.addEventListener('drop', async (e)=>{
            e.preventDefault();
            drop.classList.remove('border-primary');
            const f = e.dataTransfer.files?.[0];
            if (f) {
                try { setImageUrl(await uploadImage(f)); } catch { alert('Nie udało się przesłać pliku.'); }
            }
        });

        file?.addEventListener('change', async (e)=>{
            const f = e.target.files?.[0];
            if (f) {
                try { setImageUrl(await uploadImage(f)); } catch { alert('Nie udało się przesłać pliku.'); }
            }
        });

        imgRemoveBtn?.addEventListener('click', ()=>{ setImageUrl(''); });
    })();
}
