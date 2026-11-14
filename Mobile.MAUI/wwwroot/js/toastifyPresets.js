
export function ShowSuccess(message, durationMs = 3000) {
    try {
        console.log(message);
        window.Toastify({
            text: message,
            duration: durationMs,
            className: "toast-success",

        }).showToast();
        return true;
    } catch (e) {
        console.error("Unable to display success toast", e);
        return false;
    }
}

export function ShowError(message) {
    try {
        console.log(message);
        window.Toastify({
            text: message,

            className: "toast-error",

        }).showToast();
    } catch (e) {
        console.log(e);
    }
}
export function ShowWarning(message) {
    try {
        console.log(message);
        window.Toastify({
            text: message,

            className: "toast-warning ",

        }).showToast();
    } catch (e) {
        console.log(e);
    }
}
