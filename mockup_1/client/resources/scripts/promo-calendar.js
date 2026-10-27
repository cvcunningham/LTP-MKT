// The Promo Calendar: outreach scripts, the Promo List (every promo), and a calendar of the
// Scheduled and Completed promos (CALENDAR_STATUSES).
// Semesters 3-4 see it view only (and can Copy scripts); 5-6 and 7-8 can add and edit promos and
// script wording (EditPromos); only 7-8 can delete promos (DeletePromos).

// The mobile ordering app for each campus, filled in for [APP] in the scripts.
const ORDERING_APPS = {
    "Alabama": "Bama Dining Mobile Order app",
    "Ole Miss": "GrubHub app"
};

// Placeholders in the scripts that mean "the partner's name".
const NAME_PLACEHOLDERS = ["[SORORITY]", "[ORGANIZATION]", "[BUSINESS]"];

const fillSelect = document.getElementById("fill-promo");
const scriptsContainer = document.getElementById("scripts");
const promoRows = document.getElementById("promo-rows");
const promoCalendar = createMonthCalendar(document.getElementById("promo-calendar-grid"), (promo) => {
    if (can("EditPromos")) openPromoForm(promo);
    else showPromoDetails(promo);
});
renderColorKey(document.getElementById("promo-color-key"), PROMO_STATUS_CLASSES, CALENDAR_STATUSES);

let promos = [];
let scripts = [];
let optionsLoaded = false; // the pop-up's Partner type and Status choices only need loading once

// ----- Loading -------------------------------------------------------------------

async function loadPromoCalendar() {
    document.getElementById("add-promo").classList.toggle("d-none", !can("EditPromos"));
    if (!optionsLoaded) {
        try {
            const options = await getJson("/promos/options");
            fillOptions(document.getElementById("promo-type"), options.partnerTypes);
            fillOptions(document.getElementById("promo-status"), options.statuses);
            optionsLoaded = true;
        } catch (error) {
            showMessage(scriptsContainer, error.message, "text-danger");
            return;
        }
    }
    await Promise.all([loadScripts(), loadPromos()]);
}

function fillOptions(select, choices) {
    choices.forEach((choice) => (addElement(select, "option", choice).value = choice));
}

async function loadScripts() {
    try {
        scripts = await getJson("/promos/scripts");
        renderScripts();
    } catch (error) {
        showMessage(scriptsContainer, error.message, "text-danger");
    }
}

async function loadPromos() {
    try {
        promos = await getJson("/promos");
    } catch (error) {
        promoRows.textContent = "";
        addElement(addElement(promoRows, "tr"), "td", error.message, "text-danger").colSpan = 8;
        return;
    }
    renderPromoList();
    renderFillChoices();
    renderScripts();
    promoCalendar.setItems(promos.filter((promo) => CALENDAR_STATUSES.includes(promo.status)).map((promo) => ({
        date: promo.date,
        label: promo.name,
        className: PROMO_STATUS_CLASSES[promo.status] || "text-bg-secondary",
        data: promo
    })));
}

// ----- Outreach scripts -------------------------------------------------------------

// "Fill in from promo": every promo, soonest first. Keeps the current choice if it still exists.
function renderFillChoices() {
    const chosen = fillSelect.value;
    fillSelect.textContent = "";
    addElement(fillSelect, "option", "None (show the [placeholders])").value = "";
    promos.forEach((p) => (addElement(fillSelect, "option", `${p.name} (${p.partnerType}, ${formatDay(p.date)})`).value = p.id));
    fillSelect.value = promos.some((p) => String(p.id) === chosen) ? chosen : "";
}
fillSelect.addEventListener("change", renderScripts);

function chosenPromo() {
    return promos.find((p) => String(p.id) === fillSelect.value) || null;
}

// Replaces the placeholders with the promo's details. Anything the promo doesn't have stays as [it is].
function fillIn(text, promo) {
    if (!promo) return text;
    let filled = text;
    NAME_PLACEHOLDERS.forEach((placeholder) => (filled = filled.split(placeholder).join(promo.name)));
    if (promo.promoCode) filled = filled.split("[CODE]").join(promo.promoCode);
    filled = filled.split("[DAY/DATE]").join(parseDay(promo.date).toLocaleDateString(undefined, { weekday: "long", month: "long", day: "numeric" }));
    if (ORDERING_APPS[promo.campus]) filled = filled.split("[APP]").join(ORDERING_APPS[promo.campus]);
    return filled;
}

