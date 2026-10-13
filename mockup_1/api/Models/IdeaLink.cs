namespace api.Models;

// One row in Data/links.csv (later: the `idea_links` table in MySQL).
// Only the link is stored; the photo/video itself lives wherever the link points (e.g. OneDrive).
// A row is either a pasted link (DriveItemId empty) or a file uploaded through the app
// to the company OneDrive folder (DriveItemId = the file's id there).
public class IdeaLink
{
    public const string Photo = "Photo";
    public const string Video = "Video";

    public int Id { get; set; }
    public int IdeaId { get; set; }
    public string Type { get; set; } = Photo; // Photo or Video
    public string Label { get; set; } = "";   // optional, e.g. "Logo draft"
    public string Url { get; set; } = "";     // always starts with https://
    public string DriveItemId { get; set; } = "";

    public bool IsUpload => DriveItemId != "";
}
