using SupportDesk.Domain.Common;

namespace SupportDesk.Application.Validation;

public static class FileUploadValidator
{
    public const long MaxSizeBytes = 10 * 1024 * 1024;

    private static readonly IReadOnlyDictionary<string, string[]> Allowed =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = ["application/pdf"],
            [".png"] = ["image/png"],
            [".jpg"] = ["image/jpeg"],
            [".jpeg"] = ["image/jpeg"],
            [".webp"] = ["image/webp"],
            [".txt"] = ["text/plain"],
            [".csv"] = ["text/csv", "text/plain", "application/vnd.ms-excel"],
        };

    public static string Validate(string fileName, string contentType, long sizeBytes, ReadOnlySpan<byte> header)
    {
        if (sizeBytes <= 0)
        {
            throw new ValidationFailedException("The uploaded file is empty.");
        }

        if (sizeBytes > MaxSizeBytes)
        {
            throw new ValidationFailedException($"Attachments cannot exceed {MaxSizeBytes / (1024 * 1024)} MB.");
        }

        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(extension) || !Allowed.TryGetValue(extension, out var allowedContentTypes))
        {
            throw new ValidationFailedException("This file type is not allowed.");
        }

        var normalizedContentType = contentType.Split(';')[0].Trim();
        if (!allowedContentTypes.Contains(normalizedContentType, StringComparer.OrdinalIgnoreCase))
        {
            throw new ValidationFailedException("The declared content type is not allowed for this file extension.");
        }

        if (!SignatureMatches(extension, header))
        {
            throw new ValidationFailedException("The file content does not match its extension.");
        }

        return normalizedContentType;
    }

    private static bool SignatureMatches(string extension, ReadOnlySpan<byte> header)
    {
        return extension.ToLowerInvariant() switch
        {
            ".pdf" => header.Length >= 5 && header[..5].SequenceEqual("%PDF-"u8),
            ".png" => header.Length >= 8 && header[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
            ".jpg" or ".jpeg" => header.Length >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            ".webp" => header.Length >= 12 && header[..4].SequenceEqual("RIFF"u8) && header[8..12].SequenceEqual("WEBP"u8),
            ".txt" or ".csv" => !header.Contains((byte)0),
            _ => false,
        };
    }
}
