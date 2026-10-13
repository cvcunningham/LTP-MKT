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

- **Almost zero styling.** The only CSS is the `.card` box (border, padding) in `style.css`, used
  for idea cards in the Idea Bank and Production Tracker. No other styling, inline styles or CSS
  frameworks unless we ask.
- Keep it a **very simple mockup**: plain HTML forms, lists, and buttons.
- The users are **not technical**: use plain-language messages, confirm before final or
  destructive actions, and never clear a form when saving fails.
- Show user-entered text with `textContent` (never `innerHTML`) to prevent script injection.

## Decisions already made

**Pages** (three, linked to each other at the top)
- **Submit an idea** (`index.html`) → **Idea Bank** (`bank.html`) → **Production Tracker**
  (`tracker.html`).
- Review and Idea Bank are **one page**: the Idea Bank shows "Waiting for review" cards (with
  Approve / Reject / Delete) on top and "Rejected" cards below.
- Each idea is shown as a **card** (an `<article class="card">` box).

**Submitting an idea**
- One form = one idea.
- Fields: your name (who submitted it), content name (= title, required), campus, content type
  (the format, e.g. Reel), content category (the topic, e.g. Food), concept, why it could work,
  caption (one caption idea), platform, hashtags, and photo/video links.
- Campus, content type and content category are **free text**, not dropdowns.
- Platform: checkboxes for **Instagram** and **TikTok** (one or both).

**Photos and videos**
- **The app's own server never keeps photo or video files.** Files live in the company's
  OneDrive folder; the app saves only the link.
- The form has **links only**: any number per idea (none is allowed). Each has a
  **type (Photo or Video)** and an **optional label**, and must start with **`https://`**.
- Links open in a new tab, where OneDrive offers Download / Add to My files. Hovering shows the
  full address.
- **File upload to OneDrive is built but turned off**: the upload button is hidden on the form,
  and the API code (`api/Storage/`, Microsoft Graph) is kept, unused. Whether to remove it or turn
  it back on is undecided. Don't remove it without asking. Its rules: 500 MB per file, 2 GB per
  idea; client secret in `dotnet user-secrets`, never in git; uploaded files stay in OneDrive when
  an idea is deleted.

**Reviewing** (in the Idea Bank)
- New ideas are **Pending** ("Waiting for review"). Reviewers **Approve** or **Reject** the
  **whole idea** (the links just help explain it).
- **No reason is needed** to reject.
- **Decisions are final.** An approved or rejected idea can't be changed.
- **Approve** → the idea moves to the Production Tracker and leaves the Idea Bank.
- **Reject** → it moves to "Rejected" in the Idea Bank, then is **deleted automatically
  30 days** after the decision (checked whenever the Idea Bank loads).
- For now **anyone can review**. Login will be added later so only staff can see ideas and
  links.

**Idea Bank**
- Cards show only: timestamp, content name, campus, content category, links, concept, why it
  could work, who submitted it, and the decision (not type, caption, platform or hashtags).

**Production Tracker**
- One item per approved idea (created on approve, status "Not started").
- From the idea, can't be changed here: content name, campus, content type, content category,
  caption, and the idea's links (shown as "Content files").
- Filled in and changed anytime, by anyone, with a Save button: Filmer, Editor,
  Reviewer/approver, Posting (= **who posts it**), Target post date, Status.
- Status list: Not started → Filming → Editing → Ready for review → Scheduled → Posted.
- Filter: In progress / Posted. Posted items stay until someone deletes them.

**Deleting**
- **Delete is permanent.** It removes the idea, all its links and its tracker item, whatever
  its status (from the Idea Bank or the Production Tracker).
- No archive, no soft delete, no restore, no `.bak` copies of deleted data.

**Old data**
- Ideas from before the Idea Bank/Tracker (the old Approved "Bowl Image" and the old "Denied"
  one) are intentionally **left out**: they don't show on any page.

**Data files** (`mockup_1/api/Data/`; new columns always go at the end)
- `ideas.csv`: Id, Name, Title, Concept, Caption, Platforms (`Instagram;TikTok`), Hashtags,
  CreatedAt, ReviewStatus (Pending/Approved/Rejected), DenialReason (older ideas only),
  ReviewedAt, Campus, ContentType, ContentCategory, WhyItCouldWork
- `links.csv`: Id, IdeaId, Type, Label, Url, DriveItemId (only for uploads)
- `production.csv`: Id, IdeaId, Filmer, Editor, Reviewer, Poster, TargetPostDate, Status, UpdatedAt

## Constraints and context

- **Budget: free.**
- Expected volume: about **20–30 ideas per month**.
- Run the API from inside `mockup_1/api` (`dotnet run`); it listens on
  `http://localhost:5050`. The client pages are opened with Live Server.
