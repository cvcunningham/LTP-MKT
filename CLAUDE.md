# CLAUDE.md

Guidance for Claude when working in this repository.

## Project

A site that tracks the **progress and status of marketing content** for a business (Low Tide).
Employees submit social media content ideas; the Marketing Chair/Executive approves or denies them;
approved ideas are tracked through production to posting; and the team plans promos with partner
organizations. It will be used for many years and will eventually be deployed and connected to the
company's existing website.

The code is in `mockup_1/` (see `mockup_1/README.md` for how to run it, the API endpoints, and the
MySQL migration plan). Keep building there.

## How to work with us

- **Ask thorough questions before writing any code** for a new feature or a change in
  direction. Don't start coding until the requirements are confirmed.
- **Only work inside this repository (`LTP-MKT`).** Don't touch other folders on the machine
  (e.g. `MIS_321/MIS-321-Project`).
- Don't delete files without asking. If something needs to be deleted, tell us and we'll do it.
- Explain new concepts plainly; we're learning. Recommend the best approach, but keep it within
  our stack where possible.

## Tech stack (keep to these)

- **Front end:** plain **HTML**, **CSS** and **JavaScript** (vanilla, no frameworks or build step),
  styled with **Bootstrap 5 from a CDN** (jsDelivr, pinned version, e.g. `bootstrap@5.3.3`).
- **Back end:** **C#** ASP.NET Core Web API (.NET 10), **JSON** between the pages and the API.
- **Data:** **comma-delimited CSV files** in `mockup_1/api/Data/` for now; later a **MySQL**
  database. Keep every CSV table-shaped (one row per record, an `Id` column, other tables referred
  to by Id) so each file translates directly to a SQL table.
- Architecture: Controllers → Repository interface (e.g. `IIdeaRepository`) → CSV implementation
  (e.g. `CsvIdeaRepository`). Moving to MySQL = write the MySQL implementation and change one line
  per repository in `Program.cs`.

## Design rules

- **Use Bootstrap for styling; prefer Bootstrap classes over custom CSS** whenever Bootstrap can do
  it (layout, cards, tables, forms, buttons, badges, modals, nav). Only add custom CSS in
  `style.css` for what Bootstrap can't do (e.g. calendar grid cells). No inline styles.
