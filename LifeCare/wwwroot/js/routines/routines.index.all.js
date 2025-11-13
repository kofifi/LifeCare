(function () {
    const host = document.getElementById("allList");
    if (!host) return;

    function isInteractiveTarget(e) {
        return !!e.target.closest(
            "a,button,input,label,.dropdown-menu,[data-bs-toggle],[data-toggle-all-steps],[data-no-bubble]"
        );
    }

    host.addEventListener("click", (e) => {
        const card = e.target.closest(".clickable-card");
        if (!card) return;
        if (isInteractiveTarget(e)) return;

        const href = card.getAttribute("data-href");
        if (href) window.location.href = href;
    });

    host.addEventListener("click", (e) => {
        const btn = e.target.closest("[data-toggle-all-steps]");
        if (!btn) return;

        e.preventDefault();
        e.stopPropagation();

        const card = btn.closest(".clickable-card");
        const body = card?.querySelector("[data-all-steps]");
        if (!body) return;

        const willOpen = body.classList.contains("d-none");
        body.classList.toggle("d-none", !willOpen);
        btn.innerHTML = `<i class="fa fa-chevron-${willOpen ? "up" : "down"}"></i>`;
    });

    host.addEventListener("click", (e) => {
        const noBubbleEl = e.target.closest("[data-no-bubble]");
        if (noBubbleEl) {
            e.stopPropagation();
            return;
        }
        const modalTrigger = e.target.closest("[data-bs-toggle]");
        if (modalTrigger) {
            if (!(modalTrigger.tagName === "A" && modalTrigger.getAttribute("href"))) {
                e.preventDefault();
            }
            e.stopPropagation();
        }
    });
})();


(function () {
    function wireRoutineDeleteModal() {
        var modalEl = document.getElementById('routineDeleteModal');
        if (!modalEl) return;

        modalEl.addEventListener('show.bs.modal', function (e) {
            var btn = e.relatedTarget;                  // <button ... data-id data-name>
            if (!btn) return;

            var id = btn.getAttribute('data-id') || '';
            var name = btn.getAttribute('data-name') || '';

            var idInput = modalEl.querySelector('#deleteRoutineId');
            var nameSpan = modalEl.querySelector('#deleteRoutineName');

            if (idInput) idInput.value = id;
            if (nameSpan) nameSpan.textContent = name;
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', wireRoutineDeleteModal);
    } else {
        wireRoutineDeleteModal();
    }
})();

window.LC_Routines_All = (function () {
    const state = {tagIds: []};

    function getAllTagIdsFromUrl() {
        const p = new URLSearchParams(window.location.search);
        return p.getAll("allTagIds").map(x => String(parseInt(x, 10))).filter(v => v && v !== "NaN");
    }

    function setUrlAllTagIds(ids) {
        try {
            const url = new URL(window.location.href);
            url.searchParams.delete("allTagIds");
            (ids || []).forEach(id => url.searchParams.append("allTagIds", String(id)));
            window.history.replaceState(null, "", url.toString());
        } catch { /* no-op */
        }
    }

    function setTags(ids) {
        state.tagIds = (ids || []).map(String);
        setUrlAllTagIds(state.tagIds);
    }

    function applyFilters() {
        const host = document.getElementById('allList');
        if (!host) return;

        const statusVal = (document.getElementById('statusFilter')?.value || '').toLowerCase();

        host.querySelectorAll('.clickable-card').forEach(card => {
            const activeOk =
                !statusVal ||
                (statusVal === 'active' && card.dataset.active === '1') ||
                (statusVal === 'inactive' && card.dataset.active === '0');

            const tagAttr = (card.getAttribute('data-tags') || '')
                .split(',')
                .map(s => s.trim())
                .filter(Boolean);

            const tagsOk = !state.tagIds.length || state.tagIds.every(id => tagAttr.includes(id));

            card.style.display = (activeOk && tagsOk) ? '' : 'none';
        });
    }

    function init() {
        const fromUrl = getAllTagIdsFromUrl();
        if (fromUrl.length) state.tagIds = fromUrl;

        applyFilters();
        document.querySelector('.tf-status-apply')?.addEventListener('click', function (e) {
            e.preventDefault();
            applyFilters();
        });
        document.getElementById('statusFilter')?.addEventListener('change', applyFilters);
    }

    return {init, applyFilters, setTags};
})();
