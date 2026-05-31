using FluentAssertions;
using PdfAbstraction.Tests.Infrastructure;
using TemplateEngine.Abstractions.IronPdf;
using TemplateEngine.Abstractions.IronPdf.Options;
using Xunit;

namespace PdfAbstraction.Tests;

/// <summary>
/// End-to-end scenario tests that exercise the full pipeline from request
/// through template rendering to result, without any real PDF provider.
/// Each test represents a realistic caller scenario.
/// </summary>
public class EndToEndScenarioTests
{
    // ── Scenario 1: Minimal invoice — binary output ───────────────────────────

    [Fact]
    public async Task Scenario_MinimalInvoice_ReturnsBinaryPdf()
    {
        var (provider, _) = BuildPipeline();

        var result = await provider.GeneratePdfAsync(new PdfGenerationRequest<InvoiceModel>
        {
            Model    = new InvoiceModel("INV-001", 499.99m),
            Template = "<h1>Invoice #{{InvoiceNumber}}</h1><p>Total: {{Total}}</p>"
        });

        result.IsBinary.Should().BeTrue();
        result.Bytes.Should().NotBeNullOrEmpty();
    }

    // ── Scenario 2: Report with custom margins and landscape ──────────────────

    [Fact]
    public async Task Scenario_LandscapeReport_DocumentOptionsForwarded()
    {
        var (provider, _) = BuildPipeline();

        var request = new PdfGenerationRequest<InvoiceModel>
        {
            Model    = new InvoiceModel("RPT-001", 0m),
            Template = "<table>...</table>",
            DocumentOptions = new PdfDocumentOptions
            {
                PaperSize   = PdfPaperSize.A3,
                Orientation = PdfOrientation.Landscape,
                Margins     = PdfMargins.None
            }
        };

        await provider.GeneratePdfAsync(request);

        var ctx = provider.CapturedContext!;
        ctx.DocumentOptions.PaperSize.Should().Be(PdfPaperSize.A3);
        ctx.DocumentOptions.Orientation.Should().Be(PdfOrientation.Landscape);
        ctx.DocumentOptions.Margins.Top.Should().Be(0);
    }

    // ── Scenario 3: Branded PDF with header and footer ────────────────────────

    [Fact]
    public async Task Scenario_BrandedPdf_HeaderAndFooterBothPresent()
    {
        var (provider, _) = BuildPipeline();

        var request = new PdfGenerationRequest<InvoiceModel>
        {
            Model    = new InvoiceModel("INV-002", 1299m),
            Template = "<p>body</p>",
            HeaderOptions = new PdfHeaderOptions
            {
                HtmlContent = "<div>ACME Corp</div>",
                HeightInMm    = 15,
                ShowDivider = true,
                SkipOnPages = [1]
            },
            FooterOptions = new PdfFooterOptions
            {
                HtmlContent = "Page {page} of {total-pages}",
                HeightInMm    = 10,
                ShowDivider = true
            }
        };

        await provider.GeneratePdfAsync(request);

        var ctx = provider.CapturedContext!;
        ctx.HeaderOptions.Should().NotBeNull();
        ctx.HeaderOptions!.ShowDivider.Should().BeTrue();
        ctx.HeaderOptions!.SkipOnPages.Should().Contain(1);
        ctx.FooterOptions.Should().NotBeNull();
        ctx.FooterOptions!.HtmlContent.Should().Contain("{page}");
    }

    // ── Scenario 4: Stream response for HTTP download ─────────────────────────

    [Fact]
    public async Task Scenario_StreamedDownload_ReturnsReadableStream()
    {
        var (provider, _) = BuildPipeline(
            resultFactory: _ => PdfGenerationResult.FromStream(new MemoryStream(FakeBytes)));

        var request = new PdfGenerationRequest<InvoiceModel>
        {
            Model    = new InvoiceModel("INV-003", 50m),
            Template = "<p>download me</p>",
            OutputOptions = new PdfOutputOptions { AsStream = true }
        };

        await using var result = await provider.GeneratePdfAsync(request);

        result.IsStream.Should().BeTrue();
        result.Stream.Should().NotBeNull();
        result.Stream!.CanRead.Should().BeTrue();
    }

    // ── Scenario 5: Save to file on disk ─────────────────────────────────────

