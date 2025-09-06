(function () {
    const root = document.getElementById('routine-root');
    if (!root) return;
    const routineId = parseInt(root.dataset.routineId, 10);

    function ymd(d){ return `${d.getFullYear()}-${String(d.getMonth()+1).padStart(2,'0')}-${String(d.getDate()).padStart(2,'0')}`; }
    function mondayOf(date){ const d=new Date(date); const w=(d.getDay()+6)%7; d.setHours(0,0,0,0); d.setDate(d.getDate()-w); return d; }
    function firstOfMonth(d){ const x=new Date(d); x.setDate(1); x.setHours(0,0,0,0); return x; }
    function daysInMonth(y,m){ return new Date(y, m+1, 0).getDate(); }

    let pieChart, weekChart;
    let weekAnchor = new Date();
    let monthAnchor = new Date();

    async function loadStats(){
        const res = await fetch(`/Routines/RoutineStats?routineId=${routineId}`);
        if (!res.ok) return;
        const s = await res.json();

        const statsRow = document.getElementById('statsRow');
        if (statsRow) statsRow.style.display = 'flex';

        const percent = Math.round(s.overallPercent || 0);
        const overall = document.getElementById('overallPercent');
        if (overall) overall.textContent = `${percent}%`;

        const rangeInfo = document.getElementById('rangeInfo');
        if (rangeInfo){
            const start = new Date(s.startDateUtc);
            const end = s.endDateUtc ? new Date(s.endDateUtc) : null;
            rangeInfo.textContent = `Od ${start.toLocaleDateString('pl-PL')}` + (end ? ` do ${end.toLocaleDateString('pl-PL')}` : '');
        }

        const cur = document.getElementById('currentStreak');
        if (cur) cur.textContent = s.currentStreak ?? 0;
        const best = document.getElementById('bestStreak');
        if (best) best.textContent = s.bestStreak ?? 0;
        const total = document.getElementById('totalOcc');
        if (total) total.textContent = s.totalOccurrences ?? 0;
        const c = document.getElementById('completedDays');
        if (c) c.textContent = s.completedDays ?? 0;
        const p = document.getElementById('partialDays');
        if (p) p.textContent = s.partialDays ?? 0;
        const sk = document.getElementById('skippedDays');
        if (sk) sk.textContent = s.skippedDays ?? 0;

        const ctxP = document.getElementById('pieChart');
        if (ctxP && window.Chart){
            if (pieChart) pieChart.destroy();
            pieChart = new Chart(ctxP, {
                type: 'pie',
                data: {
                    labels: ['Pełne', 'Częściowe', 'Pominięte'],
                    datasets: [{
                        data: [s.completedDays||0, s.partialDays||0, s.skippedDays||0],
                        borderWidth: 1,
                        backgroundColor: [
                            'rgba(25,135,84,.85)',
                            'rgba(255,193,7,.9)',
                            'rgba(220,53,69,.9)'
                        ]
                    }]
                },
                options: {
                    maintainAspectRatio:false,
                    plugins:{ legend:{ position:'bottom', labels:{ boxWidth:12, boxHeight:12 } } }
                }
            });
        }
    }

    async function loadWeek(){
        const mon = mondayOf(weekAnchor);
        const sun = new Date(mon); sun.setDate(sun.getDate()+6);
        const weekLabel = document.getElementById('weekLabel');
        if (weekLabel) weekLabel.textContent = mon.toLocaleDateString('pl-PL') + ' – ' + sun.toLocaleDateString('pl-PL');

        const res = await fetch(`/Routines/RoutineEntries?routineId=${routineId}&from=${ymd(mon)}&to=${ymd(sun)}`);
        if (!res.ok) return;
        const data = await res.json();

        const labels = [];
        const values = [];
        const maxes = [];
        for (let i=0;i<7;i++){
            const d = new Date(mon); d.setDate(d.getDate()+i);
            labels.push(d.toLocaleDateString('pl-PL',{ weekday:'short' }));
            const key = ymd(d);
            const e = data.find(x => (x.date||'') === key);
            values.push(e?.completedSteps || 0);
            maxes.push(e?.totalSteps || 0);
        }

        const ctx = document.getElementById('weekChart');
        if (!ctx || !window.Chart) return;
        if (weekChart) weekChart.destroy();

        weekChart = new Chart(ctx, {
            type: 'bar',
            data: { labels, datasets:[{ data: values, borderWidth:1, barThickness:18, maxBarThickness:18, categoryPercentage:0.7, barPercentage:0.7 }] },
            options: {
                maintainAspectRatio:false,
                plugins:{ legend:{ display:false }, tooltip:{ callbacks:{ label:(c)=> `${c.raw}/${maxes[c.dataIndex]||0}` } } },
                scales:{ y:{ beginAtZero:true, suggestedMax: Math.max(...maxes, 1) } }
            }
        });
    }

    function firstOfMonth(d){ const x=new Date(d); x.setDate(1); x.setHours(0,0,0,0); return x; }
    function daysInMonth(y,m){ return new Date(y, m+1, 0).getDate(); }

    function buildMonthTable(map){
        const body = document.getElementById('monthBody');
        if (!body) return;
        body.innerHTML = '';
        const first = firstOfMonth(monthAnchor);
        const y = first.getFullYear(), m = first.getMonth();
        document.getElementById('monthLabel').textContent = first.toLocaleDateString('pl-PL',{ month:'long', year:'numeric' });

        const startOffset = (first.getDay()+6)%7;
        const totalDays = daysInMonth(y, m);

        let row = document.createElement('tr');
        for (let i=0;i<startOffset;i++) row.appendChild(document.createElement('td'));

        for (let day=1; day<=totalDays; day++){
            const td = document.createElement('td');
            td.textContent = day;
            const ds = `${y}-${String(m+1).padStart(2,'0')}-${String(day).padStart(2,'0')}`;
            const status = map[ds];

            if (status === 'full') td.classList.add('bg-success','text-white');
            else if (status === 'partial') td.classList.add('bg-warning','text-dark');
            else if (status === 'none') td.classList.add('bg-danger','text-white');
            else if (status === 'off') td.classList.add('bg-secondary','text-white');
            else if (status === 'future') td.classList.add('bg-primary','text-white');

            row.appendChild(td);
            const dayOfWeek = (startOffset + day -1) % 7;
            if (dayOfWeek === 6){ body.appendChild(row); row = document.createElement('tr'); }
        }
        if (row.children.length) body.appendChild(row);
    }

    async function loadMonth(){
        const first = firstOfMonth(monthAnchor);
        const y = first.getFullYear(), m = first.getMonth()+1;
        const res = await fetch(`/Routines/RoutineMonth?routineId=${routineId}&year=${y}&month=${m}`);
        if (!res.ok) return;
        const map = await res.json();
        buildMonthTable(map);
    }

    document.getElementById('prevWeek')?.addEventListener('click', ()=>{ weekAnchor.setDate(weekAnchor.getDate()-7); loadWeek(); });
    document.getElementById('nextWeek')?.addEventListener('click', ()=>{ weekAnchor.setDate(weekAnchor.getDate()+7); loadWeek(); });

    document.getElementById('prevMonth')?.addEventListener('click', ()=>{ monthAnchor.setMonth(monthAnchor.getMonth()-1); loadMonth(); });
    document.getElementById('nextMonth')?.addEventListener('click', ()=>{ monthAnchor.setMonth(monthAnchor.getMonth()+1); loadMonth(); });

    const toggleStructure = document.getElementById('toggleStructure');
    const structureBody = document.getElementById('structureBody');
    if (toggleStructure && structureBody){
        toggleStructure.addEventListener('click', (e)=>{
            e.preventDefault();
            e.stopPropagation();
            const willOpen = structureBody.classList.contains('d-none');
            structureBody.classList.toggle('d-none', !willOpen ? true : false);
            toggleStructure.innerHTML = `<i class="fa fa-chevron-${willOpen ? 'up':'down'}"></i>`;
        });
    }

    (async function init(){
        await loadStats();
        await loadWeek();
        await loadMonth();
    })();
})();
