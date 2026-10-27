// Shared by every page. Load this before the page's own script.
const API_BASE = "http://localhost:5050/api";

// The six pages, in navbar order. They're all sections of index.html (<section data-page="...">);
// the address after # says which one is shown, e.g. index.html#bank. See "Pages" below.
// permission = what the logged-in person needs to see the page (see "Logged in" below).
const PAGES = [
    { id: "form", title: "Ideas Form", permission: "SubmitIdeas" },
    { id: "bank", title: "Ideas Bank", permission: "ReviewIdeas" },
    { id: "tracker", title: "Production Tracker", permission: "ViewTracker" },
    { id: "content-calendar", title: "Content Calendar", permission: "ViewContentCalendar" },
    { id: "promo-calendar", title: "Promo Calendar", permission: "ViewPromos" },
    { id: "directory", title: "Intern Directory", permission: "ManageDirectory" }
];

// ----- Logged in ----------------------------------------------------------------
// auth.js logs people in and out. While logged in, `me` is { employee, permissions } from the API.
// The permissions come from the semester (api/Auth/Permissions.cs):
//   SubmitIdeas, ViewContentCalendar, ViewTracker, EditTracker, DeleteTracker,
//   ViewPromos, EditPromos, DeletePromos, ReviewIdeas, ManageDirectory
// The pages use can() to hide tabs and buttons; the API checks the same permissions on every request.
let me = null;

function can(permission) {
    return me !== null && me.permissions.includes(permission);
}

// The login token, kept until the browser tab is closed (sessionStorage).
function getToken() {
    return sessionStorage.getItem("ltp-token");
}

// The campuses, in the order employee dropdowns group them. (The Ideas Form and promo pop-up list
// their campus choices in index.html.)
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
    "Scheduled": "text-bg-primary",
    "Completed": "text-bg-success",
    "Cancelled": "text-bg-secondary"
};

// ----- Navbar -----------------------------------------------------------------

// The Bootstrap breakpoint from which the navbar is one row (below it: the ☰ menu).
const NAVBAR_BREAKPOINT = "xl";

// Fills <nav id="site-nav"> with the Bootstrap navbar: the pages the logged-in person can see, and
// their name with Change password / Log out. showCurrentPage() highlights the current page.
// Before login it's just the logo.
function renderNavbar() {
    const nav = document.getElementById("site-nav");
    nav.textContent = "";
    // One row on wide screens: logo far left, page tabs, then name / Change password / Log out far
    // right. Narrower than NAVBAR_BREAKPOINT, the tabs and buttons fold into the ☰ menu instead.
    nav.className = "navbar navbar-expand-" + NAVBAR_BREAKPOINT + " site-navbar mb-4";

    const container = document.createElement("div");
    container.className = "container-fluid px-3 px-lg-4"; // full width, so the ends reach the edges

    // The Low Tide Poke Company logo, linking back to the Ideas Form.
    const brand = addElement(container, "a");
    brand.className = "navbar-brand py-0";
    brand.href = "#form";
    const logo = addElement(brand, "img");
    logo.src = "resources/images/logo.png";
    logo.alt = "Low Tide Poke Company marketing";

    if (!me) {
        nav.appendChild(container);
        return;
    }

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
    list.className = "navbar-nav me-auto flex-nowrap";
    PAGES.filter((page) => can(page.permission)).forEach((page) => {
        const item = document.createElement("li");
        item.className = "nav-item";
        const link = addElement(item, "a", page.title, "nav-link text-nowrap");
        link.href = "#" + page.id;
        link.dataset.page = page.id;
        list.appendChild(item);
    });
    collapse.appendChild(list);

    // Who's logged in, with Change password and Log out (handled in auth.js).
    const account = addElement(collapse, "div", null,
        "d-flex flex-nowrap align-items-center gap-2 py-2 py-" + NAVBAR_BREAKPOINT + "-0 ms-" + NAVBAR_BREAKPOINT + "-3");
    addElement(account, "span", me.employee.name, "fw-semibold text-nowrap");
    const changePassword = addButton(account, "Change password", openPasswordForm, "btn-outline-primary");
    changePassword.classList.replace("me-2", "text-nowrap");
    const logOutButton = addButton(account, "Log out", logOut, "btn-primary");
    logOutButton.classList.replace("me-2", "text-nowrap");

    container.appendChild(collapse);
    nav.appendChild(container);
}

