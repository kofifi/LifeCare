window.LC_Routines_Index = (function(){
    function qs(id){ return document.querySelector(id); }
    function show(el){ el.classList.remove('d-none'); }
    function hide(el){ el.classList.add('d-none'); }

    function switchSection(targetId){
        const today = qs('#todaySection');
        const all   = qs('#allSection');
        const statusCol = qs('#statusFilterCol');
        if (targetId === '#todaySection'){
            show(today); hide(all);
            hide(statusCol);
            qs('#btnToday').classList.replace('btn-outline-primary','btn-primary');
            qs('#btnAll').classList.replace('btn-primary','btn-outline-primary');
            if (window.LC_Routines_Today && window.LC_Routines_Today.reload) window.LC_Routines_Today.reload();
        } else {
            hide(today); show(all);
            show(statusCol);
            qs('#btnAll').classList.replace('btn-outline-primary','btn-primary');
            qs('#btnToday').classList.replace('btn-primary','btn-outline-primary');
            if (window.LC_Routines_All && window.LC_Routines_All.applyFilters) window.LC_Routines_All.applyFilters();
        }
    }

    function initNav(){
        const btnToday = qs('#btnToday');
        const btnAll   = qs('#btnAll');
        btnToday.addEventListener('click', ()=> switchSection('#todaySection'));
        btnAll.addEventListener('click',   ()=> switchSection('#allSection'));
        switchSection('#todaySection');
    }

    function initFilters(){
        const cat = qs('#categoryFilter');
        const status = qs('#statusFilter');
        if (cat) cat.addEventListener('change', ()=>{
            const inAll = !qs('#allSection').classList.contains('d-none');
            if (inAll) {
                if (window.LC_Routines_All && window.LC_Routines_All.applyFilters) window.LC_Routines_All.applyFilters();
            } else {
                if (window.LC_Routines_Today && window.LC_Routines_Today.reload) window.LC_Routines_Today.reload();
            }
        });
        if (status) status.addEventListener('change', ()=>{
            if (window.LC_Routines_All && window.LC_Routines_All.applyFilters) window.LC_Routines_All.applyFilters();
        });
    }

    function init(){
        initNav();
        initFilters();
        if (window.LC_Routines_Today && window.LC_Routines_Today.init) window.LC_Routines_Today.init();
        if (window.LC_Routines_All && window.LC_Routines_All.init) window.LC_Routines_All.init();
    }

    return { init };
})();
