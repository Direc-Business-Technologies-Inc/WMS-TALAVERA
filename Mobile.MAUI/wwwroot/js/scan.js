export function startScan(dotNetObjectRef) {
    let scanfld = document.getElementById("scan_fld");
    let xtra = document.getElementById("xtra_fld");

    scanfld.addEventListener("input", args => {
        console.log(args)
        callRazorFunction(dotNetObjectRef, args.data)
    });
}

export function refocusToScan() {
    let scanfld = document.getElementById("scan_fld");
    scanfld.focus();
}

function callRazorFunction(dotNetObjectRef, scannedData) {
    dotNetObjectRef.invokeMethodAsync('ItemScanned', scannedData)
        .then(() => console.log('Instance function executed successfully!'))
        .catch(error => console.error('Error calling instance function:', error));
}
