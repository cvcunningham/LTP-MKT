// The Idea Bank: a read-only record of ideas waiting for a decision and ideas that were rejected.
const container = document.getElementById("ideas");

function renderIdea(idea) {
    const section = document.createElement("div");
    renderIdeaDetails(section, idea);

    if (idea.reviewStatus === "Rejected") {
        addElement(section, "p", `Decision: Rejected on ${formatDateTime(idea.reviewedAt)} ` +
            `(will be deleted automatically on ${formatDate(idea.deletesOn)})`);
    } else {
        const decision = addElement(section, "p", "Decision: Waiting for review ");
        const a = document.createElement("a");
        a.href = "ideas.html";
        a.textContent = "(go to Review ideas)";
        decision.appendChild(a);
    }

    section.appendChild(document.createElement("hr"));
    return section;
}

loadList(container, `${API_BASE}/ideas?view=Bank`, renderIdea, "The Idea Bank is empty.");
