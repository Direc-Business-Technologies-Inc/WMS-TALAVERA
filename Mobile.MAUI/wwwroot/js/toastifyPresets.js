
function showToast(message, className) {
    window.Toastify({
        text: message,
        duration: 2000,
        //close: true,
        //gravity: "top",
        //position: "right",
        className,
    }).showToast();
}

export const ShowSuccess = (message) => showToast(message, "toast-success");
export const ShowError = (message) => showToast(message, "toast-error");
export const ShowWarning = (message) => showToast(message, "toast-warning");