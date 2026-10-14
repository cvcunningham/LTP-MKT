// Shared by every page. Load this before the page's own script.
const API_BASE = "http://localhost:5050/api";

// The five pages, in navbar order. Each page's <body data-page="..."> says which one it is.
const PAGES = [
    { id: "form", file: "index.html", title: "Ideas Form" },
    { id: "bank", file: "bank.html", title: "Ideas Bank" },
    { id: "tracker", file: "tracker.html", title: "Production Tracker" },
    { id: "content-calendar", file: "content-calendar.html", title: "Content Calendar" },
    { id: "promo-calendar", file: "promo-calendar.html", title: "Promo Calendar" }
];

// Campus choices. "Other" lets people type a campus that isn't listed yet.
const CAMPUSES = ["Alabama", "Ole Miss"];

// Bootstrap color classes for each status (used for badges, calendar items and the color keys).
const PRODUCTION_STATUS_CLASSES = {
    "Needs filming": "text-bg-danger",
    "Needs editing": "text-bg-warning",
    "Needs final review": "text-bg-info",
    "Scheduled": "text-bg-primary",
    "Completed": "text-bg-success"
};
const PROMO_STATUS_CLASSES = {
    "Reaching out": "text-bg-warning",
    "Confirmed": "text-bg-primary",
    "Completed": "text-bg-success",
    "Cancelled": "text-bg-secondary"
};

// ----- Navbar -----------------------------------------------------------------

// Fills <nav id="site-nav"> with the Bootstrap navbar, highlighting the current page.
function renderNavbar() {
    const nav = document.getElementById("site-nav");
    const current = document.body.dataset.page;
    nav.className = "navbar navbar-expand-lg site-navbar mb-4";

    const container = document.createElement("div");
    container.className = "container";

    // The Low Tide Poke Company logo, linking back to the Ideas Form.
    const brand = addElement(container, "a");
    brand.className = "navbar-brand py-0";
    brand.href = "index.html";
    const logo = addElement(brand, "img");
    logo.src = "resources/images/logo.png";
    logo.alt = "Low Tide Poke Company marketing";

    // The ☰ button that shows the links on small screens.
    const toggler = document.createElement("button");
    toggler.className = "navbar-toggler";
    toggler.type = "button";
    toggler.setAttribute("data-bs-toggle", "collapse");
    toggler.setAttribute("data-bs-target", "#site-nav-links");
    toggler.setAttribute("aria-label", "Show the menu");
    toggler.appendChild(document.createElement("span")).className = "navbar-toggler-icon";
    container.appendChild(toggler);

    const collapse = document.createElement("div");
    collapse.className = "collapse navbar-collapse";
    collapse.id = "site-nav-links";
    const list = document.createElement("ul");
    list.className = "navbar-nav";
    PAGES.forEach((page) => {
        const item = document.createElement("li");
        item.className = "nav-item";
        const link = addElement(item, "a", page.title);
        link.className = "nav-link" + (page.id === current ? " active" : "");
        link.href = page.file;
        if (page.id === current) link.setAttribute("aria-current", "page");
        list.appendChild(item);
    });
    collapse.appendChild(list);
    container.appendChild(collapse);
    nav.appendChild(container);
}

// ----- Building page elements ----------------------------------------------------

// Uses textContent (not innerHTML) so text other users typed can't inject HTML/scripts.
function addElement(parent, tag, text, className) {
    const element = document.createElement(tag);
    if (text !== undefined && text !== null) element.textContent = text;
    if (className) element.className = className;
    parent.appendChild(element);
    return element;
}

// A Bootstrap button. style is e.g. "btn-success" or "btn-outline-danger".
function addButton(parent, text, onClick, style = "btn-outline-secondary") {
    const button = document.createElement("button");
    button.type = "button";
    button.className = "btn btn-sm me-2 " + style;
    button.textContent = text;
    button.addEventListener("click", onClick);
    parent.appendChild(button);
    return button;
}

