


let recentScannedObserver;

export function ObserveRecentScanned() {

    recentScannedObserver = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            const boxToolbarAsFloating = document.getElementById('recent-scanned-floating');
            console.log(boxToolbarAsFloating);
            if (entry.isIntersecting) {
                boxToolbarAsFloating.classList.remove('show-recent-scanned');
            } else {
                boxToolbarAsFloating.classList.add('show-recent-scanned');
            }
            console.log(entry);
        });
    }, { threshold: .1 });

    const recentScanned = document.getElementById('recent-scanned');
    recentScannedObserver.observe(recentScanned);

}

export function UnObserveRecentScanned() {
    if (recentScannedObserver != null) {
        recentScannedObserver.disconnect();
    }
}

