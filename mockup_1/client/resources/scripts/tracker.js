// The Production Tracker: approved ideas and their production details.
const container = document.getElementById("items");
const viewFilter = document.getElementById("view-filter");

let statuses = []; // the Status choices, loaded from the API

viewFilter.addEventListener("change", loadItems);

async function start() {
    try {
        statuses = await (await fetch(`${API_BASE}/production/statuses`)).json();
    } catch (error) {
        container.textContent = "Could not reach the API. Is it running? (" + error.message + ")";
        return;
    }
    loadItems();
}

function loadItems() {
    const emptyText = viewFilter.value === "Posted" ? "Nothing has been posted yet." : "Nothing in production right now.";
    loadList(container, `${API_BASE}/production?view=${viewFilter.value}`, renderItem, emptyText);
}

function renderItem(item) {
    const idea = item.idea;
    const section = document.createElement("article");
    section.className = "card";

    // From the approved idea (can't be changed here).
    addElement(section, "h3", idea.title);
    addElement(section, "p", "Campus: " + idea.campus);
    addElement(section, "p", "Content type: " + idea.contentType);
    addElement(section, "p", "Content category: " + idea.contentCategory);
    addElement(section, "p", "Caption: " + idea.caption);
    section.appendChild(renderLinks(idea.links, "Content files"));

    // Production details (anyone can change these).
    const filmer = addTextField(section, "Filmer", item.filmer);
    const editor = addTextField(section, "Editor", item.editor);
    const reviewer = addTextField(section, "Reviewer/approver", item.reviewer);
    const poster = addTextField(section, "Posting (who posts it)", item.poster);

    const date = addField(section, "Target post date", document.createElement("input"));
    date.type = "date";
    date.value = item.targetPostDate || "";

    const status = addField(section, "Status", document.createElement("select"));
    statuses.forEach((choice) => {
        const option = addElement(status, "option", choice);
        option.value = choice;
    });
    status.value = item.status;

    addElement(section, "p", "Last saved: " + formatDateTime(item.updatedAt));

    const actions = document.createElement("p");
    addButton(actions, "Save", async () => {
        const saved = await sendChange(`${API_BASE}/production/${item.id}`, "PUT", {
            filmer: filmer.value,
            editor: editor.value,
            reviewer: reviewer.value,
            poster: poster.value,
            targetPostDate: date.value || null,
            status: status.value
        });
        // If saving failed, the page is NOT reloaded, so the typed changes are kept.
        if (saved) {
            if (status.value === "Posted" && viewFilter.value !== "Posted") {
                alert(`Saved. "${idea.title}" is marked Posted, so it now shows under "Posted".`);
            }
            loadItems();
        }
    });
    addButton(actions, "Delete", async () => {
        if (confirm(`Permanently delete "${idea.title}" from the tracker, along with its idea and links? This can't be undone.`)) {
            if (await sendChange(`${API_BASE}/production/${item.id}`, "DELETE")) loadItems();
        }
    });
    section.appendChild(actions);
    return section;
}

function addTextField(parent, labelText, value) {
    const input = document.createElement("input");
    input.type = "text";
    input.value = value;
    return addField(parent, labelText, input);
}

// A label with the input inside it, on its own line.
function addField(parent, labelText, input) {
    const p = document.createElement("p");
    const label = addElement(p, "label", labelText + " ");
    label.appendChild(input);
    parent.appendChild(p);
    return input;
}

start();
