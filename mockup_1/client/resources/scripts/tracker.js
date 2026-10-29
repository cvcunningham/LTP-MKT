// The Production Tracker: approved ideas and their production details.
// Level 2 can change only the Filmer and Editor (AssignCrew); Levels 3 and 4 can change everything
// (EditTracker); only Level 4 can delete. Anyone else a Lead lets in sees it view only.
// index.html#tracker?view=Completed&item=3 opens the Completed list and scrolls to item 3
// (used by the Content Calendar's "Open in Production Tracker" link).
const container = document.getElementById("items");
const viewFilter = document.getElementById("view-filter");

let statuses = [];  // the Status choices, loaded from the API
let employees = []; // for the Filmer / Editor / Reviewer / Poster dropdowns (only loaded for AssignCrew)

viewFilter.addEventListener("change", loadItems);

async function loadTracker() {
    const view = pageParams().get("view");
    if (view === "Completed" || view === "InProgress") viewFilter.value = view;

    showMessage(container, "Loading...");
    try {
        [statuses, employees] = await Promise.all([
            getJson("/production/statuses"),
            can("AssignCrew") ? getJson("/employees") : []
        ]);
    } catch (error) {
        showMessage(container, error.message, "text-danger");
        return;
    }
    await loadItems();
    scrollToLinkedItem();
}

async function loadItems() {
    showMessage(container, "Loading...");
    let items;
    try {
        items = await getJson(`/production?view=${viewFilter.value}`);
    } catch (error) {
        showMessage(container, error.message, "text-danger");
        return;
    }
    if (items.length === 0) {
        showMessage(container, viewFilter.value === "Completed" ? "Nothing has been completed yet." : "Nothing in production right now.");
        return;
    }
    container.textContent = "";
    items.forEach((item) => container.appendChild(renderItem(item)));
}

// Scrolls to item 3 (if the page was opened with &item=3) and outlines it.
function scrollToLinkedItem() {
    const item = pageParams().get("item");
    const target = item ? document.getElementById("item-" + item) : null;
    if (!target) return;
    target.querySelector(".card").classList.add("border-primary", "border-3");
    target.scrollIntoView({ behavior: "smooth", block: "start" });
}

function renderItem(item) {
    const idea = item.idea;
    const column = document.createElement("div");
    column.className = "col-lg-6";
    column.id = "item-" + item.id;
    const card = addElement(column, "article", null, "card h-100");

    const header = addElement(card, "div", null, "card-header d-flex justify-content-between align-items-center gap-2");
    addElement(header, "h3", idea.title, "h5 mb-0");
    addBadge(header, item.status, PRODUCTION_STATUS_CLASSES);

    const body = addElement(card, "div", null, "card-body");

    // From the approved idea (can't be changed here).
    addDetail(body, "Campus", idea.campus);
    addDetail(body, "Content type", idea.contentType);
    addDetail(body, "Content category", idea.contentCategory);
    addDetail(body, "Caption", idea.caption);
    body.appendChild(renderLinks(idea.links, "Content files"));
    addElement(body, "hr");

    if (!can("AssignCrew")) {
        renderItemReadOnly(card, body, item);
        return column;
    }
    if (!can("EditTracker")) {
        renderItemCrewOnly(card, body, item);
        return column;
    }

    // Production details (Levels 3 and 4 can change these).
    const row = addElement(body, "div", null, "row g-2");
    const filmer = addPersonField(row, "Filmer", item.filmerId, item.filmer);
    const editor = addPersonField(row, "Editor", item.editorId, item.editor);
    const reviewer = addPersonField(row, "Reviewer/approver", item.reviewerId, item.reviewer);
    const poster = addPersonField(row, "Poster (who posts it)", item.posterId, item.poster);

    const date = addField(row, "Target post date", document.createElement("input"));
    date.type = "date";
    date.value = item.targetPostDate || "";

    const status = addField(row, "Status", document.createElement("select"));
    status.className = "form-select";
    statuses.forEach((choice) => (addElement(status, "option", choice).value = choice));
    status.value = item.status;

    const footer = addElement(card, "div", null, "card-footer d-flex flex-wrap align-items-center gap-2");
    addButton(footer, "Save", async () => {
        const saved = await sendChange(`${API_BASE}/production/${item.id}`, "PUT", {
            filmerId: selectedId(filmer),
            editorId: selectedId(editor),
            reviewerId: selectedId(reviewer),
            posterId: selectedId(poster),
            targetPostDate: date.value || null,
            status: status.value
        });
        // If saving failed, the page is NOT reloaded, so the changes are kept.
        if (saved) {
            if ((status.value === "Completed") !== (viewFilter.value === "Completed")) {
                alert(`Saved. "${idea.title}" is now ${status.value}, so it moved to "${status.value === "Completed" ? "Completed" : "In progress"}".`);
            }
            loadItems();
        }
    }, "btn-primary");
    if (can("DeleteTracker")) {
        addButton(footer, "Delete", async () => {
            if (confirm(`Permanently delete "${idea.title}" from the tracker, along with its idea and links? This can't be undone.`)) {
                if (await sendChange(`${API_BASE}/production/${item.id}`, "DELETE")) loadItems();
            }
        }, "btn-outline-danger");
    }
    addElement(footer, "small", "Last saved " + formatDateTime(item.updatedAt), "text-body-secondary ms-auto");
    return column;
}

