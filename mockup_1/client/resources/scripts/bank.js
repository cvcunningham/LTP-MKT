// The Idea Bank: ideas waiting for review (with Approve / Reject / Delete) and rejected ideas.
const waiting = document.getElementById("waiting");
const rejected = document.getElementById("rejected");

async function loadIdeas() {
    waiting.textContent = "Loading...";
    rejected.textContent = "Loading...";
    try {
        const ideas = await (await fetch(`${API_BASE}/ideas`)).json();
        showCards(waiting, ideas.filter((i) => i.reviewStatus === "Pending"), "No ideas are waiting for review.");
        showCards(rejected, ideas.filter((i) => i.reviewStatus === "Rejected"), "No rejected ideas.");
    } catch (error) {
        const text = "Could not reach the API. Is it running? (" + error.message + ")";
        waiting.textContent = text;
        rejected.textContent = "";
    }
}

function showCards(container, ideas, emptyText) {
    container.textContent = ideas.length === 0 ? emptyText : "";
    ideas.forEach((idea) => container.appendChild(renderCard(idea)));
}

// One idea card. Uses textContent (via addElement) so typed text can't inject HTML/scripts.
function renderCard(idea) {
    const card = document.createElement("article");
    card.className = "card";

    addElement(card, "h3", idea.title);
    addElement(card, "p", `Submitted by ${idea.name || "Anonymous"} on ${formatDateTime(idea.createdAt)}`);
    addElement(card, "p", "Campus: " + idea.campus);
    addElement(card, "p", "Content category: " + idea.contentCategory);
    addElement(card, "p", "Concept: " + idea.concept);
    addElement(card, "p", "Why it could work: " + idea.whyItCouldWork);
    card.appendChild(renderLinks(idea.links));

    if (idea.reviewStatus === "Rejected") {
        addElement(card, "p", `Decision: Rejected on ${formatDateTime(idea.reviewedAt)} ` +
            `(will be deleted automatically on ${formatDate(idea.deletesOn)})`);
    } else {
        addElement(card, "p", "Decision: Waiting for review");
        card.appendChild(renderActions(idea));
    }
    return card;
}

function renderActions(idea) {
    const actions = document.createElement("p");
    addButton(actions, "Approve", async () => {
        if (confirm(`Approve "${idea.title}"? It will move to the Production Tracker. This can't be changed later.`)) {
            await sendChange(`${API_BASE}/ideas/${idea.id}/approve`, "POST");
            loadIdeas();
        }
    });
    addButton(actions, "Reject", async () => {
        if (confirm(`Reject "${idea.title}"? This can't be changed later. ` +
                    "It will be deleted automatically in 30 days.")) {
            await sendChange(`${API_BASE}/ideas/${idea.id}/reject`, "POST");
            loadIdeas();
        }
    });
    addButton(actions, "Delete", async () => {
        if (confirm(`Permanently delete "${idea.title}" and its links? This can't be undone.`)) {
            await sendChange(`${API_BASE}/ideas/${idea.id}`, "DELETE");
            loadIdeas();
        }
    });
    return actions;
}

loadIdeas();