function addLink(parent, url, text) {
    const a = document.createElement("a");
    a.href = url;
    a.target = "_blank";   // opens in a new tab, where OneDrive etc. offer Download / Save
    a.rel = "noopener noreferrer";
    a.title = url;         // hovering shows the full address
    a.textContent = text;
    parent.appendChild(a);
    return a;
}

// A colored status badge, e.g. "Needs editing" in yellow.
function addBadge(parent, text, classes) {
    return addElement(parent, "span", text, "badge " + (classes[text] || "text-bg-secondary"));
}

// A row of status badges explaining the calendar colors.
function renderColorKey(container, classes) {
    container.textContent = "";
    addElement(container, "span", "Key:", "me-2 text-body-secondary small");
    Object.keys(classes).forEach((status) => addBadge(container, status, classes).classList.add("me-1"));
}

// "Label: value" on its own line, with the label in bold.
function addDetail(parent, label, value) {
    const p = addElement(parent, "p", null, "mb-1");
    addElement(p, "strong", label + ": ");
    p.appendChild(document.createTextNode(value || "—"));
    return p;
}

// ----- Dates ------------------------------------------------------------------------

function formatDateTime(value) {
    return new Date(value).toLocaleString();
}

function formatDate(value) {
    return new Date(value).toLocaleDateString();
}

// A date-only value from the API ("2026-10-15"). Read as a local date: new Date("2026-10-15")
// would be midnight UTC, which shows as the day before in US time zones.
function parseDay(value) {
    const [year, month, day] = value.split("-").map(Number);
    return new Date(year, month - 1, day);
}

function formatDay(value) {
    return value ? parseDay(value).toLocaleDateString(undefined, { weekday: "short", month: "short", day: "numeric", year: "numeric" }) : "No date";
}

// A Date as "2026-10-15" (local), the format the API uses.
function toDayString(date) {
    return date.getFullYear() + "-" + String(date.getMonth() + 1).padStart(2, "0") + "-" + String(date.getDate()).padStart(2, "0");
}

// ----- Dropdowns ----------------------------------------------------------------------

// A <select> of employees, grouped by campus (Alabama, Ole Miss, then any others).
// The first choice is emptyText with value "" (e.g. "Not assigned").
function createEmployeeSelect(employees, selectedId, emptyText) {
    const select = document.createElement("select");
    select.className = "form-select";
    addElement(select, "option", emptyText).value = "";

    const campusOrder = (campus) => {
        const index = CAMPUSES.indexOf(campus);
        return index >= 0 ? String(index) : "9" + campus; // listed campuses first, then A-Z
    };
    const campuses = [...new Set(employees.map((e) => e.campus))].sort((a, b) => campusOrder(a).localeCompare(campusOrder(b)));
    campuses.forEach((campus) => {
        const group = document.createElement("optgroup");
        group.label = campus || "No campus";
        employees.filter((e) => e.campus === campus).forEach((e) => {
            addElement(group, "option", e.name).value = e.id;
        });
        select.appendChild(group);
    });
    select.value = selectedId == null ? "" : String(selectedId);
    return select;
}

// The selected employee id as a number, or null for "".
function selectedId(select) {
    return select.value === "" ? null : Number(select.value);
}

// The campus control: a dropdown (Alabama, Ole Miss, Other) plus a text box shown for Other.
// Adds it to `parent` under a label, and returns { select, other, value() }.
function addCampusField(parent, id, value = "", required = false) {
    const wrapper = addElement(parent, "div", null, "mb-3");
    const label = addElement(wrapper, "label", required ? "Campus (required)" : "Campus", "form-label");
    label.htmlFor = id;

    const select = addElement(wrapper, "select", null, "form-select");
    select.id = id;
    select.required = required;
    addElement(select, "option", "Choose a campus").value = "";
    CAMPUSES.forEach((campus) => (addElement(select, "option", campus).value = campus));
    addElement(select, "option", "Other").value = "Other";

    const other = addElement(wrapper, "input", null, "form-control mt-2");
    other.type = "text";
    other.placeholder = "Type the campus";
    other.setAttribute("aria-label", "Other campus");

    const field = {
        select,
        other,
        value: () => (select.value === "Other" ? other.value.trim() : select.value),
        set(campus) {
            const listed = campus === "" || CAMPUSES.includes(campus);
            select.value = listed ? campus : "Other";
            other.value = listed ? "" : campus;
            other.hidden = select.value !== "Other";
            other.required = required && select.value === "Other";
        }
    };
    select.addEventListener("change", () => {
        other.hidden = select.value !== "Other";
        other.required = required && select.value === "Other";
        if (!other.hidden) other.focus();
    });
    field.set(value);
    return field;
}

