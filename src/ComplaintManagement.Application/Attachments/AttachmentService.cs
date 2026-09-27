using System.Security.Cryptography;
using ComplaintManagement.Application.Common.Exceptions;
using ComplaintManagement.Application.Common.Interfaces;
using ComplaintManagement.Application.Common.Security;
using ComplaintManagement.Contracts.Responses;
using ComplaintManagement.Domain.Common;
using ComplaintManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ComplaintManagement.Application.Attachments;

public sealed record AttachmentDownload(Stream Content, string FileName, string ContentType, long Length);

public interface IAttachmentService
{
    /// <summary>Staff upload to a complaint in their scope.</summary>
    Task<IReadOnlyList<AttachmentItem>> AddAsync(Guid complaintId, IReadOnlyList<UploadFile> files, CancellationToken ct);

    /// <summary>Opens an attachment for download. Needs full customer-data access; audited.</summary>
    Task<AttachmentDownload> OpenAsync(Guid complaintId, Guid attachmentId, CancellationToken ct);
}

public sealed class AttachmentService(
    IApplicationDbContext db,
    ICurrentUser user,
    IAuditLogger audit,
    AttachmentStore store,
    IFileStorage storage,
    IOptions<AttachmentOptions> options) : IAttachmentService
{
    private const string Module = "Attachment";

    public async Task<IReadOnlyList<AttachmentItem>> AddAsync(Guid complaintId, IReadOnlyList<UploadFile> files, CancellationToken ct)
    {
        var complaint = await db.Complaints.VisibleTo(user).FirstOrDefaultAsync(c => c.Id == complaintId, ct)
            ?? throw new NotFoundException("Complaint", complaintId);

        var existing = await db.ComplaintAttachments.CountAsync(a => a.ComplaintId == complaintId, ct);
        if (existing + files.Count > options.Value.MaxFilesPerComplaint)
            throw new DomainException("attachment.too_many", $"A complaint can hold at most {options.Value.MaxFilesPerComplaint} attachments.");

        var stored = await store.StoreAsync(complaint.Id, files, user.EmployeeId, user.Name, ct);
        try
        {
            complaint.UpdatedAt = stored[0].UploadedAt;
            foreach (var a in stored) audit.Log("UPLOAD_ATTACHMENT", Module, complaint.Id.ToString(), $"{a.Id} {a.ContentType} {a.FileSize}B");
            await db.SaveChangesAsync(ct);
        }
        catch
        {
            await store.DiscardAsync(stored);
            throw;
        }
        return stored.Select(ToItem).ToList();
    }

    public async Task<AttachmentDownload> OpenAsync(Guid complaintId, Guid attachmentId, CancellationToken ct)
    {
        // Attachments often carry account statements and IDs: same bar as seeing unmasked customer data.
        if (!user.HasPermission(Permissions.ComplaintViewUnmasked))
            throw new ForbiddenAccessException("Your role cannot open customer documents.");

        var visible = await db.Complaints.VisibleTo(user).AnyAsync(c => c.Id == complaintId, ct);
        var attachment = visible
            ? await db.ComplaintAttachments.AsNoTracking().FirstOrDefaultAsync(a => a.Id == attachmentId && a.ComplaintId == complaintId, ct)
            : null;
        if (attachment is null) throw new NotFoundException("Attachment", attachmentId);

        audit.Log("DOWNLOAD_ATTACHMENT", Module, complaintId.ToString(), attachment.Id.ToString());
        await db.SaveChangesAsync(ct);

        var content = await storage.OpenReadAsync(attachment.StorageKey, ct);
        return new AttachmentDownload(content, attachment.FileName, attachment.ContentType, attachment.FileSize);
    }

    public static AttachmentItem ToItem(ComplaintAttachment a) => new(
        a.Id, a.FileName, a.ContentType, a.FileSize, a.UploadedAt,
        new EmployeeRef(a.UploadedBy, a.UploadedByName), a.ScanStatus);
}

/// <summary>
/// Validates, scans, hashes and stores files, and stages their rows on the DbContext (the caller saves).
/// Every file is checked before any is stored, so a bad file rejects the whole upload.
/// </summary>
public sealed class AttachmentStore(IApplicationDbContext db, IFileStorage storage, IMalwareScanner scanner, TimeProvider clock, IOptions<AttachmentOptions> options)
{
    public async Task<IReadOnlyList<ComplaintAttachment>> StoreAsync(
        Guid complaintId, IReadOnlyList<UploadFile> files, string uploadedBy, string? uploadedByName, CancellationToken ct)
    {
        var o = options.Value;
        if (files.Count == 0) throw new DomainException("attachment.none", "Choose at least one file.");
        if (files.Count > o.MaxFilesPerUpload)
            throw new DomainException("attachment.too_many", $"Attach at most {o.MaxFilesPerUpload} files at a time.");

        // 1. Read and validate every file first (bounded by MaxFileBytes).
        var buffered = new List<(byte[] Bytes, string Name, string ContentType)>();
        foreach (var f in files)
        {
            if (f.Length > o.MaxFileBytes)
                throw new DomainException("attachment.too_large", $"\"{AttachmentRules.SafeFileName(f.FileName)}\" is larger than {o.MaxFileBytes / (1024 * 1024)} MB.");
            using var ms = new MemoryStream((int)Math.Max(f.Length, 0));
            await using (var input = f.OpenRead()) await input.CopyToAsync(ms, ct);
            var bytes = ms.ToArray();
            var (name, contentType) = AttachmentRules.Validate(f.FileName, bytes.LongLength, bytes.AsSpan(0, Math.Min(bytes.Length, AttachmentRules.HeaderBytes)), o);
            buffered.Add((bytes, name, contentType));
        }

        // 2. Scan, store and stage.
        var now = clock.GetUtcNow();
        var saved = new List<ComplaintAttachment>();
        try
        {
            foreach (var (bytes, name, contentType) in buffered)
            {
                var verdict = await scanner.ScanAsync(new MemoryStream(bytes, writable: false), name, ct);
                if (verdict == MalwareScanVerdict.Infected)
                    throw new DomainException("attachment.infected", $"\"{name}\" was rejected by the virus scan.");

                var key = await storage.SaveAsync(new MemoryStream(bytes, writable: false), ct);
                var attachment = new ComplaintAttachment
                {
                    ComplaintId = complaintId,
                    FileName = name,
                    StorageKey = key,
                    ContentType = contentType,
                    FileSize = bytes.LongLength,
                    Sha256 = Convert.ToHexStringLower(SHA256.HashData(bytes)),
                    ScanStatus = verdict == MalwareScanVerdict.Clean ? "CLEAN" : "NOT_SCANNED",
                    UploadedBy = uploadedBy,
                    UploadedByName = uploadedByName,
                    UploadedAt = now,
                };
                saved.Add(attachment);
                db.ComplaintAttachments.Add(attachment);
            }
        }
        catch
        {
            await DiscardAsync(saved);
            throw;
        }
        return saved;
    }

    /// <summary>Best-effort removal of stored files when the database save does not happen.</summary>
    public async Task DiscardAsync(IEnumerable<ComplaintAttachment> attachments)
    {
        foreach (var a in attachments)
        {
            try { await storage.DeleteAsync(a.StorageKey); } catch { /* orphaned file; a cleanup job can sweep it */ }
        }
    }
}
