

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

