# Content Ideas – Mockup 1

Users submit a content idea (title, description, caption, platforms, hashtags) with any number
of photos/videos, either **uploaded** from their computer or as **pasted links**. Reviewers
watch/open them, then approve or deny the whole idea.

## How the data is stored

**The app never keeps photo or video files on its own server.** There are two ways to attach one:

- **Upload a file** (up to 500 MB each, up to 2 GB per idea): the API sends it to the company's
  SharePoint/OneDrive folder through Microsoft Graph and saves the file's OneDrive address and id.
  Allowed types: .jpg .jpeg .png .heic .gif (Photo) and .mp4 .mov .m4v (Video).
- **Paste a link** to a file that's already in OneDrive. Only the link is saved.

```
api/Data/ideas.csv      <- one row per idea
api/Data/links.csv      <- one row per photo/video (uploaded or pasted), linked to its idea by IdeaId
```

| ideas.csv | links.csv |
|---|---|
| Id | Id |
| Name | IdeaId → ideas.Id |
| Title | Type (Photo / Video) |
| Description | Label (optional) |
| Caption | Url (must start with https://) |
| Platforms (e.g. `Instagram;TikTok`) | DriveItemId (the file's OneDrive id; empty for pasted links) |
| Hashtags | |
| CreatedAt | |
| ReviewStatus (Pending / Approved / Denied) | |
| DenialReason (only for Denied) | |
| ReviewedAt | |

## How review works

- New ideas start as **Pending**.
- A reviewer opens the links (each opens in a new tab, where OneDrive offers Download / Add
  to My files), then clicks **Approve**, or writes a reason and clicks **Deny**.
- Uploaded files also play (videos) or show (photos) right on the review page, with a
  **Download** link. iPhone .mov/.heic files may not preview in every browser; Download still works.
- **Decisions are final.** An approved or denied idea can't be changed.
- **Delete** permanently removes an idea and all its links, whatever its status. It can't be
  undone, so admins can clear out old ideas to free up space. **Uploaded files are kept** in the
  OneDrive folder; delete them there if they're no longer needed.
- For now anyone can review (no login yet).

## Running it

1. In a terminal:
   ```
   cd api
   dotnet run
   ```
   Always run it from inside `api/`, because `Data/` is found relative to it.
   The API runs at http://localhost:5050.
2. Open `client/index.html` with Live Server (or just double-click it).

Uploading files only works after the OneDrive setup below. Until then, pasting links still
works, and trying to upload shows "Uploading files isn't set up yet."

## Setting up OneDrive uploads

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
4. **Fill in the settings.** Non-secret values go in `api/appsettings.json`:
   ```json
   "OneDrive": {
     "TenantId": "...",
     "ClientId": "...",
     "DriveId": "...",
     "FolderPath": "Content Ideas"
   }
   ```
   `FolderPath` is the folder inside the library where uploads go (it's created if missing).
   The **secret never goes in a file in git**. Store it with .NET's user-secrets (run in `api/`):
   ```
   dotnet user-secrets set "OneDrive:ClientSecret" "the-secret-value"
   ```
   User-secrets are saved in your Windows user profile, not in this repo, so each person running
   the API sets it once. On a real server, set the environment variable `OneDrive__ClientSecret`.
   Client secrets expire (you choose 6–24 months), so set a reminder to make a new one.
5. Restart the API. Uploaded files are named like `2026-10-12 beach-video.mp4`; if a name is
   taken, OneDrive adds a number instead of overwriting.

How an upload works: the browser sends the form and files to the API (the page shows
"Uploading... 45%"), then the API sends each file to OneDrive in 10 MB pieces (Graph's
"upload session"). If anything fails, **nothing is saved**: any files already sent for that
submission are removed from OneDrive, and the form stays filled in so the person can try again.

## Pages

- `client/index.html`: submit an idea. "+ Upload a file" adds a row to pick a file from the
  computer; "+ Add a link" adds a row for a pasted OneDrive link.
- `client/ideas.html`: review page. Shows "Waiting for review" first, with filters for
  Approved and Denied.

## API endpoints

| Method | URL | Body (JSON) | What it does |
|---|---|---|---|
| GET | `/api/ideas?view=Pending` | | List ideas (Pending, Approved or Denied) |
| POST | `/api/ideas` | multipart form: `idea` = JSON (with `links` and `uploads` arrays), `files` = the files, same order as `uploads` | Submit an idea |
| GET | `/api/ideas/files/{linkId}` | | Redirects to a short-lived OneDrive address for an uploaded file (used to play/show/download it) |
| POST | `/api/ideas/{id}/approve` | | Approve (Pending only) |
| POST | `/api/ideas/{id}/deny` | `{ "reason": "..." }` | Deny with a reason (Pending only) |
| DELETE | `/api/ideas/{id}` | | Permanently delete the idea and its links |

## Things to know about links

- A link only works while the file exists and is shared. If someone deletes the file, moves
  it, or leaves the company, the link breaks. **Keep all files in one shared company folder**
  so the company owns them, not individual people.
- Set the sharing to "People in your organization" (or "Anyone with the link") so reviewers
  don't see "You need permission."
- Uploaded files go into that one shared folder automatically, so the company owns them.

## Moving to MySQL later

1. Create the tables:
   ```sql
   CREATE TABLE ideas (
       id            INT AUTO_INCREMENT PRIMARY KEY,
       name          VARCHAR(100),
       title         VARCHAR(200) NOT NULL,
       description   TEXT,
       caption       TEXT,
       platforms     VARCHAR(100),
       hashtags      VARCHAR(500),
       created_at    DATETIME NOT NULL,
       review_status VARCHAR(20) NOT NULL DEFAULT 'Pending',
       denial_reason TEXT,
       reviewed_at   DATETIME NULL
   );

   CREATE TABLE idea_links (
       id      INT AUTO_INCREMENT PRIMARY KEY,
       idea_id INT NOT NULL,
       type    VARCHAR(10) NOT NULL,   -- 'Photo' or 'Video'
       label   VARCHAR(200),
       url     VARCHAR(2048) NOT NULL,
       drive_item_id VARCHAR(100) NOT NULL DEFAULT '',  -- OneDrive file id; '' for pasted links
       FOREIGN KEY (idea_id) REFERENCES ideas(id) ON DELETE CASCADE  -- deleting an idea deletes its links
   );
   ```
2. Add the `MySqlConnector` NuGet package and write `MySqlIdeaRepository : IIdeaRepository`.
   - `AddAsync`: insert the idea, get its new id, insert each link, all in one transaction.
   - `SetReviewAsync`: `UPDATE ideas SET review_status = @s, denial_reason = @r, reviewed_at = NOW()
     WHERE id = @id AND review_status = 'Pending'` (0 rows changed = already decided).
3. In `Program.cs`, change
   `AddSingleton<IIdeaRepository, CsvIdeaRepository>()` to use `MySqlIdeaRepository`.

## Known mockup shortcuts

- No login: the name is just a text box, and anyone can approve, deny or delete.
- The app can't check that a link actually works or is shared correctly.
- Anyone who can reach the API can play/download uploaded files through `/api/ideas/files/...`
  (the API fetches them with its own permission). Login will need to protect this too.
- If someone deletes or moves an uploaded file in OneDrive, its preview on the review page breaks.
- New ids are "highest id + 1", so if the newest idea is deleted its id gets reused. MySQL's
  AUTO_INCREMENT never reuses ids, so this goes away after the move.
