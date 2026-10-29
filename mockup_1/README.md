# LTP Marketing – Mockup 1

Tracks the progress and status of the business's marketing content. Employees submit social media
content ideas; Leads (access Level 4) approve or deny them; approved ideas are tracked through
production to posting on the Production Tracker and Content Calendar; and the Promo Calendar plans
promos with sororities, student orgs and businesses.

Styled with **Bootstrap 5** (from a CDN); `style.css` only holds what Bootstrap can't do (the calendar grid).

## Pages

The whole site is **one HTML file, `client/index.html`**. Each page is a `<section data-page="...">`
in it, and only one is shown at a time: the one named after `#` in the address. The navbar links
change that address, so refresh and the browser's Back button work as if they were separate pages.

| Page | Address | Script | What it's for |
|---|---|---|---|
| Ideas Form | `index.html` (or `#form`) | `submit.js` | One form = one idea, submitted as whoever is logged in; campus is Alabama / Ole Miss / Either; content category is Food/Drink / Customer/Student Life / Employee / Other. |
| Ideas Bank | `index.html#bank` | `bank.js` | "Waiting for review" cards with **Approve**, **Deny** and **Delete**, then "Denied" cards. |
| Production Tracker | `index.html#tracker` | `tracker.js` | Approved ideas: Filmer / Editor / Reviewer / Poster (employee dropdowns), target post date, status. Filter: In progress / Completed. |
| Content Calendar | `index.html#content-calendar` | `content-calendar.js` | Month calendar of the tracker's **Scheduled and Completed** items on their target post dates, colored by status. Click for details. |
| Promo Calendar | `index.html#promo-calendar` | `promo-calendar.js` | From top to bottom: the Promo List, a calendar of the promos, and the outreach scripts (Copy, Edit, auto-fill from a promo). |
| Intern Directory | `index.html#directory` | `directory.js` | Every employee by access level (1-4). Add, edit (including ID, semester, level and per-page Yes/No access) and delete. |

The HTML is all in `index.html`, but the JavaScript and CSS stay in their own files:
`resources/scripts/` (one script per page, plus `common.js`, `calendar.js` and `auth.js`) and
`resources/styles/style.css`.

- `common.js` builds the navbar (`renderNavbar()`) and switches pages (`showCurrentPage()`).
- Each page's script registers its loading function with `onPageShow("bank", loadIdeas)`. It runs
  **every time that page is shown**, so each page always has the latest data (e.g. an idea approved
  in the Ideas Bank is in the Production Tracker when you open it). The Ideas Form keeps what
  you've typed when you switch pages.
