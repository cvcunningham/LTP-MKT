# Content Ideas – Mockup 1

Staff submit social media content ideas with links to photos/videos. Reviewers approve or
reject them. Approved ideas move to a Production Tracker, where the team records who films,
edits, reviews and posts each one.

## Pages

| Page | File | What it's for |
|---|---|---|
| Submit an idea | `client/index.html` | One form = one idea. "+ Add a link" adds a row for each photo/video link. |
| Idea Bank | `client/bank.html` | "Waiting for review" cards with **Approve**, **Reject** and **Delete**, then "Rejected" cards. |
| Production Tracker | `client/tracker.html` | Approved ideas and their production details. Filter: In progress / Posted. |

The pages link to each other at the top and share `resources/scripts/common.js`.
Each idea is shown as a **card** (a box with a border); that's the only styling, in `style.css`.

An idea card in the Idea Bank shows: content name, who submitted it and when (the timestamp),
campus, content category, concept, why it could work, the links, and the decision.

## How an idea moves through the site

```
Submit an idea ──► Idea Bank: Waiting for review ──► Approve ──► Production Tracker (Not started ... Posted)
                                                  │
                                                  └──► Reject ──► Idea Bank: Rejected
                                                                  └─ deleted automatically after 30 days
```

- New ideas are **Pending** and show under "Waiting for review" in the Idea Bank.
- **Approve** moves the idea to the Production Tracker (status "Not started"). It leaves the Idea Bank.
- **Reject** needs no reason. The idea moves to "Rejected" in the Idea Bank, showing the date
  it will be deleted, and the app deletes it (with its links) **30 days** after the decision.
  The check runs whenever the Idea Bank is loaded.
- **Decisions are final.** An approved or rejected idea can't be changed.
- **Delete** (Idea Bank or Production Tracker) permanently removes the idea, its links and its
  tracker entry. It can't be undone.
- For now anyone can do all of this (no login yet).

## Production Tracker

Each item shows, from the approved idea (can't be changed here): content name, campus, content
type, content category, caption, and the idea's links as its content files.

These can be filled in and changed anytime, then saved with **Save**:

- Filmer, Editor, Reviewer/approver, Posting (who posts it): free text
- Target post date: a date picker (items are listed soonest first; no date goes last)
- Status: Not started → Filming → Editing → Ready for review → Scheduled → Posted

Items marked **Posted** move to the "Posted" filter. They stay until someone deletes them.

## How the data is stored

**The app never keeps photo or video files on its own server.** People put files in the
company OneDrive folder and paste the share link. Only the link is saved.

```
api/Data/ideas.csv       <- one row per idea
api/Data/links.csv       <- one row per photo/video link, linked to its idea by IdeaId
api/Data/production.csv  <- one row per approved idea, linked to its idea by IdeaId
```

| ideas.csv | links.csv | production.csv |
|---|---|---|
| Id | Id | Id |
| Name (who submitted it) | IdeaId → ideas.Id | IdeaId → ideas.Id |
| Title (content name) | Type (Photo / Video) | Filmer |
| Concept | Label (optional) | Editor |
| Caption | Url (must start with https://) | Reviewer (reviewer/approver) |
| Platforms (e.g. `Instagram;TikTok`) | DriveItemId (OneDrive file id, only for uploads; see below) | Poster (who posts it) |
| Hashtags | | TargetPostDate (`yyyy-MM-dd`, may be empty) |
| CreatedAt (the timestamp) | | Status |
| ReviewStatus (Pending / Approved / Rejected) | | UpdatedAt (last saved) |
| DenialReason (only on older ideas) | | |
| ReviewedAt | | |
| Campus | | |
| ContentType (the format, e.g. Reel) | | |
| ContentCategory (the topic, e.g. Food) | | |
| WhyItCouldWork | | |

New columns are always added at the **end** of a file. When the API starts, it upgrades older
CSV files by adding the missing columns (empty), so no data is lost.

Ideas from before the Idea Bank/Tracker were added are left out on purpose: the old Approved
idea ("Bowl Image") has no tracker entry, and the old "Denied" one isn't "Rejected", so neither
shows on any page. Delete those rows from the CSV files if you no longer need them.

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
| GET | `/api/ideas` | | The Idea Bank: ideas waiting for review + rejected ideas (with `deletesOn`) |
| POST | `/api/ideas` | multipart form: `idea` = the idea as JSON (with a `links` array) | Submit an idea |
| POST | `/api/ideas/{id}/approve` | | Approve (Pending only); creates its tracker item |
| POST | `/api/ideas/{id}/reject` | | Reject (Pending only) |
| DELETE | `/api/ideas/{id}` | | Permanently delete the idea, its links and its tracker item |
| GET | `/api/production?view=InProgress` | | Tracker items not yet Posted (`view=Posted` for posted ones) |
| GET | `/api/production/statuses` | | The Status choices, in order |
| PUT | `/api/production/{id}` | JSON: `filmer`, `editor`, `reviewer`, `poster`, `targetPostDate`, `status` | Save production details |
| DELETE | `/api/production/{id}` | | Permanently delete the item, its idea and links |
| GET | `/api/ideas/files/{linkId}` | | For uploaded files only (see below) |

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

   CREATE TABLE production_items (
       id               INT AUTO_INCREMENT PRIMARY KEY,
       idea_id          INT NOT NULL UNIQUE,   -- one tracker item per approved idea
       filmer           VARCHAR(100),
       editor           VARCHAR(100),
       reviewer         VARCHAR(100),
       poster           VARCHAR(100),
       target_post_date DATE NULL,
       status           VARCHAR(30) NOT NULL DEFAULT 'Not started',
       updated_at       DATETIME NOT NULL,
       FOREIGN KEY (idea_id) REFERENCES ideas(id) ON DELETE CASCADE
   );
   ```
2. Add the `MySqlConnector` NuGet package and write `MySqlIdeaRepository : IIdeaRepository`.
   - `AddAsync`: insert the idea, get its new id, insert each link, all in one transaction.
   - `ApproveAsync`: in one transaction, `UPDATE ideas SET review_status = 'Approved',
     reviewed_at = NOW() WHERE id = @id AND review_status = 'Pending'` (0 rows changed = already
     decided), then `INSERT INTO production_items (idea_id, updated_at) VALUES (@id, NOW())`.
   - `DeleteRejectedBeforeAsync`: `DELETE FROM ideas WHERE review_status = 'Rejected' AND
     reviewed_at < @cutoff` (the CASCADE removes the links).
3. In `Program.cs`, change
   `AddSingleton<IIdeaRepository, CsvIdeaRepository>()` to use `MySqlIdeaRepository`.

## Known mockup shortcuts

- No login: names are just text boxes, and anyone can approve, reject, edit or delete.
- The app can't check that a link actually works or is shared correctly.
- If two people edit the same tracker item at the same time, the last one to click Save wins.
- New ids are "highest id + 1", so if the newest idea is deleted its id gets reused. MySQL's
  AUTO_INCREMENT never reuses ids, so this goes away after the move.
