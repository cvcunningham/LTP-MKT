// Logging in and out, and changing your own password. Load after common.js, before the page scripts.
// The login token is kept in sessionStorage, so closing the browser tab logs you out.

const loginForm = document.getElementById("login-form");
const loginError = document.getElementById("login-error");

// Runs once every script has loaded: if this tab is already logged in, carry on where it was.
document.addEventListener("DOMContentLoaded", async () => {
    if (getToken()) {
        try {
            const response = await apiFetch(`${API_BASE}/auth/me`);
            if (response.ok) me = await response.json();
            else sessionStorage.removeItem("ltp-token");
        } catch (error) {
            showLoginError("Could not reach the API. Is it running?");
        }
    }
    renderNavbar();
    showCurrentPage();
});

loginForm.addEventListener("submit", async (event) => {
    event.preventDefault();
    loginForm.classList.add("was-validated");
    if (!loginForm.checkValidity()) {
        showLoginError("Please enter your employee ID and password.");
        return;
    }

    const button = document.getElementById("login-button");
    button.disabled = true;
    try {
        const response = await fetch(`${API_BASE}/auth/login`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
                id: Number(document.getElementById("login-id").value),
                password: document.getElementById("login-password").value
            })
        });
        const result = await response.json().catch(() => ({}));
        if (response.ok) {
            sessionStorage.setItem("ltp-token", result.token);
            me = result.me;
            loginForm.reset();
            loginForm.classList.remove("was-validated");
            loginError.textContent = "";
            renderNavbar();
            showCurrentPage();
        } else {
            document.getElementById("login-password").value = "";
            showLoginError(formatErrors(result.errors));
        }
    } catch (error) {
        showLoginError("Could not reach the API. Is it running?");
    }
    button.disabled = false;
});

function showLoginError(text) {
    loginError.textContent = "";
    addElement(loginError, "div", text, "alert alert-danger mb-0");
}

// Log out button. Reloads the page so nothing from this person's session stays on screen.
async function logOut() {
    try {
        await apiFetch(`${API_BASE}/auth/logout`, { method: "POST" });
    } catch (error) {
        // Logged out on this side anyway.
    }
    sessionStorage.removeItem("ltp-token");
    location.href = location.pathname;
}

// The API no longer accepts the token (e.g. it was restarted, or the login expired).
function sessionEnded() {
    sessionStorage.removeItem("ltp-token");
    me = null;
    renderNavbar();
    showCurrentPage();
    showLoginError("Your login has ended. Please log in again.");
}

// Re-reads who is logged in (e.g. after a Lead edits their own details in the Intern Directory).
async function refreshMe() {
    me = await getJson("/auth/me");
    renderNavbar();
    showCurrentPage();
}

// ----- Change password pop-up -------------------------------------------------------------

const passwordModal = new bootstrap.Modal(document.getElementById("password-modal"));
const passwordForm = document.getElementById("password-form");
const passwordMessage = document.getElementById("password-message");

function openPasswordForm() {
    passwordForm.reset();
    passwordForm.classList.remove("was-validated");
    passwordMessage.textContent = "";
    passwordModal.show();
}

passwordForm.addEventListener("submit", async (event) => {
    event.preventDefault();
    const newPassword = document.getElementById("password-new").value;
    const confirmField = document.getElementById("password-confirm");
    confirmField.setCustomValidity(confirmField.value === newPassword ? "" : "Doesn't match");
    passwordForm.classList.add("was-validated");
    if (!passwordForm.checkValidity()) {
        showPasswordMessage("danger", confirmField.value === newPassword
            ? "Please fill in the fields outlined in red. The new password needs at least 8 characters."
            : "The two new passwords don't match.");
        return;
    }

    const button = document.getElementById("password-save");
    button.disabled = true;
    try {
        const response = await apiFetch(`${API_BASE}/auth/password`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
                currentPassword: document.getElementById("password-current").value,
                newPassword
            })
        });
        if (response.ok) {
            passwordForm.reset();
            passwordForm.classList.remove("was-validated");
            showPasswordMessage("success", "Your password was changed.");
        } else {
            const result = await response.json().catch(() => ({}));
            showPasswordMessage("danger", "Your password was NOT changed. " + formatErrors(result.errors));
        }
    } catch (error) {
        showPasswordMessage("danger", "Your password was NOT changed because the server couldn't be reached.");
    }
    button.disabled = false;
});

function showPasswordMessage(kind, text) {
    passwordMessage.textContent = "";
    addElement(passwordMessage, "div", text, "alert alert-" + kind + " mb-0");
}
