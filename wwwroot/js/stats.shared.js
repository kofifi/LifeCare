// wwwroot/js/stats.shared.js
(function () {
    function ymd(d){ return `${d.getFullYear()}-${String(d.getMonth()+1).padStart(2,'0')}-${String(d.getDate()).padStart(2,'0')}`; }
    function mondayOf(date){ const d=new Date(date); const w=(d.getDay()+6)%7; d.setHours(0,0,0,0); d.setDate(d.getDate()-w); return d; }
    function firstOfMonth(d){ const x=new Date(d); x.setDate(1); x.setHours(0,0,0,0); return x; }
    function daysInMonth(y,m){ return new Date(y, m+1, 0).getDate(); }

    function mountWeekChart(ctx, labels, values, maxes){
        if (!ctx || !window.Chart) return null;
        return new Chart(ctx, {
            type: 'bar',
            data: {
                labels,
                datasets: [{ data: values, borderWidth: 1, barThickness: 18, maxBarThickness: 18, categoryPercentage: 0.7, barPercentage: 0.7 }]
            },
            options: {
                maintainAspectRatio: false,
                plugins: { legend: { display:false }, tooltip: { callbacks: { label: (c)=> (maxes ? `${c.raw}/${maxes[c.dataIndex]||0}` : c.raw) } } },
                scales: { y: { beginAtZero:true, suggestedMax: Math.max(...(maxes||values), 1) } }
            }
        });
    }

    function mountPieChart(ctx, labels, data, colors){
        if (!ctx || !window.Chart) return null;
        return new Chart(ctx, {
            type: 'pie',
            data: { labels, datasets: [{ data, borderWidth: 1, backgroundColor: colors }] },
            options: { maintainAspectRatio:false, plugins:{ legend:{ position:'bottom', labels:{ boxWidth:12, boxHeight:12 } } } }
        });
    }

    async function json(url){
        const r = await fetch(url);
        if (!r.ok) return null;
        return r.json();
    }

    window.StatsShared = {
        init: async function init(opts){
            const {
                rootId,                                 // "#routine-root" / "#habit-root"
                kind,                                   // "routine" | "habit"
                entityId,                               // number
                endpoints,                              // { stats, week, month }
                labels,                                 // { weekTitle?, pieLabels:[], pieColors:[], calendarLegend? }
                domIds,                                 // { weekLabel, weekCanvas, pieCanvas, statsRow, monthLabel, monthBody, skipsCard?, skipsList? }
                fillStats                               // function(statsJson) -> ustawia pola liczbowe dla danego widoku
            } = opts;

            const root = document.querySelector(rootId);
            if (!root) return;

            let pieChart = null, weekChart = null;
            let weekAnchor = new Date();
            let monthAnchor = new Date();

            async function loadStats(){
                const s = await json(`${endpoints.stats}?${kind}Id=${entityId}`);
                if (!s) return;

                const statsRow = document.getElementById(domIds.statsRow);
                if (statsRow) statsRow.style.display = 'flex';

                fillStats(s);

                if (domIds.skipsCard && domIds.skipsList && s.topSkippedSteps !== undefined){
                    const raw = s.topSkippedSteps || [];
                    const list = raw.filter(i => (i.skippedCount||0) > 0);
                    const skipsCard = document.getElementById(domIds.skipsCard);
                    const skipsList = document.getElementById(domIds.skipsList);
                    if (skipsCard && skipsList){
                        skipsCard.style.display = '';
                        skipsList.innerHTML = list.length ? '' : `<div class="text-muted">Brak pominiętych kroków</div>`;
                        list.forEach(i=>{
                            const row = document.createElement('div');
                            row.className = 'd-flex justify-content-between align-items-center';
                            row.innerHTML = `<div class="text-truncate me-3">${i.name}</div><div class="small text-muted">${i.skippedCount}× pominięte</div>`;
                            skipsList.appendChild(row);
                        });
                    }
                }

                if (domIds.pieCanvas){
                    const ctxP = document.getElementById(domIds.pieCanvas);
                    if (pieChart) pieChart.destroy();
                    if (kind === 'routine'){
                        pieChart = mountPieChart(
                            ctxP,
                            labels.pieLabels || ['Pełne','Częściowe','Pominięte'],
                            [s.completedDays||0, s.partialDays||0, s.skippedDays||0],
                            labels.pieColors || ['rgba(25,135,84,.85)','rgba(255,193,7,.9)','rgba(220,53,69,.9)']
                        );
                    } else {
                        pieChart = mountPieChart(
                            ctxP,
                            labels.pieLabels || ['Ukończone','Nieukończone'],
                            [s.completed||0, s.skipped||0],
                            labels.pieColors || ['rgba(25,135,84,.85)','rgba(220,53,69,.9)']
                        );
                    }
                }
            }

            async function loadWeek(){
                const mon = mondayOf(weekAnchor);
                const sun = new Date(mon); sun.setDate(sun.getDate()+6);

                const weekLbl = document.getElementById(domIds.weekLabel);
                if (weekLbl) weekLbl.textContent = mon.toLocaleDateString('pl-PL') + ' – ' + sun.toLocaleDateString('pl-PL');

                const data = await json(`${endpoints.week}?${kind}Id=${entityId}&from=${ymd(mon)}&to=${ymd(sun)}`);
                if (!data) return;

                const labelsArr = [];
                const values = [];
                const maxes = (kind === 'routine') ? [] : null;

                for (let i=0;i<7;i++){
                    const d = new Date(mon); d.setDate(d.getDate()+i);
                    labelsArr.push(d.toLocaleDateString('pl-PL', { weekday:'short' }));
                    const key = ymd(d);
                    if (kind === 'routine'){
                        const e = data.find(x => (x.date||'') === key);
                        values.push(e?.completedSteps || 0);
                        maxes.push(e?.totalSteps || 0);
                    } else {
                        const e = data.find(x => (x.date||'').startsWith(key));
                        values.push(e?.completed ? 1 : (e?.quantity ?? 0));
                    }
                }

                const ctx = document.getElementById(domIds.weekCanvas);
                if (!ctx) return;
                if (weekChart) weekChart.destroy();
                weekChart = mountWeekChart(ctx, labelsArr, values, maxes);
            }

            function buildMonthTable(map){
                const body = document.getElementById(domIds.monthBody);
                if (!body) return;
                body.innerHTML = '';
                const first = firstOfMonth(monthAnchor);
                const y = first.getFullYear(), m = first.getMonth();

                const monthLbl = document.getElementById(domIds.monthLabel);
                if (monthLbl) monthLbl.textContent = first.toLocaleDateString('pl-PL',{ month:'long', year:'numeric' });

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
                    const dayOfWeek = (startOffset + day - 1) % 7;
                    if (dayOfWeek === 6){ body.appendChild(row); row = document.createElement('tr'); }
                }
                if (row.children.length) body.appendChild(row);
            }

            async function loadMonth(){
                const first = firstOfMonth(monthAnchor);
                const y = first.getFullYear(), m = first.getMonth()+1;
                const map = await json(`${endpoints.month}?${kind}Id=${entityId}&year=${y}&month=${m}`);
                if (map) buildMonthTable(map);
            }

            document.getElementById('prevWeek')?.addEventListener('click', ()=>{ weekAnchor.setDate(weekAnchor.getDate()-7); loadWeek(); });
            document.getElementById('nextWeek')?.addEventListener('click', ()=>{ weekAnchor.setDate(weekAnchor.getDate()+7); loadWeek(); });
            document.getElementById('prevMonth')?.addEventListener('click', ()=>{ monthAnchor.setMonth(monthAnchor.getMonth()-1); loadMonth(); });
            document.getElementById('nextMonth')?.addEventListener('click', ()=>{ monthAnchor.setMonth(monthAnchor.getMonth()+1); loadMonth(); });

            await loadStats();
            await loadWeek();
            await loadMonth();

            return {
                reloadAll: async ()=>{ await loadStats(); await loadWeek(); await loadMonth(); }
            };
        }
    };
})();
