# Content Ideas – Mockup 1

Users submit a content idea (title, description, caption, platforms, hashtags) with any number
of **links** to photos/videos. Reviewers open the links, then approve or deny the whole idea.

## How the data is stored

**No photo or video files are stored by this app.** Users put their files somewhere online
(the company OneDrive folder), copy the share link, and paste it into the form. Only the link
is saved.

```
api/Data/ideas.csv      <- one row per idea
api/Data/links.csv      <- one row per photo/video link, linked to its idea by IdeaId
```

| ideas.csv | links.csv |
|---|---|
| Id | Id |
| Name | IdeaId → ideas.Id |
| Title | Type (Photo / Video) |
| Description | Label (optional) |
| Caption | Url (must start with https://) |
| Platforms (e.g. `Instagram;TikTok`) | |
| Hashtags | |
| CreatedAt | |
| ReviewStatus (Pending / Approved / Denied) | |
| DenialReason (only for Denied) | |
| ReviewedAt | |

## How review works

- New ideas start as **Pending**.
- A reviewer opens the links (each opens in a new tab, where OneDrive offers Download / Add
  to My files), then clicks **Approve**, or writes a reason and clicks **Deny**.
- **Decisions are final.** An approved or denied idea can't be changed.
- **Delete** permanently removes an idea and all its links, whatever its status. It can't be
  undone, so admins can clear out old ideas to free up space.
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

## Pages

- `client/index.html`: submit an idea. "+ Add a link" adds a row for each photo/video link.
- `client/ideas.html`: review page. Shows "Waiting for review" first, with filters for
  Approved and Denied.

## API endpoints

| Method | URL | Body (JSON) | What it does |
|---|---|---|---|
| GET | `/api/ideas?view=Pending` | | List ideas (Pending, Approved or Denied) |
| POST | `/api/ideas` | idea with `links` array | Submit an idea |
| POST | `/api/ideas/{id}/approve` | | Approve (Pending only) |
| POST | `/api/ideas/{id}/deny` | `{ "reason": "..." }` | Deny with a reason (Pending only) |
| DELETE | `/api/ideas/{id}` | | Permanently delete the idea and its links |

## Things to know about links

- A link only works while the file exists and is shared. If someone deletes the file, moves
  it, or leaves the company, the link breaks. **Keep all files in one shared company folder**
  so the company owns them, not individual people.
- Set the sharing to "People in your organization" (or "Anyone with the link") so reviewers
  don't see "You need permission."
- Later, the form could upload a file to OneDrive automatically (Microsoft Graph API) and save
  the returned link. That needs the company's Microsoft 365 admin to register the app.

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
- New ids are "highest id + 1", so if the newest idea is deleted its id gets reused. MySQL's
  AUTO_INCREMENT never reuses ids, so this goes away after the move.
