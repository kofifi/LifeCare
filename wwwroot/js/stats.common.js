(function(){
    function ymd(d){return `${d.getFullYear()}-${String(d.getMonth()+1).padStart(2,'0')}-${String(d.getDate()).padStart(2,'0')}`}
    function mondayOf(date){const d=new Date(date);const w=(d.getDay()+6)%7;d.setHours(0,0,0,0);d.setDate(d.getDate()-w);return d}
    function firstOfMonth(d){const x=new Date(d);x.setDate(1);x.setHours(0,0,0,0);return x}
    function daysInMonth(y,m){return new Date(y,m+1,0).getDate()}

    function normalizeStats(stats, kind){
        if(kind==='routine'){
            return {
                overallPercent: Math.round(stats.overallPercent||0),
                currentStreak: stats.currentStreak||0,
                bestStreak: stats.bestStreak||0,
                total: stats.totalOccurrences||0,
                completed: stats.completedDays||0,
                partial: stats.partialDays||0,
                skipped: stats.skippedDays||0,
                startDateUtc: stats.StartDateUtc || stats.startDateUtc,
                endDateUtc: stats.EndDateUtc || stats.endDateUtc,
                topSkippedSteps: stats.topSkippedSteps||[]
            }
        } else {
            return {
                overallPercent: Math.round(stats.overallPercent||0),
                currentStreak: stats.currentStreak||0,
                bestStreak: stats.bestStreak||0,
                total: stats.total||0,
                completed: stats.completed||0,
                partial: stats.partial||0,
                skipped: stats.skipped||0,
                startDateUtc: stats.startDateUtc||null,
                endDateUtc: stats.endDateUtc||null,
                topSkippedSteps: []
            }
        }
    }

    async function fetchJson(url){const r=await fetch(url);if(!r.ok) return null;return await r.json()}

    async function init(opts){
        const {
            kind,
            selectors,
            endpoints
        } = opts;

        const pieCtx = document.querySelector(selectors.pieCanvas);
        const weekCtx = document.querySelector(selectors.weekCanvas);
        const weekLabelEl = document.querySelector(selectors.weekLabel);
        const monthLabelEl = document.querySelector(selectors.monthLabel);
        const monthBodyEl = document.querySelector(selectors.monthBody);
        const statsRowEl = document.querySelector(selectors.statsRow);
        const overallEl = document.querySelector(selectors.overallPercent);
        const curEl = document.querySelector(selectors.currentStreak);
        const bestEl = document.querySelector(selectors.bestStreak);
        const totalEl = document.querySelector(selectors.totalCount);
        const compEl = document.querySelector(selectors.completedCount);
        const partEl = document.querySelector(selectors.partialCount);
        const skipEl = document.querySelector(selectors.skippedCount);
        const rangeInfoEl = document.querySelector(selectors.rangeInfo||null);
        const skipsCard = document.querySelector(selectors.skipsCard||null);
        const skipsList = document.querySelector(selectors.skipsList||null);

        let pieChart, weekChart;
        let weekAnchor = new Date();
        let monthAnchor = new Date();

        async function loadStats(){
            const data = await fetchJson(endpoints.stats());
            if(!data) return;
            const s = normalizeStats(data, kind);
            if(statsRowEl) statsRowEl.style.display='flex';
            if(overallEl) overallEl.textContent = `${s.overallPercent}%`;
            if(curEl) curEl.textContent = s.currentStreak;
            if(bestEl) bestEl.textContent = s.bestStreak;
            if(totalEl) totalEl.textContent = s.total;
            if(compEl) compEl.textContent = s.completed;
            if(partEl) partEl.textContent = s.partial;
            if(skipEl) skipEl.textContent = s.skipped;
            if(rangeInfoEl){
                const start = s.startDateUtc? new Date(s.startDateUtc):null;
                const end = s.endDateUtc? new Date(s.endDateUtc):null;
                rangeInfoEl.textContent = start? `Od ${start.toLocaleDateString('pl-PL')}` + (end? ` do ${end.toLocaleDateString('pl-PL')}`:'') : '';
            }
            if(skipsCard && skipsList){
                const list = (s.topSkippedSteps||[]).filter(x=> (x.skippedCount||0)>0);
                skipsCard.style.display='';
                skipsList.innerHTML='';
                if(list.length){
                    list.forEach(i=>{
                        const row=document.createElement('div');
                        row.className='d-flex justify-content-between align-items-center';
                        row.innerHTML = `<div class="text-truncate me-3">${i.name}</div><div class="small text-muted">${i.skippedCount}× pominięte</div>`;
                        skipsList.appendChild(row);
                    });
                } else {
                    skipsList.innerHTML = `<div class="text-muted">Brak pominiętych kroków</div>`;
                }
            }
            if(pieCtx && window.Chart){
                if(pieChart) pieChart.destroy();
                const labels = kind==='routine' ? ['Pełne','Częściowe','Pominięte'] : ['Ukończone','Nieukończone'];
                const dataArr = kind==='routine' ? [s.completed, s.partial, s.skipped] : [s.completed, s.skipped];
                const colors = kind==='routine'
                    ? ['rgba(25,135,84,.85)','rgba(255,193,7,.9)','rgba(220,53,69,.9)']
                    : ['rgba(25,135,84,.85)','rgba(220,53,69,.9)'];
                pieChart = new Chart(pieCtx, {
                    type:'pie',
                    data:{ labels, datasets:[{ data:dataArr, borderWidth:1, backgroundColor: colors }] },
                    options:{ maintainAspectRatio:false, plugins:{ legend:{ position:'bottom', labels:{ boxWidth:12, boxHeight:12 } } } }
                });
            }
        }

        async function loadWeek(){
            const mon = mondayOf(weekAnchor);
            const sun = new Date(mon); sun.setDate(sun.getDate()+6);
            if(weekLabelEl) weekLabelEl.textContent = mon.toLocaleDateString('pl-PL')+' – '+sun.toLocaleDateString('pl-PL');
            const data = await fetchJson(endpoints.week(ymd(mon), ymd(sun)));
            if(!data) return;
            const labels=[], values=[], maxes=[];
            for(let i=0;i<7;i++){
                const d=new Date(mon); d.setDate(d.getDate()+i);
                labels.push(d.toLocaleDateString('pl-PL',{weekday:'short'}));
                const key=ymd(d);
                const e = data.find(x => (x.date||'')===key);
                values.push(e?.completedSteps||0);
                maxes.push(e?.totalSteps||0);
            }
            if(!weekCtx || !window.Chart) return;
            if(weekChart) weekChart.destroy();
            weekChart = new Chart(weekCtx,{
                type:'bar',
                data:{ labels, datasets:[{ data:values, borderWidth:1, barThickness:18, maxBarThickness:18, categoryPercentage:.7, barPercentage:.7 }]},
                options:{
                    maintainAspectRatio:false,
                    plugins:{ legend:{display:false}, tooltip:{ callbacks:{ label:(c)=>`${c.raw}/${maxes[c.dataIndex]||0}` } } },
                    scales:{ y:{ beginAtZero:true, suggestedMax: Math.max(...maxes,1) } }
                }
            });
        }

        function buildMonthTable(map){
            if(!monthBodyEl) return;
            monthBodyEl.innerHTML='';
            const first=firstOfMonth(monthAnchor);
            const y=first.getFullYear(), m=first.getMonth();
            if(monthLabelEl) monthLabelEl.textContent = first.toLocaleDateString('pl-PL',{month:'long',year:'numeric'});
            const startOffset=(first.getDay()+6)%7;
            const totalDays=daysInMonth(y,m);
            let row=document.createElement('tr');
            for(let i=0;i<startOffset;i++) row.appendChild(document.createElement('td'));
            for(let day=1;day<=totalDays;day++){
                const td=document.createElement('td'); td.textContent=day;
                const ds=`${y}-${String(m+1).padStart(2,'0')}-${String(day).padStart(2,'0')}`;
                const status=map[ds];
                if(status==='full') td.classList.add('bg-success','text-white');
                else if(status==='partial') td.classList.add('bg-warning','text-dark');
                else if(status==='none') td.classList.add('bg-danger','text-white');
                else if(status==='off') td.classList.add('bg-secondary','text-white');
                else if(status==='future') td.classList.add('bg-primary','text-white');
                row.appendChild(td);
                const dow=(startOffset+day-1)%7;
                if(dow===6){ monthBodyEl.appendChild(row); row=document.createElement('tr'); }
            }
            if(row.children.length) monthBodyEl.appendChild(row);
        }

        async function loadMonth(){
            const first=firstOfMonth(monthAnchor);
            const y=first.getFullYear(), m=first.getMonth()+1;
            const map = await fetchJson(endpoints.month(y,m));
            if(!map) return;
            buildMonthTable(map);
        }

        document.querySelector(selectors.prevWeek)?.addEventListener('click', ()=>{ weekAnchor.setDate(weekAnchor.getDate()-7); loadWeek(); });
        document.querySelector(selectors.nextWeek)?.addEventListener('click', ()=>{ weekAnchor.setDate(weekAnchor.getDate()+7); loadWeek(); });
        document.querySelector(selectors.prevMonth)?.addEventListener('click', ()=>{ monthAnchor.setMonth(monthAnchor.getMonth()-1); loadMonth(); });
        document.querySelector(selectors.nextMonth)?.addEventListener('click', ()=>{ monthAnchor.setMonth(monthAnchor.getMonth()+1); loadMonth(); });

        await loadStats();
        await loadWeek();
        await loadMonth();

        return { reloadAll: async()=>{ await loadStats(); await loadWeek(); await loadMonth(); } };
    }

    window.StatsCommon = { init };
})();
