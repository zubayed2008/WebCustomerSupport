using SupportDesk.Application.Validation;
using SupportDesk.Domain.Common;

namespace SupportDesk.UnitTests;

public sealed class FileUploadValidatorTests
{
    private static readonly byte[] PdfHeader = "%PDF-"u8.ToArray();

    [Fact]
    public void Validate_AcceptsPdfWithMatchingSignature()
    {
        var contentType = FileUploadValidator.Validate("report.pdf", "application/pdf", 1024, PdfHeader);

        Assert.Equal("application/pdf", contentType);
    }

    [Fact]
    public void Validate_RejectsDisallowedExtension()
    {
        Assert.Throws<ValidationFailedException>(() =>
            FileUploadValidator.Validate("script.exe", "application/octet-stream", 10, [0x4D, 0x5A]));
    }

    [Fact]
    public void Validate_RejectsMismatchedSignature()
    {
        Assert.Throws<ValidationFailedException>(() =>
            FileUploadValidator.Validate("fake.pdf", "application/pdf", 100, "not-a-pdf"u8));
    }

    [Fact]
    public void Validate_RejectsDeclaredContentTypeMismatch()
    {
        Assert.Throws<ValidationFailedException>(() =>
            FileUploadValidator.Validate("image.png", "text/plain", 100, [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]));
    }

    [Fact]
    public void Validate_RejectsOversizedFile()
    {
        Assert.Throws<ValidationFailedException>(() =>
            FileUploadValidator.Validate("report.pdf", "application/pdf", FileUploadValidator.MaxSizeBytes + 1, PdfHeader));
    }

    [Fact]
    public void Validate_RejectsEmptyFile()
    {
        Assert.Throws<ValidationFailedException>(() =>
            FileUploadValidator.Validate("report.pdf", "application/pdf", 0, PdfHeader));
    }

    [Fact]
    public void Validate_AcceptsTextWithoutNullBytes()
    {
        var contentType = FileUploadValidator.Validate("notes.txt", "text/plain", 12, "hello world"u8);

        Assert.Equal("text/plain", contentType);
    }

    [Fact]
    public void Validate_RejectsBinaryContentDisguisedAsText()
    {
        Assert.Throws<ValidationFailedException>(() =>
            FileUploadValidator.Validate("notes.txt", "text/plain", 4, [0x68, 0x00, 0x69, 0x00]));
    }
}