- **Brand look: match the Low Tide Poke Company logo** (`mockup_1/client/resources/images/logo.png`:
  chunky purple "LOW TIDE" and coral "POKE COMPANY" on teal). Colors the owner gave:
  ```css
  background-color: #3EDCC9; /* logo background */
  color: #565397;            /* LOW TIDE text */
  color: #FFA07A;            /* POKE COMPANY text */
  ```
  The site uses the colors **sampled from the logo image** so the logo blends into the header:
  teal **#1CCC9E**, purple **#344789**, coral **#F38D65**. They're set once at the top of
  `style.css` (`--ltp-teal`, `--ltp-purple`, `--ltp-coral`, plus `--ltp-purple-dark` and
  `--ltp-teal-light`), and Bootstrap's own variables point at them: **purple is Bootstrap's
  `primary`** (buttons, links, focus rings, ticked checkboxes, headings) and **teal is `info`**.
  So keep using normal Bootstrap classes (`btn-primary`, `text-bg-info`, ...) and don't hard-code hex
  colors anywhere else.
  - Header: teal navbar with the logo image as the brand, dark purple links, the current page as a
    purple pill, and a coral stripe underneath.
  - Page background is a very light teal; titles are purple with a short coral bar under them;
    table headers (calendars, Promo List) are purple with white text.
  - Font: **Fredoka** (Google Fonts, rounded like "POKE COMPANY") for headings, the navbar and
    buttons; normal Bootstrap font for body text. Each page loads it in its `<head>`.
  - Other colors may be used where they read better (e.g. Bootstrap's red/yellow/green statuses).
  - **Contrast:** white text only on purple. On teal or coral use dark purple text, and don't use
    teal or coral for small text on white.
- Every page shares one **Bootstrap navbar** linking the five pages (built by `renderNavbar()` in
  `common.js`; each page's `<body data-page="...">` marks the current one).
- Both calendars use the shared month grid in `calendar.js` (no calendar library).
- The users are **not technical**: use plain-language messages, confirm before final or
  destructive actions, and never clear a form when saving fails.
- Show user-entered text with `textContent` (never `innerHTML`) to prevent script injection.

## The five pages

```
Ideas Form ──► Ideas Bank ──► Approve ──► Production Tracker ◄──► Content Calendar
                         └──► Deny ──► Rejected (auto-deleted after 30 days)
Promo Calendar (separate): outreach scripts + Promo List table + promo calendar
```

### 1. Ideas Form (`index.html`)
Employees submit their marketing/content ideas. One form = one idea. Your name is **required**;
picking it fills in your campus (which can still be changed).
- Fields: your name (**dropdown from `employees.csv`**), content name (= title, required), campus,
  content type (the format, e.g. Reel; free text), content category (the topic, e.g. Food; free
  text), concept, why it could work, caption (one caption idea), platform, hashtags, and photo/video
  links.
- **Campus is a dropdown: Alabama, Ole Miss, Other.** Choosing Other shows a text box to type the
  campus. Stored as plain text, so new campuses need no code change. (Same campus control is used
  wherever campus is entered.)
- Platform: checkboxes for **Instagram** and **TikTok** (one or both).

### 2. Ideas Bank (`bank.html`)
Where ideas are reviewed: **Approve**, **Deny** (shown as "Reject" in older code) or **Delete**.
- Intended reviewers are the **Marketing Chair / Executive**, but **for now anyone can review**
  (no login, no role check). Restricting it by `employees.csv` Role is planned for later.
- Shows "Waiting for review" cards on top and "Rejected" cards below.
- Cards show only: timestamp, content name, campus, content category, links, concept, why it could
  work, who submitted it, and the decision (not type, caption, platform or hashtags).
- New ideas are **Pending**. Reviewers decide on the **whole idea**; **no reason is needed** to deny.
- **Decisions are final.** An approved or rejected idea can't be changed.
- **Approve** → the idea moves to the Production Tracker and leaves the Ideas Bank.
- **Deny** → it moves to "Rejected", then is **deleted automatically 30 days** after the decision
  (checked whenever the Ideas Bank loads).

### 3. Production Tracker (`tracker.html`)
One item per approved idea (created on approve).
- From the idea, can't be changed here: content name, campus, content type, content category,
  caption, and the idea's links (shown as "Content files").
- Assigned and changed anytime, by anyone, with a Save button: **Filmer, Editor,
  Reviewer/approver, Poster** (each a **dropdown of all employees, grouped by campus**), **Target
  post date**, and **Status**.
- **Status list: Needs filming → Needs editing → Needs final review → Scheduled → Completed.**
  A newly approved idea starts at **Needs filming**. (Older statuses map: Not started/Filming →
  Needs filming, Editing → Needs editing, Ready for review → Needs final review, Posted → Completed.)
- Filter: In progress / Completed. Completed items stay until someone deletes them.

### 4. Content Calendar (`content-calendar.html`)
A visual month calendar of the **target post dates** from the Production Tracker (same data, no
separate file).
- Shows **every tracker item that has a target post date**, **colored by status**, with a color key.
- ‹ › arrows move between months; opens on the current month.
- Clicking an item opens a **view-only details pop-up** (Bootstrap modal): content name, campus,
  status, filmer / editor / reviewer / poster, caption, and an "Open in Production Tracker" link.
  Editing happens only in the tracker.

### 5. Promo Calendar (`promo-calendar.html`)
Planning promos with partner organizations. Three parts:
- **Outreach scripts**: one per partner type (**Sorority, Student org, Business**), each with a
  message to the partner and a second **"forward" message** for the partner to send to their
  members. Each has a **Copy** button, and anyone can **Edit** and save the wording
  (`scripts.csv`). Choosing a promo from the Promo List **auto-fills** the scripts: `[SORORITY]`,
  `[ORGANIZATION]` or `[BUSINESS]` → the promo's name, `[CODE]` → its promo code, `[DAY/DATE]` → its
  date, and `[APP]` → the campus's ordering app (**Alabama: Bama Dining Mobile Order app; Ole Miss:
  GrubHub app**; set in `promo-calendar.js`). The Sorority script is the team's own; the Student org and
  Business scripts were drafted by Claude.
- **Promo List** table: name, partner type, campus, date, promo code, status, partner contact
  (name + email/phone). Anyone can add, edit and delete (delete is permanent, with a confirm).
- **Promo calendar**: a month calendar showing each promo on its date, matching the Promo List
  exactly (same data). Colored by status, with a key. Clicking a promo opens it for editing.
- **Promo status: Reaching out → Confirmed → Completed, or Cancelled.**
- Past promos **stay** until someone deletes them.

## Photos and videos

- **The app's own server never keeps photo or video files.** Files live in the company's OneDrive
  folder; the app saves only the link.
- The form has **links only**: any number per idea (none is allowed). Each has a **type (Photo or
  Video)** and an **optional label**, and must start with **`https://`**.
- Links open in a new tab, where OneDrive offers Download / Add to My files. Hovering shows the full
  address.
- **File upload to OneDrive is built but turned off**: the upload button is hidden on the form, and
  the API code (`api/Storage/`, Microsoft Graph) is kept, unused. Whether to remove it or turn it
  back on is undecided. Don't remove it without asking. Its rules: 500 MB per file, 2 GB per idea;
  client secret in `dotnet user-secrets`, never in git; uploaded files stay in OneDrive when an idea
  is deleted.

## Deleting

- **Delete is permanent.** Deleting an idea removes the idea, all its links and its tracker item,
  whatever its status (from the Ideas Bank or the Production Tracker). Deleting a promo removes
  just that promo.
- No archive, no soft delete, no restore, no `.bak` copies of deleted data.

## Employees

- `employees.csv` is the list of employees, **edited by hand for now** (no Employees page).
- It fills the name dropdowns (Ideas Form "Your name"; tracker Filmer / Editor / Reviewer / Poster;
  ideas and tracker rows store the **employee Id**, so fixing a name in the CSV fixes it everywhere).
- Role is **Member, Marketing Chair or Executive**. It's stored now; it will control who can
  review once login is added.
- Semester in the program is a **number** (1, 2, 3, ...), updated by hand each term.
- It currently holds only the test data's people (Caroline, Ryan, Haley; emails, semesters and roles
  still to be filled in). Add the real team.

