namespace Sencilla.Component.Files.AmazonS3.Tests;

/// <summary>
/// The rules that decide where bytes land and how a resumable chunk maps onto a multipart part.
/// No endpoint needed — these are the invariants that, when broken, corrupt files silently.
/// </summary>
public class S3PathsTests
{
    private const int PartSize = 8 * 1024 * 1024;

    // ── Key mapping ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ToKey_UsesWholePathAsKey_SoTheBucketNeverComesFromTheFirstSegment()
    {
        var key = S3Paths.ToKey(null, "user3f2b/project9a1c/photo.jpg");

        Assert.Equal("user3f2b/project9a1c/photo.jpg", key);
    }

    [Theory]
    [InlineData("dev/", "dev/user1/a.jpg")]
    [InlineData("dev", "dev/user1/a.jpg")]
    [InlineData("/dev/", "dev/user1/a.jpg")]
    [InlineData("", "user1/a.jpg")]
    [InlineData(null, "user1/a.jpg")]
    public void ToKey_AppliesPrefixWithExactlyOneSeparator(string? prefix, string expected)
    {
        Assert.Equal(expected, S3Paths.ToKey(prefix, "user1/a.jpg"));
    }

    [Fact]
    public void ToKey_NormalisesWindowsSeparatorsAndLeadingSlash()
    {
        // Paths are built with Path.Combine, so they carry the host's separator. S3 keys are
        // always forward-slashed, and a leading slash would create an empty first segment.
        Assert.Equal("dev/user1/a.jpg", S3Paths.ToKey("dev/", "\\user1\\a.jpg"));
        Assert.Equal("dev/user1/a.jpg", S3Paths.ToKey("dev/", "/user1/a.jpg"));
    }

    [Fact]
    public void ToKey_KeepsEnvironmentsApart_SoARestoredDatabaseCannotReachAnotherPrefix()
    {
        const string path = "user1/project2/photo.jpg";

        Assert.NotEqual(S3Paths.ToKey("dev/", path), S3Paths.ToKey("stage/", path));
    }

    [Fact]
    public void ToFolderPrefix_AlwaysEndsWithSlash_SoListingCannotMatchASiblingFolder()
    {
        Assert.Equal("dev/user1/project2/", S3Paths.ToFolderPrefix("dev/", "user1/project2"));
        Assert.Equal("dev/user1/project2/", S3Paths.ToFolderPrefix("dev/", "user1/project2/"));
    }

    // ── Part arithmetic ─────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(0, 1)]
    [InlineData(8 * 1024 * 1024, 2)]
    [InlineData(16 * 1024 * 1024, 3)]
    public void PartNumber_IsDerivedFromTheOffset(long offset, int expected)
    {
        Assert.Equal(expected, S3Paths.PartNumber(offset, PartSize));
    }

    [Fact]
    public void PartNumber_IsStableForAResentChunk_SoTheRetryOverwritesItsOwnPart()
    {
        // The Azure provider corrupted uploads because a chunk whose acknowledgement was lost got
        // appended twice. Deriving the part number from the offset makes the retry an overwrite.
        const long offset = 16 * 1024 * 1024;

        Assert.Equal(S3Paths.PartNumber(offset, PartSize), S3Paths.PartNumber(offset, PartSize));
    }

    [Fact]
    public void IsLastChunk_TrueOnlyWhenTheChunkReachesTheDeclaredSize()
    {
        Assert.False(S3Paths.IsLastChunk(0, PartSize, 20 * 1024 * 1024, PartSize));
        Assert.True(S3Paths.IsLastChunk(16 * 1024 * 1024, 4 * 1024 * 1024, 20 * 1024 * 1024, PartSize));
    }

    [Fact]
    public void IsLastChunk_WithoutADeclaredSize_TheShortChunkEndsTheUpload()
    {
        // Resolution uploads build their File with Size = 0. Keying completion off the declared
        // size would leave the multipart upload open forever and the object absent.
        Assert.False(S3Paths.IsLastChunk(0, PartSize, size: 0, PartSize));
        Assert.True(S3Paths.IsLastChunk(PartSize, 2048, size: 0, PartSize));
    }

