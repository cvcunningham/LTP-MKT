// The Intern Directory: every employee, grouped by access level. Level 4 (ManageDirectory) gets
// Add / Edit / Delete / Reset password; anyone else a Lead lets in sees it view only.
// The Edit pop-up sets the level and a Yes/No switch per page (what the level allows on each page is
// in api/Auth/Permissions.cs). A Lead can't delete themselves or take away their own access to edit
// the directory (the API refuses). Role isn't chosen here: the API makes Level 4 "Lead".

// The access level groups, in order. value = what's saved in employees.csv's AccessLevel column.
const ACCESS_LEVEL_GROUPS = [
    { value: 1, title: "Level 1 (Semesters 1 & 2)" },
    { value: 2, title: "Level 2 (Semesters 3 & 4)" },
    { value: 3, title: "Level 3 (Semesters 5 & 6)" },
    { value: 4, title: "Level 4: Leads (Semesters 7 & 8)" }
];

const directoryGroups = document.getElementById("directory-groups");
let interns = [];
let levelPages = null; // for each level, the pages that are Yes by default (loaded once)

async function loadDirectory() {
    const message = document.getElementById("directory-load-message");
    const canManage = can("ManageDirectory");
    document.getElementById("add-intern").classList.toggle("d-none", !canManage);
    document.getElementById("directory-edit-help").classList.toggle("d-none", !canManage);
    showMessage(message, "Loading...");
    try {
        [interns, levelPages] = await Promise.all([getJson("/employees"), levelPages || getJson("/employees/level-pages")]);
        message.textContent = "";
        renderDirectory();
    } catch (error) {
        showMessage(message, error.message, "text-danger");
    }
}

// One table per access level. Anyone without a level (e.g. typed by hand in the CSV) is listed at
// the end, so they can be fixed with Edit.
function renderDirectory() {
    directoryGroups.textContent = "";
    ACCESS_LEVEL_GROUPS.forEach((group) => {
        renderLevelGroup(group.title, interns.filter((e) => e.accessLevel === group.value), "No interns at this level yet.");
    });
    const known = ACCESS_LEVEL_GROUPS.map((group) => group.value);
    const others = interns.filter((e) => !known.includes(e.accessLevel));
    if (others.length > 0) renderLevelGroup("No access level set", others, "");
}

function renderLevelGroup(title, people, emptyText) {
    const section = addElement(directoryGroups, "section", null, "mb-4");
    const heading = addElement(section, "h2", title, "h4");
    addElement(heading, "span", String(people.length), "badge text-bg-secondary ms-2 align-middle fs-6");

    if (people.length === 0) {
        addElement(section, "p", emptyText, "text-body-secondary");
        return;
    }
    const table = addElement(addElement(section, "div", null, "table-responsive"), "table", null, "table table-hover align-middle");
    const headRow = addElement(addElement(table, "thead"), "tr");
    ["ID", "Name", "Campus", "Semester", "Role", "Email", "Phone"].forEach((text) => addElement(headRow, "th", text));
    if (can("ManageDirectory")) addElement(addElement(headRow, "th"), "span", "Actions", "visually-hidden");

    const body = addElement(table, "tbody");
    people.forEach((person) => {
        const row = addElement(body, "tr");
        addElement(row, "td", String(person.id), "text-body-secondary");
        const name = addElement(row, "td", person.name, "fw-semibold");
        // A Lead turned at least one page on or off differently from the level's usual pages.
        if (person.customAccess) addElement(name, "span", "Custom page access", "badge text-bg-warning ms-2 fw-normal");
        addElement(row, "td", person.campus || "—");
        addElement(row, "td", person.semester || "—", "text-nowrap");
        const role = addElement(row, "td");
        if (person.role) addElement(role, "span", person.role, "badge text-bg-primary");
        else role.textContent = "—";
        addElement(row, "td", person.email || "—");
        addElement(row, "td", person.phone || "—", "text-nowrap");
        if (!can("ManageDirectory")) return;
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
    internLevel.value = person ? person.accessLevel || "" : "";
    renderPageAccess(person ? person.pageAccess : defaultPageAccess(internLevel.value));
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
        accessLevel: Number(internLevel.value),
        pageAccess: readPageAccess(),
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

// ----- Access level and page Yes/No switches ------------------------------------------------

const internLevel = document.getElementById("intern-level");
const internPageAccess = document.getElementById("intern-page-access");

// Changing the level resets the switches to that level's pages (they can still be changed before Save).
internLevel.addEventListener("change", () => renderPageAccess(defaultPageAccess(internLevel.value)));

// A new intern's level follows their semester (1 & 2 -> Level 1, ...) until a level is chosen.
document.getElementById("intern-semester").addEventListener("change", (event) => {
    if (editingIntern || internLevel.value) return;
    const level = ["1-2", "3-4", "5-6", "7-8"].indexOf(event.target.value) + 1;
    if (level > 0) {
        internLevel.value = level;
        renderPageAccess(defaultPageAccess(level));
    }
});

// { "form": true, "bank": false, ... } for a level ("" = no level yet: every page No).
function defaultPageAccess(level) {
    const pages = (levelPages && levelPages[level]) || [];
    return Object.fromEntries(PAGES.map((page) => [page.id, pages.includes(page.id)]));
}

// One row per page: its name, what Yes gives at the chosen level, and a Yes / No switch.
function renderPageAccess(pageAccess) {
    internPageAccess.textContent = "";
    const levelDefaults = defaultPageAccess(internLevel.value);
    PAGES.forEach((page) => {
        const row = addElement(internPageAccess, "div", null, "list-group-item d-flex justify-content-between align-items-center gap-3");
        const text = addElement(row, "div");
        addElement(text, "div", page.title, "fw-semibold");
        if (internLevel.value) {
            addElement(text, "div", levelDefaults[page.id] ? `Level ${internLevel.value} access` : "Yes = view only", "small text-body-secondary");
        }

        const group = addElement(row, "div", null, "btn-group btn-group-sm flex-shrink-0");
        group.setAttribute("role", "group");
        group.setAttribute("aria-label", `${page.title} access`);
        [["yes", "Yes", "btn-outline-success"], ["no", "No", "btn-outline-secondary"]].forEach(([value, label, style]) => {
            const input = addElement(group, "input", null, "btn-check");
            input.type = "radio";
            input.name = "intern-access-" + page.id;
            input.id = `intern-access-${page.id}-${value}`;
            input.value = value;
            input.checked = (value === "yes") === Boolean(pageAccess[page.id]);
            const button = addElement(group, "label", label, "btn " + style);
            button.htmlFor = input.id;
        });
    });
}

function readPageAccess() {
    return Object.fromEntries(PAGES.map((page) => [
        page.id,
        internPageAccess.querySelector(`input[name="intern-access-${page.id}"]:checked`).value === "yes"
    ]));
}

function showInternError(text) {
    internError.textContent = "";
    addElement(internError, "div", text, "alert alert-danger mb-0");
}

onPageShow("directory", loadDirectory);