## Test data

- The **only test data** is idea #1 "Bowl" (Alabama, submitted by Caroline), approved, with its photo
  link and a **Completed** tracker item (filmed and edited by Ryan, reviewed and posted by Haley, target
  post date 2026-10-15). It shows in the Production Tracker's Completed filter and on the Content
  Calendar.
- Older rows may still have typed names in the `Name` / `Filmer` / `Editor` / `Reviewer` / `Poster`
  columns; those are shown when no employee Id is set, and the tracker's typed names are cleared the next
  time the item is saved.

## Data files

All in `mockup_1/api/Data/`. **New columns always go at the end** (the API upgrades older files by
adding missing columns, empty).

- `ideas.csv`: Id, Name (typed name, older ideas only), Title, Concept, Caption, Platforms
  (`Instagram;TikTok`), Hashtags, CreatedAt, ReviewStatus (Pending/Approved/Rejected; the pages say
  "Denied" for Rejected), DenialReason (older ideas only), ReviewedAt, Campus, ContentType,
  ContentCategory, WhyItCouldWork, SubmittedById → employees.Id
- `links.csv`: Id, IdeaId, Type, Label, Url, DriveItemId (only for uploads)
- `production.csv`: Id, IdeaId, Filmer, Editor, Reviewer, Poster (typed names, older rows only),
  TargetPostDate, Status, UpdatedAt, FilmerId, EditorId, ReviewerId, PosterId → employees.Id
- `employees.csv`: Id, Name, Email, Phone, Campus, Semester, Role
- `promos.csv`: Id, Name, PartnerType (Sorority/Student org/Business), Campus, Date (`yyyy-MM-dd`),
  PromoCode, Status, ContactName, ContactInfo, CreatedAt, UpdatedAt
- `scripts.csv`: Id, PartnerType, Message, ForwardMessage, UpdatedAt (one row per partner type)

Dates are stored as `yyyy-MM-dd`; timestamps as UTC round-trip (`2026-10-14T00:06:20.0372153Z`).

## Constraints and context

- **Budget: free.**
- Expected volume: about **20–30 ideas per month**.
- Run the API from inside `mockup_1/api` (`dotnet run`); it listens on `http://localhost:5050`.
  The client pages are opened with Live Server.