    [Fact]
    public void IsChunked_TrueForAResumableChunk()
    {
        Assert.True(S3Paths.IsChunked(0, PartSize, 20 * 1024 * 1024, PartSize, lengthProvided: true));
        Assert.True(S3Paths.IsChunked(PartSize, 1024, 0, PartSize, lengthProvided: true));
    }

    [Fact]
    public void IsChunked_FalseForTheZeroBytePlaceholder()
    {
        // File creation writes an empty object while File.Size already holds the declared upload
        // length. Treating that as a first chunk rejects every upload before it starts.
        Assert.False(S3Paths.IsChunked(0, 0, 3_000_000, PartSize, lengthProvided: true));
    }

    [Fact]
    public void IsChunked_FalseForAnOverwriteSmallerThanTheRecordedSize()
    {
        // A sanitised SVG or a re-compressed derivative replaces the object with fewer bytes than
        // File.Size records. That is a whole object, not a partial upload.
        Assert.False(S3Paths.IsChunked(0, 500, 800, PartSize, lengthProvided: false));
    }

    [Fact]
    public void IsChunked_FalseForAWholeObjectWriteThatHappensToBeOnePartLong()
    {
        // Same byte count as a first chunk, but no explicit length: a server-side write, which
        // must go up whole rather than wait forever for a second chunk.
        Assert.False(S3Paths.IsChunked(0, PartSize, 0, PartSize, lengthProvided: false));
    }

    [Fact]
    public void IsChunked_FalseWhenOneChunkCarriesTheWholeFile()
    {
        Assert.False(S3Paths.IsChunked(0, PartSize, PartSize, PartSize, lengthProvided: true));
    }

    // ── Fail-loud validation ────────────────────────────────────────────────────────────────

    [Fact]
    public void ValidateChunk_RejectsAPartSizeBelowTheS3Minimum()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => S3Paths.ValidateChunk(0, 4 * 1024 * 1024, 100 * 1024 * 1024, 4 * 1024 * 1024));

        Assert.Contains("5 MiB", ex.Message);
    }

    [Fact]
    public void ValidateChunk_RejectsAClientChunkSmallerThanPartSize_BeforeAnyBytesAreUploaded()
    {
        // The 4 MiB default client chunk against an 8 MiB part size: every part uploads fine and
        // only CompleteMultipartUpload fails with EntityTooSmall, after the whole file was sent.
        var ex = Assert.Throws<InvalidOperationException>(
            () => S3Paths.ValidateChunk(0, 4 * 1024 * 1024, 100 * 1024 * 1024, PartSize));

        Assert.Contains("final chunk", ex.Message);
    }

    [Fact]
    public void ValidateChunk_RejectsAnOffsetThatIsNotAPartBoundary()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => S3Paths.ValidateChunk(1234, PartSize, 100 * 1024 * 1024, PartSize));

        Assert.Contains("multiple of PartSize", ex.Message);
    }

    [Fact]
    public void ValidateChunk_RejectsAnObjectPastTheTenThousandPartCeiling()
    {
        var offset = (long)PartSize * S3Paths.MaxParts;

        var ex = Assert.Throws<InvalidOperationException>(
            () => S3Paths.ValidateChunk(offset, PartSize, offset * 2, PartSize));

        Assert.Contains("10000", ex.Message.Replace(",", "").Replace(" ", ""));
    }

    [Fact]
    public void ValidateChunk_AcceptsAShortFinalChunk()
    {
        S3Paths.ValidateChunk(16 * 1024 * 1024, 1024, 16 * 1024 * 1024 + 1024, PartSize);
    }

    [Fact]
    public void ValidateChunk_AcceptsAShortChunkWhenNoSizeWasDeclared()
    {
        // Undeclared size: the short chunk is the last one, so it is legal.
        S3Paths.ValidateChunk(PartSize, 1024, size: 0, PartSize);
    }
}
