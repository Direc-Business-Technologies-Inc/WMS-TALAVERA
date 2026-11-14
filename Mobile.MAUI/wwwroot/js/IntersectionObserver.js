

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


let boxToolbarEl = null;
export function Observe() {

    boxToolbarEl = document.getElementById('box-toolbar-page');

    if (boxToolbarEl != null) {
        observer.observe(boxToolbarEl);
    }
}

export function UnObserve() {
    boxToolbarEl = document.getElementById('box-toolbar-page');
    if (boxToolbarEl != null) {

        observer.unobserve(boxToolbarEl);
        observer.disconnect();
    }
}

// "Last Scanned" panel.
//
// #recent-scanned is an (almost) empty stack pinned to the top of every item
// view. While it is on screen the panel stays hidden so it never covers the
// heading or the first rows; once the operator scrolls into the list the panel
// follows them. A scan is the one exception: it is revealed for a short moment
// even at the top, as scan confirmation.
const LAST_SCANNED_DWELL_MS = 3000;
const LAST_SCANNED_MAX_RENDER_FRAMES = 20;

let lastScannedObserver = null;
let lastScannedHideTimer = null;
let lastScannedAtTop = true;
let lastScannedStateKnown = false;

function lastScannedPanel() {
    // Re-resolved on every call: the panel is destroyed and recreated on
    // navigation, and the module instance survives that.
    return document.getElementById('recent-scanned-floating');
}

function hideLastScannedPanel() {
    lastScannedPanel()?.classList.remove('show-recent-scanned');
}

function clearLastScannedTimer() {
    if (lastScannedHideTimer == null) return;

    clearTimeout(lastScannedHideTimer);
    lastScannedHideTimer = null;
}

export function ObserveLastScannedPanel() {
    UnObserveLastScannedPanel();

    const sentinel = document.getElementById('recent-scanned');
    if (sentinel == null) return;

    lastScannedObserver = new IntersectionObserver((entries) => {
        const entry = entries[entries.length - 1];
        if (entry == null) return;

        const atTop = entry.isIntersecting;

        // The first callback only reports where the operator already is. It
        // must not cancel the reveal that started this observation.
        if (!lastScannedStateKnown) {
            lastScannedStateKnown = true;
            lastScannedAtTop = atTop;

            if (!atTop) {
                clearLastScannedTimer();
                lastScannedPanel()?.classList.add('show-recent-scanned');
            }

            return;
        }

        if (atTop === lastScannedAtTop) return;

        lastScannedAtTop = atTop;
        clearLastScannedTimer();
        lastScannedPanel()?.classList.toggle('show-recent-scanned', !atTop);
    }, { threshold: 0 });

    lastScannedObserver.observe(sentinel);
}

export function RevealLastScanned() {
    // The panel only renders once LastScanned is set, which happens after this
    // call - retry across frames until Blazor has put it in the DOM.
    const attempt = (remaining) => {
        const panel = lastScannedPanel();

        if (panel == null) {
            if (remaining > 0) requestAnimationFrame(() => attempt(remaining - 1));
            return;
        }

        panel.classList.add('show-recent-scanned');
        clearLastScannedTimer();

        // At the top the panel gets out of the way again on its own; scrolled
        // into the list it stays until the operator returns to the top.
        if (lastScannedAtTop) {
            lastScannedHideTimer = setTimeout(() => {
                lastScannedHideTimer = null;
                hideLastScannedPanel();
            }, LAST_SCANNED_DWELL_MS);
        }
    };

    attempt(LAST_SCANNED_MAX_RENDER_FRAMES);
}

export function UnObserveLastScannedPanel() {
    clearLastScannedTimer();

    lastScannedAtTop = true;
    lastScannedStateKnown = false;

    if (lastScannedObserver == null) return;

    lastScannedObserver.disconnect();
    lastScannedObserver = null;
}