// ----- Links on ideas -----------------------------------------------------------------

// The photo/video links of an idea, as a list.
function renderLinks(links, heading = "Links") {
    const wrapper = document.createElement("div");
    if (links.length === 0) {
        addDetail(wrapper, heading, "none");
        return wrapper;
    }

    addElement(addElement(wrapper, "p", null, "mb-1"), "strong", heading + ":");
    const list = addElement(wrapper, "ul", null, "mb-2");
    links.forEach((link) => {
        const item = document.createElement("li");
        item.appendChild(document.createTextNode(link.type + ": "));

        // Only https links become clickable (the API enforces this too).
        if (link.url.startsWith("https://")) {
            addLink(item, link.url, link.label || "Open " + link.type.toLowerCase());
        } else {
            item.appendChild(document.createTextNode(link.url));
        }

        // Files uploaded through the app can be played/shown right here, and downloaded.
        if (link.isUpload) {
            const fileUrl = `${API_BASE}/ideas/files/${link.id}`;
            item.appendChild(document.createTextNode(" | "));
            addLink(item, fileUrl, "Download");
            item.appendChild(document.createElement("br"));
            item.appendChild(renderPreview(link, fileUrl));
        }
        list.appendChild(item);
    });
    return wrapper;
}

// width is an HTML attribute so big photos/4K videos don't fill the whole screen.
function renderPreview(link, fileUrl) {
    if (link.type === "Video") {
        const video = document.createElement("video");
        video.src = fileUrl;
        video.controls = true;
        video.preload = "metadata"; // only loads the video when someone presses play
        video.width = 320;
        video.className = "img-fluid";
        video.textContent = "This browser can't play this video here. Use Download instead.";
        return video;
    }
    const img = document.createElement("img");
    img.src = fileUrl;
    img.alt = (link.label || "Uploaded photo") + " (if it doesn't show, use Download)";
    img.width = 320;
    img.className = "img-fluid";
    return img;
}

// ----- Talking to the API ---------------------------------------------------------------

// Gets JSON from the API. Throws an Error with a plain-language message if it fails.
async function getJson(path) {
    let response;
    try {
        response = await fetch(API_BASE + path);
    } catch (error) {
        throw new Error("Could not reach the API. Is it running?");
    }
    if (!response.ok) throw new Error("The server had a problem loading this page. Please refresh.");
    return response.json();
}

// Shows a message in place of a list (e.g. "Loading..." or an error).
function showMessage(container, text, style = "text-body-secondary") {
    container.textContent = "";
    addElement(container, "p", text, style);
}

// Sends a change to the API. Returns true if it worked; otherwise tells the user why.
async function sendChange(url, method, body) {
    const options = { method };
    if (body) {
        options.headers = { "Content-Type": "application/json" };
        options.body = JSON.stringify(body);
    }
    try {
        const response = await fetch(url, options);
        if (response.ok) return true;
        const result = await response.json().catch(() => ({}));
        alert("That didn't work. " + formatErrors(result.errors));
    } catch (error) {
        alert("That didn't work because the server couldn't be reached. Please try again.");
    }
    return false;
}

// Our controllers return errors as an array; ASP.NET's built-in validation returns an object.
function formatErrors(errors) {
    if (!errors) return "Something went wrong on the server.";
    if (Array.isArray(errors)) return errors.join(" ");
    return Object.values(errors).flat().join(" ");
}

renderNavbar();
