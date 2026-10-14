// The Content Calendar: every Production Tracker item that has a target post date, colored by status.
// It reads the same data as the tracker (GET /api/production?view=All), so the two always match.
const details = new bootstrap.Modal(document.getElementById("details"));
const calendar = createMonthCalendar(document.getElementById("calendar"), showDetails);

renderColorKey(document.getElementById("color-key"), PRODUCTION_STATUS_CLASSES);

async function load() {
    const message = document.getElementById("load-message");
    showMessage(message, "Loading...");
    try {
        const items = await getJson("/production?view=All");
        calendar.setItems(items
            .filter((item) => item.targetPostDate)
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
    document.getElementById("details-open").href = `tracker.html?view=${view}#item-${item.id}`;
    details.show();
}

load();
