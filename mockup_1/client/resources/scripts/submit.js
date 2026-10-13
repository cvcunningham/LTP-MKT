const API_URL = "http://localhost:5050/api/ideas";

const MAX_FILE_BYTES = 500 * 1024 * 1024; // 500 MB (the API checks this too)
const ALLOWED_EXTENSIONS = [".jpg", ".jpeg", ".png", ".heic", ".gif", ".mp4", ".mov", ".m4v"];

const form = document.getElementById("idea-form");
const uploadsContainer = document.getElementById("uploads");
const linksContainer = document.getElementById("links");
const submitButton = document.getElementById("submit-button");
const message = document.getElementById("message");

document.getElementById("add-upload").addEventListener("click", addUploadRow);
document.getElementById("add-link").addEventListener("click", addLinkRow);

// Each upload row: a file picker, optional Label, and a Remove button.
// Photo or Video is worked out from the file type.
function addUploadRow() {
    const row = document.createElement("p");
    row.className = "upload-row";
    row.innerHTML = `
        <input type="file" class="upload-file" accept="${ALLOWED_EXTENSIONS.join(",")}" required>
        <input type="text" class="upload-label" placeholder="Label (optional)">
        <button type="button">Remove</button>`;
    row.querySelector("button").addEventListener("click", () => row.remove());
    uploadsContainer.appendChild(row);
}

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

form.addEventListener("submit", (event) => {
    event.preventDefault();

    const data = new FormData(form);
    const uploadRows = Array.from(uploadsContainer.querySelectorAll(".upload-row"));
    const files = uploadRows.map((row) => row.querySelector(".upload-file").files[0]);

    const problems = checkFiles(files);
    if (problems.length > 0) {
        message.textContent = "Your idea was NOT saved. " + problems.join(" ");
        return;
    }

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
        })),
        uploads: uploadRows.map((row) => ({ label: row.querySelector(".upload-label").value }))
    };

    // The idea goes as JSON in the "idea" part; files go in "files" in the same order as idea.uploads.
    const body = new FormData();
    body.append("idea", JSON.stringify(idea));
    files.forEach((file) => body.append("files", file));

    submitButton.disabled = true; // prevents double submissions
    message.textContent = "Saving...";
    send(body, files.length > 0);
});

// Checks size and type before sending, so people don't wait for a big upload just to get an error.
function checkFiles(files) {
    const problems = [];
    files.forEach((file, i) => {
        const extension = file.name.slice(file.name.lastIndexOf(".")).toLowerCase();
        if (!ALLOWED_EXTENSIONS.includes(extension)) {
            problems.push(`File ${i + 1} ("${file.name}"): only photos and videos can be uploaded (${ALLOWED_EXTENSIONS.join(" ")}).`);
        }
        if (file.size === 0) problems.push(`File ${i + 1} ("${file.name}") is empty.`);
        if (file.size > MAX_FILE_BYTES) problems.push(`File ${i + 1} ("${file.name}") is bigger than 500 MB.`);
    });
    return problems;
}

// Uses XMLHttpRequest instead of fetch because it can report upload progress.
function send(body, hasFiles) {
    const request = new XMLHttpRequest();
    request.open("POST", API_URL);

    request.upload.addEventListener("progress", (event) => {
        if (!hasFiles || !event.lengthComputable) return;
        const percent = Math.round((event.loaded / event.total) * 100);
        message.textContent = percent < 100
            ? `Uploading... ${percent}%. Please keep this page open.`
            : "Sending your files to OneDrive... Please keep this page open.";
    });

    request.addEventListener("load", () => {
        let result = {};
        try { result = JSON.parse(request.responseText); } catch { }

        if (request.status >= 200 && request.status < 300) {
            message.textContent = `Saved! Your idea "${result.title}" was sent for review with ${result.links.length} photo/video(s).`;
            form.reset();
            uploadsContainer.textContent = "";
            linksContainer.textContent = "";
        } else if (request.status === 413) {
            message.textContent = "Your idea was NOT saved because the files are too big all together " +
                "(the limit is 2 GB per idea). Your entries are still filled in.";
        } else {
            // The form is NOT reset, so the user can fix the problem and submit again.
            message.textContent = "Your idea was NOT saved. " + formatErrors(result.errors);
        }
        submitButton.disabled = false;
    });

    request.addEventListener("error", () => {
        message.textContent = "Your idea was NOT saved because the server couldn't be reached. " +
            "Your entries are still filled in. Please try again.";
        submitButton.disabled = false;
    });

    request.send(body);
}

// Our controller returns errors as an array; ASP.NET's built-in validation returns an object.
function formatErrors(errors) {
    if (!errors) return "Something went wrong on the server.";
    if (Array.isArray(errors)) return errors.join(" ");
    return Object.values(errors).flat().join(" ");
}
