using ComplaintManagement.Application.Attachments;
using ComplaintManagement.Application.Common.Interfaces;
using ComplaintManagement.Domain.Common;
using ComplaintManagement.UnitTests.Support;
using Microsoft.Extensions.Options;

namespace ComplaintManagement.UnitTests;

public class AttachmentRulesTests
{
    private static readonly AttachmentOptions Options = new();

    public static readonly byte[] Pdf = [.. "%PDF-1.7\n"u8.ToArray(), .. new byte[32]];
    public static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0];
    public static readonly byte[] Exe = [0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00, 0, 0];

    [Theory]
    [InlineData("statement.pdf", "application/pdf")]
    [InlineData("STATEMENT.PDF", "application/pdf")]
    public void Real_pdf_is_accepted_with_detected_type(string name, string expected) =>
        Assert.Equal(expected, AttachmentRules.Validate(name, Pdf.Length, Pdf, Options).ContentType);

    [Fact]
    public void Renamed_executable_is_rejected() =>
        Assert.Equal("attachment.content_mismatch",
            Assert.Throws<DomainException>(() => AttachmentRules.Validate("receipt.pdf", Exe.Length, Exe, Options)).Code);

    [Fact]
    public void Png_named_as_jpg_is_rejected() =>
        Assert.Throws<DomainException>(() => AttachmentRules.Validate("photo.jpg", Png.Length, Png, Options));

    [Theory]
    [InlineData("setup.exe")]
    [InlineData("macro.xlsm")]
    [InlineData("page.html")]
    [InlineData("noextension")]
    public void Disallowed_types_are_rejected(string name) =>
        Assert.Equal("attachment.type_not_allowed",
            Assert.Throws<DomainException>(() => AttachmentRules.Validate(name, Pdf.Length, Pdf, Options)).Code);

    [Fact]
    public void Oversized_and_empty_files_are_rejected()
    {
        Assert.Throws<DomainException>(() => AttachmentRules.Validate("a.pdf", Options.MaxFileBytes + 1, Pdf, Options));
        Assert.Throws<DomainException>(() => AttachmentRules.Validate("a.pdf", 0, Pdf, Options));
    }

    [Theory]
    [InlineData("../../etc/passwd.pdf", "passwd.pdf")]
    [InlineData(@"C:\Users\x\bank statement.pdf", "bank statement.pdf")]
    [InlineData("a<b>c|d.pdf", "a_b_c_d.pdf")]
    [InlineData("...hidden.pdf", "hidden.pdf")]
    [InlineData("", "attachment")]
    public void File_names_are_made_safe(string input, string expected) =>
        Assert.Equal(expected, AttachmentRules.SafeFileName(input));

    [Fact]
    public void Long_names_keep_their_extension()
    {
        var name = AttachmentRules.SafeFileName(new string('a', 300) + ".xlsx");
        Assert.Equal(150, name.Length);
        Assert.EndsWith(".xlsx", name);
    }
}

public class AttachmentStoreTests
{
    private sealed class MemoryStorage : IFileStorage
    {
        public Dictionary<string, byte[]> Files { get; } = [];
        public async Task<string> SaveAsync(Stream content, CancellationToken cancellationToken = default)
        {
            using var ms = new MemoryStream();
            await content.CopyToAsync(ms, cancellationToken);
            var key = Guid.NewGuid().ToString("N");
            Files[key] = ms.ToArray();
            return key;
        }
        public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default) => Task.FromResult<Stream>(new MemoryStream(Files[storageKey]));
        public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default) { Files.Remove(storageKey); return Task.CompletedTask; }
    }

    private sealed class Scanner(MalwareScanVerdict verdict) : IMalwareScanner
    {
        public Task<MalwareScanVerdict> ScanAsync(Stream content, string fileName, CancellationToken cancellationToken = default) => Task.FromResult(verdict);
    }

    private readonly TestDbContext _db = TestDbContext.Create();
    private readonly MemoryStorage _storage = new();

    private AttachmentStore Store(MalwareScanVerdict verdict = MalwareScanVerdict.NotScanned) =>
        new(_db, _storage, new Scanner(verdict), new FixedClock(DateTimeOffset.UtcNow), Options.Create(new AttachmentOptions()));

    private static UploadFile File(string name, byte[] bytes) => new(name, bytes.Length, () => new MemoryStream(bytes));

    [Fact]
    public async Task Stores_under_opaque_key_with_hash_and_scan_status()
    {
        var saved = await Store().StoreAsync(Guid.NewGuid(), [File("../x/statement.pdf", AttachmentRulesTests.Pdf)], "300001", "RAKESH", default);

        var a = Assert.Single(saved);
        Assert.Equal(("statement.pdf", "application/pdf", "NOT_SCANNED"), (a.FileName, a.ContentType, a.ScanStatus));
        Assert.DoesNotContain("statement", a.StorageKey);
        Assert.Equal(64, a.Sha256.Length);
        Assert.Equal(AttachmentRulesTests.Pdf, _storage.Files[a.StorageKey]);
    }

    [Fact]
    public async Task One_bad_file_rejects_the_whole_upload_and_stores_nothing()
    {
        await Assert.ThrowsAsync<DomainException>(() => Store().StoreAsync(Guid.NewGuid(),
            [File("ok.pdf", AttachmentRulesTests.Pdf), File("fake.png", AttachmentRulesTests.Exe)], "300001", null, default));
        Assert.Empty(_storage.Files);
    }

    [Fact]
    public async Task Infected_file_is_rejected_and_nothing_is_kept()
    {
        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            Store(MalwareScanVerdict.Infected).StoreAsync(Guid.NewGuid(), [File("a.pdf", AttachmentRulesTests.Pdf)], "CUSTOMER", null, default));
        Assert.Equal("attachment.infected", ex.Code);
        Assert.Empty(_storage.Files);
    }

    [Fact]
    public async Task Too_many_files_are_refused()
    {
        var files = Enumerable.Range(0, 6).Select(i => File($"{i}.pdf", AttachmentRulesTests.Pdf)).ToList();
        await Assert.ThrowsAsync<DomainException>(() => Store().StoreAsync(Guid.NewGuid(), files, "CUSTOMER", null, default));
    }
}
