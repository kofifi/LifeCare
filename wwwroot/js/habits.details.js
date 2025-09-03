(function () {
    const editCard = document.getElementById('editCard');
    const toggleBtn = document.getElementById('toggleEditBtn');
    const cancel2 = document.getElementById('cancelEditBtn2');

    function showEdit() {
        if (editCard) {
            editCard.style.display = 'block';
            window.scrollTo({top: 0, behavior: 'smooth'});
        }
    }

    function hideEdit() {
        if (editCard) editCard.style.display = 'none';
    }

    if (toggleBtn) toggleBtn.addEventListener('click', showEdit);
    if (cancel2) cancel2.addEventListener('click', hideEdit);

    const root = document.getElementById('habit-root');
    if (!root) return;

    const type = root.dataset.type;
    const q = document.getElementById('quantityFields');
    if (q) q.style.display = (type === "Quantity") ? 'block' : 'none';
})();

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

function fmt(d) {
    return ymdLocal(d);
}

let pieChart;

async function loadStats() {
    const res = await fetch(`/Habits/HabitStats?habitId=${habitId}`);
    if (!res.ok) return;
    const s = await res.json();

    const statsRow = document.getElementById('statsRow');
    if (statsRow) statsRow.style.display = 'flex';

    const elOverall = document.getElementById('overallPercent');
    if (elOverall) elOverall.innerText = Math.round(s.overallPercent) + '%';
    const elCur = document.getElementById('currentStreak');
    if (elCur) elCur.innerText = s.currentStreak;
    const elBest = document.getElementById('bestStreak');
    if (elBest) elBest.innerText = s.bestStreak;
    const elTotal = document.getElementById('totalSessions');
    if (elTotal) elTotal.innerText = s.total;
    const elComp = document.getElementById('completedSessions');
    if (elComp) elComp.innerText = s.completed;
    const elSkip = document.getElementById('skippedSessions');
    if (elSkip) elSkip.innerText = s.skipped;

    const ctxP = document.getElementById('pieChart');
    if (ctxP && window.Chart) {
        if (pieChart) pieChart.destroy();
        pieChart = new Chart(ctxP, {
            type: 'pie',
            data: {
                labels: ['Ukończone', 'Nieukończone'],
                datasets: [{data: [s.completed, s.skipped], borderWidth: 1}]
            },
            options: {
                maintainAspectRatio: false,
                plugins: {
                    legend: {position: 'bottom', labels: {boxWidth: 12, boxHeight: 12}}
                },
                layout: {padding: 0}
            }
        });
    }
}

let weekAnchor = new Date();
const weekLabel = document.getElementById('weekLabel');
let weekChart;

async function loadWeek() {
    const mon = mondayOf(weekAnchor);
    const sun = new Date(mon);
    sun.setDate(sun.getDate() + 6);

    if (weekLabel) weekLabel.textContent = mon.toLocaleDateString('pl-PL') + ' – ' + sun.toLocaleDateString('pl-PL');

    const res = await fetch(`/Habits/HabitEntries?habitId=${habitId}&from=${fmt(mon)}&to=${fmt(sun)}`);
    if (!res.ok) return;
    const data = await res.json();

    const labels = [];
    const values = [];
    for (let i = 0; i < 7; i++) {
        const d = new Date(mon);
        d.setDate(d.getDate() + i);
        const key = fmt(d);
        labels.push(d.toLocaleDateString('pl-PL', {weekday: 'short'}));
        const e = data.find(x => (x.date || '').startsWith(key));
        if (isQuantity) {
            const q = e?.quantity ?? 0;
            values.push(q);
        } else {
            values.push(e?.completed ? 1 : 0);
        }
    }

    const ctx = document.getElementById('weekChart');
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
                legend: {display: false},
                tooltip: {callbacks: {label: (ctx) => (isQuantity ? ctx.raw : (ctx.raw ? 'Tak' : 'Nie'))}}
            },
            layout: {padding: 0},
            scales: {
                y: {
                    beginAtZero: true,
                    suggestedMax: isQuantity ? Math.max(target || 10, ...values) : 1,
                    ticks: {stepSize: isQuantity ? undefined : 1}
                }
            }
        }
    });
}

document.getElementById('prevWeek')?.addEventListener('click', () => {
    weekAnchor.setDate(weekAnchor.getDate() - 7);
    loadWeek();
});
document.getElementById('nextWeek')?.addEventListener('click', () => {
    weekAnchor.setDate(weekAnchor.getDate() + 7);
    loadWeek();
});

const monthLabel = document.getElementById('monthLabel');
const monthBody = document.getElementById('monthBody');
let monthAnchor = new Date();

function firstOfMonth(d) {
    const x = new Date(d);
    x.setDate(1);
    x.setHours(0, 0, 0, 0);
    return x;
}

function daysInMonth(y, m) {
    return new Date(y, m + 1, 0).getDate();
}

async function loadMonth() {
    const first = firstOfMonth(monthAnchor);
    const y = first.getFullYear(), m = first.getMonth();
    if (monthLabel) monthLabel.textContent = first.toLocaleDateString('pl-PL', {month: 'long', year: 'numeric'});

    const res = await fetch(`/Habits/HabitMonth?habitId=${habitId}&year=${y}&month=${m + 1}`);
    if (!res.ok) return;
    const map = await res.json();

    if (!monthBody) return;
    monthBody.innerHTML = '';
    const startOffset = (first.getDay() + 6) % 7;
    const totalDays = daysInMonth(y, m);

    let row = document.createElement('tr');
    for (let i = 0; i < startOffset; i++) {
        row.appendChild(document.createElement('td'));
    }

    for (let day = 1; day <= totalDays; day++) {
        const td = document.createElement('td');
        const ds = `${y}-${String(m + 1).padStart(2, '0')}-${String(day).padStart(2, '0')}`;
        td.textContent = day;

        const status = map[ds];
        if (status === 'full') td.classList.add('bg-success', 'text-white');
        else if (status === 'partial') td.classList.add('bg-warning', 'text-dark');
        else if (status === 'none') td.classList.add('bg-danger', 'text-white');

        row.appendChild(td);
        const dayOfWeek = (startOffset + day - 1) % 7;
        if (dayOfWeek === 6) {
            monthBody.appendChild(row);
            row = document.createElement('tr');
        }
    }
    if (row.children.length) monthBody.appendChild(row);
}

document.getElementById('prevMonth')?.addEventListener('click', () => {
    monthAnchor.setMonth(monthAnchor.getMonth() - 1);
    loadMonth();
});
document.getElementById('nextMonth')?.addEventListener('click', () => {
    monthAnchor.setMonth(monthAnchor.getMonth() + 1);
    loadMonth();
});

(async function init() {
    await loadStats();
    await loadWeek();
    await loadMonth();
})();

window.HabitsDetails = window.HabitsDetails || {};
window.HabitsDetails.refreshAll = async function () {
    await loadStats();
    await loadWeek();
    await loadMonth();
};

window.addEventListener('habit:entry-updated', function (e) {
    if (e?.detail?.habitId && e.detail.habitId !== habitId) return;
    window.HabitsDetails.refreshAll();
});
