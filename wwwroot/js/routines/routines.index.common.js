window.LC_Routines_Index = (function () {
    function qs(sel) {
        return document.querySelector(sel);
    }

    function show(el) {
        el?.classList.remove("d-none");
    }

    function hide(el) {
        el?.classList.add("d-none");
    }

    function getSectionFromUrl() {
        const p = new URLSearchParams(window.location.search);
        const sec = (p.get("section") || "").toLowerCase();
        return (sec === "all" || sec === "today") ? `#${sec}Section` : "#todaySection";
    }

    function switchSection(targetId) {
        const today = qs("#todaySection");
        const all = qs("#allSection");
        const statusCol = qs("#statusFilterCol");

        if (targetId === "#todaySection") {
            show(today);
            hide(all);
            hide(statusCol);
            qs("#btnToday")?.classList.replace("btn-outline-primary", "btn-primary");
            qs("#btnAll")?.classList.replace("btn-primary", "btn-outline-primary");
            if (window.LC_Routines_Today?.reload) window.LC_Routines_Today.reload();
        } else {
            hide(today);
            show(all);
            show(statusCol);
            qs("#btnAll")?.classList.replace("btn-outline-primary", "btn-primary");
            qs("#btnToday")?.classList.replace("btn-primary", "btn-outline-primary");
            if (window.LC_Routines_All?.applyFilters) window.LC_Routines_All.applyFilters();
        }
    }

    function initNav() {
        const btnToday = qs("#btnToday");
        const btnAll = qs("#btnAll");
        btnToday?.addEventListener("click", () => {
            const url = new URL(window.location.href);
            url.searchParams.set("section", "today");
            window.location.href = url.toString();
        });
        btnAll?.addEventListener("click", () => {
            const url = new URL(window.location.href);
            url.searchParams.set("section", "all");
            window.location.href = url.toString();
        });

        switchSection(getSectionFromUrl());
    }

    function initFilters() {
        const cat = qs("#categoryFilter");
        const status = qs("#statusFilter");

        if (cat) cat.addEventListener("change", () => {
            const inAll = !qs("#allSection").classList.contains("d-none");
            if (inAll) {
                if (window.LC_Routines_All?.applyFilters) window.LC_Routines_All.applyFilters();
            } else {
                if (window.LC_Routines_Today?.reload) window.LC_Routines_Today.reload();
            }
        });

        if (status) status.addEventListener("change", () => {
            if (window.LC_Routines_All?.applyFilters) window.LC_Routines_All.applyFilters();
        });

        const statusApplyBtn = document.querySelector(".tf-status-apply");
        if (statusApplyBtn) {
            statusApplyBtn.addEventListener("click", (e) => {
                e.preventDefault();
                if (window.LC_Routines_All?.applyFilters) window.LC_Routines_All.applyFilters();
            });
        }
    }

    document.addEventListener('tagfilter:change', (e) => {
        e.preventDefault();

        const ids = (e.detail?.selectedIds || []).map(String);
        const section = (e.detail?.section || 'today').toLowerCase();

        if (section === 'all') {
            window.LC_Routines_All?.setTags(ids);
            window.LC_Routines_All?.applyFilters?.();
        } else {
            window.LC_Routines_Today?.setTags?.(ids);
            window.LC_Routines_Today?.reload?.();
        }
    });


    function init() {
        initNav();
        initFilters();
        if (window.LC_Routines_Today?.init) window.LC_Routines_Today.init();
        if (window.LC_Routines_All?.init) window.LC_Routines_All.init?.();
    }

    return {init, switchSection};
})();

document.addEventListener('tagfilter:change', function (e) {
    e.preventDefault();
    const sec = (e.detail?.section || 'today').toLowerCase();
    const ids = e.detail?.selectedIds || [];

    if (sec === 'all') {
        if (window.LC_Routines_All?.setTags && window.LC_Routines_All?.applyFilters) {
            window.LC_Routines_All.setTags(ids);
            window.LC_Routines_All.applyFilters();
        }
    } else {
        if (window.LC_Routines_Today?.setTags && window.LC_Routines_Today?.reload) {
            window.LC_Routines_Today.setTags(ids);
            window.LC_Routines_Today.reload();
        }
    }
});
