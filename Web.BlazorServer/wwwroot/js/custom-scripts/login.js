class Login {
    UserName = "";
    Password = "";
    Success = false;
    Message = "";

    constructor(username, password, message, status = false) {
        this.UserName = username;
        this.Password = password;
        this.Message = message;
        this.Success = status;
    }
}

async function LoginAPI(username, password, uri) {
    try {
        const user = new Login(username, password, "Login Attempt");

        const response = await fetch(uri, {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
            },
            body: JSON.stringify(user),
        });

        let message = "";

        if (response.ok) {
            message = await response.text();
        } else {
            const error = await response.json();
            message = error.message ?? error.detail;
        }

        return new Login(username, password, message, response.ok);
    }
    catch (err) {
        console.error(err);

        return new Login(username, password, "Unexpected error", false);
    }
}
