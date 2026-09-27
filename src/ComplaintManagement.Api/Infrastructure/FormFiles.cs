using ComplaintManagement.Application.Attachments;

namespace ComplaintManagement.Api.Infrastructure;

public static class FormFiles
{
    /// <summary>Largest multipart request accepted: five 5 MB files plus form fields.</summary>
    public const long MaxRequestBytes = 30 * 1024 * 1024;

    public static IReadOnlyList<UploadFile> ToUploads(this IFormFileCollection? files) =>
        files is null ? [] : files.Select(f => new UploadFile(f.FileName, f.Length, f.OpenReadStream)).ToList();
}
