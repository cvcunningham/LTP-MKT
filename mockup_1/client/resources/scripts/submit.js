// The Ideas Form: one form = one idea.
const form = document.getElementById("idea-form");
const linksContainer = document.getElementById("links");
const submitButton = document.getElementById("submit-button");
const message = document.getElementById("message");

const campus = document.getElementById("campus");

document.getElementById("add-link").addEventListener("click", addLinkRow);

// The idea is submitted as whoever is logged in (the API records it). Their campus is filled in
// if none is chosen yet (it can still be changed). Nothing else is reset, so switching pages and
// coming back keeps whatever you've typed.
function fillSubmitter() {
    document.getElementById("submitting-as").textContent = me.employee.name;
    if (campus.value === "") campus.value = me.employee.campus;
}

// Each link row: Type (Photo/Video), optional Label, URL, and a Remove button.
function addLinkRow() {
    const row = addElement(linksContainer, "div", null, "row g-2 mb-2 link-row");

    const type = addElement(addElement(row, "div", null, "col-sm-3"), "select", null, "form-select link-type");
    type.setAttribute("aria-label", "Photo or video");
    ["Photo", "Video"].forEach((choice) => (addElement(type, "option", choice).value = choice));

    const label = addElement(addElement(row, "div", null, "col-sm-3"), "input", null, "form-control link-label");
    label.type = "text";
    label.placeholder = "Label (optional)";
    label.setAttribute("aria-label", "Label (optional)");

    const url = addElement(addElement(row, "div", null, "col-sm-4"), "input", null, "form-control link-url");
    url.type = "url";
    url.placeholder = "https://...";
    url.pattern = "https://.*";
    url.required = true;
    url.setAttribute("aria-label", "Link (starts with https://)");

    addButton(addElement(row, "div", null, "col-sm-2"), "Remove", () => row.remove(), "btn-outline-danger w-100");
    url.focus();
}

// A Bootstrap alert under the form. kind is "success", "danger" or "warning".
function showAlert(kind, text) {
    message.textContent = "";
    addElement(message, "div", text, "alert alert-" + kind);
}

form.addEventListener("submit", async (event) => {
    event.preventDefault();

    // The browser checks the required fields and https:// links first, and outlines what's wrong.
    form.classList.add("was-validated");
    if (!form.checkValidity()) {
        showAlert("danger", "Your idea was NOT saved yet. Please fix the fields outlined in red.");
        return;
    }

    const data = new FormData(form);
    const idea = {
        title: data.get("title"),
        campus: data.get("campus"),
        contentType: data.get("contentType"),
        contentCategory: data.get("contentCategory"),
        concept: data.get("concept"),
        caption: data.get("caption"),
        links: Array.from(linksContainer.querySelectorAll(".link-row")).map((row) => ({
            type: row.querySelector(".link-type").value,
            label: row.querySelector(".link-label").value,
            url: row.querySelector(".link-url").value
        }))
    };

    // The API takes a multipart form (so it can also accept uploaded files later):
    // the idea goes as JSON in the "idea" part.
    const body = new FormData();
    body.append("idea", JSON.stringify(idea));

    submitButton.disabled = true; // prevents double submissions
    showAlert("secondary", "Saving...");
    try {
        const response = await apiFetch(`${API_BASE}/ideas`, { method: "POST", body });
        const result = await response.json().catch(() => ({}));

        if (response.ok) {
            showAlert("success", `Saved! Your idea "${result.title}" was sent for review with ${result.links.length} link(s).`);
            form.reset();
            form.classList.remove("was-validated");
            linksContainer.textContent = "";
            fillSubmitter();
        } else {
            // The form is NOT reset, so the user can fix the problem and submit again.
            showAlert("danger", "Your idea was NOT saved. " + formatErrors(result.errors));
        }
    } catch (error) {
        showAlert("danger", "Your idea was NOT saved because the server couldn't be reached. " +
            "Your entries are still filled in. Please try again.");
    }
    submitButton.disabled = false;
});

onPageShow("form", fillSubmitter);
