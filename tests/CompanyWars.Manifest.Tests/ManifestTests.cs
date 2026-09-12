using System.IO.Compression;
using CompanyWars.Content;
using CompanyWars.Tools;
using Xunit;

namespace CompanyWars.Manifest.Tests;

public class ManifestTests
{
    private static readonly string Root = RepoRoot.Find(AppContext.BaseDirectory);

    [Fact]
    public void CommittedManifestValidatesWithNoArtPresent()
    {
        ManifestReport report = ManifestValidator.Validate(Root, ContentLoader.Load(Root));
        Assert.Empty(report.Errors);
        Assert.Equal(193, report.Coverage.Total);
        Assert.Equal(190, report.Coverage.Gated);
        Assert.Equal(0, report.Coverage.VerifyPending);
    }

    [Fact]
    public void OverhangDerivationMatchesTheWhiteboardExample()
    {
        var e = new ManifestEntry("furn.whiteboard", "furniture", "support", "Whiteboard", new FootprintTiles(1, 1), new SpriteSpec(32, 40, new Anchor(0.5, 1.0), "assets/topdown/furn/whiteboard.png", null, null), -5, "topdown", new[] { "build" }, 1, "x", null, false, true, null, null, null);
        Assert.Equal(new Overhang(0, 8, 0, 0), ManifestMath.DeriveOverhang(e, 32));
    }

    [Fact]
    public void DrawOrderIsTotal()
    {
        var a = (1L, 2L, 0L, 3L, "emp.a");
        var b = (1L, 2L, 0L, 3L, "emp.b");
        Assert.True(ManifestMath.CompareDrawOrder(a, b) < 0);
        Assert.True(ManifestMath.CompareDrawOrder((0L, 9L, 9L, 9L, "z"), (1L, 0L, 0L, 0L, "a")) < 0);
        Assert.True(ManifestMath.CompareDrawOrder((1L, 1L, -10L, 0L, "z"), (1L, 1L, 0L, 0L, "a")) < 0);
    }

    [Fact]
    public void AbsentAssetIsValidButAWrongSizedPresentOneIsNot()
    {
        // Copy the repo's manifest, schema and content into a scratch root, then drop a 16x16 PNG where a 32x32 one belongs.
        string scratch = Path.Combine(Path.GetTempPath(), "cw-manifest-" + Guid.NewGuid().ToString("N"));
        try
        {
            CopyDir(Path.Combine(Root, "manifest"), Path.Combine(scratch, "manifest"));
            CopyDir(Path.Combine(Root, "schema"), Path.Combine(scratch, "schema"));
            CopyDir(Path.Combine(Root, "content"), Path.Combine(scratch, "content"));
            ContentDb db = ContentLoader.Load(scratch);
            Assert.Empty(ManifestValidator.Validate(scratch, db).Errors);

            SpriteManifest m = ManifestValidator.Load(scratch);
            ManifestEntry intern = m.Entries.First(e => e.Id == "emp.intern");
            string asset = Path.Combine(scratch, intern.Sprite.Asset);
            Directory.CreateDirectory(Path.GetDirectoryName(asset)!);
            File.WriteAllBytes(asset, Png(16, 16));
            ManifestReport bad = ManifestValidator.Validate(scratch, db);
            Assert.Contains(bad.Errors, e => e.StartsWith("emp.intern:", StringComparison.Ordinal) && e.Contains("16x16", StringComparison.Ordinal));

            File.WriteAllBytes(asset, Png(intern.Sprite.W, intern.Sprite.H));
            ManifestReport good = ManifestValidator.Validate(scratch, db);
            Assert.Empty(good.Errors);
            Assert.Equal(1, good.Coverage.WithArt);
            Assert.Contains("emp.intern", good.Present);
        }
        finally
        {
            if (Directory.Exists(scratch)) Directory.Delete(scratch, recursive: true);
        }
    }

    private static void CopyDir(string from, string to)
    {
        Directory.CreateDirectory(to);
        foreach (string f in Directory.GetFiles(from)) File.Copy(f, Path.Combine(to, Path.GetFileName(f)));
        foreach (string d in Directory.GetDirectories(from)) CopyDir(d, Path.Combine(to, Path.GetFileName(d)));
    }

    /// <summary>A minimal valid PNG of the given size: 8-bit greyscale, one filter byte per row, zlib-deflated.</summary>
    public static byte[] Png(long width, long height)
    {
        using var ms = new MemoryStream();
        ms.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        var ihdr = new byte[13];
        WriteBe(ihdr, 0, (uint)width);
        WriteBe(ihdr, 4, (uint)height);
        ihdr[8] = 8; // bit depth
        ihdr[9] = 0; // greyscale
        Chunk(ms, "IHDR", ihdr);
        var raw = new byte[(width + 1) * height];
        using (var zs = new MemoryStream())
        {
            using (var z = new ZLibStream(zs, CompressionLevel.Fastest, leaveOpen: true)) z.Write(raw);
            Chunk(ms, "IDAT", zs.ToArray());
        }
        Chunk(ms, "IEND", Array.Empty<byte>());
        return ms.ToArray();
    }

    private static void Chunk(Stream s, string type, byte[] data)
    {
        var len = new byte[4];
        WriteBe(len, 0, (uint)data.Length);
        s.Write(len);
        byte[] t = System.Text.Encoding.ASCII.GetBytes(type);
        s.Write(t);
        s.Write(data);
        var crc = new byte[4];
        WriteBe(crc, 0, Crc32(t.Concat(data).ToArray()));
        s.Write(crc);
    }

    private static void WriteBe(byte[] b, int at, uint v)
    {
        b[at] = (byte)(v >> 24);
        b[at + 1] = (byte)(v >> 16);
        b[at + 2] = (byte)(v >> 8);
        b[at + 3] = (byte)v;
    }

    private static uint Crc32(byte[] data)
    {
        uint c = 0xffffffffu;
        foreach (byte b in data)
        {
            c ^= b;
            for (int k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xedb88320u ^ (c >> 1) : c >> 1;
        }
        return c ^ 0xffffffffu;
    }
}