function renderScripts() {
    const promo = chosenPromo();
    scriptsContainer.textContent = "";
    scripts.forEach((script) => scriptsContainer.appendChild(renderScript(script, promo)));
}

// One partner type's script: the message, the forward message, Copy buttons, and Edit.
function renderScript(script, promo) {
    const column = document.createElement("div");
    column.className = "col-lg-4";
    const matches = promo && promo.partnerType === script.partnerType;
    const card = addElement(column, "div", null, "card h-100" + (matches ? " border-primary border-2" : ""));

    const header = addElement(card, "div", null, "card-header d-flex justify-content-between align-items-center");
    addElement(header, "h3", script.partnerType, "h6 mb-0");
    if (matches) addElement(header, "span", "Matches this promo", "badge text-bg-primary");

    const body = addElement(card, "div", null, "card-body");
    const footer = addElement(card, "div", null, "card-footer");

    showScriptText(body, footer, script, promo);
    return column;
}

function showScriptText(body, footer, script, promo) {
    body.textContent = "";
    footer.textContent = "";
    addCopyBlock(body, "Message to the partner", fillIn(script.message, promo));
    if (script.forwardMessage) addCopyBlock(body, "Message for them to forward", fillIn(script.forwardMessage, promo));
    if (can("EditPromos")) addButton(footer, "Edit wording", () => showScriptEditor(body, footer, script, promo));
    if (script.updatedAt) addElement(footer, "small", "Last edited " + formatDateTime(script.updatedAt), "text-body-secondary");
}

function addCopyBlock(parent, heading, text) {
    const top = addElement(parent, "div", null, "d-flex justify-content-between align-items-center mb-1");
    addElement(top, "strong", heading, "small");
    const copy = addButton(top, "Copy", () => copyText(text, copy), "btn-outline-primary");
    copy.classList.remove("me-2");
    addElement(parent, "p", text, "script-text border rounded p-2 bg-body-tertiary small");
}

async function copyText(text, button) {
    try {
        await navigator.clipboard.writeText(text);
        button.textContent = "Copied!";
        setTimeout(() => (button.textContent = "Copy"), 1500);
    } catch (error) {
        alert("Your browser didn't allow copying. Select the text and copy it by hand instead.");
    }
}

// Edit the original wording (with its [placeholders]), then Save.
function showScriptEditor(body, footer, script, promo) {
    body.textContent = "";
    footer.textContent = "";
    addElement(body, "p", "Use [CODE], [DAY/DATE], [APP] and " + NAME_PLACEHOLDERS.join(" / ") +
        " where the promo's details should go.", "small text-body-secondary");
    const message = addTextArea(body, "Message to the partner", script.message);
    const forward = addTextArea(body, "Message for them to forward", script.forwardMessage);

    addButton(footer, "Save", async () => {
        const saved = await sendChange(`${API_BASE}/promos/scripts/${script.id}`, "PUT", {
            message: message.value,
            forwardMessage: forward.value
        });
        // If saving failed, the editor stays open with the changes kept.
        if (saved) loadScripts();
    }, "btn-primary");
    addButton(footer, "Cancel", () => showScriptText(body, footer, script, promo));
}

let textAreaCount = 0;
function addTextArea(parent, labelText, value) {
    const label = addElement(parent, "label", labelText, "form-label small fw-semibold");
    const area = addElement(parent, "textarea", null, "form-control mb-3 small");
    area.id = "script-text-" + ++textAreaCount;
    label.htmlFor = area.id;
    area.rows = 8;
    area.value = value;
    return area;
}

// ----- Promo List -----------------------------------------------------------------------

function renderPromoList() {
    promoRows.textContent = "";
    if (promos.length === 0) {
        const emptyText = can("EditPromos") ? 'No promos yet. Click "+ Add promo" to add one.' : "No promos yet.";
        addElement(addElement(promoRows, "tr"), "td", emptyText, "text-body-secondary").colSpan = 8;
        return;
    }
    promos.forEach((promo) => {
        const row = addElement(promoRows, "tr");
        addElement(row, "td", formatDay(promo.date), "text-nowrap");
        addElement(row, "td", promo.name, "fw-semibold");
        addElement(row, "td", promo.partnerType);
        addElement(row, "td", promo.campus);
        addElement(row, "td", promo.promoCode || "—");
        addBadge(addElement(row, "td"), promo.status, PROMO_STATUS_CLASSES);
        const contact = addElement(row, "td", promo.contactName || "—");
        if (promo.contactInfo) addElement(contact, "div", promo.contactInfo, "small text-body-secondary");
        const actions = addElement(row, "td", null, "text-nowrap text-end");
        if (can("EditPromos")) addButton(actions, "Edit", () => openPromoForm(promo));
        else addButton(actions, "Details", () => showPromoDetails(promo));
        if (can("DeletePromos")) addButton(actions, "Delete", () => deletePromo(promo), "btn-outline-danger");
    });
}

