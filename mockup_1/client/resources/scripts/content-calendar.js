// The Content Calendar: the Production Tracker items that are Scheduled or Completed (CALENDAR_STATUSES)
// and have a target post date, colored by status. It reads the same data as the tracker
// (GET /api/production?view=All), so the two always match.
const details = new bootstrap.Modal(document.getElementById("details"));
const contentCalendar = createMonthCalendar(document.getElementById("content-calendar-grid"), showDetails);

renderColorKey(document.getElementById("content-color-key"), PRODUCTION_STATUS_CLASSES, CALENDAR_STATUSES);

async function loadContentCalendar() {
    const message = document.getElementById("content-load-message");
    showMessage(message, "Loading...");
    try {
        const items = await getJson("/production?view=All");
        contentCalendar.setItems(items
            .filter((item) => item.targetPostDate && CALENDAR_STATUSES.includes(item.status))
            .map((item) => ({
                date: item.targetPostDate,
                label: item.idea.title,
                className: PRODUCTION_STATUS_CLASSES[item.status] || "text-bg-secondary",
                data: item
            })));
        message.textContent = "";
    } catch (error) {
        showMessage(message, error.message, "text-danger");
    }
}

// The view-only details pop-up for one item.
function showDetails(item) {
    const idea = item.idea;
    document.getElementById("details-title").textContent = idea.title;

    const body = document.getElementById("details-body");
    body.textContent = "";
    addBadge(addElement(body, "p"), item.status, PRODUCTION_STATUS_CLASSES);
    addDetail(body, "Target post date", formatDay(item.targetPostDate));
    addDetail(body, "Campus", idea.campus);
    addDetail(body, "Filmer", item.filmer);
    addDetail(body, "Editor", item.editor);
    addDetail(body, "Reviewer/approver", item.reviewer);
    addDetail(body, "Poster", item.poster);
    addDetail(body, "Caption", idea.caption);

    // Completed items are under the tracker's "Completed" filter.
    const view = item.status === "Completed" ? "Completed" : "InProgress";
    const open = document.getElementById("details-open");
    open.href = `#tracker?view=${view}&item=${item.id}`;
    open.classList.toggle("d-none", !can("ViewTracker")); // e.g. Level 1 can't see the tracker
    details.show();
}

onPageShow("content-calendar", loadContentCalendar);
