namespace api.Storage;

// Where uploaded photos/videos go. The controller only talks to this interface, so the
// storage could change later (e.g. a different cloud) by writing a new class and changing
// one line in Program.cs, the same way CSV will change to MySQL.
public interface IFileStorage
{
    // False until the OneDrive settings are filled in (see README "Setting up OneDrive uploads").
    bool IsConfigured { get; }

    // Saves the file and returns its id in storage plus a web address people can open.
    Task<StoredFile> UploadAsync(Stream content, long length, string fileName, CancellationToken cancellationToken);

    // A short-lived direct address to the file's contents, used to play/show it on the review page.
    Task<string> GetContentUrlAsync(string fileId, CancellationToken cancellationToken);

    // Only used to clean up when a submission fails partway through.
    Task DeleteAsync(string fileId, CancellationToken cancellationToken);
}

public record StoredFile(string Id, string WebUrl);
