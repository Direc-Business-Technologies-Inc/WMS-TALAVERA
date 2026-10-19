

 const observer = new IntersectionObserver((entries) => {
    entries.forEach(entry => {
        const boxToolbarAsFloating = document.getElementById('box-toolbar-floating');
        if (boxToolbarAsFloating == null) return;
        if (entry.isIntersecting) {
            boxToolbarAsFloating.classList.remove('show-floating');
        } else {
            boxToolbarAsFloating.classList.add('show-floating');
        }
    });
}, { threshold:.75 });


// Scroll distance (px) past which the "Last Scanned" panel hides itself so it
// never sits on top of the item list.
const LAST_SCANNED_HIDE_AFTER_PX = 80;

let lastScannedPanel = null;
let lastScannedScrollHandler = null;
let boxToolbarEl = null;
export function Observe() {

    boxToolbarEl = document.getElementById('box-toolbar-page');

    if (boxToolbarEl != null) {
        observer.observe(boxToolbarEl);
    }
}

// Reveals the floating "Last Scanned" panel. Driven by the scan itself rather
// than by scroll: the panel has to be on screen the moment an item is scanned.
// The old implementation observed #recent-scanned, an empty element carrying a
// class that no longer exists in app.css, so the panel only ever appeared as a
// side effect of the list re-rendering after a save.
export function ShowLastScanned() {
    lastScannedPanel = lastScannedPanel ?? document.getElementById('recent-scanned-floating');
    if (lastScannedPanel == null) return;

    lastScannedPanel.classList.add('show-recent-scanned');
}

export function ObserveLastScannedScroll() {

    UnObserveLastScannedScroll();

    lastScannedScrollHandler = () => {
        if (window.scrollY <= LAST_SCANNED_HIDE_AFTER_PX) return;

        lastScannedPanel = lastScannedPanel ?? document.getElementById('recent-scanned-floating');
        if (lastScannedPanel == null) return;

        lastScannedPanel.classList.remove('show-recent-scanned');
    };

    window.addEventListener('scroll', lastScannedScrollHandler, { passive: true });
}

export function UnObserve() {
    boxToolbarEl = document.getElementById('box-toolbar-page');
    if (boxToolbarEl != null) {

        observer.unobserve(boxToolbarEl);
        observer.disconnect();
    }
}
export function UnObserveLastScannedScroll() {
    if (lastScannedScrollHandler == null) return;

    window.removeEventListener('scroll', lastScannedScrollHandler);
    lastScannedScrollHandler = null;
}

