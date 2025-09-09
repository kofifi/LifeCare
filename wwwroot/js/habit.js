function ymdLocal(d = new Date()) {
    const y = d.getFullYear();
    const m = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${y}-${m}-${day}`;
}

const tagFilterState = { tagIds: [] };

function applyHabitFilters() {
    const selected = tagFilterState.tagIds.map(String);
    document.querySelectorAll(".habit-card").forEach(card => {
        const tags = (card.getAttribute("data-tags") || "")
            .split(",")
            .map(s => s.trim())
            .filter(Boolean);

        const visible = !selected.length || selected.every(id => tags.includes(id));
        card.style.display = visible ? "block" : "none";
    });
}

let selectedHabitId = null;
let selectedDate = new Date();
let weekOffset = 0;

new Sortable(document.getElementById('habit-list'), {
    animation: 150,
    onEnd: function () {
        const order = Array.from(document.querySelectorAll('.habit-card'))
            .map(el => el.getAttribute('data-id'));

        fetch('/Habits/UpdateOrder', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(order)
        }).then(res => {
            if (!res.ok) alert('Błąd zapisu kolejności');
        });
    }
});

document.getElementById("categoryFilter").addEventListener("change", function () {
    const selected = this.value;
    document.querySelectorAll(".habit-card").forEach(el => {
        el.style.display = (!selected || el.dataset.categoryId === selected) ? "block" : "none";
    });
});

function renderCalendar() {
    const container = document.getElementById("calendar-scroll");
    container.innerHTML = "";

    const baseDate = new Date();
    baseDate.setDate(baseDate.getDate() + weekOffset * 7);
    const monday = new Date(baseDate);
    monday.setDate(monday.getDate() - (monday.getDay() + 6) % 7);

    for (let i = 0; i < 7; i++) {
        const d = new Date(monday);
        d.setDate(d.getDate() + i);
        const btn = document.createElement("button");
        btn.className = "btn btn-outline-primary mx-1 day-button";
        btn.dataset.date = ymdLocal(d);
        btn.textContent = d.toLocaleDateString('pl-PL', { weekday: 'short', day: '2-digit', month: '2-digit' });

        if (d.toDateString() === selectedDate.toDateString()) {
            btn.classList.add("active");
        }

        btn.addEventListener("click", function () {
            document.querySelectorAll(".day-button").forEach(b => b.classList.remove("active"));
            this.classList.add("active");
            selectedDate = new Date(this.dataset.date);
            loadEntriesForDate(this.dataset.date);
        });

        container.appendChild(btn);
    }

    loadEntriesForDate(ymdLocal(selectedDate));
}

document.getElementById("prevWeek").addEventListener("click", () => {
    weekOffset--;
    renderCalendar();
});

document.getElementById("nextWeek").addEventListener("click", () => {
    weekOffset++;
    renderCalendar();
});

function loadEntriesForDate(date) {
    fetch(`/Habits/GetEntries?date=${date}`)
        .then(res => res.json())
        .then(entries => {
            document.querySelectorAll(".habit-card").forEach(card => {
                const id = parseInt(card.dataset.id);
                const entry = entries.find(e => e.habitId === id);
                const checkbox = card.querySelector('input[type=checkbox]');
                const progress = card.querySelector('.habit-progress');
                const target = parseFloat(card.getAttribute('data-target')) || 0;

                if (checkbox) {
                    checkbox.checked = entry?.completed || false;
                }

                if (progress) {
                    const quantity = entry?.quantity || 0;
                    progress.innerHTML = `${quantity}/${target}`;
                    progress.style.color = quantity >= target ? card.dataset.color : 'gray';
                    if (quantity >= target) {
                        progress.innerHTML += ' <i class="fa fa-check text-success"></i>';
                    }
                }
            });
        });
}

function openQuantityModal(habitId, card, current, target) {
    selectedHabitId = habitId;
    const name = card.querySelector('strong').textContent;
    const unit = card.getAttribute('data-unit') || "";

    document.getElementById("modalHabitName").textContent = name;
    document.getElementById("modalUnit").textContent = unit;
    document.getElementById("modalQuantityInput").value = current;
    const modal = new bootstrap.Modal(document.getElementById("quantityModal"));
    modal.show();
}

document.getElementById("confirmQuantityBtn").addEventListener("click", async function () {
    const quantity = parseFloat(document.getElementById("modalQuantityInput").value);
    if (!isNaN(quantity)) {
        const res = await fetch('/Habits/SaveEntry', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                habitId: selectedHabitId,
                date: ymdLocal(selectedDate),
                completed: true,
                quantity: quantity
            })
        });

        if (res.ok) {
            bootstrap.Modal.getInstance(document.getElementById("quantityModal")).hide();
            loadEntriesForDate(ymdLocal(selectedDate));
        } else {
            alert('Wystąpił błąd podczas zapisu.');
        }
    }
});

document.getElementById('habit-list').addEventListener('click', function (e) {
    if (e.target.closest('button') && e.target.closest('.habit-card')) {
        const button = e.target.closest('button');
        const card = button.closest('.habit-card');
        const habitId = parseInt(card.dataset.id);
        const progress = card.querySelector('.habit-progress');
        const quantity = parseFloat(progress?.textContent.split('/')[0]) || 0;
        const target = parseFloat(card.dataset.target) || 0;
        openQuantityModal(habitId, card, quantity, target);
    }
});

document.getElementById('habit-list').addEventListener('change', function (e) {
    if (e.target.type === 'checkbox' && e.target.closest('.habit-card')) {
        const card = e.target.closest('.habit-card');
        const habitId = parseInt(card.dataset.id);
        saveEntry(habitId, ymdLocal(selectedDate), e.target.checked);
    }
});

function saveEntry(habitId, date, completed, quantity = null) {
    fetch('/Habits/SaveEntry', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ habitId, date, completed, quantity })
    });
}

renderCalendar();

document.addEventListener('tagfilter:change', (e) => {
    e.preventDefault();
    tagFilterState.tagIds = (e.detail?.selectedIds || []).map(String);
    applyHabitFilters();
});
