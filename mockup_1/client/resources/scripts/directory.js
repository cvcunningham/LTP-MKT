// The Intern Directory (semesters 7-8 only): every employee, grouped by semester, with Add / Edit /
// Delete / Reset password. A Lead can't delete themselves or move themselves out of 7-8 (the API refuses).
// Role isn't chosen here: the API makes semesters 7-8 "Lead" and leaves everyone else blank.

// The semester groups, in order. value = what's saved in employees.csv's Semester column.
const SEMESTER_GROUPS = [
    { value: "1-2", title: "Semester 1 & 2" },
    { value: "3-4", title: "Semester 3 & 4" },
    { value: "5-6", title: "Semester 5 & 6" },
    { value: "7-8", title: "Semester 7 & 8" }
];

const directoryGroups = document.getElementById("directory-groups");
let interns = [];

async function loadDirectory() {
    const message = document.getElementById("directory-load-message");
    showMessage(message, "Loading...");
    try {
        interns = await getJson("/employees");
        message.textContent = "";
        renderDirectory();
    } catch (error) {
        showMessage(message, error.message, "text-danger");
    }
}

// One table per semester group. Anyone whose semester isn't one of the groups (e.g. typed by
// hand in the CSV) is listed at the end, so they can be fixed with Edit.
function renderDirectory() {
    directoryGroups.textContent = "";
    SEMESTER_GROUPS.forEach((group) => {
        renderSemesterGroup(group.title, interns.filter((e) => e.semester === group.value), "No interns in these semesters yet.");
    });
    const known = SEMESTER_GROUPS.map((group) => group.value);
    const others = interns.filter((e) => !known.includes(e.semester));
    if (others.length > 0) renderSemesterGroup("No semester set", others, "");
}

function renderSemesterGroup(title, people, emptyText) {
    const section = addElement(directoryGroups, "section", null, "mb-4");
    const heading = addElement(section, "h2", title, "h4");
    addElement(heading, "span", String(people.length), "badge text-bg-secondary ms-2 align-middle fs-6");

    if (people.length === 0) {
        addElement(section, "p", emptyText, "text-body-secondary");
        return;
    }
    const table = addElement(addElement(section, "div", null, "table-responsive"), "table", null, "table table-hover align-middle");
    const headRow = addElement(addElement(table, "thead"), "tr");
    ["ID", "Name", "Campus", "Role", "Email", "Phone"].forEach((text) => addElement(headRow, "th", text));
    addElement(addElement(headRow, "th"), "span", "Actions", "visually-hidden");

    const body = addElement(table, "tbody");
    people.forEach((person) => {
        const row = addElement(body, "tr");
        addElement(row, "td", String(person.id), "text-body-secondary");
        addElement(row, "td", person.name, "fw-semibold");
        addElement(row, "td", person.campus || "—");
        const role = addElement(row, "td");
        if (person.role) addElement(role, "span", person.role, "badge text-bg-primary");
        else role.textContent = "—";
        addElement(row, "td", person.email || "—");
        addElement(row, "td", person.phone || "—", "text-nowrap");
        const actions = addElement(row, "td", null, "text-nowrap text-end");
        addButton(actions, "Edit", () => openInternForm(person));
        if (person.id !== me.employee.id) addButton(actions, "Delete", () => deleteIntern(person), "btn-outline-danger");
    });
}

async function deleteIntern(person) {
    if (!confirm(`Permanently delete ${person.name} (ID ${person.id}) from the directory? This can't be undone. ` +
                 `Ideas and tracker jobs they're on will show "(former employee #${person.id})".`)) return false;
    const deleted = await sendChange(`${API_BASE}/employees/${person.id}`, "DELETE");
    if (deleted) loadDirectory();
    return deleted;
}

// ----- Add / edit pop-up --------------------------------------------------------------------

const internModal = new bootstrap.Modal(document.getElementById("intern-modal"));
const internForm = document.getElementById("intern-form");
const internError = document.getElementById("intern-error");
const internDeleteButton = document.getElementById("intern-delete");
const internResetButton = document.getElementById("intern-reset-password");
let editingIntern = null; // the employee being edited, or null when adding

document.getElementById("add-intern").addEventListener("click", () => openInternForm(null));

async function openInternForm(person) {
    editingIntern = person;
    internForm.classList.remove("was-validated");
    internError.textContent = "";
    document.getElementById("intern-modal-title").textContent = person ? "Edit intern" : "Add intern";
    document.getElementById("intern-id").value = person ? person.id : "";
    document.getElementById("intern-name").value = person ? person.name : "";
    document.getElementById("intern-campus").value = person ? person.campus : "";
    document.getElementById("intern-semester").value = person ? person.semester : "";
    document.getElementById("intern-email").value = person ? person.email : "";
    document.getElementById("intern-phone").value = person ? person.phone : "";
    internDeleteButton.hidden = !person || person.id === me.employee.id;
    internResetButton.hidden = !person;
    internModal.show();

    // A new intern gets the next free ID filled in (it can still be changed).
    if (!person) {
        try {
            document.getElementById("intern-id").value = (await getJson("/employees/next-id")).id;
        } catch (error) {
            showInternError(error.message);
        }
    }
}

// Sets their password back to LTPMKT (e.g. they forgot it). They can change it again after logging in.
internResetButton.addEventListener("click", async () => {
    if (!editingIntern) return;
    if (!confirm(`Reset ${editingIntern.name}'s password to the starting password (LTPMKT)?`)) return;
    if (await sendChange(`${API_BASE}/employees/${editingIntern.id}/reset-password`, "POST")) {
        alert(`${editingIntern.name}'s password is now LTPMKT.`);
    }
});

internDeleteButton.addEventListener("click", async () => {
    if (editingIntern && await deleteIntern(editingIntern)) internModal.hide();
});

internForm.addEventListener("submit", async (event) => {
    event.preventDefault();
    internForm.classList.add("was-validated");
    if (!internForm.checkValidity()) {
        showInternError("Please fill in the fields outlined in red.");
        return;
    }

    const body = {
        id: Number(document.getElementById("intern-id").value),
        name: document.getElementById("intern-name").value,
        campus: document.getElementById("intern-campus").value,
        semester: document.getElementById("intern-semester").value,
        email: document.getElementById("intern-email").value,
        phone: document.getElementById("intern-phone").value
    };

    // The pop-up stays open (with everything still filled in) if saving fails.
    const saveButton = document.getElementById("intern-save");
    saveButton.disabled = true;
    try {
        const response = await apiFetch(editingIntern ? `${API_BASE}/employees/${editingIntern.id}` : `${API_BASE}/employees`, {
            method: editingIntern ? "PUT" : "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(body)
        });
        if (response.ok) {
            internModal.hide();
            // Editing yourself (e.g. your name or ID) updates the navbar and your login.
            if (editingIntern && editingIntern.id === me.employee.id) await refreshMe();
            loadDirectory();
        } else {
            const result = await response.json().catch(() => ({}));
            showInternError("The intern was NOT saved. " + formatErrors(result.errors));
        }
    } catch (error) {
        showInternError("The intern was NOT saved because the server couldn't be reached. Please try again.");
    }
    saveButton.disabled = false;
});

function showInternError(text) {
    internError.textContent = "";
    addElement(internError, "div", text, "alert alert-danger mb-0");
}

onPageShow("directory", loadDirectory);