- Extra settings go after a `?`: `index.html#tracker?view=Completed&item=3` opens the tracker's
  Completed list and scrolls to item 3 (the Content Calendar's "Open in Production Tracker" link).
- Because every script runs on the same page, **top-level names and element ids must be unique
  across all of them** (e.g. `contentCalendar` / `promoCalendar`, `content-calendar-grid` /
  `promo-calendar-grid`).
- Both calendars use `resources/scripts/calendar.js`.

## How an idea moves through the site

```
Ideas Form ──► Ideas Bank: Waiting for review ──► Approve ──► Production Tracker ──► Content Calendar
                                               │              (Needs filming ... Completed)
                                               └──► Deny ──► Ideas Bank: Denied
                                                             └─ deleted automatically after 30 days
```

- New ideas are **Pending** and show under "Waiting for review" in the Ideas Bank.
- **Approve** moves the idea to the Production Tracker (status "Needs filming"). It leaves the Ideas Bank.
- **Deny** needs no reason. The idea moves to "Denied", showing the date it will be deleted, and the
  app deletes it (with its links) **30 days** after the decision. The check runs whenever the Ideas
  Bank is loaded. (The API and CSV call this "Rejected".)
- **Decisions are final.** An approved or denied idea can't be changed.
- **Delete** (Ideas Bank or Production Tracker) permanently removes the idea, its links and its
  tracker entry. It can't be undone.
- Only Leads (Level 4) can approve / deny / delete. Others see the Ideas Bank only if a Lead sets it to Yes (view only).

## Logging in

Everyone logs in with their **employee ID** and **password** (starting password: **LTPMKT**). The
navbar shows only the pages that person can use, with **Change password** and **Log out**. Login
lasts until the browser tab is closed (12 hours at most), and restarting the API logs everyone out.

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

- The rules live in `api/Auth/Permissions.cs`. Every API action is marked with
  `[RequirePermission(...)]` (`api/Auth/RequirePermission.cs`), so the API refuses anything the
  person isn't allowed to do (401 = not logged in, 403 = not allowed), even if the page were changed.
- Passwords are stored hashed (`api/Auth/Passwords.cs`) in `employees.csv`'s `PasswordHash` column.
  Empty = LTPMKT. Changing needs 8+ characters. Leads can reset someone's password in the Intern
  Directory.
- The page sends the login token with every request (`apiFetch()` in `common.js`); `auth.js` has
  the login page, Log out and Change password.

## Production Tracker

Each item shows, from the approved idea (can't be changed here): content name, campus, content
type, content category, caption, and the idea's links as its content files.

These can be changed anytime, then saved with **Save**:

- Filmer, Editor, Reviewer/approver, Poster (who posts it): dropdowns of every employee, grouped by campus
- Target post date: a date picker (items are listed soonest first; no date goes last)
- Status: Needs filming → Needs editing → Needs final review → Scheduled → Completed
  (**Scheduled needs a target post date**; Save is refused without one)

Items marked **Completed** move to the "Completed" filter. They stay until someone deletes them.
Only **Scheduled** and **Completed** items show on the **Content Calendar** (on their target post
date). Items still in production stay in the tracker only. The list of calendar statuses is
`CALENDAR_STATUSES` in `common.js`, shared by both calendars.

## Promo Calendar

The page shows the Promo List first, then the calendar, then the outreach scripts.

- **Outreach scripts**: one per partner type (Sorority, Student org, Business), each with a message to
  the partner and a message for them to forward to their members. Choose a promo in "Fill in from promo"
  and the placeholders are filled in: `[SORORITY]` / `[ORGANIZATION]` / `[BUSINESS]` (its name), `[CODE]`,
  `[DAY/DATE]`, and `[APP]` (Alabama: Bama Dining Mobile Order app; Ole Miss: GrubHub app; the list is
  `ORDERING_APPS` in `promo-calendar.js`). **Copy** copies the filled-in text. **Edit wording** changes
  the saved script.
- **Promo List**: every promo: name, partner type, campus, date, promo code, status (Reaching out →
  Scheduled → Completed, or Cancelled) and partner contact. Add, edit or delete (permanent).
- **Calendar**: only the **Scheduled** and **Completed** promos, on their dates, colored by status.
  Reaching out and Cancelled promos stay in the list only. Click one to edit it.

## Intern Directory

- Four tables, one per access level, each sorted by campus then name. Level 4 interns are **Leads**
  (Role is set automatically from the level; it isn't in the form).
- **+ Add intern** / **Edit** open a pop-up: ID, name, campus (Alabama / Ole Miss), semester and
  access level are required; email and phone are optional. A new intern's ID is pre-filled with the
  next free one, and their level follows the semester until one is chosen.
- **Page access:** a Yes / No switch per page. Changing the level resets them to that level's pages.
  Only the switches that differ from the level are saved, in `employee_page_access.csv`.
- **Changing an ID** moves that person's ideas and Production Tracker jobs to the new ID. An ID is
  refused if another employee has it, or if a deleted employee's old ideas/jobs still use it.
- **Delete** is permanent. Their ideas and jobs show "(former employee #3)".

## How the data is stored

**The app never keeps photo or video files on its own server.** People put files in the
company OneDrive folder and paste the share link. Only the link is saved.

```
api/Data/ideas.csv       <- one row per idea
api/Data/links.csv       <- one row per photo/video link, linked to its idea by IdeaId
api/Data/production.csv  <- one row per approved idea, linked to its idea by IdeaId
api/Data/employees.csv   <- one row per employee (edited in the Intern Directory)
api/Data/promos.csv      <- one row per promo
api/Data/scripts.csv     <- one outreach script per partner type
```

| ideas.csv | links.csv | production.csv |
|---|---|---|
| Id | Id | Id |
| Name (typed name, older ideas only) | IdeaId → ideas.Id | IdeaId → ideas.Id |
| Title (content name) | Type (Photo / Video) | Filmer, Editor, Reviewer, Poster (typed names, older rows only) |
| Concept (the concept and why it could work) | Label (optional) | TargetPostDate (`yyyy-MM-dd`, may be empty) |
| Caption | Url (must start with https://) | Status |
| CreatedAt (the timestamp) | DriveItemId (OneDrive file id, only for uploads; see below) | UpdatedAt (last saved) |
| ReviewStatus (Pending / Approved / Rejected) | | FilmerId → employees.Id |
| DenialReason (only on older ideas) | | EditorId → employees.Id |
| ReviewedAt | | ReviewerId → employees.Id |
| Campus (Alabama / Ole Miss / Either) | | PosterId → employees.Id |
| ContentType (the format, e.g. Reel) | | |
| ContentCategory (Food/Drink / Customer/Student Life / Employee / Other) | | |
| SubmittedById → employees.Id | | |

| employees.csv | promos.csv | scripts.csv |
|---|---|---|
| Id | Id | Id |
| Name | Name (the partner) | PartnerType |
| Email | PartnerType (Sorority / Student org / Business) | Message (to the partner) |
| Phone | Campus | ForwardMessage (for their members) |
| Campus | Date (`yyyy-MM-dd`) | UpdatedAt |
| Semester (1-2 / 3-4 / 5-6 / 7-8; information only) | PromoCode | |
| AccessLevel (1-4; empty = from the semester) | Status | |
| Role (Lead for Level 4, otherwise blank) | ContactName, ContactInfo | |
| | CreatedAt, UpdatedAt | |

**employee_page_access.csv**: `Id`, `EmployeeId` → employees.Id, `Page` (form / bank / tracker /
content-calendar / promo-calendar / directory), `Allowed` (Yes / No). One row per page a Lead set
differently from the person's level.

When the API starts, it upgrades older CSV files to the current columns, **matching columns by
name**: columns still in use keep their values, new columns are added empty, and removed columns
are dropped (e.g. `Platforms`, `Hashtags` and `WhyItCouldWork` were removed from `ideas.csv`; the
"why it could work" text was merged into `Concept` first). Older tracker statuses are read
as the new ones (Not started/Filming → Needs filming, Editing → Needs editing, Ready for review →
Needs final review, Posted → Completed), and the old promo status "Confirmed" is read as "Scheduled".

**employees.csv** is edited in the Intern Directory (it can still be edited by hand; changes show up
the next time a page loads, without restarting the API). If editing by hand, don't reuse or delete
an employee's Id while ideas or tracker items use it; a missing Id shows as "(former employee #3)".

**Test data:** idea #1 "Bowl" (submitted by Caroline), approved, with a Completed tracker item
(Ryan films and edits, Haley reviews and posts).

## Running it

1. In a terminal:
   ```
   cd api
   dotnet run
   ```
   Always run it from inside `api/`, because `Data/` is found relative to it.
   The API runs at http://localhost:5050.
2. Open `client/index.html` with Live Server (or just double-click it).
3. Log in with your employee ID and password (starting password **LTPMKT**). After restarting the
   API, everyone has to log in again.

## API endpoints

Everything except `POST /api/auth/login` needs the login token (`Authorization: Bearer <token>`)
and the permission in brackets (see "Logging in").

| Method | URL | Body | What it does |
|---|---|---|---|
| POST | `/api/auth/login` | JSON: `id`, `password` | Log in: returns `token` and `me` (employee + permissions) |
| GET | `/api/auth/me` | | Who is logged in, and their permissions |
| POST | `/api/auth/logout` | | Log out |
| POST | `/api/auth/password` | JSON: `currentPassword`, `newPassword` | Change your own password (8+ characters) |
| POST | `/api/employees/{id}/reset-password` | | [ManageDirectory] Set their password back to LTPMKT |
| GET | `/api/ideas` | | [ViewIdeas] The Ideas Bank: ideas waiting for review + denied ideas (with `deletesOn`) |
| POST | `/api/ideas` | multipart form: `idea` = the idea as JSON (with a `links` array) | [SubmitIdeas] Submit an idea (as the logged-in person) |
| POST | `/api/ideas/{id}/approve` | | [ReviewIdeas] Approve (Pending only); creates its tracker item |
| POST | `/api/ideas/{id}/reject` | | [ReviewIdeas] Deny (Pending only) |
| DELETE | `/api/ideas/{id}` | | [ReviewIdeas] Permanently delete the idea, its links and its tracker item |
| GET | `/api/ideas/files/{linkId}` | | [no login: see Known mockup shortcuts] For uploaded files only (see below) |
| GET | `/api/production?view=InProgress` | | [ViewTracker; `view=All` needs only ViewContentCalendar] Tracker items not yet Completed (`view=Completed` for completed ones, `view=All` for the Content Calendar) |
| GET | `/api/production/statuses` | | [ViewTracker] The Status choices, in order |
| PUT | `/api/production/{id}` | JSON: `filmerId`, `editorId`, `reviewerId`, `posterId`, `targetPostDate`, `status` | [AssignCrew or EditTracker] Save production details (AssignCrew only: just `filmerId` / `editorId`; the rest is kept) |
| DELETE | `/api/production/{id}` | | [DeleteTracker] Permanently delete the item, its idea and links |
| GET | `/api/employees` | | [AssignCrew, EditTracker, ViewDirectory or ManageDirectory] Every employee, by campus then name (with `accessLevel`, `pageAccess`, `customAccess`) |
| GET | `/api/employees/level-pages` | | [ViewDirectory or ManageDirectory] For each level, the pages that are Yes by default |
| GET | `/api/employees/next-id` | | [ManageDirectory] The ID to suggest for a new intern (one higher than any ID in use) |
| POST | `/api/employees` | JSON: `id`, `name`, `email`, `phone`, `campus`, `semester`, `accessLevel`, `pageAccess` (`{ "tracker": true, ... }`) | [ManageDirectory] Add an employee |
| PUT | `/api/employees/{id}` | same as POST (a different `id` changes their ID and moves their ideas and tracker jobs) | [ManageDirectory] Save an employee |
| DELETE | `/api/employees/{id}` | | [ManageDirectory] Permanently delete an employee |
| GET | `/api/promos` | | [ViewPromos] Every promo, soonest first |
| GET | `/api/promos/options` | | [ViewPromos] The Partner type and Status choices |
| POST | `/api/promos` | JSON: `name`, `partnerType`, `campus`, `date`, `promoCode`, `status`, `contactName`, `contactInfo` | [EditPromos] Add a promo |
| PUT | `/api/promos/{id}` | same as POST | [EditPromos] Save a promo |
| DELETE | `/api/promos/{id}` | | [DeletePromos] Permanently delete a promo |
| GET | `/api/promos/scripts` | | [ViewPromos] The outreach scripts |
| PUT | `/api/promos/scripts/{id}` | JSON: `message`, `forwardMessage` | [EditPromos] Save a script's wording |

## Things to know about links

- A link only works while the file exists and is shared. If someone deletes the file, moves
  it, or leaves the company, the link breaks. **Keep all files in one shared company folder**
  so the company owns them, not individual people.
- Set the sharing to "People in your organization" (or "Anyone with the link") so reviewers
  don't see "You need permission."

## File uploads to OneDrive (built, but turned off)

The **"+ Upload a file" button is hidden** on the submit form for now. The API code for it is
still there (`api/Storage/`, and the `files` part of `POST /api/ideas`), so it can be turned
back on or removed later. To bring it back, the form's upload button and script need re-adding
(see the git history for commit "Add file uploads to OneDrive").

How it works when it's on: the API sends each file (up to 500 MB, 2 GB per idea; .jpg .jpeg .png
.heic .gif .mp4 .mov .m4v) to the company's SharePoint/OneDrive folder through Microsoft Graph,
saves the file's address and its OneDrive id (`DriveItemId`), and the pages can play/show it
through `GET /api/ideas/files/{linkId}`. Deleting an idea leaves its uploaded files in OneDrive.

### Setting up OneDrive uploads

The API signs in to Microsoft 365 as **itself** (not as a person), so it needs an "app
registration". This is free with Microsoft 365, but **a Microsoft 365 admin has to do steps 1–2**.

1. **Register the app** (admin): in the [Microsoft Entra admin center](https://entra.microsoft.com),
   go to *App registrations → New registration*, name it e.g. "Content Ideas". Copy the
   **Application (client) ID** and **Directory (tenant) ID**. Then *Certificates & secrets →
   New client secret* and copy the secret's **Value** (it's only shown once).
2. **Give it permission** (admin): *API permissions → Add → Microsoft Graph → Application
   permissions*, then either:
   - **Sites.Selected** (recommended: the app can only touch the one site you allow), then grant
     it write access to the marketing site. See Microsoft's "Sites.Selected" docs. Or
   - **Files.ReadWrite.All** (simpler, but the app could reach every site's files).

   Then click **Grant admin consent**.
3. **Find the Drive ID** of the site's document library. In
   [Graph Explorer](https://developer.microsoft.com/graph/graph-explorer), signed in:
   - `GET https://graph.microsoft.com/v1.0/sites/{company}.sharepoint.com:/sites/{site-name}` → copy `id`
   - `GET https://graph.microsoft.com/v1.0/sites/{that id}/drives` → copy the `id` of "Documents"
4. **Fill in the settings.** Non-secret values go in `api/appsettings.json`
   (`TenantId`, `ClientId`, `DriveId`, `FolderPath`). The **secret never goes in a file in git**.
   Store it with .NET's user-secrets (run in `api/`):
   ```
   dotnet user-secrets set "OneDrive:ClientSecret" "the-secret-value"
   ```
   On a real server, set the environment variable `OneDrive__ClientSecret` instead. Client
   secrets expire (6–24 months), so set a reminder to make a new one.
5. Restart the API.

## Moving to MySQL later

1. Create the tables:
   ```sql
   CREATE TABLE ideas (
       id                INT AUTO_INCREMENT PRIMARY KEY,
       name              VARCHAR(100),
       title             VARCHAR(200) NOT NULL,
       campus            VARCHAR(100),
       content_type      VARCHAR(100),
       content_category  VARCHAR(100),
       concept           TEXT,          -- the concept and why it could work
       caption           TEXT,
       created_at        DATETIME NOT NULL,
       review_status     VARCHAR(20) NOT NULL DEFAULT 'Pending',
       denial_reason     TEXT,          -- only on older ideas
       reviewed_at       DATETIME NULL
   );

   CREATE TABLE idea_links (
       id            INT AUTO_INCREMENT PRIMARY KEY,
       idea_id       INT NOT NULL,
       type          VARCHAR(10) NOT NULL,   -- 'Photo' or 'Video'
       label         VARCHAR(200),
       url           VARCHAR(2048) NOT NULL,
       drive_item_id VARCHAR(100) NOT NULL DEFAULT '',  -- OneDrive file id; '' for pasted links
       FOREIGN KEY (idea_id) REFERENCES ideas(id) ON DELETE CASCADE  -- deleting an idea deletes its links
   );

   CREATE TABLE employees (
       id       INT AUTO_INCREMENT PRIMARY KEY,
       name     VARCHAR(100) NOT NULL,
       email    VARCHAR(200),
       phone    VARCHAR(30),
       campus   VARCHAR(100),
       semester VARCHAR(3),                      -- '1-2', '3-4', '5-6' or '7-8' (information only)
       access_level TINYINT,                     -- 1-4 (decides access)
       role     VARCHAR(30) NOT NULL DEFAULT ''  -- 'Lead' for Level 4
   );

   CREATE TABLE employee_page_access (           -- only pages set differently from the level
       id          INT AUTO_INCREMENT PRIMARY KEY,
       employee_id INT NOT NULL,
       page        VARCHAR(30) NOT NULL,         -- 'form', 'bank', 'tracker', ...
       allowed     BOOLEAN NOT NULL,
       UNIQUE (employee_id, page),
       FOREIGN KEY (employee_id) REFERENCES employees(id) ON DELETE CASCADE ON UPDATE CASCADE
   );

   CREATE TABLE production_items (
       id               INT AUTO_INCREMENT PRIMARY KEY,
       idea_id          INT NOT NULL UNIQUE,   -- one tracker item per approved idea
       filmer_id        INT NULL,
       editor_id        INT NULL,
       reviewer_id      INT NULL,
       poster_id        INT NULL,
       target_post_date DATE NULL,
       status           VARCHAR(30) NOT NULL DEFAULT 'Needs filming',
       updated_at       DATETIME NOT NULL,
       FOREIGN KEY (idea_id) REFERENCES ideas(id) ON DELETE CASCADE
       -- No foreign keys to employees: a deleted employee's id stays on their jobs ("former
       -- employee"), which a foreign key wouldn't allow. Changing an employee's id updates these
       -- columns in code (ReplaceEmployeeIdAsync), the same as the CSV version.
   );

   CREATE TABLE promos (
       id           INT AUTO_INCREMENT PRIMARY KEY,
       name         VARCHAR(200) NOT NULL,
       partner_type VARCHAR(30) NOT NULL,
       campus       VARCHAR(100) NOT NULL,
       promo_date   DATE NOT NULL,
       promo_code   VARCHAR(50),
       status       VARCHAR(30) NOT NULL DEFAULT 'Reaching out',
       contact_name VARCHAR(100),
       contact_info VARCHAR(200),
       created_at   DATETIME NOT NULL,
       updated_at   DATETIME NOT NULL
   );

   CREATE TABLE promo_scripts (
       id              INT AUTO_INCREMENT PRIMARY KEY,
       partner_type    VARCHAR(30) NOT NULL UNIQUE,
       message         TEXT NOT NULL,
       forward_message TEXT,
       updated_at      DATETIME NULL
   );
   ```
   (Add `submitted_by_id INT NULL` to `ideas`, with no foreign key, for the same reason. The old typed-name columns only need copying if older rows still use them.)
2. Add the `MySqlConnector` NuGet package and write `MySqlIdeaRepository : IIdeaRepository`.
   - `AddAsync`: insert the idea, get its new id, insert each link, all in one transaction.
   - `ApproveAsync`: in one transaction, `UPDATE ideas SET review_status = 'Approved',
     reviewed_at = NOW() WHERE id = @id AND review_status = 'Pending'` (0 rows changed = already
     decided), then `INSERT INTO production_items (idea_id, updated_at) VALUES (@id, NOW())`.
   - `DeleteRejectedBeforeAsync`: `DELETE FROM ideas WHERE review_status = 'Rejected' AND
     reviewed_at < @cutoff` (the CASCADE removes the links).
3. Write `MySqlEmployeeRepository` and `MySqlPromoRepository` the same way, and in `Program.cs`
   change each `AddSingleton<I...Repository, Csv...Repository>()` line to the MySQL version.

## Known mockup shortcuts

- Logins are kept in the API's memory, so restarting the API logs everyone out. There's no limit
  on wrong password attempts yet.
- `GET /api/ideas/files/{linkId}` (uploads, turned off) isn't behind the login, because `<img>` and
  `<video>` tags can't send the token. It needs its own check if uploads are turned back on.
- The app can't check that a link actually works or is shared correctly.
- If two people edit the same tracker item, promo or script at the same time, the last one to click
  Save wins.
- New ids are "highest id + 1", so if the newest idea is deleted its id gets reused. MySQL's
  AUTO_INCREMENT never reuses ids, so this goes away after the move.
