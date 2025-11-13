const root = document.getElementById('habit-root');
const habitId = +root.dataset.habitId;
const habitType = root.dataset.type;
const target = +(root.dataset.target || 0);
const isQuantity = habitType === "Quantity";

function ymdLocal(d = new Date()) {
    const y = d.getFullYear();
    const m = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${y}-${m}-${day}`;
}
function mondayOf(date) {
    const d = new Date(date);
    const day = (d.getDay() + 6) % 7;
    d.setHours(0, 0, 0, 0);
    d.setDate(d.getDate() - day);
    return d;
}
function fmt(d) { return ymdLocal(d); }

const $ = (id, fallbackId) => document.getElementById(id) || (fallbackId ? document.getElementById(fallbackId) : null);

let pieChart;

async function loadStats() {
    try {
        const res = await fetch(`/Habits/HabitStats?habitId=${habitId}`);
        if (!res.ok) return;
        const s = await res.json();

        const statsRow = document.getElementById('habit_statsRow') || document.getElementById('statsRow');
        if (statsRow) statsRow.style.display = 'flex';

        const overall = document.getElementById('habit_overallPercent') || document.getElementById('overallPercent');
        if (overall) overall.textContent = Math.round(s.overallPercent) + '%';

        const cur = document.getElementById('habit_currentStreak') || document.getElementById('currentStreak');
        if (cur) cur.textContent = s.currentStreak;

        const best = document.getElementById('habit_bestStreak') || document.getElementById('bestStreak');
        if (best) best.textContent = s.bestStreak;

        const total = document.getElementById('habit_totalCount') || document.getElementById('totalSessions') || document.getElementById('totalOcc');
        if (total) total.textContent = s.total;

        const comp = document.getElementById('habit_completedCount') || document.getElementById('completedSessions') || document.getElementById('completedDays');
        if (comp) comp.textContent = s.completed;

        const part = document.getElementById('habit_partialCount') || document.getElementById('partialSessions') || document.getElementById('partialDays');
        if (part) part.textContent = s.partial || 0;

        const skip = document.getElementById('habit_skippedCount') || document.getElementById('skippedSessions') || document.getElementById('skippedDays');
        if (skip) skip.textContent = s.skipped;

        const rangeInfo = document.getElementById('habit_rangeInfo') || document.getElementById('rangeInfo');
        if (rangeInfo && s.startDateUtc) {
            const start = new Date(s.startDateUtc);
            rangeInfo.textContent = `Od ${start.toLocaleDateString('pl-PL')}`;
        }

        const ctxP = document.getElementById('habit_pieChart') || document.getElementById('pieChart');
        if (ctxP && window.Chart) {
            if (pieChart) pieChart.destroy();
            if (isQuantity) {
                pieChart = new Chart(ctxP, {
                    type: 'pie',
                    data: {
                        labels: ['Pełne', 'Częściowe', 'Pominięte'],
                        datasets: [{
                            data: [s.completed, s.partial || 0, s.skipped],
                            borderWidth: 1,
                            backgroundColor: [
                                'rgba(25,135,84,.85)',
                                'rgba(255,193,7,.9)',
                                'rgba(220,53,69,.9)'
                            ]
                        }]
                    },
                    options: { maintainAspectRatio: false, plugins: { legend: { position: 'bottom', labels: { boxWidth: 12, boxHeight: 12 } } } }
                });
            } else {
                pieChart = new Chart(ctxP, {
                    type: 'pie',
                    data: { labels: ['Ukończone', 'Nieukończone'], datasets: [{ data: [s.completed, s.skipped], borderWidth: 1 }] },
                    options: { maintainAspectRatio: false, plugins: { legend: { position: 'bottom', labels: { boxWidth: 12, boxHeight: 12 } } } }
                });
            }
        }
    } catch (_) { }
}

let weekAnchor = new Date();
const weekLabel = $('habit_weekLabel', 'weekLabel');
let weekChart;

async function loadWeek() {
    const mon = mondayOf(weekAnchor);
    const sun = new Date(mon); sun.setDate(sun.getDate() + 6);

    if (weekLabel) weekLabel.textContent = mon.toLocaleDateString('pl-PL') + ' – ' + sun.toLocaleDateString('pl-PL');

    const res = await fetch(`/Habits/HabitEntries?habitId=${habitId}&from=${fmt(mon)}&to=${fmt(sun)}`);
    if (!res.ok) return;
    const data = await res.json();

    const labels = [];
    const values = [];
    for (let i = 0; i < 7; i++) {
        const d = new Date(mon); d.setDate(d.getDate() + i);
        const key = fmt(d);
        labels.push(d.toLocaleDateString('pl-PL', { weekday: 'short' }));
        const e = data.find(x => (x.date || '').startsWith(key));
        if (isQuantity) {
            const q = e?.quantity ?? 0;
            values.push(q);
        } else {
            values.push(e?.completed ? 1 : 0);
        }
    }

    const ctx = $('habit_weekChart', 'weekChart');
    if (!ctx || !window.Chart) return;
    if (weekChart) weekChart.destroy();

    weekChart = new Chart(ctx, {
        type: 'bar',
        data: {
            labels,
            datasets: [{
                data: values,
                borderWidth: 1,
                barThickness: 18,
                maxBarThickness: 18,
                categoryPercentage: 0.7,
                barPercentage: 0.7
            }]
        },
        options: {
            maintainAspectRatio: false,
            plugins: {
                legend: { display: false },
                tooltip: { callbacks: { label: (ctx) => (isQuantity ? ctx.raw : (ctx.raw ? 'Tak' : 'Nie')) } }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    suggestedMax: isQuantity ? Math.max(target || 10, ...values) : 1,
                    ticks: { stepSize: isQuantity ? undefined : 1 }
                }
            }
        }
    });
}

$('habit_prevWeek', 'prevWeek')?.addEventListener('click', () => { weekAnchor.setDate(weekAnchor.getDate() - 7); loadWeek(); });
$('habit_nextWeek', 'nextWeek')?.addEventListener('click', () => { weekAnchor.setDate(weekAnchor.getDate() + 7); loadWeek(); });

const monthLabel = $('habit_monthLabel', 'monthLabel');
const monthBody  = $('habit_monthBody', 'monthBody');
let monthAnchor = new Date();

function firstOfMonth(d) { const x=new Date(d); x.setDate(1); x.setHours(0,0,0,0); return x; }
function daysInMonth(y,m){ return new Date(y, m+1, 0).getDate(); }

async function loadMonth() {
    const first = firstOfMonth(monthAnchor);
    const y = first.getFullYear(), m = first.getMonth();
    if (monthLabel) monthLabel.textContent = first.toLocaleDateString('pl-PL', { month: 'long', year: 'numeric' });

    const res = await fetch(`/Habits/HabitMonth?habitId=${habitId}&year=${y}&month=${m+1}`);
    if (!res.ok) return;
    const map = await res.json();

    if (!monthBody) return;
    monthBody.innerHTML = '';
    const startOffset = (first.getDay()+6)%7;
    const totalDays = daysInMonth(y, m);

    let row = document.createElement('tr');
    for (let i=0;i<startOffset;i++) row.appendChild(document.createElement('td'));

    for (let day=1; day<=totalDays; day++){
        const td = document.createElement('td');
        const ds = `${y}-${String(m+1).padStart(2,'0')}-${String(day).padStart(2,'0')}`;
        td.textContent = day;

        const status = map[ds];
        if (status === 'full') td.classList.add('bg-success','text-white');
        else if (status === 'partial') td.classList.add('bg-warning','text-dark');
        else if (status === 'none') td.classList.add('bg-danger','text-white');
        else if (status === 'off') td.classList.add('bg-secondary','text-white');
        else if (status === 'future') td.classList.add('bg-primary','text-white');

        row.appendChild(td);
        const dayOfWeek = (startOffset + day -1) % 7;
        if (dayOfWeek === 6) { monthBody.appendChild(row); row = document.createElement('tr'); }
    }
    if (row.children.length) monthBody.appendChild(row);
}

$('habit_prevMonth', 'prevMonth')?.addEventListener('click', ()=>{ monthAnchor.setMonth(monthAnchor.getMonth()-1); loadMonth(); });
$('habit_nextMonth', 'nextMonth')?.addEventListener('click', ()=>{ monthAnchor.setMonth(monthAnchor.getMonth()+1); loadMonth(); });

(async function init(){
    await loadStats();
    await loadWeek();
    await loadMonth();
})();
