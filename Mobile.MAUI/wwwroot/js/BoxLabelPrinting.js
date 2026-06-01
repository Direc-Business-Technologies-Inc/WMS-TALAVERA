

 async function GetPrinters() {
    try {

        const res = await fetch(`${baseUri}/printerlist`, {
            method: "GET",
            headers: {
                'Content-Type': 'application/json'
            }
        });

        if (res.ok) {
            const data = await res.json()

            return { StatusCode: 200, Message: "Success", Data: data };
        } else {
            const error = await res.text();
            return { StatusCode: 400, Message: error };;
        }
    } catch (e) {
        console.log(e)
        return { StatusCode: 500, Message: e };
    }
}
 async function GetBoxLabelLayouts(baseUri) {
    try {

        const res = await fetch(`${baseUri}/getrptlayout`, {
            method: "GET",
            headers: {
                'Content-Type': 'application/json'
            }
        });

        if (res.ok) {
            const data = await res.json()

            return { StatusCode: 200, Message: "Success", Data: data };
        } else {
            const error = await res.text();
            return { StatusCode: 400, Message: error };;
        }
    } catch (e) {
        console.log(e)
        return { StatusCode: 500, Message: e };
    }
}

async function PrintLayout(baseUri, packingNumber, boxLabelLayout, selectedPrinter) {
    try {
        if (!fileUrl || !boxLabelLayout || !selectedPrinter) {
            return false;
        }
        const response = await fetch(fileUrl);

        const blob = await response.blob();

        const formData = new FormData();
        const arr = fileUrl.split('/');
        const fileName = arr[arr.length - 1];
        console.log(fileName);
        formData.append('file', blob, fileName);

        const res1 = await fetch(`${baseUri}/uploadfile`, {
            method: 'POST',
            body: formData
        });
        console.log(res1, await res1.json());
        if (res1.ok) {
            const res2 = await fetch(`${baseUri}/pgprintreport?parameter=${packingNumber}&FilePath=${boxLabelLayout}`, {
                method: 'GET',

            });

            if (res2.ok) {
                return { StatusCode: 200, Message: "Success", Data: true };
            } else {
                return { StatusCode: 400, Message: "Failed", Data: false };
            }
        }


    } catch (e) {
        console.log(e);
        return { StatusCode: 500, Message: "Failed", Data: null };
    }
}
