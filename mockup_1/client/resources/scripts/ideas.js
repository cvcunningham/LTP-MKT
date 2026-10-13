// The Review page: ideas waiting for a decision, with Approve / Reject / Delete.
const container = document.getElementById("ideas");

function loadIdeas() {
    loadList(container, `${API_BASE}/ideas?view=Review`, renderIdea, "No ideas are waiting for review.");
}

function renderIdea(idea) {
    const section = document.createElement("div");
    renderIdeaDetails(section, idea);

    const actions = document.createElement("p");
    addButton(actions, "Approve", async () => {
        if (confirm(`Approve "${idea.title}"? It will move to the Production Tracker. This can't be changed later.`)) {
            await sendChange(`${API_BASE}/ideas/${idea.id}/approve`, "POST");
            loadIdeas();
        }
    });
    addButton(actions, "Reject", async () => {
        if (confirm(`Reject "${idea.title}"? This can't be changed later. ` +
                    "It stays in the Idea Bank for 30 days, then it is deleted automatically.")) {
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
    section.appendChild(actions);
    section.appendChild(document.createElement("hr"));
    return section;
}

loadIdeas();
