# CLAUDE.md

Guidance for Claude when working in this repository.

## Project

A site that tracks the **progress and status of marketing content** for a business (Low Tide).
Employees log in and submit social media content ideas; Leads (access Level 4) approve or deny them;
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
- **Data:** a **MySQL** database (`low_tide`, local MySQL 8 for now, a cloud host later), or the
  **comma-delimited CSV files** in `mockup_1/api/Data/`. `"Database": { "Provider": "MySql" | "Csv" }`
  in `appsettings.json` picks which. The MySQL connection string is in `dotnet user-secrets`
  (`ConnectionStrings:LowTide`), never in git. On start the API creates the database and tables if
  missing (`MySqlDatabase.cs`) and, only if every table is empty, copies the CSV files in. Keep the
  CSVs and the tables in step (same columns, snake_case in SQL): a new column goes in both.
- Architecture: Controllers → Repository interface (e.g. `IIdeaRepository`) → a CSV implementation
  (`CsvIdeaRepository`) and a MySQL one (`MySqlIdeaRepository`, plain ADO.NET with
  `MySqlConnector` and @parameters, never values pasted into SQL). A change to an interface needs
  both implementations.

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
    purple pill, and a coral stripe underneath. **One row, full width:** logo far left, the page
    tabs beside it (never wrapping), and a **📁 Team Drive** link (the team's Google Drive folder,
    `TEAM_DRIVE_URL` in `common.js`, new tab, for everyone) + the logged-in name + Change password +
    Log out far right.
    From 1200px wide (Bootstrap `xl`, `NAVBAR_BREAKPOINT` in `common.js`) it's one row; between
    1200 and 1399px the tabs are slightly tighter (`style.css`) so a Lead's six tabs fit; narrower
    screens get the ☰ menu.
  - Page background is a very light teal; titles are purple with a short coral bar under them;
    table headers (calendars, Promo List) are purple with white text.
  - Font: **Fredoka** (Google Fonts, rounded like "POKE COMPANY") for headings, the navbar and
    buttons; normal Bootstrap font for body text. Loaded in `index.html`'s `<head>`.
  - Other colors may be used where they read better (e.g. Bootstrap's red/yellow/green statuses).
  - **Contrast:** white text only on purple. On teal or coral use dark purple text, and don't use
    teal or coral for small text on white.
- Every page shares one **Bootstrap navbar** linking the six pages (built by `renderNavbar()` in
  `common.js`).

## File structure (keep it this way)

- **All the HTML is in ONE file: `mockup_1/client/index.html`.** Don't add more `.html` pages.
  Each page is a `<section data-page="...">` in it (`form`, `bank`, `tracker`, `content-calendar`,
  `promo-calendar`, `directory`); pop-ups (modals) go after `</main>`. A new page = a new section plus an entry
  in `PAGES` in `common.js`.
- **JavaScript and CSS stay in SEPARATE files**, never inline in `index.html` (no `<script>` code
  blocks, no `<style>` blocks, no `style="..."`):
  - `resources/scripts/common.js`: shared helpers, the navbar, page switching, and `can()` /
    `apiFetch()` for the login. Loaded first.
  - `resources/scripts/auth.js`: the login page, Log out, Change password; starts the site.
  - `resources/scripts/calendar.js`: the month grid used by both calendars.
  - One script per page: `submit.js` (Ideas Form), `bank.js`, `tracker.js`,
    `content-calendar.js`, `promo-calendar.js`, `directory.js`.
  - `resources/styles/style.css`: only what Bootstrap can't do (see Design rules).
- **Page switching:** the part of the address after `#` picks the page (`index.html#bank`; no `#`
  = Ideas Form). `showCurrentPage()` in `common.js` shows that section and hides the others with
  Bootstrap's `d-none` class, highlights the navbar link and sets the tab title. Extra settings
  go after `?`, read with `pageParams()` (e.g. `#tracker?view=Completed&item=3`). Links between
  pages are plain `<a href="#tracker">`.
- Each page's script calls `onPageShow("<page id>", loadFunction)` instead of loading at the
  bottom of the file. The function runs every time the page is shown, so data is always fresh.
  Anything that should only load once (e.g. dropdown options) must check that itself.
- **All scripts share one page**, so top-level names (`const`, `let`, `function`) and element `id`s
  must be **unique across every script and section**. Prefix them with the page when they could
  clash (e.g. `contentCalendar` / `promoCalendar`, `content-color-key` / `promo-color-key`).
- Both calendars use the shared month grid in `calendar.js` (no calendar library).
- The users are **not technical**: use plain-language messages, confirm before final or
  destructive actions, and never clear a form when saving fails.
- Show user-entered text with `textContent` (never `innerHTML`) to prevent script injection.

## The six pages

```
Ideas Form ──► Ideas Bank ──► Approve ──► Production Tracker ◄──► Content Calendar
                         └──► Deny ──► Rejected (auto-deleted after 30 days)
Promo Calendar (separate): Promo List table + promo calendar + outreach scripts
Intern Directory (separate): the employee list used by every name dropdown
```

## Login and access

- Everyone logs in with their **employee ID** (from the Intern Directory) and **password**. The
  starting password is **`LTPMKT`** for everyone, including new interns and after a reset.
- Passwords are stored **hashed** (PBKDF2) in `employees.csv`'s `PasswordHash` column; an empty
  `PasswordHash` means the starting password. Anyone can **Change password** from the navbar
  (8+ characters; changing is optional). **Leads can reset** anyone's password back to LTPMKT in the
  Intern Directory's Edit pop-up.
- Login lasts **until the browser tab is closed** (the token is in `sessionStorage`); the API also
  ends sessions after 12 hours. Sessions are kept in the API's memory, so **restarting the API logs
  everyone out**. There's a **Log out** button in the navbar.
- **Access comes from the access level (1-4), not the semester.** Every intern has a Semester
  (information only) and an Access level, set separately in the Intern Directory (a new intern's
  level follows their semester until changed; rows from before levels were read from the semester).
- **What each level can do** (the page hides what you can't use, and the API refuses it too):

| Page | Level 1 (usually sem 1-2) | Level 2 (sem 3-4) | Level 3 (sem 5-6) | Level 4 = Leads (sem 7-8) |
|---|---|---|---|---|
| Ideas Form | submit | submit | submit | submit |
| Ideas Bank | No | No | No | approve / deny / delete |
| Production Tracker | No | view + change Filmer / Editor | edit | edit + delete |
| Content Calendar | view only | view only | view only | view only |
| Promo Calendar | No | view only (+ Copy scripts) | add / edit promos + script wording | + delete promos |
| Intern Directory | No | No | No | add / edit / delete / reset passwords |

Each cell is the level's default. In the Intern Directory a Lead can switch any page to **Yes** or
**No** for one person: "No" hides the page; "Yes" on a page the level normally has gives the level's
access above, and "Yes" on a page it doesn't (a "No" cell) gives **view only** (e.g. the Ideas Bank
without Approve/Deny, the Directory without Add/Edit).

- The permissions are defined once, in `api/Auth/Permissions.cs` (`SubmitIdeas`, `ViewIdeas`,
  `ReviewIdeas`, `ViewTracker`, `AssignCrew`, `EditTracker`, `DeleteTracker`, `ViewContentCalendar`,
  `ViewPromos`, `EditPromos`, `DeletePromos`, `ViewDirectory`, `ManageDirectory`), with what each level
  gets on each page and what a view-only "Yes" gives. Every API action has `[RequirePermission(...)]`;
  the page gets the same list at login and checks it with `can("EditPromos")`. **Change access there,
  not in the page.** A level or Yes/No change applies on the next request.
- The Yes/No choices are stored in `employee_page_access.csv` (`Id, EmployeeId, Page, Allowed`),
  **only for pages set differently from the level's default**. Changing someone's level resets their
  switches to the new level's pages (in the pop-up, before Save).
- View-only pages show plain text, no buttons (e.g. tracker jobs as names; a promo's details in a
  read-only pop-up). Without the tracker, "Open in Production Tracker" is hidden on the Content Calendar.
- A Lead can't delete themselves or take away their own Intern Directory editing (Level 4 with
  Intern Directory = Yes), so nobody gets locked out.