// ----- Pages ---------------------------------------------------------------------------

// Each page's script registers a function that loads its data. It runs every time that page is
// shown, so e.g. an idea approved in the Ideas Bank is in the Production Tracker when you open it.
const pageLoaders = {};
function onPageShow(pageId, load) {
    pageLoaders[pageId] = load;
}

// The page named after # ("#tracker?view=Completed&item=3" -> "tracker"). No # means the Ideas Form.
function currentPageId() {
    const id = location.hash.slice(1).split("?")[0];
    return PAGES.some((page) => page.id === id) ? id : "form";
}

// The settings after the page name, e.g. pageParams().get("view") -> "Completed".
function pageParams() {
    return new URLSearchParams(location.hash.split("?")[1] || "");
}

// Shows the current page's section, hides the others, and loads its data. Before login, that's the
// login page. A page the person isn't allowed to see goes to the Ideas Form instead (everyone has it).
function showCurrentPage() {
    let id = me ? currentPageId() : "login";
    if (me && !can(PAGES.find((page) => page.id === id).permission)) {
        id = "form";
        history.replaceState(null, "", "#form");
    }
    document.querySelectorAll("section[data-page]").forEach((section) => {
        section.classList.toggle("d-none", section.dataset.page !== id);
    });
    document.querySelectorAll("#site-nav .nav-link").forEach((link) => {
        const current = link.dataset.page === id;
        link.classList.toggle("active", current);
        if (current) link.setAttribute("aria-current", "page");
        else link.removeAttribute("aria-current");
    });
    document.title = id === "login" ? "Log in" : PAGES.find((page) => page.id === id).title;

    // Close anything left open from the page before: a pop-up, or the menu on small screens.
    document.querySelectorAll(".modal.show").forEach((modal) => bootstrap.Modal.getInstance(modal).hide());
    const menu = document.getElementById("site-nav-links");
    if (menu && menu.classList.contains("show")) bootstrap.Collapse.getOrCreateInstance(menu).hide();

    window.scrollTo(0, 0);
    if (pageLoaders[id]) pageLoaders[id]();
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

// Both calendars only show items with one of these statuses. Everything else stays in the
// Production Tracker / Promo List until it's scheduled.
const CALENDAR_STATUSES = ["Scheduled", "Completed"];

// A row of status badges explaining the calendar colors.
function renderColorKey(container, classes, statuses) {
    container.textContent = "";
    addElement(container, "span", "Key:", "me-2 text-body-secondary small");
    statuses.forEach((status) => addBadge(container, status, classes).classList.add("me-1"));
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

// fetch() plus the login token. Every call to the API goes through this. If the API says the
// login is no longer valid (401, e.g. the API was restarted), it goes back to the login page.
async function apiFetch(url, options = {}) {
    const headers = { ...(options.headers || {}) };
    const token = getToken();
    if (token) headers.Authorization = "Bearer " + token;
    const response = await fetch(url, { ...options, headers });
    if (response.status === 401 && me) sessionEnded();
    return response;
}

// Gets JSON from the API. Throws an Error with a plain-language message if it fails.
async function getJson(path) {
    let response;
    try {
        response = await apiFetch(API_BASE + path);
    } catch (error) {
        throw new Error("Could not reach the API. Is it running?");
    }
    if (response.status === 403) throw new Error("You don't have access to this.");
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
        const response = await apiFetch(url, options);
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

// auth.js starts the site (checks the login, then shows the page) once every script has loaded.
window.addEventListener("hashchange", showCurrentPage);
