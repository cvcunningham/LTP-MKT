// The Ideas Bank: ideas waiting for review (with Approve / Deny / Delete) and denied ideas.
// (The API and CSV call a denied idea "Rejected".)
const waiting = document.getElementById("waiting");
const rejected = document.getElementById("rejected");

async function loadIdeas() {
    showMessage(waiting, "Loading...");
    showMessage(rejected, "Loading...");
    try {
        const ideas = await getJson("/ideas");
        showCards(waiting, ideas.filter((i) => i.reviewStatus === "Pending"), "No ideas are waiting for review.");
        showCards(rejected, ideas.filter((i) => i.reviewStatus === "Rejected"), "No denied ideas.");
    } catch (error) {
        showMessage(waiting, error.message, "text-danger");
        rejected.textContent = "";
    }
}

function showCards(container, ideas, emptyText) {
    if (ideas.length === 0) {
        showMessage(container, emptyText);
        return;
    }
    container.textContent = "";
    ideas.forEach((idea) => container.appendChild(renderCard(idea)));
}

// One idea card. Uses textContent (via addElement) so typed text can't inject HTML/scripts.
function renderCard(idea) {
    const column = document.createElement("div");
    column.className = "col-md-6 col-xl-4";
    const denied = idea.reviewStatus === "Rejected";
    const card = addElement(column, "article", null, "card h-100" + (denied ? " border-secondary" : ""));

    const body = addElement(card, "div", null, "card-body");
    addElement(body, "h3", idea.title, "card-title h5");
    addElement(body, "p", `Submitted by ${idea.name || "Anonymous"} on ${formatDateTime(idea.createdAt)}`, "card-subtitle mb-3 text-body-secondary small");
    addDetail(body, "Campus", idea.campus);
    addDetail(body, "Content category", idea.contentCategory);
    addDetail(body, "Concept", idea.concept);
    addDetail(body, "Why it could work", idea.whyItCouldWork);
    body.appendChild(renderLinks(idea.links));

    const footer = addElement(card, "div", null, "card-footer");
    if (denied) {
        addElement(footer, "small", `Denied on ${formatDateTime(idea.reviewedAt)}. ` +
            `It will be deleted automatically on ${formatDate(idea.deletesOn)}.`, "text-body-secondary");
    } else {
        renderActions(footer, idea);
    }
    return column;
}

function renderActions(footer, idea) {
    addButton(footer, "Approve", async () => {
        if (confirm(`Approve "${idea.title}"? It will move to the Production Tracker. This can't be changed later.`)) {
            await sendChange(`${API_BASE}/ideas/${idea.id}/approve`, "POST");
            loadIdeas();
        }
    }, "btn-success");
    addButton(footer, "Deny", async () => {
        if (confirm(`Deny "${idea.title}"? This can't be changed later. ` +
                    "It will be deleted automatically in 30 days.")) {
            await sendChange(`${API_BASE}/ideas/${idea.id}/reject`, "POST");
            loadIdeas();
        }
    }, "btn-outline-secondary");
    addButton(footer, "Delete", async () => {
        if (confirm(`Permanently delete "${idea.title}" and its links? This can't be undone.`)) {
            await sendChange(`${API_BASE}/ideas/${idea.id}`, "DELETE");
            loadIdeas();
        }
    }, "btn-outline-danger");
}

loadIdeas();
