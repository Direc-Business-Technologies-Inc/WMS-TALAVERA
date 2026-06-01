async function LogoutAPI(uri) {
    try {
        const response = await fetch(uri, {
            method: "GET",
            credentials: "include",
        });

        if (!response.ok) {
            console.error("Logout failed");
            return false;
        }
        return true;
    }
    catch (error) {
        console.error(error);
        return false;
    }
}
