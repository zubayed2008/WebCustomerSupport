namespace SupportDesk.Application.Abstractions;

public sealed record StoredFile(string StorageKey, string Sha256, long SizeBytes);

public interface IFileStorage
{
    Task<StoredFile> SaveAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken);

    Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken);

    Task DeleteAsync(string storageKey, CancellationToken cancellationToken);
}
