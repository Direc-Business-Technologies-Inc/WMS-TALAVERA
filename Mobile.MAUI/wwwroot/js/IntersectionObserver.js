

 const observer = new IntersectionObserver((entries) => {
    entries.forEach(entry => {
        const boxToolbarAsFloating = document.getElementById('box-toolbar-floating');
        if (boxToolbarAsFloating == null) return;
        console.log("toolbar-floating:",boxToolbarAsFloating)
        if (entry.isIntersecting) {
            boxToolbarAsFloating.classList.remove('show-floating');
        } else {
            boxToolbarAsFloating.classList.add('show-floating');
        }
        console.log(entry);
    });
}, { threshold:.75 });


let recentScannedObserver;
let boxToolbarEl = null;
export function Observe() {

    boxToolbarEl = document.getElementById('box-toolbar-page');

    if (boxToolbarEl != null) {
        observer.observe(boxToolbarEl);
    }
}

export function ObserveRecentScanned() {

    recentScannedObserver = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            const boxToolbarAsFloating = document.getElementById('recent-scanned-floating');
            if (entry.isIntersecting) {
                boxToolbarAsFloating.classList.remove('show-floating');
            } else {
                boxToolbarAsFloating.classList.add('show-floating');
            }
            console.log(entry);
        });
    }, { threshold: .75 });

    const recentScanned = document.getElementById('recent-scanned');
    recentScannedObserver.observe(recentScanned);
    
}

export function UnObserve() {
    boxToolbarEl = document.getElementById('box-toolbar-page');
    if (boxToolbarEl != null) {

        observer.unobserve(boxToolbarEl);
        observer.disconnect();
    }
}
export function UnObserveRecentScanned() {
    if (recentScannedObserver != null) {
        recentScannedObserver.disconnect();
    }
}

