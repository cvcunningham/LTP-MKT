const form = document.getElementById("idea-form");
const linksContainer = document.getElementById("links");
const submitButton = document.getElementById("submit-button");
const message = document.getElementById("message");

document.getElementById("add-link").addEventListener("click", addLinkRow);

// Each link row: Type (Photo/Video), optional Label, URL, and a Remove button.
function addLinkRow() {
    const row = document.createElement("p");
    row.className = "link-row";
    row.innerHTML = `
        <select class="link-type">
            <option value="Photo">Photo</option>
            <option value="Video">Video</option>
        </select>
        <input type="text" class="link-label" placeholder="Label (optional)">
        <input type="url" class="link-url" placeholder="https://..." pattern="https://.*" required>
        <button type="button">Remove</button>`;
    row.querySelector("button").addEventListener("click", () => row.remove());
    linksContainer.appendChild(row);
}

form.addEventListener("submit", async (event) => {
    event.preventDefault();

    const data = new FormData(form);
    const idea = {
        name: data.get("name"),
        title: data.get("title"),
        campus: data.get("campus"),
        contentType: data.get("contentType"),
        contentCategory: data.get("contentCategory"),
        concept: data.get("concept"),
        whyItCouldWork: data.get("whyItCouldWork"),
        caption: data.get("caption"),
        platforms: data.getAll("platforms"),
        hashtags: data.get("hashtags"),
        links: Array.from(linksContainer.querySelectorAll(".link-row")).map((row) => ({
            type: row.querySelector(".link-type").value,
            label: row.querySelector(".link-label").value,
            url: row.querySelector(".link-url").value
        }))
    };

    // The API takes a multipart form (so it can also accept uploaded files later):
    // the idea goes as JSON in the "idea" part.
    const body = new FormData();
    body.append("idea", JSON.stringify(idea));

    submitButton.disabled = true; // prevents double submissions
    message.textContent = "Saving...";
    try {
        const response = await fetch(`${API_BASE}/ideas`, { method: "POST", body });
        const result = await response.json().catch(() => ({}));

        if (response.ok) {
            message.textContent = `Saved! Your idea "${result.title}" was sent for review with ${result.links.length} link(s).`;
            form.reset();
            linksContainer.textContent = "";
        } else {
            // The form is NOT reset, so the user can fix the problem and submit again.
            message.textContent = "Your idea was NOT saved. " + formatErrors(result.errors);
        }
    } catch (error) {
        message.textContent = "Your idea was NOT saved because the server couldn't be reached. " +
            "Your entries are still filled in. Please try again.";
    }
    submitButton.disabled = false;
});
