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
        if (e.target.closest("[data-no-bubble]") || e.target.closest("[data-bs-toggle]")) {
            e.preventDefault();
            e.stopPropagation();
        }
    });
})();
