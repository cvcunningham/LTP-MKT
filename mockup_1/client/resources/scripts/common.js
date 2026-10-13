// Shared by every page. Load this before the page's own script.
const API_BASE = "http://localhost:5050/api";

// Uses textContent (not innerHTML) so text other users typed can't inject HTML/scripts.
function addElement(parent, tag, text) {
    const element = document.createElement(tag);
    element.textContent = text;
    parent.appendChild(element);
    return element;
}

function addButton(parent, text, onClick) {
    const button = document.createElement("button");
    button.type = "button";
    button.textContent = text;
    button.addEventListener("click", onClick);
    parent.appendChild(button);
    parent.appendChild(document.createTextNode(" "));
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
}

function formatDateTime(value) {
    return new Date(value).toLocaleString();
}

function formatDate(value) {
    return new Date(value).toLocaleDateString();
}

// The photo/video links of an idea, as a list.
function renderLinks(links, heading = "Links") {
    const wrapper = document.createElement("div");
    if (links.length === 0) {
        addElement(wrapper, "p", heading + ": none");
        return wrapper;
    }

    addElement(wrapper, "p", heading + ":");
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
            const fileUrl = `${API_BASE}/ideas/files/${link.id}`;
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

// Everything the person submitted about an idea. Used on the Review and Idea Bank pages.
function renderIdeaDetails(parent, idea) {
    addElement(parent, "h2", idea.title);
    addElement(parent, "p", `Submitted by ${idea.name || "Anonymous"} on ${formatDateTime(idea.createdAt)}`);
    addElement(parent, "p", "Campus: " + idea.campus);
    addElement(parent, "p", "Content type: " + idea.contentType);
    addElement(parent, "p", "Content category: " + idea.contentCategory);
    addElement(parent, "p", "Concept: " + idea.concept);
    addElement(parent, "p", "Why it could work: " + idea.whyItCouldWork);
    addElement(parent, "p", "Caption: " + idea.caption);
    addElement(parent, "p", "Platforms: " + idea.platforms.join(", "));
    addElement(parent, "p", "Hashtags: " + idea.hashtags);
    parent.appendChild(renderLinks(idea.links));
}

// Loads a list from the API and shows each entry, or a plain message if something's wrong.
async function loadList(container, url, renderOne, emptyText) {
    container.textContent = "Loading...";
    try {
        const response = await fetch(url);
        const items = await response.json();

        container.textContent = "";
        if (items.length === 0) {
            container.textContent = emptyText;
            return;
        }
        items.forEach((item) => container.appendChild(renderOne(item)));
    } catch (error) {
        container.textContent = "Could not reach the API. Is it running? (" + error.message + ")";
    }
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