    [Fact]
    public async Task Scenario_SaveToFile_FilePathReturnedInResult()
    {
        var expectedPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.pdf");
        var (provider, _) = BuildPipeline(
            resultFactory: _ => PdfGenerationResult.FromFile(expectedPath));

        var request = new PdfGenerationRequest<InvoiceModel>
        {
            Model    = new InvoiceModel("INV-004", 0m),
            Template = "<p>save me</p>",
            OutputOptions = new PdfOutputOptions
            {
                DataFormat = PdfDataFormat.File,
                FilePath   = expectedPath
            }
        };

        var result = await provider.GeneratePdfAsync(request);

        result.IsFile.Should().BeTrue();
        result.FilePath.Should().Be(expectedPath);
    }

    // ── Scenario 6: Template engine renders model data ────────────────────────

    [Fact]
    public async Task Scenario_TemplateEngineReceivesCorrectModel()
    {
        var (provider, engine) = BuildPipeline();
        var model = new InvoiceModel("INV-XYZ", 12345.67m);

        await provider.GeneratePdfAsync(new PdfGenerationRequest<InvoiceModel>
        {
            Model    = model,
            Template = "{{InvoiceNumber}} — {{Total}}"
        });

        engine.Calls.Should().ContainSingle();
        engine.Calls[0].Model.Should().Be(model);
        engine.Calls[0].Template.Should().Be("{{InvoiceNumber}} — {{Total}}");
    }

    // ── Scenario 7: JS-heavy template with render delay ──────────────────────

    [Fact]
    public async Task Scenario_JsHeavyTemplate_RenderDelayForwarded()
    {
        var (provider, _) = BuildPipeline();

        var request = new PdfGenerationRequest<InvoiceModel>
        {
            Model    = new InvoiceModel("INV-005", 0m),
            Template = "<canvas id='chart'></canvas>",
            DocumentOptions = new PdfDocumentOptions { RenderDelayMs = 1500 }
        };

        await provider.GeneratePdfAsync(request);

        provider.CapturedContext!.DocumentOptions.RenderDelayMs.Should().Be(1500);
    }

    // ── Scenario 8: Cover page — header/footer skipped on page 1 ─────────────

    [Fact]
    public async Task Scenario_CoverPage_SkipOnPagesContainsPage1()
    {
        var (provider, _) = BuildPipeline();

        var request = new PdfGenerationRequest<InvoiceModel>
        {
            Model    = new InvoiceModel("RPT-COVER", 0m),
            Template = "<p>cover + body</p>",
            HeaderOptions = new PdfHeaderOptions { SkipOnPages = [1] },
            FooterOptions = new PdfFooterOptions { SkipOnPages = [1] }
        };

        await provider.GeneratePdfAsync(request);

        provider.CapturedContext!.HeaderOptions!.SkipOnPages.Should().Contain(1);
        provider.CapturedContext!.FooterOptions!.SkipOnPages.Should().Contain(1);
    }

    // ── Scenario 9: ToBytesAsync normalises any result to bytes ──────────────

    [Fact]
    public async Task Scenario_ToBytesAsync_NormalisesStreamResultToBytes()
    {
        var (provider, _) = BuildPipeline(
            resultFactory: _ => PdfGenerationResult.FromStream(new MemoryStream(FakeBytes)));

        await using var result = await provider.GeneratePdfAsync(
            Requests.WithOutput(new PdfOutputOptions { AsStream = true }));

        var bytes = await result.ToBytesAsync();
        bytes.Should().BeEquivalentTo(FakeBytes);
    }

    // ── Scenario 10: Multiple sequential calls — state is not shared ──────────

    [Fact]
    public async Task Scenario_MultipleSequentialCalls_EachGetsItsOwnContext()
    {
        var (provider, _) = BuildPipeline();

        await provider.GeneratePdfAsync(new PdfGenerationRequest<InvoiceModel>
        {
            Model    = new InvoiceModel("FIRST", 1m),
            Template = "first"
        });
        var firstContext = provider.CapturedContext;

        await provider.GeneratePdfAsync(new PdfGenerationRequest<InvoiceModel>
        {
            Model    = new InvoiceModel("SECOND", 2m),
            Template = "second"
        });
        var secondContext = provider.CapturedContext;

        firstContext.Should().NotBeSameAs(secondContext);
        provider.CallCount.Should().Be(2);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static readonly byte[] FakeBytes = "%PDF-fake"u8.ToArray();

    private static (CapturingPdfProvider provider, FakeTemplateEngine engine) BuildPipeline(
        Func<PdfRenderContext, PdfGenerationResult>? resultFactory = null)
    {
        var engine   = new FakeTemplateEngine();
        var provider = new CapturingPdfProvider(engine,
            resultFactory ?? (_ => PdfGenerationResult.FromBytes(FakeBytes)));
        return (provider, engine);
    }
}
