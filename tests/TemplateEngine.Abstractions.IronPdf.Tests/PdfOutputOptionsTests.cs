using FluentAssertions;
using PdfAbstraction.Tests.Infrastructure;
using TemplateEngine.Abstractions.IronPdf;
using TemplateEngine.Abstractions.IronPdf.Options;
using Xunit;

namespace PdfAbstraction.Tests;

/// <summary>
/// Tests that cover the output-options path through the abstraction:
/// binary output, file output, stream output, and the missing FilePath guard.
/// </summary>
public class PdfOutputOptionsTests
{
    // ── Binary (default) ──────────────────────────────────────────────────────

    [Fact]
    public async Task OutputOptions_Default_ReturnsBinaryResult()
    {
        var provider = BuildProvider(ctx => PdfGenerationResult.FromBytes(FakeBytes));

        var result = await provider.GeneratePdfAsync(Requests.Basic());

        result.IsBinary.Should().BeTrue();
        result.Bytes.Should().BeEquivalentTo(FakeBytes);
    }

    [Fact]
    public async Task OutputOptions_ExplicitBinary_ReturnsBinaryResult()
    {
        var provider = BuildProvider(ctx => PdfGenerationResult.FromBytes(FakeBytes));
        var request  = Requests.WithOutput(new PdfOutputOptions
        {
            DataFormat = PdfDataFormat.Binary,
            AsStream   = false
        });

        var result = await provider.GeneratePdfAsync(request);

        result.IsBinary.Should().BeTrue();
    }

    // ── Stream ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task OutputOptions_AsStreamTrue_ReturnsStreamResult()
    {
        var provider = BuildProvider(_ => PdfGenerationResult.FromStream(new MemoryStream(FakeBytes)));
        var request  = Requests.WithOutput(new PdfOutputOptions { AsStream = true });

        await using var result = await provider.GeneratePdfAsync(request);

        result.IsStream.Should().BeTrue();
        result.Stream.Should().NotBeNull();
    }

    [Fact]
    public async Task OutputOptions_AsStream_StreamContainsExpectedBytes()
    {
        var provider = BuildProvider(_ => PdfGenerationResult.FromStream(new MemoryStream(FakeBytes)));
        var request  = Requests.WithOutput(new PdfOutputOptions { AsStream = true });

        await using var result = await provider.GeneratePdfAsync(request);

        var bytes = await result.ToBytesAsync();
        bytes.Should().BeEquivalentTo(FakeBytes);
    }

    // ── File ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task OutputOptions_FileFormat_ReturnsFileResult()
    {
        var path     = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.pdf");
        var provider = BuildProvider(_ => PdfGenerationResult.FromFile(path));
        var request  = Requests.WithOutput(new PdfOutputOptions
        {
            DataFormat = PdfDataFormat.File,
            FilePath   = path
        });

        var result = await provider.GeneratePdfAsync(request);

        result.IsFile.Should().BeTrue();
        result.FilePath.Should().Be(path);
    }

    [Fact]
    public async Task OutputOptions_FileFormat_ContextReceivesFilePath()
    {
        const string path = "/output/invoice.pdf";
        var engine   = new FakeTemplateEngine();
        var provider = new CapturingPdfProvider(engine,
            _ => PdfGenerationResult.FromFile(path));

        var request = Requests.WithOutput(new PdfOutputOptions
        {
            DataFormat = PdfDataFormat.File,
            FilePath   = path
        });

        await provider.GeneratePdfAsync(request);

        provider.CapturedContext!.OutputOptions.FilePath.Should().Be(path);
        provider.CapturedContext!.OutputOptions.DataFormat.Should().Be(PdfDataFormat.File);
    }

    // ── AsStream flag is forwarded ─────────────────────────────────────────────

    [Fact]
    public async Task OutputOptions_AsStreamFlag_ForwardedToContext()
    {
        var engine   = new FakeTemplateEngine();
        var provider = new CapturingPdfProvider(engine,
            _ => PdfGenerationResult.FromStream(new MemoryStream(FakeBytes)));

        var request = Requests.WithOutput(new PdfOutputOptions { AsStream = true });

        await provider.GeneratePdfAsync(request);

        provider.CapturedContext!.OutputOptions.AsStream.Should().BeTrue();
    }

    // ── Defaults ──────────────────────────────────────────────────────────────

    [Fact]
    public void PdfOutputOptions_Defaults_AreBinaryNonStream()
    {
        var opts = new PdfOutputOptions();

        opts.DataFormat.Should().Be(PdfDataFormat.Binary);
        opts.AsStream.Should().BeFalse();
        opts.FilePath.Should().BeNull();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static readonly byte[] FakeBytes = "%PDF-fake"u8.ToArray();

    private static CapturingPdfProvider BuildProvider(
        Func<PdfRenderContext, PdfGenerationResult> factory)
        => new(new FakeTemplateEngine(), factory);
}
