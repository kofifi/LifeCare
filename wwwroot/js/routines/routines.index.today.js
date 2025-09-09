window.LC_Routines_Today = (function () {
    const state = {
        selectedDate: new Date(),
        weekOffset: 0,
        expanded: new Set(),
        tagIds: []
    };

    const qs = (sel, root = document) => root.querySelector(sel);
    const qsa = (sel, root = document) => Array.from(root.querySelectorAll(sel));

    function fmtYmd(d) {
        return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;
    }

    function isSameDay(a, b) {
        return a.getFullYear() === b.getFullYear() &&
            a.getMonth() === b.getMonth() &&
            a.getDate() === b.getDate();
    }

    function fmtTimeMaybe(t) {
        if (!t) return "";
        if (typeof t === "string" && t.includes(":")) {
            const [hh, mm] = t.split(":");
            return `${parseInt(hh, 10)}:${mm}`;
        }
        return t.toString().slice(0, 5);
    }

    function getSelectedTagIdsFromUrl() {
        const p = new URLSearchParams(window.location.search);
        const ids = p.getAll("todayTagIds").map(x => String(parseInt(x, 10))).filter(v => v && v !== "NaN");
        return ids;
    }

    function setUrlTodayTagIds(ids) {
        try {
            const url = new URL(window.location.href);
            url.searchParams.delete("todayTagIds");
            (ids || []).forEach(id => url.searchParams.append("todayTagIds", String(id)));
            window.history.replaceState(null, "", url.toString());
        } catch { /* no-op */ }
    }
    
    function setUrlTagIds(ids) {
        try {
            const url = new URL(window.location.href);
            url.searchParams.delete("tagIds");
            (ids || []).forEach(id => url.searchParams.append("tagIds", String(id)));
            window.history.replaceState(null, "", url.toString());
        } catch { /* no-op */ }
    }

    function isInteractiveTarget(e) {
        return !!e.target.closest(
            "a, button, input, label, .dropdown-menu, [data-bs-toggle], [data-toggle-steps]"
        );
    }

    function getRoutineTagIds(r) {
        if (Array.isArray(r?.tagIds)) return r.tagIds.map(String);
        if (Array.isArray(r?.selectedTagIds)) return r.selectedTagIds.map(String);
        return [];
    }

    async function loadForDate(dateStr) {
        const url = new URL(window.location.origin + `/Routines/ForDate`);
        url.searchParams.set("date", dateStr);

        const urlTagIds = getSelectedTagIdsFromUrl();
        for (const id of urlTagIds) url.searchParams.append("tagIds", String(id));
        url.searchParams.set("_", Date.now().toString());

        const res = await fetch(url.toString());
        const list = await res.json();

        let filtered = list;
        if (urlTagIds.length > 0) {
            filtered = list.filter(it => {
                const tags = getRoutineTagIds(it);
                if (!tags.length) return false;
                return urlTagIds.every(t => tags.includes(String(t)));
            });
        }

        if (state.tagIds.length > 0) {
            filtered = filtered.filter(it => {
                const tags = getRoutineTagIds(it);
                if (!tags.length) return false;
                return state.tagIds.every(id => tags.includes(id));
            });
        }

        renderList(filtered, dateStr);
    }

    async function reload() {
        await loadForDate(fmtYmd(state.selectedDate));
    }

    function setTags(ids) {
        state.tagIds = (ids || []).map(String);
        setUrlTodayTagIds(state.tagIds);
    }

    function updateTitle() {
        const titleEl = qs("#todayTitle");
        const today = new Date();
        if (!titleEl) return;

        if (isSameDay(state.selectedDate, today)) {
            titleEl.textContent = "Dzisiejsze rutyny";
        } else {
            titleEl.textContent = `Rutyny z dnia ${state.selectedDate.toLocaleDateString("pl-PL", {
                day: "2-digit",
                month: "2-digit",
                year: "numeric",
            })}`;
        }
    }

    function renderCalendar() {
        const calendarHost = qs("#calendar-scroll");
        if (!calendarHost) return;

        calendarHost.innerHTML = "";

        const base = new Date();
        base.setDate(base.getDate() + state.weekOffset * 7);

        const monday = new Date(base);
        const delta = (monday.getDay() + 6) % 7; // pn=0
        monday.setDate(monday.getDate() - delta);

        for (let i = 0; i < 7; i++) {
            const d = new Date(monday);
            d.setDate(d.getDate() + i);

            const btn = document.createElement("button");
            btn.className = "btn btn-outline-primary mx-1 day-button";
            btn.dataset.date = fmtYmd(d);
            btn.textContent = d.toLocaleDateString("pl-PL", {
                weekday: "short",
                day: "2-digit",
                month: "2-digit",
            });

            if (isSameDay(d, state.selectedDate)) btn.classList.add("active");

            btn.addEventListener("click", () => {
                qsa(".day-button").forEach((b) => b.classList.remove("active"));
                btn.classList.add("active");
                state.selectedDate = new Date(`${btn.dataset.date}T00:00:00`);
                updateTitle();
                loadForDate(btn.dataset.date);
            });

            calendarHost.appendChild(btn);
        }

        updateTitle();
        loadForDate(fmtYmd(state.selectedDate));
    }

    function bindWeekButtons() {
        qs("#prevWeek")?.addEventListener("click", () => {
            state.weekOffset--;
            renderCalendar();
        });

        qs("#nextWeek")?.addEventListener("click", () => {
            state.weekOffset++;
            renderCalendar();
        });
    }

    function renderList(list, dateStr) {
        const host = qs("#todayList");
        if (!host) return;

        host.innerHTML = "";

        if (!list.length) {
            host.innerHTML = `<div class="soft-card text-muted">Brak rutyn na ten dzień.</div>`;
            return;
        }

        list.forEach((r) => {
            const card = document.createElement("div");
            card.className = "soft-card routine-card clickable-card";
            card.style.borderLeft = `8px solid ${r.color}`;
            card.setAttribute("data-href", `/Routines/Details/${r.routineId}`);

            const tagIds = getRoutineTagIds(r);
            if (tagIds.length) {
                card.setAttribute("data-tags", tagIds.join(","));
            } else {
                card.setAttribute("data-tags", "");
            }

            if (r.completed) card.classList.add("completed");

            const header = document.createElement("div");
            header.className = "d-flex align-items-center justify-content-between gap-3";
            header.innerHTML = `
        <div class="d-flex align-items-center gap-3">
          <i class="fa ${r.icon}" style="color:${r.color}"></i>
          <div>
            <div class="fw-semibold ${r.completed ? "strike" : ""}">${r.name}</div>
            ${r.description ? `<div class="small text-muted">${r.description}</div>` : ``}
            ${r.timeOfDay ? `<div class="small text-muted">${fmtTimeMaybe(r.timeOfDay)}</div>` : ``}
          </div>
        </div>
        <div class="d-flex align-items-center gap-2">
          <span class="badge bg-secondary">${r.doneSteps}/${r.totalSteps}</span>

          <button class="icon-btn" type="button" data-edit
                  title="Edytuj" aria-label="Edytuj">
            <i class="fa fa-edit"></i>
          </button>

          <button class="icon-btn icon-btn-danger" type="button" data-delete
                  data-bs-toggle="modal" data-bs-target="#routineDeleteModal"
                  data-id="${r.routineId}" data-name="${r.name}"
                  title="Usuń" aria-label="Usuń">
            <i class="fa fa-trash"></i>
          </button>

          <button class="btn btn-outline-secondary btn-sm" data-toggle-steps type="button" title="Rozwiń">
            <i class="fa fa-chevron-down"></i>
          </button>
        </div>
      `;

            const body = document.createElement("div");
            body.className = "mt-2 d-none";
            body.innerHTML = `
        <div class="d-flex align-items-center justify-content-end mb-2">
          <div class="form-check">
            <input class="form-check-input" type="checkbox" id="checkall_${r.routineId}">
          </div>
        </div>
        <div class="d-flex flex-column gap-2" data-steps></div>
        <div class="d-flex justify-content-end mt-3"></div>
      `;

            const stepsHost = qs("[data-steps]", body);

            r.steps.forEach((s, idx) => {
                const isAny = s.rotationEnabled && (s.rotationMode || "").toUpperCase() === "ANY";
                const showStepCheckbox = !isAny;

                const row = document.createElement("div");
                row.className = `step-card ${s.completed || s.skipped ? "completed" : ""}`;

                const headerHtml = `
          <div class="d-flex align-items-start justify-content-between">
            <div class="me-3 flex-grow-1">
              <div class="step-title">
                <span class="step-num">${idx + 1}</span>
                <span>${s.name}</span>
              </div>
              ${s.description ? `<div class="product-note mt-1">${s.description}</div>` : ``}
            </div>
            ${showStepCheckbox ? `
              <div class="form-check mt-1">
                <input class="form-check-input" type="checkbox" ${s.completed ? "checked" : ""} data-step-id="${s.stepId}">
              </div>` : ``}
          </div>
        `;

                let productsHtml = "";
                if (s.products && s.products.length) {
                    productsHtml = `
            <div class="mt-2 d-flex flex-column gap-2">
              ${s.products.map(p => `
                <div class="product-row">
                  ${p.imageUrl ? `<img src="${p.imageUrl}" alt="">` : ``}
                  <div class="flex-grow-1">
                    <div class="product-name-line">
                      <div class="fw-semibold">${p.name}</div>
                      ${p.note ? `<div class="product-note">— ${p.note}</div>` : ``}
                    </div>
                    ${p.url ? `<div class="small"><a href="${p.url}" target="_blank" rel="noopener">link</a></div>` : ``}
                  </div>
                  ${isAny ? `
                    <div class="form-check mt-1">
                      <input class="form-check-input" type="checkbox"
                             data-prod-id="${p.productId}"
                             data-step-id="${s.stepId}"
                             ${p.completed ? "checked" : ""}>
                    </div>` : ``}
                </div>
              `).join("")}
            </div>
          `;
                }

                row.innerHTML = `${headerHtml}${productsHtml}`;
                stepsHost.appendChild(row);
            });

            card.addEventListener("click", (e) => {
                if (isInteractiveTarget(e)) return;
                const href = card.getAttribute("data-href");
                if (href) window.location.href = href;
            });

            stepsHost.addEventListener("change", async (e) => {
                if (e.target.matches("input[type=checkbox][data-prod-id]")) {
                    const prodId = parseInt(e.target.getAttribute("data-prod-id"), 10);
                    const stepId = parseInt(e.target.getAttribute("data-step-id"), 10);
                    const completed = e.target.checked;

                    const ok = await fetch("/Routines/ToggleProduct", {
                        method: "POST",
                        headers: {"Content-Type": "application/json"},
                        body: JSON.stringify({
                            routineId: r.routineId,
                            stepId,
                            productId: prodId,
                            date: dateStr,
                            completed
                        }),
                    }).then((x) => x.ok);

                    if (!ok) {
                        e.target.checked = !completed;
                        alert("Błąd zapisu.");
                    } else {
                        await reload();
                    }
                    return;
                }

                if (e.target.matches("input[type=checkbox][data-step-id]") && !e.target.hasAttribute("data-prod-id")) {
                    const stepId = parseInt(e.target.getAttribute("data-step-id"), 10);
                    const completed = e.target.checked;

                    const ok = await fetch("/Routines/ToggleStep", {
                        method: "POST",
                        headers: {"Content-Type": "application/json"},
                        body: JSON.stringify({routineId: r.routineId, stepId, date: dateStr, completed, note: null}),
                    }).then((x) => x.ok);

                    if (!ok) {
                        e.target.checked = !completed;
                        alert("Błąd zapisu.");
                    } else {
                        await reload();
                    }
                }
            });

            const toggleBtn = header.querySelector("[data-toggle-steps]");
            const setArrow = (open) => (toggleBtn.innerHTML = `<i class="fa fa-chevron-${open ? "up" : "down"}"></i>`);

            toggleBtn.addEventListener("click", (ev) => {
                ev.preventDefault();
                ev.stopPropagation();
                const isOpen = !body.classList.contains("d-none");
                if (isOpen) {
                    body.classList.add("d-none");
                    state.expanded.delete(r.routineId);
                    setArrow(false);
                } else {
                    body.classList.remove("d-none");
                    state.expanded.add(r.routineId);
                    setArrow(true);
                }
            });

            const checkAllEl = body.querySelector(`#checkall_${r.routineId}`);
            const allChecked = r.totalSteps > 0 && r.steps.every((s) => s.completed || s.skipped);
            checkAllEl.checked = allChecked;

            checkAllEl.addEventListener("change", async (e) => {
                const completed = e.target.checked;

                const ok = await fetch("/Routines/SetAll", {
                    method: "POST",
                    headers: {"Content-Type": "application/json"},
                    body: JSON.stringify({routineId: r.routineId, date: dateStr, completed}),
                }).then((x) => x.ok);

                if (ok) {
                    await reload();
                } else {
                    e.target.checked = !completed;
                    alert("Nie udało się zapisać.");
                }
            });

            if (state.expanded.has(r.routineId)) {
                body.classList.remove("d-none");
                setArrow(true);
            } else {
                setArrow(false);
            }

            card.appendChild(header);
            header.querySelector('[data-edit]')?.addEventListener('click', (ev) => {
                ev.preventDefault();
                ev.stopPropagation();
                window.location.href = `/Routines/Edit/${r.routineId}`;
            });

            header.querySelector('[data-delete]')?.addEventListener('click', (ev) => {
                ev.preventDefault();
                ev.stopPropagation();
            });

            card.appendChild(body);
            host.appendChild(card);
        });
    }

    function init() {
        const fromUrl = getSelectedTagIdsFromUrl();
        if (fromUrl.length) state.tagIds = fromUrl.map(String);

        bindWeekButtons();
        renderCalendar();
    }

    return {
        init,
        reload,
        setTags
    };
})();
