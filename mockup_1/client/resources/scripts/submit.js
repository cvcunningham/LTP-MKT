const API_URL = "http://localhost:5050/api/ideas";

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
        description: data.get("description"),
        caption: data.get("caption"),
        platforms: data.getAll("platforms"),
        hashtags: data.get("hashtags"),
        links: Array.from(linksContainer.querySelectorAll(".link-row")).map((row) => ({
            type: row.querySelector(".link-type").value,
            label: row.querySelector(".link-label").value,
            url: row.querySelector(".link-url").value
        }))
    };

    submitButton.disabled = true; // prevents double submissions
    message.textContent = "Saving...";
    try {
        const response = await fetch(API_URL, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(idea)
        });
        const body = await response.json().catch(() => ({}));

        if (response.ok) {
            message.textContent = `Saved! Your idea "${body.title}" was sent for review with ${body.links.length} link(s).`;
            form.reset();
            linksContainer.textContent = "";
        } else {
            // The form is NOT reset, so the user can fix the problem and submit again.
            message.textContent = "Your idea was NOT saved. " + formatErrors(body.errors);
        }
    } catch (error) {
        message.textContent = "Your idea was NOT saved because the server couldn't be reached. " +
            "Your entries are still filled in. Please try again.";
    }
    submitButton.disabled = false;
});

// Our controller returns errors as an array; ASP.NET's built-in validation returns an object.
function formatErrors(errors) {
    if (!errors) return "Something went wrong on the server.";
    if (Array.isArray(errors)) return errors.join(" ");
    return Object.values(errors).flat().join(" ");
}
