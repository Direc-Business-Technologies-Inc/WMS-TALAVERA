window.blurActiveElement = function () {
    if (document.activeElement instanceof HTMLElement) {
        document.activeElement.blur();
    }
};

document.addEventListener('keydown', function (e) {
    if ((e.key === 'Enter' || e.key === ' ') &&
        document.activeElement &&
        document.activeElement.tagName === 'BUTTON') {
        e.preventDefault();
    }
}, true);
