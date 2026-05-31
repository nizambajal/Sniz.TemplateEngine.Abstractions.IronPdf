using FluentAssertions;
using PdfAbstraction.Tests.Infrastructure;
using TemplateEngine.Abstractions.IronPdf;
using TemplateEngine.Abstractions.IronPdf.Options;
using Xunit;

namespace PdfAbstraction.Tests;

/// <summary>
/// Tests for <see cref="PdfRenderContext"/> assembly and <see cref="PdfGenerationRequest{T}"/> validation.
/// </summary>
public class PdfRenderContextTests
{
    // ── Context is a value record ─────────────────────────────────────────────

    [Fact]
    public void PdfRenderContext_EqualityByValue()
    {
        var docOpts = new PdfDocumentOptions();
        var outOpts = new PdfOutputOptions();

        var a = new PdfRenderContext("<p/>", docOpts, null, null, outOpts);
        var b = new PdfRenderContext("<p/>", docOpts, null, null, outOpts);

        a.Should().Be(b);
    }

    [Fact]
    public void PdfRenderContext_DifferentHtml_NotEqual()
    {
        var docOpts = new PdfDocumentOptions();
        var outOpts = new PdfOutputOptions();

        var a = new PdfRenderContext("<p>A</p>", docOpts, null, null, outOpts);
        var b = new PdfRenderContext("<p>B</p>", docOpts, null, null, outOpts);

        a.Should().NotBe(b);
    }

    // ── Context carries all resolved data ─────────────────────────────────────

    [Fact]
    public async Task Context_HtmlIsRenderedOutput_NotRawTemplate()
    {
        const string rawTemplate  = "{{Name}}";
        const string renderedHtml = "<h1>Alice</h1>";

        var engine   = new FakeTemplateEngine((_, _) => renderedHtml);
        var provider = new CapturingPdfProvider(engine);

        await provider.GeneratePdfAsync(new PdfGenerationRequest<InvoiceModel>
        {
            Model    = new InvoiceModel("INV-001", 1m),
            Template = rawTemplate
        });

        provider.CapturedContext!.Html.Should().Be(renderedHtml);
        provider.CapturedContext!.Html.Should().NotBe(rawTemplate);
    }

    // ── Request init properties ───────────────────────────────────────────────

    [Fact]
    public void PdfGenerationRequest_RequiredProperties_MustBeSet()
    {
        // `required` keyword enforced at compile time; we verify the values are stored.
        var request = new PdfGenerationRequest<InvoiceModel>
        {
            Model    = new InvoiceModel("INV-999", 0m),
            Template = "<p>test</p>"
        };

        request.Model.InvoiceNumber.Should().Be("INV-999");
        request.Template.Should().Be("<p>test</p>");
    }

    [Fact]
    public void PdfGenerationRequest_OptionalProperties_DefaultToNull()
    {
        var request = new PdfGenerationRequest<InvoiceModel>
        {
            Model    = new InvoiceModel("INV-001", 1m),
            Template = "<p/>"
        };

        request.DocumentOptions.Should().BeNull();
        request.HeaderOptions.Should().BeNull();
        request.FooterOptions.Should().BeNull();
        request.OutputOptions.Should().BeNull();
    }

    // ── Full context assembly ─────────────────────────────────────────────────

    [Fact]
    public async Task FullRequest_AllContextFields_PopulatedCorrectly()
    {
        var engine   = new FakeTemplateEngine((_, _) => "<html>final</html>");
        var provider = new CapturingPdfProvider(engine);

        var docOpts  = new PdfDocumentOptions { Dpi = 200 };
        var header   = new PdfHeaderOptions   { HtmlContent = "<b>H</b>" };
        var footer   = new PdfFooterOptions   { HtmlContent = "<i>F</i>" };
        var outOpts  = new PdfOutputOptions   { AsStream = true };

        await provider.GeneratePdfAsync(new PdfGenerationRequest<InvoiceModel>
        {
            Model           = new InvoiceModel("INV-001", 1m),
            Template        = "irrelevant",
            DocumentOptions = docOpts,
            HeaderOptions   = header,
            FooterOptions   = footer,
            OutputOptions   = outOpts
        });

        var ctx = provider.CapturedContext!;
        ctx.Html.Should().Be("<html>final</html>");
        ctx.DocumentOptions.Dpi.Should().Be(200);
        ctx.HeaderOptions!.HtmlContent.Should().Be("<b>H</b>");
        ctx.FooterOptions!.HtmlContent.Should().Be("<i>F</i>");
        ctx.OutputOptions.AsStream.Should().BeTrue();
    }
}
