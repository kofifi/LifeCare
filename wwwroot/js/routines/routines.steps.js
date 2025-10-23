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

        const startInput = () => document.getElementById('startDate');

        function addDays(d, n){ const x = new Date(d); x.setUTCDate(x.getUTCDate()+n); return x; }
        function addMonths(d, n){ const x = new Date(d); x.setUTCMonth(x.getUTCMonth()+n); return x; }
        function fmtDateUTC(d){
            const y = d.getUTCFullYear();
            const m = String(d.getUTCMonth()+1).padStart(2,'0');
            const da = String(d.getUTCDate()).padStart(2,'0');
            return `${y}-${m}-${da}`;
        }

        function parseRRule(rrule){
            if (!rrule) return {};
            return rrule.split(';')
                .map(s=>s.split('='))
                .filter(a=>a.length===2)
                .reduce((m,[k,v]) => (m[k.toUpperCase()] = v.toUpperCase(), m), {});
        }

        function ensureUniqueRadioNames(stepCard){
            let uid = stepCard.getAttribute('data-step-uid');
            if (!uid) {
                uid = 's' + Math.random().toString(36).slice(2,8);
                stepCard.setAttribute('data-step-uid', uid);
            }
            const freqName = `freq_${uid}`;
            const endName  = `end_${uid}`;
            stepCard.querySelectorAll('input.step-freq[name="__freq__"]').forEach(r => r.name = freqName);
            stepCard.querySelectorAll('input.step-end-mode[name="__end__"]').forEach(r => r.name = endName);
            return { freqName, endName };
        }

        function toggleFreqBlocks(recur, freq){
            const w = recur.querySelector('.step-weekly');
            const m = recur.querySelector('.step-monthly');
            if (w) w.classList.toggle('d-none', freq!=='WEEKLY');
            if (m) m.classList.toggle('d-none', freq!=='MONTHLY');
        }

        function hydrateStepRecurrence(stepCard, rrule){
            const recur = stepCard.querySelector('[data-recur]');
            if (!recur) return;

            const noRepeat = recur.querySelector('.step-no-repeat');
            const body = recur.querySelector('.step-recur-body');
            const { freqName, endName } = ensureUniqueRadioNames(stepCard);

            const map = parseRRule(rrule || '');

            const monthlyGrid = recur.querySelector('.step-monthly .d-grid');
            if (monthlyGrid && !monthlyGrid.querySelector('.step-md-btn[data-day="31"]')){
                monthlyGrid.innerHTML = Array.from({length:31}, (_,i)=>i+1)
                    .map(d=>`<button type="button" class="btn btn-outline-secondary btn-sm step-md-btn" data-day="${d}">${d}</button>`)
                    .join('');
            }

            if (!rrule || (map.COUNT === '1')) {
                noRepeat.checked = true;
                body.classList.add('d-none');
                return;
            }

            noRepeat.checked = false;
            body.classList.remove('d-none');

            const interval = parseInt(map.INTERVAL || '1', 10);
            recur.querySelector('.step-interval-input').value = interval;
            recur.querySelectorAll('.step-interval-btn').forEach(b => {
                b.classList.toggle('active', +b.dataset.val === interval);
            });

            const freq = map.FREQ || 'DAILY';
            stepCard.querySelectorAll(`input.step-freq[name="${freqName}"]`).forEach(r => {
                r.checked = (r.value === freq);
            });
            toggleFreqBlocks(recur, freq);

            if (freq === 'WEEKLY' && map.BYDAY){
                const set = new Set(map.BYDAY.split(','));
                recur.querySelectorAll('.step-day-btn').forEach(b => {
                    b.classList.toggle('active', set.has(b.dataset.day));
                });
            }

            if (freq === 'MONTHLY' && map.BYMONTHDAY){
                const set = new Set(map.BYMONTHDAY.split(',').map(Number));
                recur.querySelectorAll('.step-md-btn').forEach(b => {
                    b.classList.toggle('active', set.has(+b.dataset.day));
                });
            }

            const endSwitch = recur.querySelector('.step-end-switch');
            const endBody = recur.querySelector('.step-end-body');
            const endModeRadios = stepCard.querySelectorAll(`.step-end-mode[name="${endName}"]`);
            const endDate = recur.querySelector('.step-end-date');

            if (map.UNTIL){
                endSwitch.checked = true;
                endBody.classList.remove('d-none');
                if (/^\d{4}-\d{2}-\d{2}$/.test(map.UNTIL)){
                    endModeRadios.forEach(r => r.checked = (r.value === 'DATE'));
                    endDate.value = map.UNTIL;
                } else {
                    endModeRadios.forEach(r => r.checked = (r.value === 'AFTER'));
                }
            } else {
                endSwitch.checked = false;
                endBody.classList.add('d-none');
            }
        }

        function buildStepRRule(stepCard){
            const recur = stepCard.querySelector('[data-recur]');
            if (!recur) return null;

            const noRepeat = recur.querySelector('.step-no-repeat');
            if (noRepeat.checked) return 'COUNT=1';

            const { freqName, endName } = ensureUniqueRadioNames(stepCard);

            const interval = parseInt(recur.querySelector('.step-interval-input').value || '1', 10) || 1;
            const freq = stepCard.querySelector(`input.step-freq[name="${freqName}"]:checked`)?.value || 'DAILY';

            const parts = [];
            parts.push(`FREQ=${freq}`);
            if (interval > 1) parts.push(`INTERVAL=${interval}`);

            if (freq === 'WEEKLY'){
                const activeDays = Array.from(recur.querySelectorAll('.step-day-btn.active')).map(b => b.dataset.day);
                if (activeDays.length > 0) parts.push(`BYDAY=${activeDays.join(',')}`);
            }
            if (freq === 'MONTHLY'){
                const activeMd = Array.from(recur.querySelectorAll('.step-md-btn.active')).map(b => +b.dataset.day);
                if (activeMd.length > 0) parts.push(`BYMONTHDAY=${activeMd.join(',')}`);
            }

            const endSwitch = recur.querySelector('.step-end-switch');
            if (endSwitch.checked){
                const endMode = stepCard.querySelector(`.step-end-mode[name="${endName}"]:checked`)?.value || 'AFTER';
                const startStr = (startInput()?.value || '').trim(); // yyyy-MM-dd
                if (endMode === 'DATE'){
                    const u = recur.querySelector('.step-end-date').value;
                    if (u) parts.push(`UNTIL=${u}`);
                } else {
                    if (startStr){
                        const [y,m,d] = startStr.split('-').map(Number);
                        const start = new Date(Date.UTC(y, m-1, d));
                        const count = Math.max(1, parseInt(recur.querySelector('.step-end-count').value || '30', 10));
                        const unit = recur.querySelector('.step-end-unit').value; // DAYS/WEEKS/MONTHS
                        let until = new Date(start);
                        if (unit === 'DAYS')   until = addDays(start, count);
                        if (unit === 'WEEKS')  until = addDays(start, count*7);
                        if (unit === 'MONTHS') until = addMonths(start, count);
                        parts.push(`UNTIL=${fmtDateUTC(until)}`);
                    }
                }
            }

            return parts.join(';');
        }

        function wireStepRecurrence(stepCard, stepObj){
            const recur = stepCard.querySelector('[data-recur]');
            if (!recur) return;

            ensureUniqueRadioNames(stepCard);

            recur.addEventListener('click', (e)=>{
                const b = e.target.closest('.step-interval-btn');
                if (!b) return;
                recur.querySelectorAll('.step-interval-btn').forEach(x => x.classList.remove('active'));
                b.classList.add('active');
                recur.querySelector('.step-interval-input').value = b.dataset.val;
                stepObj.rrule = buildStepRRule(stepCard) || null;
                syncHidden();
            });

            stepCard.addEventListener('change', (e)=>{
                const r = e.target.closest('input.step-freq');
                if (!r) return;
                toggleFreqBlocks(recur, r.value);
                stepObj.rrule = buildStepRRule(stepCard) || null;
                syncHidden();
            });

            recur.addEventListener('click', (e)=>{
                const d = e.target.closest('.step-day-btn');
                if (d) {
                    d.classList.toggle('active');
                    stepObj.rrule = buildStepRRule(stepCard) || null;
                    syncHidden();
                    return;
                }
                const md = e.target.closest('.step-md-btn');
                if (md) {
                    md.classList.toggle('active');
                    stepObj.rrule = buildStepRRule(stepCard) || null;
                    syncHidden();
                    return;
                }
            });

            recur.querySelector('.step-no-repeat').addEventListener('change', (e)=>{
                const body = recur.querySelector('.step-recur-body');
                body.classList.toggle('d-none', e.target.checked);
                stepObj.rrule = buildStepRRule(stepCard) || null;
                syncHidden();
            });

            recur.querySelector('.step-end-switch').addEventListener('change', (e)=>{
                recur.querySelector('.step-end-body').classList.toggle('d-none', !e.target.checked);
                stepObj.rrule = buildStepRRule(stepCard) || null;
                syncHidden();
            });

            recur.addEventListener('change', (e)=>{
                if (e.target.classList.contains('step-end-mode')
                    || e.target.classList.contains('step-end-count')
                    || e.target.classList.contains('step-end-unit')
                    || e.target.classList.contains('step-end-date')) {
                    stepObj.rrule = buildStepRRule(stepCard) || null;
                    syncHidden();
                }
            });
        }

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
          
          <div class="step-recur mt-3" data-recur>
              <div class="d-flex justify-content-between align-items-center">
                <strong>Powtarzalność</strong>
                <div class="form-check form-switch">
                  <input class="form-check-input step-no-repeat" type="checkbox">
                  <label class="form-check-label">Nigdy</label>
                </div>
              </div>
            
              <div class="step-recur-body mt-2">
                <div class="mb-2">
                  <div class="btn-group flex-wrap" role="group" aria-label="Interwał">
                    <input type="hidden" class="step-interval-input" value="1"/>
                    <button type="button" class="btn btn-outline-primary btn-sm step-interval-btn active" data-val="1">Każdy</button>
                    <button type="button" class="btn btn-outline-primary btn-sm step-interval-btn" data-val="2">2</button>
                    <button type="button" class="btn btn-outline-primary btn-sm step-interval-btn" data-val="3">3</button>
                    <button type="button" class="btn btn-outline-primary btn-sm step-interval-btn" data-val="4">4</button>
                    <button type="button" class="btn btn-outline-primary btn-sm step-interval-btn" data-val="5">5</button>
                    <button type="button" class="btn btn-outline-primary btn-sm step-interval-btn" data-val="6">6</button>
                    <button type="button" class="btn btn-outline-primary btn-sm step-interval-btn" data-val="7">7</button>
                    <button type="button" class="btn btn-outline-primary btn-sm step-interval-btn" data-val="8">8</button>
                    <button type="button" class="btn btn-outline-primary btn-sm step-interval-btn" data-val="9">9</button>
                    <button type="button" class="btn btn-outline-primary btn-sm step-interval-btn" data-val="10">10</button>
                  </div>
                  <div class="form-text">Interwał (np. „co 2 tygodnie”)</div>
                </div>
            
                <div class="mb-2">
                  <div class="form-check">
                    <input class="form-check-input step-freq" type="radio" name="__freq__" value="DAILY" checked>
                    <label class="form-check-label">Dzień</label>
                  </div>
                  <div class="form-check">
                    <input class="form-check-input step-freq" type="radio" name="__freq__" value="WEEKLY">
                    <label class="form-check-label">Tydzień</label>
                  </div>
                  <div class="form-check">
                    <input class="form-check-input step-freq" type="radio" name="__freq__" value="MONTHLY">
                    <label class="form-check-label">Miesiąc</label>
                  </div>
                </div>
            
                <div class="mb-2 step-weekly d-none">
                  <div class="mb-1">W wybranych dniach:</div>
                  <div class="btn-group flex-wrap">
                    <button type="button" class="btn btn-outline-secondary btn-sm step-day-btn" data-day="MO">Pn</button>
                    <button type="button" class="btn btn-outline-secondary btn-sm step-day-btn" data-day="TU">Wt</button>
                    <button type="button" class="btn btn-outline-secondary btn-sm step-day-btn" data-day="WE">Śr</button>
                    <button type="button" class="btn btn-outline-secondary btn-sm step-day-btn" data-day="TH">Cz</button>
                    <button type="button" class="btn btn-outline-secondary btn-sm step-day-btn" data-day="FR">Pt</button>
                    <button type="button" class="btn btn-outline-secondary btn-sm step-day-btn" data-day="SA">Sb</button>
                    <button type="button" class="btn btn-outline-secondary btn-sm step-day-btn" data-day="SU">Nd</button>
                  </div>
                </div>
            
                <div class="mb-2 step-monthly d-none">
                  <div class="mb-1">Dni miesiąca:</div>
                  <div class="d-grid" style="grid-template-columns: repeat(7, minmax(0,1fr)); gap:.25rem;">
                  </div>
                </div>
            
                <div class="mt-3">
                  <div class="form-check form-switch mb-2">
                    <input class="form-check-input step-end-switch" type="checkbox">
                    <label class="form-check-label">Zakończ się po pewnym czasie</label>
                  </div>
            
                  <div class="step-end-body d-none">
                    <div class="mb-3">
                      <div class="form-check">
                        <input class="form-check-input step-end-mode" type="radio" name="__end__" value="AFTER" checked>
                        <label class="form-check-label">Po czasie</label>
                      </div>
            
                      <div class="d-flex flex-wrap align-items-center gap-2 mt-1">
                        <input type="number" class="form-control step-end-count" min="1" value="30" style="max-width: 140px;"/>
                        <select class="form-select step-end-unit" style="max-width: 200px;">
                          <option value="DAYS">dniach</option>
                          <option value="WEEKS">tygodniach</option>
                          <option value="MONTHS">miesiącach</option>
                        </select>
                      </div>
                    </div>
            
                    <div class="mb-2">
                      <div class="form-check">
                        <input class="form-check-input step-end-mode" type="radio" name="__end__" value="DATE">
                        <label class="form-check-label">Do dnia</label>
                      </div>
                      <input type="date" class="form-control mt-1 step-end-date" style="max-width: 260px;"/>
                    </div>
                  </div>
                </div>
              </div>
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
                if (list) {
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
                }

                const step = state.steps[idx];
                wireStepRecurrence(card, step);
                hydrateStepRecurrence(card, step.rrule || null);
            });

            const addBelow = document.createElement('div');
            addBelow.className = 'mt-3 d-flex justify-content-end';
            addBelow.innerHTML = `
        <button type="button" class="btn btn-outline-primary btn-sm" id="addStepBtnBottom">
          <i class="fa fa-plus me-1"></i>Dodaj krok
        </button>`;
            stepsRoot.appendChild(addBelow);

            $('#addStepBtnBottom')?.addEventListener('click', () => {
                state.steps.push({ id: 0, name: '', minutes: 0, desc: '', products: [], rotation: { enabled: false, mode: 'ALL' }, rrule: 'COUNT=1' });
                render();
            });

            syncHidden();
        }

        if (addStepBtn) {
            addStepBtn.addEventListener('click', () => {
                state.steps.push({ id: 0, name: '', minutes: 0, desc: '', products: [], rotation: { enabled: false, mode: 'ALL' }, rrule: 'COUNT=1' });
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

            const newR = buildStepRRule(card);
            step.rrule = newR || null;

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
                if (Array.isArray(parsed)) {
                    parsed.forEach(s => { if (typeof s.rrule === 'undefined') s.rrule = 'COUNT=1'; });
                    state.steps = parsed;
                }
            }
        } catch {}

        render();

        $('#startDate')?.addEventListener('change', ()=>{
            $$('.step-card', stepsRoot).forEach((card, idx)=>{
                const newR = buildStepRRule(card);
                state.steps[idx].rrule = newR || null;
            });
            syncHidden();
        });

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
                $$('.step-card', stepsRoot).forEach((card, idx)=>{
                    const r = buildStepRRule(card);
                    state.steps[idx].rrule = r || null;
                });
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