async function deletePromo(promo) {
    if (!confirm(`Permanently delete the promo with ${promo.name} on ${formatDay(promo.date)}? This can't be undone.`)) return false;
    const deleted = await sendChange(`${API_BASE}/promos/${promo.id}`, "DELETE");
    if (deleted) loadPromos();
    return deleted;
}

// View only (semesters 3-4): a promo's details in a pop-up, with nothing to change.
const promoDetails = new bootstrap.Modal(document.getElementById("promo-details"));

function showPromoDetails(promo) {
    document.getElementById("promo-details-title").textContent = promo.name;
    const body = document.getElementById("promo-details-body");
    body.textContent = "";
    addBadge(addElement(body, "p"), promo.status, PROMO_STATUS_CLASSES);
    addDetail(body, "Date", formatDay(promo.date));
    addDetail(body, "Partner type", promo.partnerType);
    addDetail(body, "Campus", promo.campus);
    addDetail(body, "Promo code", promo.promoCode);
    addDetail(body, "Contact", promo.contactName);
    addDetail(body, "Contact email or phone", promo.contactInfo);
    promoDetails.show();
}

// ----- Add / edit pop-up --------------------------------------------------------------------

const promoModal = new bootstrap.Modal(document.getElementById("promo-modal"));
const promoForm = document.getElementById("promo-form");
const promoCampus = document.getElementById("promo-campus");
const promoError = document.getElementById("promo-error");
const deleteButton = document.getElementById("promo-delete");
let editing = null; // the promo being edited, or null when adding

document.getElementById("add-promo").addEventListener("click", () => openPromoForm(null));

function openPromoForm(promo) {
    editing = promo;
    promoForm.classList.remove("was-validated");
    promoError.textContent = "";
    document.getElementById("promo-modal-title").textContent = promo ? "Edit promo" : "Add promo";
    document.getElementById("promo-name").value = promo ? promo.name : "";
    document.getElementById("promo-type").value = promo ? promo.partnerType : "Sorority";
    document.getElementById("promo-date").value = promo ? promo.date : "";
    promoCampus.value = promo ? promo.campus : "";
    document.getElementById("promo-code").value = promo ? promo.promoCode : "";
    document.getElementById("promo-status").value = promo ? promo.status : "Reaching out";
    document.getElementById("promo-contact-name").value = promo ? promo.contactName : "";
    document.getElementById("promo-contact-info").value = promo ? promo.contactInfo : "";
    deleteButton.hidden = !promo || !can("DeletePromos");
    promoModal.show();
}

deleteButton.addEventListener("click", async () => {
    if (editing && await deletePromo(editing)) promoModal.hide();
});

promoForm.addEventListener("submit", async (event) => {
    event.preventDefault();
    promoForm.classList.add("was-validated");
    if (!promoForm.checkValidity()) {
        showFormError("Please fill in the fields outlined in red.");
        return;
    }

    const body = {
        name: document.getElementById("promo-name").value,
        partnerType: document.getElementById("promo-type").value,
        campus: promoCampus.value,
        date: document.getElementById("promo-date").value,
        promoCode: document.getElementById("promo-code").value,
        status: document.getElementById("promo-status").value,
        contactName: document.getElementById("promo-contact-name").value,
        contactInfo: document.getElementById("promo-contact-info").value
    };

    // The pop-up stays open (with everything still filled in) if saving fails.
    const saveButton = document.getElementById("promo-save");
    saveButton.disabled = true;
    try {
        const response = await apiFetch(editing ? `${API_BASE}/promos/${editing.id}` : `${API_BASE}/promos`, {
            method: editing ? "PUT" : "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(body)
        });
        if (response.ok) {
            promoModal.hide();
            loadPromos();
        } else {
            const result = await response.json().catch(() => ({}));
            showFormError("The promo was NOT saved. " + formatErrors(result.errors));
        }
    } catch (error) {
        showFormError("The promo was NOT saved because the server couldn't be reached. Please try again.");
    }
    saveButton.disabled = false;
});

function showFormError(text) {
    promoError.textContent = "";
    addElement(promoError, "div", text, "alert alert-danger mb-0");
}

onPageShow("promo-calendar", loadPromoCalendar);