- Opening a page you can't see goes to the Ideas Form.

### 1. Ideas Form (`index.html#form`, `submit.js`)
Employees submit their marketing/content ideas. One form = one idea. The idea is **submitted as
whoever is logged in** ("Submitting as Haley King"); their campus is filled in (it can be changed).
- Fields: content name (= title, required),
  campus (required), content type (the format, e.g. Reel; free text), content category (required),
  **concept and why it could work** (one field, saved as `Concept`), caption (one caption idea), and
  photo/video links.
- **Campus is a dropdown: Alabama, Ole Miss, Either** ("Either" = it works for both campuses).
  No type-in box. The **promo pop-up's campus is Alabama or Ole Miss only**, because every promo
  must be at one campus (it picks the ordering app).
- **Content category is a dropdown: Food/Drink, Customer/Student Life, Employee, Other.** "Other" is
  saved as just "Other" (no type-in box).
- The choices are plain `<option>`s in `index.html`, and the API rejects anything else (lists in
  `Idea.Campuses`, `Idea.ContentCategories` and `Promo.Campuses`). Change both when a list changes.
- There is **no platform or hashtags field** anymore (removed from the page, API and CSV).

### 2. Ideas Bank (`index.html#bank`, `bank.js`)
Where ideas are reviewed: **Approve**, **Deny** (shown as "Reject" in older code) or **Delete**.
- **Only Leads (semesters 7-8)** can see it and review.
- Shows "Waiting for review" cards on top and "Rejected" cards below.
- Cards show only: timestamp, content name, campus, content category, links, concept and why it
  could work, who submitted it, and the decision (not type or caption).
