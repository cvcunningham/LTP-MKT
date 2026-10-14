# LTP Marketing – Mockup 1

Tracks the progress and status of the business's marketing content. Employees submit social media
content ideas; the Marketing Chair/Executive approves or denies them; approved ideas are tracked through
production to posting on the Production Tracker and Content Calendar; and the Promo Calendar plans
promos with sororities, student orgs and businesses.

Styled with **Bootstrap 5** (from a CDN); `style.css` only holds what Bootstrap can't do (the calendar grid).

## Pages

| Page | File | What it's for |
|---|---|---|
| Ideas Form | `client/index.html` | One form = one idea. Your name comes from `employees.csv`; campus is Alabama / Ole Miss / Other. |
| Ideas Bank | `client/bank.html` | "Waiting for review" cards with **Approve**, **Deny** and **Delete**, then "Denied" cards. |
| Production Tracker | `client/tracker.html` | Approved ideas: Filmer / Editor / Reviewer / Poster (employee dropdowns), target post date, status. Filter: In progress / Completed. |
| Content Calendar | `client/content-calendar.html` | Month calendar of the tracker's target post dates, colored by status. Click for details. |
| Promo Calendar | `client/promo-calendar.html` | Outreach scripts (Copy, Edit, auto-fill from a promo), the Promo List, and a calendar of the promos. |

Every page has the same navbar (built by `renderNavbar()` in `resources/scripts/common.js`). Both
calendars use `resources/scripts/calendar.js`.

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
- For now anyone can do all of this (no login yet). Later only the Marketing Chair/Executive will review.

## Production Tracker

