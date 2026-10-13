# CLAUDE.md

Guidance for Claude when working in this repository.

## Project

A content-idea submission and review site for a company's marketing team. Staff submit
social media content ideas with links to photos/videos; reviewers approve or deny them.
It will be used for many years and will eventually be deployed and connected to the
company's existing website.

The current code is an early mockup in `mockup_1/` (see `mockup_1/README.md` for how to run it,
the API endpoints, and the MySQL migration plan).

## How to work with us

- **Ask thorough questions before writing any code** for a new feature or a change in
  direction. Don't start coding until the requirements are confirmed.
- **Only work inside this repository (`LTP-MKT`).** Don't touch other folders on the machine
  (e.g. `MIS_321/MIS-321-Project`).
- Don't delete files without asking. If something needs to be deleted, tell us and we'll do it.
- Explain new concepts plainly; we're learning. Recommend the best approach, but keep it within
  our stack where possible.

## Tech stack (keep to these)

- **C#** (ASP.NET Core Web API, .NET 10), **HTML**, **JavaScript** (plain, no frameworks),
  **JSON**, **CSV**, **MySQL**.
- Data starts in **CSV files**, and later moves to **MySQL**. Keep the CSV design table-shaped
  so it translates directly to SQL tables.
- Architecture: Controllers → Repository interface (`IIdeaRepository`) → CSV implementation
  (`CsvIdeaRepository`). Moving to MySQL = write `MySqlIdeaRepository` and change one line in
  `Program.cs`.

## Design rules

- **Zero styling.** `style.css` stays empty; no inline styles or CSS frameworks.
- Keep it a **very simple mockup**: plain HTML forms, lists, and buttons.
- The users are **not technical**: use plain-language messages, confirm before final or
  destructive actions, and never clear a form when saving fails.
- Show user-entered text with `textContent` (never `innerHTML`) to prevent script injection.

## Decisions already made

**Submitting an idea**
- One form = one idea.
- Fields: your name, title (required), description, caption (one caption idea), platform,
  hashtags, and photo/video links.
- Platform: checkboxes for **Instagram** and **TikTok** (one or both).

**Photos and videos**
- **The app's own server never keeps photo or video files.** Files live in the company's
  **SharePoint/OneDrive folder**; the app saves only the address (and OneDrive id).
- Two ways to attach a file (both allowed on one idea):
  - **Upload a file**: the API sends it to the SharePoint site folder via **Microsoft Graph**
    (app-only sign-in, chunked upload session). **500 MB per file**, 2 GB per idea. Allowed:
    .jpg .jpeg .png .heic .gif (Photo), .mp4 .mov .m4v (Video); type is set from the extension.
    No local/test storage mode: uploads only work once the OneDrive settings are filled in.
    The client secret goes in `dotnet user-secrets`, never in git.
  - **Paste a link** to a file already in OneDrive.
- If a submission fails partway, nothing is saved and files already sent are removed from OneDrive.
- **Any number of files/links per idea**, and an idea with none is allowed.
- Each one has a **type (Photo or Video)** and an **optional label**.
- Links must start with **`https://`**.
- Links open in a new tab, where OneDrive offers Download / Add to My files. Hovering shows the
  full address.
- Uploaded files **play/show on the review page** with a **Download** link.

**Reviewing**
- New ideas are **Pending**. Reviewers **Approve** or **Deny** the **whole idea** (the links
  just help explain it).
- **Denials require a reason**; approvals don't.
- **Decisions are final.** An approved or denied idea can't be changed.
- The review page shows **Waiting for review** by default, with filters for **Approved** and
  **Denied** only.
- For now **anyone can review**. Login will be added later so only staff can see ideas and
  links.

**Deleting**
- **Delete is permanent.** It removes the idea and all its links completely, whatever its
  status. Admins delete old ideas to free up space.
- **Uploaded files stay in OneDrive** when an idea is deleted (only the CSV rows are removed).
- No archive, no soft delete, no restore, no `.bak` copies of deleted data.

**Data files** (`mockup_1/api/Data/`)
- `ideas.csv`: Id, Name, Title, Description, Caption, Platforms (`Instagram;TikTok`),
  Hashtags, CreatedAt, ReviewStatus, DenialReason, ReviewedAt
- `links.csv`: Id, IdeaId, Type, Label, Url, DriveItemId (empty for pasted links)

## Constraints and context

- **Budget: free.**
- Expected volume: about **20–30 ideas per month**.
- Run the API from inside `mockup_1/api` (`dotnet run`); it listens on
  `http://localhost:5050`. The client pages are opened with Live Server.