- New ideas are **Pending**. Reviewers decide on the **whole idea**; **no reason is needed** to deny.
- **Decisions are final.** An approved or rejected idea can't be changed.
- **Approve** → the idea moves to the Production Tracker and leaves the Ideas Bank.
- **Deny** → it moves to "Rejected", then is **deleted automatically 30 days** after the decision
  (checked whenever the Ideas Bank loads).

### 3. Production Tracker (`index.html#tracker`, `tracker.js`)
One item per approved idea (created on approve).
- From the idea, can't be changed here: content name, campus, content type, content category,
  caption, and the idea's links (shown as "Content files").
- Assigned and changed anytime by **Levels 3-4** (Level 2 can change only **Filmer and Editor**;
  the API keeps everything else as it was; only Level 4 can delete), with a Save button: **Filmer, Editor,
  Reviewer/approver, Poster** (each a **dropdown of all employees, grouped by campus**), **Target
  post date**, and **Status**.
- **Status list: Needs filming → Needs editing → Needs final review → Scheduled → Completed.**
  A newly approved idea starts at **Needs filming**. (Older statuses map: Not started/Filming →
  Needs filming, Editing → Needs editing, Ready for review → Needs final review, Posted → Completed.)
- **Scheduled needs a target post date** (the API refuses to save Scheduled without one).
  Completed may have no date (it just won't show on the calendar).
- Filter: In progress / Completed. Completed items stay until someone deletes them.

### 4. Content Calendar (`index.html#content-calendar`, `content-calendar.js`)
A visual month calendar of the **target post dates** from the Production Tracker (same data, no
separate file).
- Shows **only tracker items that are Scheduled or Completed** (and have a target post date).
  Items still in production (Needs filming / editing / final review) stay in the tracker only.
  Colored by status, with a color key of just those two statuses. The list is `CALENDAR_STATUSES`
  in `common.js`, shared with the promo calendar. No pop-up when an item moves on or off the
  calendar; the page text explains it.
- ‹ › arrows move between months; opens on the current month.
- Clicking an item opens a **view-only details pop-up** (Bootstrap modal): content name, campus,
  status, filmer / editor / reviewer / poster, caption, and an "Open in Production Tracker" link.
  Editing happens only in the tracker.

### 5. Promo Calendar (`index.html#promo-calendar`, `promo-calendar.js`)
Planning promos with partner organizations. Three parts, shown top to bottom in this order:
**Promo List**, **promo calendar**, then **outreach scripts**.
- **Outreach scripts**: one per partner type (**Sorority, Student org, Business**), each with a
  message to the partner and a second **"forward" message** for the partner to send to their
  members. Each has a **Copy** button, and semesters 5-8 can **Edit** and save the wording
  (`scripts.csv`). Choosing a promo from the Promo List **auto-fills** the scripts: `[SORORITY]`,
  `[ORGANIZATION]` or `[BUSINESS]` → the promo's name, `[CODE]` → its promo code, `[DAY/DATE]` → its
  date, and `[APP]` → the campus's ordering app (**Alabama: Bama Dining Mobile Order app; Ole Miss:
  GrubHub app**; set in `promo-calendar.js`). The Sorority script is the team's own; the Student org and
  Business scripts were drafted by Claude.