Each item shows, from the approved idea (can't be changed here): content name, campus, content
type, content category, caption, and the idea's links as its content files.

These can be changed anytime, then saved with **Save**:

- Filmer, Editor, Reviewer/approver, Poster (who posts it): dropdowns of every employee, grouped by campus
- Target post date: a date picker (items are listed soonest first; no date goes last)
- Status: Needs filming → Needs editing → Needs final review → Scheduled → Completed

Items marked **Completed** move to the "Completed" filter. They stay until someone deletes them.
Every item with a target post date also shows on the **Content Calendar**.

## Promo Calendar

- **Outreach scripts**: one per partner type (Sorority, Student org, Business), each with a message to
  the partner and a message for them to forward to their members. Choose a promo in "Fill in from promo"
  and the placeholders are filled in: `[SORORITY]` / `[ORGANIZATION]` / `[BUSINESS]` (its name), `[CODE]`,
  `[DAY/DATE]`, and `[APP]` (Alabama: Bama Dining Mobile Order app; Ole Miss: GrubHub app; the list is
  `ORDERING_APPS` in `promo-calendar.js`). **Copy** copies the filled-in text. **Edit wording** changes
  the saved script.
- **Promo List**: name, partner type, campus, date, promo code, status (Reaching out → Confirmed →
  Completed, or Cancelled) and partner contact. Add, edit or delete (permanent).
- **Calendar**: the same promos on their dates, colored by status. Click one to edit it.

## How the data is stored

**The app never keeps photo or video files on its own server.** People put files in the
company OneDrive folder and paste the share link. Only the link is saved.

```
api/Data/ideas.csv       <- one row per idea
api/Data/links.csv       <- one row per photo/video link, linked to its idea by IdeaId
api/Data/production.csv  <- one row per approved idea, linked to its idea by IdeaId
api/Data/employees.csv   <- one row per employee (edited by hand for now)
api/Data/promos.csv      <- one row per promo
api/Data/scripts.csv     <- one outreach script per partner type
```

| ideas.csv | links.csv | production.csv |
|---|---|---|
| Id | Id | Id |
| Name (typed name, older ideas only) | IdeaId → ideas.Id | IdeaId → ideas.Id |
| Title (content name) | Type (Photo / Video) | Filmer, Editor, Reviewer, Poster (typed names, older rows only) |
| Concept | Label (optional) | TargetPostDate (`yyyy-MM-dd`, may be empty) |
| Caption | Url (must start with https://) | Status |
| Platforms (e.g. `Instagram;TikTok`) | DriveItemId (OneDrive file id, only for uploads; see below) | UpdatedAt (last saved) |
| Hashtags | | FilmerId → employees.Id |
| CreatedAt (the timestamp) | | EditorId → employees.Id |
| ReviewStatus (Pending / Approved / Rejected) | | ReviewerId → employees.Id |
| DenialReason (only on older ideas) | | PosterId → employees.Id |
| ReviewedAt | | |
| Campus | | |
| ContentType (the format, e.g. Reel) | | |
| ContentCategory (the topic, e.g. Food) | | |
| WhyItCouldWork | | |
| SubmittedById → employees.Id | | |

| employees.csv | promos.csv | scripts.csv |
|---|---|---|
| Id | Id | Id |
| Name | Name (the partner) | PartnerType |
| Email | PartnerType (Sorority / Student org / Business) | Message (to the partner) |
| Phone | Campus | ForwardMessage (for their members) |
| Campus | Date (`yyyy-MM-dd`) | UpdatedAt |
| Semester (1, 2, 3, ...) | PromoCode | |
| Role (Member / Marketing Chair / Executive) | Status | |
| | ContactName, ContactInfo | |
| | CreatedAt, UpdatedAt | |

New columns are always added at the **end** of a file. When the API starts, it upgrades older
CSV files by adding the missing columns (empty), so no data is lost. Older tracker statuses are read
as the new ones (Not started/Filming → Needs filming, Editing → Needs editing, Ready for review →
Needs final review, Posted → Completed).

**employees.csv** is edited by hand (no Employees page yet). Changes show up the next time a page
loads, without restarting the API. Don't reuse or delete an employee's Id while ideas or tracker items
use it; a missing Id shows as "(former employee #3)".

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

## API endpoints

| Method | URL | Body | What it does |
|---|---|---|---|
| GET | `/api/ideas` | | The Ideas Bank: ideas waiting for review + denied ideas (with `deletesOn`) |
| POST | `/api/ideas` | multipart form: `idea` = the idea as JSON (with `submittedById` and a `links` array) | Submit an idea |
| POST | `/api/ideas/{id}/approve` | | Approve (Pending only); creates its tracker item |
| POST | `/api/ideas/{id}/reject` | | Deny (Pending only) |
| DELETE | `/api/ideas/{id}` | | Permanently delete the idea, its links and its tracker item |
| GET | `/api/ideas/files/{linkId}` | | For uploaded files only (see below) |
| GET | `/api/production?view=InProgress` | | Tracker items not yet Completed (`view=Completed` for completed ones, `view=All` for the Content Calendar) |
| GET | `/api/production/statuses` | | The Status choices, in order |
| PUT | `/api/production/{id}` | JSON: `filmerId`, `editorId`, `reviewerId`, `posterId`, `targetPostDate`, `status` | Save production details |
| DELETE | `/api/production/{id}` | | Permanently delete the item, its idea and links |
| GET | `/api/employees` | | Every employee, by campus then name |
| GET | `/api/promos` | | Every promo, soonest first |
| GET | `/api/promos/options` | | The Partner type and Status choices |
| POST | `/api/promos` | JSON: `name`, `partnerType`, `campus`, `date`, `promoCode`, `status`, `contactName`, `contactInfo` | Add a promo |
| PUT | `/api/promos/{id}` | same as POST | Save a promo |
| DELETE | `/api/promos/{id}` | | Permanently delete a promo |
| GET | `/api/promos/scripts` | | The outreach scripts |
| PUT | `/api/promos/scripts/{id}` | JSON: `message`, `forwardMessage` | Save a script's wording |

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
       concept           TEXT,
       why_it_could_work TEXT,
       caption           TEXT,
       platforms         VARCHAR(100),
       hashtags          VARCHAR(500),
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
       semester INT NULL,
       role     VARCHAR(30) NOT NULL DEFAULT 'Member'
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
       FOREIGN KEY (idea_id) REFERENCES ideas(id) ON DELETE CASCADE,
       FOREIGN KEY (filmer_id) REFERENCES employees(id),
       FOREIGN KEY (editor_id) REFERENCES employees(id),
       FOREIGN KEY (reviewer_id) REFERENCES employees(id),
       FOREIGN KEY (poster_id) REFERENCES employees(id)
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
   (Create `employees` before `ideas`, and add `submitted_by_id INT NULL REFERENCES employees(id)` to
   `ideas`. The old typed-name columns only need copying if older rows still use them.)
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

- No login: anyone can pick any name, and anyone can approve, deny, edit or delete.
- The app can't check that a link actually works or is shared correctly.
- If two people edit the same tracker item, promo or script at the same time, the last one to click
  Save wins.
- New ids are "highest id + 1", so if the newest idea is deleted its id gets reused. MySQL's
  AUTO_INCREMENT never reuses ids, so this goes away after the move.