// Level 2: Filmer and Editor dropdowns with a Save button; everything else as plain text.
// (The API ignores any other change from them.)
function renderItemCrewOnly(card, body, item) {
    const row = addElement(body, "div", null, "row g-2 mb-3");
    const filmer = addPersonField(row, "Filmer", item.filmerId, item.filmer);
    const editor = addPersonField(row, "Editor", item.editorId, item.editor);
    addDetail(body, "Reviewer/approver", item.reviewer || "Not assigned");
    addDetail(body, "Poster (who posts it)", item.poster || "Not assigned");
    addDetail(body, "Target post date", formatDay(item.targetPostDate));
    addDetail(body, "Status", item.status);

    const footer = addElement(card, "div", null, "card-footer d-flex flex-wrap align-items-center gap-2");
    addButton(footer, "Save", async () => {
        const saved = await sendChange(`${API_BASE}/production/${item.id}`, "PUT", {
            filmerId: selectedId(filmer),
            editorId: selectedId(editor)
        });
        // If saving failed, the page is NOT reloaded, so the changes are kept.
        if (saved) loadItems();
    }, "btn-primary");
    addElement(footer, "small", "Last saved " + formatDateTime(item.updatedAt), "text-body-secondary ms-auto");
}

// View only: the production details as plain text, no Save or Delete.
function renderItemReadOnly(card, body, item) {
    addDetail(body, "Filmer", item.filmer || "Not assigned");
    addDetail(body, "Editor", item.editor || "Not assigned");
    addDetail(body, "Reviewer/approver", item.reviewer || "Not assigned");
    addDetail(body, "Poster (who posts it)", item.poster || "Not assigned");
    addDetail(body, "Target post date", formatDay(item.targetPostDate));
    addDetail(body, "Status", item.status);
    const footer = addElement(card, "div", null, "card-footer");
    addElement(footer, "small", "Last saved " + formatDateTime(item.updatedAt), "text-body-secondary");
}

// An employee dropdown for one job. On rows from before employees.csv, the typed name is shown
// underneath until someone is chosen and saved.
function addPersonField(row, labelText, employeeId, name) {
    const select = addField(row, labelText, createEmployeeSelect(employees, employeeId, "Not assigned"));
    if (employeeId == null && name) {
        addElement(select.parentElement, "div", `Typed earlier: ${name}`, "form-text");
    }
    return select;
}

// A label and input in half of the card's width (full width on phones).
let fieldCount = 0;
function addField(row, labelText, input) {
    const column = addElement(row, "div", null, "col-sm-6");
    input.id = "field-" + ++fieldCount;
    if (!input.className) input.className = "form-control";
    const label = addElement(column, "label", labelText, "form-label small mb-1");
    label.htmlFor = input.id;
    column.appendChild(input);
    return input;
}

onPageShow("tracker", loadTracker);