- **Promo List** table: name, partner type, campus, date, promo code, status, partner contact
  (name + email/phone). Semesters 5-8 can add and edit; only 7-8 can delete (permanent, with a confirm).
- **Promo calendar**: a month calendar showing **only Scheduled and Completed promos** on their
  dates (same data as the Promo List, `CALENDAR_STATUSES`). Reaching out and Cancelled promos stay
  in the Promo List only. Colored by status, with a key. Clicking a promo opens it for editing.
- **Promo status: Reaching out → Scheduled → Completed, or Cancelled.** (Older "Confirmed" promos
  are read as Scheduled.)
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

### 6. Intern Directory (`index.html#directory`, `directory.js`)
The list of employees (`employees.csv`), **added, edited and deleted here by Leads (Level 4) only**
(anyone else a Lead sets to Yes sees it view only).
- Grouped into four tables by access level: **Level 1 (Semesters 1 & 2), Level 2 (3 & 4), Level 3
  (5 & 6), Level 4: Leads (7 & 8)** (empty groups say so). Columns: ID, name (+ a "Custom page
  access" badge if any switch differs from the level), campus, semester, role, email, phone;
  sorted by campus, then name.
- Add / Edit is a pop-up (like the Promo List). **Required: ID, name, campus (Alabama / Ole Miss),
  semester, access level.** Email and phone are optional. Adding pre-fills the next free ID
  (`GET /api/employees/next-id`), which can be changed.
- **Page access:** a Yes / No switch beside each of the six pages, with what "Yes" gives at the
  chosen level ("Level N access" or "Yes = view only"). The level's default pages come from
  `GET /api/employees/level-pages`.
- **IDs can be edited.** Changing someone's ID also moves their ideas (`SubmittedById`) and tracker
  jobs (Filmer/Editor/Reviewer/Poster) to the new ID. An ID is refused if another employee has it,
  **or** if old ideas/tracker jobs still point to it (a deleted employee's ID), so nobody inherits
  someone else's work.
- **Delete** is permanent, with a confirm, and logs them out. Their ideas and tracker jobs keep the
  ID and show "(former employee #N)"; that ID isn't suggested for anyone new.
- **Reset password** (in the Edit pop-up) sets their password back to LTPMKT.

## Employees

- `employees.csv` is who can log in, and fills the tracker's Filmer / Editor / Reviewer / Poster
  dropdowns. Ideas and tracker rows store the **employee Id**, so fixing a name fixes it
  everywhere. It can still be edited by hand; changes show on the next page load.
- **Semester is a pair: `1-2`, `3-4`, `5-6` or `7-8`**, moved up by hand each year. It's
  information only; the **AccessLevel** column (1-4) decides access. Level 4 = Role "Lead".
- **Role is automatic from the semester: `Lead` for 7-8, blank for everyone else.** It isn't in the
  form. **Access comes from the semester**, not the role (see "Login and access").
- The team (Oct 2026): Haley King (#1, Alabama, Lead), then in order Ryder diTargiani, Savannah
  Ross, Peyton Stevenson, Sophia Peña (Ole Miss, 1-2), Joanna House (Alabama, 1-2), Graycie Glenn
  (Ole Miss, 1-2), Mackenzie Weston (Ole Miss, 3-4), Jeorgia Luger (Alabama, 3-4), Leigh Powell
  (Ole Miss, 3-4), Dacey Leonard (#11, Ole Miss, Lead). Emails and phones still to be filled in.

## Test data

- The test ideas (e.g. #1 "Bowl", approved, with a **Completed** tracker item, target post date
  2026-10-15) were made by the old test employees (Caroline, Ryan, Haley). When the real team
  replaced them, the old Haley's tracker jobs moved to Haley King (#1) and Caroline's / Ryan's were
  cleared, so those ideas show "Anonymous" and those jobs "Not assigned".
- Older rows may still have typed names in the `Name` / `Filmer` / `Editor` / `Reviewer` / `Poster`
  columns; those are shown when no employee Id is set, and the tracker's typed names are cleared the next
  time the item is saved.

## Data files

All in `mockup_1/api/Data/`. When the API starts, `CsvFile.EnsureSchema` upgrades older files to
the current header **by column name**: existing values are kept, new columns are added empty, and
removed columns are dropped. Put new columns at the end anyway, and if a removed column has data
worth keeping, merge it into another column in the CSV first.

- `ideas.csv`: Id, Name (typed name, older ideas only), Title, Concept (concept and why it could
  work), Caption, CreatedAt, ReviewStatus (Pending/Approved/Rejected; the pages say "Denied" for
  Rejected), DenialReason (older ideas only), ReviewedAt, Campus (Alabama/Ole Miss/Either),
  ContentType, ContentCategory (Food/Drink/Customer/Student Life/Employee/Other),
  SubmittedById → employees.Id
- `links.csv`: Id, IdeaId, Type, Label, Url, DriveItemId (only for uploads)
- `production.csv`: Id, IdeaId, Filmer, Editor, Reviewer, Poster (typed names, older rows only),
  TargetPostDate, Status, UpdatedAt, FilmerId, EditorId, ReviewerId, PosterId → employees.Id
- `employees.csv`: Id, Name, Email, Phone, Campus (Alabama/Ole Miss), Semester (1-2/3-4/5-6/7-8),
  Role (Lead for 7-8, otherwise blank), PasswordHash (empty = LTPMKT; never shown on the pages)
- `promos.csv`: Id, Name, PartnerType (Sorority/Student org/Business), Campus, Date (`yyyy-MM-dd`),
  PromoCode, Status, ContactName, ContactInfo, CreatedAt, UpdatedAt
- `scripts.csv`: Id, PartnerType, Message, ForwardMessage, UpdatedAt (one row per partner type)

Dates are stored as `yyyy-MM-dd`; timestamps as UTC round-trip (`2026-10-14T00:06:20.0372153Z`).

## Constraints and context

- **Budget: free.**
- Expected volume: about **20–30 ideas per month**.
- Run the API from inside `mockup_1/api` (`dotnet run`); it listens on `http://localhost:5050`.
  The client (`mockup_1/client/index.html`) is opened with Live Server.
