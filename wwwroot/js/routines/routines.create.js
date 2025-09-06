document.addEventListener('DOMContentLoaded', function () {
    const startDateEl   = document.getElementById('startDate');
    const timeEl        = document.getElementById('timeOfDay');
    const noRepeat      = document.getElementById('noRepeatSwitch');
    const repeatBody    = document.getElementById('repeatBody');

    const intervalGroup = document.getElementById('intervalGroup');
    const intervalBtns  = Array.from(intervalGroup?.querySelectorAll('.interval-btn') || []);
    let   intervalInput = document.getElementById('intervalInput');
    if (!intervalInput && intervalGroup) {
        intervalInput = document.createElement('input');
        intervalInput.type  = 'hidden';
        intervalInput.id    = 'intervalInput';
        intervalInput.value = '1';
        intervalGroup.appendChild(intervalInput);
    }

    const freqRadios    = Array.from(document.querySelectorAll('input.freq-radio[name="freq"]'));
    const weeklyPicker  = document.getElementById('weeklyPicker');
    const bydayBtns     = Array.from(document.querySelectorAll('.day-btn'));
    const monthlyPicker = document.getElementById('monthlyPicker');
    const mdBtns        = Array.from(document.querySelectorAll('.md-btn'));

    const endSwitch = document.getElementById('endSwitch');
    const endBody   = document.getElementById('endBody');
    const endRadios = Array.from(document.querySelectorAll('.end-radio'));
    const endCount  = document.getElementById('endCount');
    const endUnit   = document.getElementById('endUnit');
    const endDate   = document.getElementById('endDate');

    const rruleHidden = document.getElementById('rruleHidden') || document.getElementById('RRule');
    const form        = document.getElementById('routineForm') || document.querySelector('form[asp-action="Create"]');

    const isEdit = window.__LC_IS_EDIT === true;

    if (timeEl && !timeEl.value) timeEl.value = '06:00';

    const toggleDescBtn = document.getElementById('toggleDescription');
    if (toggleDescBtn) {
        toggleDescBtn.addEventListener('click', function () {
            const w = document.getElementById('descriptionWrapper');
            const visible = w.style.display !== 'none';
            w.style.display = visible ? 'none' : 'block';
            this.innerHTML = visible
                ? '<i class="fa fa-plus"></i> Dodaj opis'
                : '<i class="fa fa-minus"></i> Ukryj opis';
        });
    }

    intervalBtns.forEach(btn => {
        btn.addEventListener('click', () => {
            intervalBtns.forEach(b => b.classList.remove('active'));
            btn.classList.add('active');
            if (intervalInput) intervalInput.value = btn.dataset.val || btn.dataset.value || '1';
        });
    });

    function updateFreqUI() {
        const selected = document.querySelector('input.freq-radio[name="freq"]:checked');
        const val = selected ? selected.value : 'DAILY';
        if (weeklyPicker)  weeklyPicker.style.display  = (val === 'WEEKLY')  ? '' : 'none';
        if (monthlyPicker) monthlyPicker.style.display = (val === 'MONTHLY') ? '' : 'none';
    }
    freqRadios.forEach(r => r.addEventListener('change', updateFreqUI));
    updateFreqUI();

    function updateNoRepeat() {
        if (!repeatBody || !noRepeat) return;
        repeatBody.style.display = noRepeat.checked ? 'none' : '';
    }
    if (noRepeat) {
        noRepeat.addEventListener('change', updateNoRepeat);
        updateNoRepeat();
    }

    function updateEnd() {
        if (!endBody || !endSwitch) return;
        endBody.style.display = endSwitch.checked ? '' : 'none';
    }
    if (endSwitch) {
        endSwitch.addEventListener('change', updateEnd);
        updateEnd();
    }

    bydayBtns.forEach(b => b.addEventListener('click', () => b.classList.toggle('active')));
    mdBtns.forEach(b   => b.addEventListener('click', () => b.classList.toggle('active')));

    function anyDaySelected() {
        return bydayBtns.some(b => b.classList.contains('active')) ||
            mdBtns.some(b   => b.classList.contains('active'));
    }
    function toByDay(dow) { return ['SU','MO','TU','WE','TH','FR','SA'][dow]; }
    function autoPickFromStart() {
        if (!startDateEl || !startDateEl.value) return;
        if (isEdit && anyDaySelected()) return;

        const d = new Date(startDateEl.value + 'T00:00:00');

        bydayBtns.forEach(b => b.classList.remove('active'));
        const wd = toByDay(d.getDay());
        const dayBtn = bydayBtns.find(x => (x.dataset.day || x.dataset.value) === wd);
        if (dayBtn) dayBtn.classList.add('active');

        mdBtns.forEach(b => b.classList.remove('active'));
        const dom = d.getDate();
        const mdBtn = mdBtns.find(x => (x.dataset.day || x.dataset.value) == String(dom));
        if (mdBtn) mdBtn.classList.add('active');
    }
    if (startDateEl) {
        startDateEl.addEventListener('change', () => {
            if (!anyDaySelected()) autoPickFromStart();
        });
        if (!isEdit) autoPickFromStart();
    }

    function buildRRule() {
        if (noRepeat && noRepeat.checked) return 'COUNT=1';

        const freqEl   = document.querySelector('input.freq-radio[name="freq"]:checked');
        const freq     = freqEl ? freqEl.value : 'DAILY';
        const interval = Math.max(1, parseInt(intervalInput?.value || '1', 10));
        const parts    = [`FREQ=${freq}`, `INTERVAL=${interval}`];

        if (freq === 'WEEKLY') {
            const days = bydayBtns
                .filter(b => b.classList.contains('active'))
                .map(b => b.dataset.day || b.dataset.value)
                .filter(Boolean);
            if (days.length) parts.push(`BYDAY=${days.join(',')}`);
        }

        if (freq === 'MONTHLY') {
            const md = mdBtns
                .filter(b => b.classList.contains('active'))
                .map(b => b.dataset.day || b.dataset.value)
                .filter(Boolean);
            if (md.length) parts.push(`BYMONTHDAY=${md.join(',')}`);
        }

        if (endSwitch && endSwitch.checked) {
            const mode = document.querySelector('.end-radio:checked')?.value || 'AFTER';
            if (mode === 'DATE') {
                if (endDate?.value) parts.push(`UNTIL=${endDate.value}`);
            } else {
                const n = Math.max(1, parseInt(endCount?.value || '1', 10));
                if (startDateEl?.value) {
                    const d = new Date(startDateEl.value + 'T00:00:00');
                    const unit = endUnit?.value || 'DAYS';
                    if (unit === 'DAYS')   d.setDate(d.getDate() + n - 1);
                    if (unit === 'WEEKS')  d.setDate(d.getDate() + 7 * n - 1);
                    if (unit === 'MONTHS') d.setMonth(d.getMonth() + n - 1);
                    const y = d.getFullYear();
                    const m = String(d.getMonth()+1).padStart(2,'0');
                    const dd= String(d.getDate()).padStart(2,'0');
                    parts.push(`UNTIL=${y}-${m}-${dd}`);
                } else {
                    parts.push(`COUNT=${n}`);
                }
            }
        }

        return parts.join(';');
    }
    window.buildRRule = buildRRule;

    form.addEventListener('submit', (e) => {
        const warn = document.getElementById('stepsValidationMsg');
        const stepsJsonEl = document.getElementById('StepsJson');
        let steps = [];
        try { steps = JSON.parse(stepsJsonEl?.value || '[]'); } catch { steps = []; }

        if (!steps.length) {
            e.preventDefault(); e.stopPropagation();
            if (warn) {
                warn.classList.remove('d-none');
                warn.textContent = 'Dodaj co najmniej jeden krok.';
                warn.scrollIntoView({ behavior: 'smooth', block: 'center' });
            }
            return;
        } else if (warn) {
            warn.classList.add('d-none');
        }

        const rule = buildRRule();
        if (rruleHidden) rruleHidden.value = rule;
    });
});
