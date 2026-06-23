
export function ShowSuccess(message) {
    try {
        console.log(message);
        window.Toastify({
            text: message,
            
            className: "toast-success",

        }).showToast();
    } catch (e) {
        console.log(e);
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