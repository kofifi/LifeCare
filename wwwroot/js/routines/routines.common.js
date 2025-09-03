window.Routines = window.Routines || {};

(function(ns){
    ns.ymdLocal = function(d = new Date()) {
        const y = d.getFullYear();
        const m = String(d.getMonth()+1).padStart(2,'0');
        const day = String(d.getDate()).padStart(2,'0');
        return `${y}-${m}-${day}`;
    };

    ns.setSingleActive = function(container, btnSelector, activeBtn){
        container.querySelectorAll(btnSelector).forEach(b => b.classList.remove('active'));
        if (activeBtn) activeBtn.classList.add('active');
    };

    ns.toggleActive = function(btn){ btn.classList.toggle('active'); };

    ns.getSelectedValues = function(container, selector){
        return Array.from(container.querySelectorAll(selector + '.active')).map(b => b.dataset.value);
    };

    ns.toByDay = ['MO','TU','WE','TH','FR','SA','SU'];
})(window.Routines);
