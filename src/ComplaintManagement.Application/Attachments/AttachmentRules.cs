using System.Text;
using ComplaintManagement.Domain.Common;

namespace ComplaintManagement.Application.Attachments;

public sealed class AttachmentOptions
{
    public const string SectionName = "Attachments";

    /// <summary>Largest single file accepted.</summary>
    public long MaxFileBytes { get; set; } = 5 * 1024 * 1024;

    /// <summary>Most files in one upload (and with one customer complaint).</summary>
    public int MaxFilesPerUpload { get; set; } = 5;

    /// <summary>Most files a complaint may hold in total.</summary>
    public int MaxFilesPerComplaint { get; set; } = 20;
}

/// <summary>A file received over HTTP, independent of ASP.NET types.</summary>
public sealed record UploadFile(string FileName, long Length, Func<Stream> OpenRead);

/// <summary>
/// Allowed attachment types (PDF, JPG/JPEG, PNG, XLS, XLSX). A file is accepted only when its extension
/// is allowed AND its leading bytes match that type, so a renamed executable is refused. The content type
/// stored and served is the detected one, never what the client claimed.
/// </summary>
public static class AttachmentRules
{
    private sealed record FileKind(string ContentType, byte[] Signature);

    private static readonly FileKind Pdf = new("application/pdf", "%PDF-"u8.ToArray());
    private static readonly FileKind Jpeg = new("image/jpeg", [0xFF, 0xD8, 0xFF]);
    private static readonly FileKind Png = new("image/png", [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
    private static readonly FileKind Xls = new("application/vnd.ms-excel", [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1]);
    private static readonly FileKind Xlsx = new("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", [0x50, 0x4B, 0x03, 0x04]);

    private static readonly Dictionary<string, FileKind> ByExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = Pdf, [".jpg"] = Jpeg, [".jpeg"] = Jpeg, [".png"] = Png, [".xls"] = Xls, [".xlsx"] = Xlsx,
    };

    public static IReadOnlyCollection<string> AllowedExtensions => ByExtension.Keys;

    public const int HeaderBytes = 8;

    /// <summary>Validates one file and returns its safe display name and detected content type.</summary>
    public static (string SafeName, string ContentType) Validate(string fileName, long length, ReadOnlySpan<byte> header, AttachmentOptions options)
    {
        var name = SafeFileName(fileName);
        var extension = Path.GetExtension(name);
        if (!ByExtension.TryGetValue(extension, out var kind))
            throw new DomainException("attachment.type_not_allowed", $"\"{name}\" is not an allowed file type. Use PDF, JPG, PNG, XLS or XLSX.");
        if (length <= 0)
            throw new DomainException("attachment.empty", $"\"{name}\" is empty.");
        if (length > options.MaxFileBytes)
            throw new DomainException("attachment.too_large", $"\"{name}\" is larger than {options.MaxFileBytes / (1024 * 1024)} MB.");
        if (!header.StartsWith(kind.Signature))
            throw new DomainException("attachment.content_mismatch", $"\"{name}\" does not look like a real {extension.TrimStart('.').ToUpperInvariant()} file.");
        return (name, kind.ContentType);
    }

    /// <summary>
    /// Display name only: strips any path, control and reserved characters, and caps the length while keeping
    /// the extension. It is never used to build a storage path.
    /// </summary>
    public static string SafeFileName(string? fileName)
    {
        var raw = (fileName ?? "").Replace('\\', '/');
        raw = raw[(raw.LastIndexOf('/') + 1)..];
        var sb = new StringBuilder(raw.Length);
        foreach (var ch in raw)
            sb.Append(char.IsControl(ch) || "<>:\"|?*".Contains(ch) ? '_' : ch);
        var name = sb.ToString().Trim().TrimStart('.');
        if (name.Length == 0) name = "attachment";

        const int max = 150;
        if (name.Length > max)
        {
            var ext = Path.GetExtension(name);
            name = name[..(max - ext.Length)] + ext;
        }
        return name;
    }
}
