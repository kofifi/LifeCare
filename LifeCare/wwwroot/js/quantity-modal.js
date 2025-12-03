(function () {
    function init() {
        var modal = document.getElementById('quantityModal');
        if (!modal) return;

        modal.addEventListener('click', function (e) {
            var btn = e.target.closest('[data-qty-step]');
            if (!btn) return;

            var delta = parseFloat(btn.getAttribute('data-qty-step') || '0');
            if (isNaN(delta)) delta = 0;

            var input = modal.querySelector('#modalQuantityInput');
            if (!input) return;

            var current = parseFloat(input.value || '0');
            if (isNaN(current)) current = 0;

            var next = current + delta;
            if (next < 0) next = 0;

            input.value = next;
            input.focus();
            input.select();
        });

        var closeBtn = modal.querySelector('.quantity-modal-close');
        if (closeBtn) {
            closeBtn.addEventListener('click', function (ev) {
                ev.preventDefault();
                if (window.bootstrap && bootstrap.Modal) {
                    var instance = bootstrap.Modal.getOrCreateInstance(modal);
                    instance.hide();
                }
            });
        }
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
