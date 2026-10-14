// The Ideas Form: one form = one idea.
const form = document.getElementById("idea-form");
const linksContainer = document.getElementById("links");
const submitButton = document.getElementById("submit-button");
const message = document.getElementById("message");

const campus = addCampusField(document.getElementById("campus-field"), "campus");
let nameSelect = null; // filled in once the employee list has loaded

document.getElementById("add-link").addEventListener("click", addLinkRow);

// "Your name" is a dropdown of employees (from employees.csv), grouped by campus.
async function loadNames() {
    const field = document.getElementById("name-field");
    try {
        const employees = await getJson("/employees");
        nameSelect = createEmployeeSelect(employees, null, "Choose your name");
        nameSelect.id = "submittedBy";
        nameSelect.required = true;
        // Picking your name fills in your campus (you can still change it).
        nameSelect.addEventListener("change", () => {
            const me = employees.find((e) => e.id === selectedId(nameSelect));
            if (me && campus.value() === "") campus.set(me.campus);
        });
        field.replaceChildren(nameSelect);
        if (employees.length === 0) showAlert("warning", "There are no employees in employees.csv yet, so no one can submit an idea.");
    } catch (error) {
        showMessage(field, error.message, "text-danger");
    }
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
        submittedById: nameSelect ? selectedId(nameSelect) : null,
        title: data.get("title"),
        campus: campus.value(),
        contentType: data.get("contentType"),
        contentCategory: data.get("contentCategory"),
        concept: data.get("concept"),
        whyItCouldWork: data.get("whyItCouldWork"),
        caption: data.get("caption"),
        platforms: data.getAll("platforms"),
        hashtags: data.get("hashtags"),
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
        const response = await fetch(`${API_BASE}/ideas`, { method: "POST", body });
        const result = await response.json().catch(() => ({}));

        if (response.ok) {
            showAlert("success", `Saved! Your idea "${result.title}" was sent for review with ${result.links.length} link(s).`);
            form.reset();
            form.classList.remove("was-validated");
            campus.set("");
            linksContainer.textContent = "";
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

loadNames();
