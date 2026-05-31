using FluentAssertions;
using PdfAbstraction.Tests.Infrastructure;
using TemplateEngine.Abstractions.IronPdf;
using TemplateEngine.Abstractions.IronPdf.Options;
using Xunit;

namespace PdfAbstraction.Tests;

/// <summary>
/// Tests for <see cref="PdfHeaderOptions"/> and <see cref="PdfFooterOptions"/>:
/// defaults, optional usage, skip-pages, and forwarding through the pipeline.
/// </summary>
public class PdfHeaderFooterOptionsTests
{
    // ── Header defaults ───────────────────────────────────────────────────────

    [Fact]
    public void HeaderOptions_Defaults_AreCorrect()
    {
        var opts = new PdfHeaderOptions();

        opts.HtmlContent.Should().BeNull();
        opts.HeightInMm.Should().Be(15);
        opts.ShowDivider.Should().BeFalse();
        opts.SkipOnPages.Should().BeEmpty();
    }

    // ── Footer defaults ───────────────────────────────────────────────────────

    [Fact]
    public void FooterOptions_Defaults_AreCorrect()
    {
        var opts = new PdfFooterOptions();

        opts.HtmlContent.Should().BeNull();
        opts.HeightInMm.Should().Be(15);
        opts.ShowDivider.Should().BeFalse();
        opts.SkipOnPages.Should().BeEmpty();
    }

    // ── Null header / footer → not forwarded ─────────────────────────────────

    [Fact]
    public async Task NullHeader_IsPassedAsNull_ToProvider()
    {
        var engine   = new FakeTemplateEngine();
        var provider = new CapturingPdfProvider(engine);
        var request  = new PdfGenerationRequest<InvoiceModel>
        {
            Model         = new InvoiceModel("INV-001", 1m),
            Template      = "<p/>",
            HeaderOptions = null
        };

        await provider.GeneratePdfAsync(request);

        provider.CapturedContext!.HeaderOptions.Should().BeNull();
    }

    [Fact]
    public async Task NullFooter_IsPassedAsNull_ToProvider()
    {
        var engine   = new FakeTemplateEngine();
        var provider = new CapturingPdfProvider(engine);
        var request  = new PdfGenerationRequest<InvoiceModel>
        {
            Model         = new InvoiceModel("INV-001", 1m),
            Template      = "<p/>",
            FooterOptions = null
        };

        await provider.GeneratePdfAsync(request);

        provider.CapturedContext!.FooterOptions.Should().BeNull();
    }

    // ── Content and height forwarded ──────────────────────────────────────────

    [Fact]
    public async Task HeaderOptions_HtmlContentAndHeight_ForwardedToProvider()
    {
        var engine   = new FakeTemplateEngine();
        var provider = new CapturingPdfProvider(engine);
        var header   = new PdfHeaderOptions
        {
            HtmlContent = "<b>Company Name</b>",
            HeightInMm    = 20
        };

        await provider.GeneratePdfAsync(new PdfGenerationRequest<InvoiceModel>
        {
            Model         = new InvoiceModel("INV-001", 1m),
            Template      = "<p/>",
            HeaderOptions = header
        });

        var captured = provider.CapturedContext!.HeaderOptions!;
        captured.HtmlContent.Should().Be("<b>Company Name</b>");
        captured.HeightInMm.Should().Be(20);
    }

    [Fact]
    public async Task FooterOptions_PageTokens_ForwardedToProvider()
    {
        var engine   = new FakeTemplateEngine();
        var provider = new CapturingPdfProvider(engine);
        var footer   = new PdfFooterOptions
        {
            HtmlContent = "Page {page} of {total-pages}",
            HeightInMm    = 10
        };

        await provider.GeneratePdfAsync(new PdfGenerationRequest<InvoiceModel>
        {
            Model         = new InvoiceModel("INV-001", 1m),
            Template      = "<p/>",
            FooterOptions = footer
        });

        provider.CapturedContext!.FooterOptions!.HtmlContent
            .Should().Be("Page {page} of {total-pages}");
    }

    // ── ShowDivider ───────────────────────────────────────────────────────────

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task HeaderOptions_ShowDivider_ForwardedCorrectly(bool showDivider)
    {
        var engine   = new FakeTemplateEngine();
        var provider = new CapturingPdfProvider(engine);

        await provider.GeneratePdfAsync(new PdfGenerationRequest<InvoiceModel>
        {
            Model         = new InvoiceModel("INV-001", 1m),
            Template      = "<p/>",
            HeaderOptions = new PdfHeaderOptions { ShowDivider = showDivider }
        });

        provider.CapturedContext!.HeaderOptions!.ShowDivider.Should().Be(showDivider);
    }

    // ── SkipOnPages ───────────────────────────────────────────────────────────

    [Fact]
    public async Task HeaderOptions_SkipOnPages_ForwardedToProvider()
    {
        var engine   = new FakeTemplateEngine();
        var provider = new CapturingPdfProvider(engine);
        var header   = new PdfHeaderOptions { SkipOnPages = [1, 3] };

        await provider.GeneratePdfAsync(new PdfGenerationRequest<InvoiceModel>
        {
            Model         = new InvoiceModel("INV-001", 1m),
            Template      = "<p/>",
            HeaderOptions = header
        });

        provider.CapturedContext!.HeaderOptions!.SkipOnPages
            .Should().BeEquivalentTo(new[] { 1, 3 });
    }

    [Fact]
    public async Task FooterOptions_SkipOnPages_ForwardedToProvider()
    {
        var engine   = new FakeTemplateEngine();
        var provider = new CapturingPdfProvider(engine);
        var footer   = new PdfFooterOptions { SkipOnPages = [1] };

        await provider.GeneratePdfAsync(new PdfGenerationRequest<InvoiceModel>
        {
            Model         = new InvoiceModel("INV-001", 1m),
            Template      = "<p/>",
            FooterOptions = footer
        });

        provider.CapturedContext!.FooterOptions!.SkipOnPages
            .Should().BeEquivalentTo(new[] { 1 });
    }

    // ── Both header and footer together ───────────────────────────────────────

    [Fact]
    public async Task BothHeaderAndFooter_ForwardedIndependently()
    {
        var engine   = new FakeTemplateEngine();
        var provider = new CapturingPdfProvider(engine);

        await provider.GeneratePdfAsync(new PdfGenerationRequest<InvoiceModel>
        {
            Model         = new InvoiceModel("INV-001", 1m),
            Template      = "<p/>",
            HeaderOptions = new PdfHeaderOptions { HtmlContent = "<b>Header</b>" },
            FooterOptions = new PdfFooterOptions { HtmlContent = "<i>Footer</i>" }
        });

        provider.CapturedContext!.HeaderOptions!.HtmlContent.Should().Be("<b>Header</b>");
        provider.CapturedContext!.FooterOptions!.HtmlContent.Should().Be("<i>Footer</i>");
    }
}
