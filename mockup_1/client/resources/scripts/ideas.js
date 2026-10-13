const API_URL = "http://localhost:5050/api/ideas";

const container = document.getElementById("ideas");
const viewFilter = document.getElementById("view-filter");

viewFilter.addEventListener("change", loadIdeas);

async function loadIdeas() {
    container.textContent = "Loading...";
    try {
        const response = await fetch(`${API_URL}?view=${viewFilter.value}`);
        const ideas = await response.json();

        container.textContent = "";
        if (ideas.length === 0) {
            container.textContent = "No ideas here.";
            return;
        }
        ideas.forEach((idea) => container.appendChild(renderIdea(idea)));
    } catch (error) {
        container.textContent = "Could not reach the API. Is it running? (" + error.message + ")";
    }
}

// Uses textContent (not innerHTML) so text other users typed can't inject HTML/scripts.
function renderIdea(idea) {
    const section = document.createElement("div");

    addElement(section, "h2", idea.title);
    addElement(section, "p", `Submitted by ${idea.name || "Anonymous"} on ${new Date(idea.createdAt).toLocaleString()}`);
    addElement(section, "p", "Description: " + idea.description);
    addElement(section, "p", "Caption: " + idea.caption);
    addElement(section, "p", "Platforms: " + idea.platforms.join(", "));
    addElement(section, "p", "Hashtags: " + idea.hashtags);

    section.appendChild(renderLinks(idea.links));

    let status = "Review status: " + idea.reviewStatus;
    if (idea.reviewedAt) status += ` (on ${new Date(idea.reviewedAt).toLocaleString()})`;
    addElement(section, "p", status);
    if (idea.reviewStatus === "Denied") addElement(section, "p", "Reason denied: " + idea.denialReason);

    section.appendChild(renderActions(idea));
    section.appendChild(document.createElement("hr"));
    return section;
}

function renderLinks(links) {
    const wrapper = document.createElement("div");
    if (links.length === 0) {
        addElement(wrapper, "p", "No photo or video links.");
        return wrapper;
    }

    addElement(wrapper, "p", "Photos and videos:");
    const list = document.createElement("ul");
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
            const fileUrl = `${API_URL}/files/${link.id}`;
            item.appendChild(document.createTextNode(" | "));
            addLink(item, fileUrl, "Download");
            item.appendChild(document.createElement("br"));
            item.appendChild(renderPreview(link, fileUrl));
        }
        list.appendChild(item);
    });
    wrapper.appendChild(list);
    return wrapper;
}

// width is an HTML attribute (not CSS) so big photos/4K videos don't fill the whole screen.
function renderPreview(link, fileUrl) {
    if (link.type === "Video") {
        const video = document.createElement("video");
        video.src = fileUrl;
        video.controls = true;
        video.preload = "metadata"; // only loads the video when someone presses play
        video.width = 320;
        video.textContent = "This browser can't play this video here. Use Download instead.";
        return video;
    }
    const img = document.createElement("img");
    img.src = fileUrl;
    img.alt = (link.label || "Uploaded photo") + " (if it doesn't show, use Download)";
    img.width = 320;
    return img;
}

function addLink(parent, url, text) {
    const a = document.createElement("a");
    a.href = url;
    a.target = "_blank";   // opens in a new tab, where OneDrive etc. offer Download / Save
    a.rel = "noopener noreferrer";
    a.title = url;         // hovering shows the full address
    a.textContent = text;
    parent.appendChild(a);
}

function renderActions(idea) {
    const actions = document.createElement("div");

    if (idea.reviewStatus === "Pending") {
        const approve = document.createElement("p");
        addButton(approve, "Approve", () => {
            if (confirm(`Approve "${idea.title}"? This can't be changed later.`)) send(`${idea.id}/approve`, "POST");
        });
        actions.appendChild(approve);

        const deny = document.createElement("p");
        const reason = document.createElement("textarea");
        reason.rows = 2;
        reason.cols = 50;
        reason.placeholder = "Reason for denying (required to deny)";
        deny.appendChild(reason);
        deny.appendChild(document.createElement("br"));
        addButton(deny, "Deny", () => {
            if (reason.value.trim() === "") {
                alert("Please write a reason before denying.");
                return;
            }
            if (confirm(`Deny "${idea.title}"? This can't be changed later.`)) {
                send(`${idea.id}/deny`, "POST", { reason: reason.value });
            }
        });
        actions.appendChild(deny);
    }

    const del = document.createElement("p");
    addButton(del, "Delete", () => {
        if (confirm(`Permanently delete "${idea.title}" and its links? This can't be undone. ` +
                    "(Uploaded files stay in the company OneDrive folder.)")) {
            send(String(idea.id), "DELETE");
        }
    });
    actions.appendChild(del);
    return actions;
}

async function send(path, method, body) {
    const options = { method };
    if (body) {
        options.headers = { "Content-Type": "application/json" };
        options.body = JSON.stringify(body);
    }
    const response = await fetch(`${API_URL}/${path}`, options);
    if (!response.ok) {
        const result = await response.json().catch(() => ({}));
        alert(result.errors ? [].concat(...Object.values(result.errors)).join(" ") : "That didn't work. Please try again.");
    }
    loadIdeas();
}

function addElement(parent, tag, text) {
    const element = document.createElement(tag);
    element.textContent = text;
    parent.appendChild(element);
}

function addButton(parent, text, onClick) {
    const button = document.createElement("button");
    button.textContent = text;
    button.addEventListener("click", onClick);
    parent.appendChild(button);
    parent.appendChild(document.createTextNode(" "));
}

loadIdeas();
