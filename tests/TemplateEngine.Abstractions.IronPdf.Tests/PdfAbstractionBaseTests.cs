using FluentAssertions;
using PdfAbstraction.Tests.Infrastructure;
using TemplateEngine.Abstractions.IronPdf;
using TemplateEngine.Abstractions.IronPdf.Options;
using Xunit;

namespace PdfAbstraction.Tests;

/// <summary>
/// Tests for <see cref="PdfAbstractionBase"/> orchestration logic:
/// template rendering, option defaulting, context assembly, error propagation.
/// </summary>
public class PdfAbstractionBaseTests
{
    // ── Null guard ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GeneratePdfAsync_NullRequest_ThrowsArgumentNullException()
    {
        var provider = new CapturingPdfProvider();

        Func<Task> act = () => provider.GeneratePdfAsync<InvoiceModel>(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    // ── Template engine is called ─────────────────────────────────────────────

    [Fact]
    public async Task GeneratePdfAsync_CallsTemplateEngine_WithCorrectArguments()
    {
        var provider = new CapturingPdfProvider();
        var model = new InvoiceModel("INV-042", 250m);
        var request = new PdfGenerationRequest<InvoiceModel>
        {
            Model = model,
            Template = "<h1>{{InvoiceNumber}}</h1>"
        };

        await provider.GeneratePdfAsync(request);

        provider.Engine.Calls.Should().HaveCount(1);
        provider.Engine.Calls[0].Template.Should().Be("<h1>{{InvoiceNumber}}</h1>");
        provider.Engine.Calls[0].Model.Should().Be(model);
    }

    [Fact]
    public async Task GeneratePdfAsync_PassesRenderedHtml_ToProvider()
    {
        const string expectedHtml = "<h1>Rendered!</h1>";
        var provider = new CapturingPdfProvider(new FakeTemplateEngine((_, _) => expectedHtml));

        await provider.GeneratePdfAsync(Requests.Basic());

        provider.CapturedContext!.Html.Should().Be(expectedHtml);
    }

    // ── Default options are applied when not supplied ─────────────────────────

    [Fact]
    public async Task GeneratePdfAsync_NullDocumentOptions_UsesDefaults()
    {
        var provider = new CapturingPdfProvider();
        var request = new PdfGenerationRequest<InvoiceModel>
        {
            Model = new InvoiceModel("INV-001", 1m),
            Template = "<p/>",
            DocumentOptions = null   // explicitly omitted
        };

        await provider.GeneratePdfAsync(request);

        var ctx = provider.CapturedContext!;
        ctx.DocumentOptions.Should().NotBeNull();
        ctx.DocumentOptions.PaperSize.Should().Be(PdfPaperSize.A4);
        ctx.DocumentOptions.Orientation.Should().Be(PdfOrientation.Portrait);
        ctx.DocumentOptions.Dpi.Should().Be(96);
        ctx.DocumentOptions.PrintBackground.Should().BeTrue();
        ctx.DocumentOptions.Zoom.Should().Be(1);
        ctx.DocumentOptions.RenderDelayMs.Should().Be(0);
        ctx.DocumentOptions.Margins.Top.Should().Be(25);
        ctx.DocumentOptions.Margins.Bottom.Should().Be(25);
        ctx.DocumentOptions.Margins.Left.Should().Be(25);
        ctx.DocumentOptions.Margins.Right.Should().Be(25);
    }

    [Fact]
    public async Task GeneratePdfAsync_NullOutputOptions_DefaultsToBinary()
    {
        var provider = new CapturingPdfProvider();
        var request = new PdfGenerationRequest<InvoiceModel>
        {
            Model = new InvoiceModel("INV-001", 1m),
            Template = "<p/>",
            OutputOptions = null
        };

        await provider.GeneratePdfAsync(request);

        var ctx = provider.CapturedContext!;
        ctx.OutputOptions.DataFormat.Should().Be(PdfDataFormat.Binary);
        ctx.OutputOptions.AsStream.Should().BeFalse();
    }

    // ── Supplied options are forwarded unchanged ──────────────────────────────

    [Fact]
    public async Task GeneratePdfAsync_SuppliedDocumentOptions_ForwardedToProvider()
    {
        var provider = new CapturingPdfProvider();
        var docOpts = new PdfDocumentOptions
        {
            PaperSize = PdfPaperSize.Letter,
            Orientation = PdfOrientation.Landscape,
            Dpi = 150,
            PrintBackground = false,
            Zoom = 1,
            RenderDelayMs = 300,
            Margins = new PdfMargins { Top = 10, Bottom = 10, Left = 5, Right = 5 }
        };
        var request = new PdfGenerationRequest<InvoiceModel>
        {
            Model = new InvoiceModel("INV-001", 1m),
            Template = "<p/>",
            DocumentOptions = docOpts
        };

        await provider.GeneratePdfAsync(request);

        provider.CapturedContext!.DocumentOptions.Should().BeEquivalentTo(docOpts);
    }

    [Fact]
    public async Task GeneratePdfAsync_HeaderOptions_ForwardedToProvider()
    {
        var provider = new CapturingPdfProvider();
        var header = new PdfHeaderOptions
        {
            HtmlContent = "<div>Header</div>",
            HeightInMm = 20,
            ShowDivider = true,
            SkipOnPages = [1]
        };
        var request = new PdfGenerationRequest<InvoiceModel>
        {
            Model = new InvoiceModel("INV-001", 1m),
            Template = "<p/>",
            HeaderOptions = header
        };

        await provider.GeneratePdfAsync(request);

        provider.CapturedContext!.HeaderOptions.Should().BeEquivalentTo(header);
    }

    [Fact]
    public async Task GeneratePdfAsync_FooterOptions_ForwardedToProvider()
    {
        var provider = new CapturingPdfProvider();
        var footer = new PdfFooterOptions
        {
            HtmlContent = "<div>Page {page} of {total-pages}</div>",
            HeightInMm = 12,
            ShowDivider = true,
            SkipOnPages = [1]
        };
        var request = new PdfGenerationRequest<InvoiceModel>
        {
            Model = new InvoiceModel("INV-001", 1m),
            Template = "<p/>",
            FooterOptions = footer
        };

        await provider.GeneratePdfAsync(request);

        provider.CapturedContext!.FooterOptions.Should().BeEquivalentTo(footer);
    }

    [Fact]
    public async Task GeneratePdfAsync_NullHeaderAndFooter_PassedAsNullToProvider()
    {
        var provider = new CapturingPdfProvider();

        await provider.GeneratePdfAsync(Requests.Basic());

        provider.CapturedContext!.HeaderOptions.Should().BeNull();
        provider.CapturedContext!.FooterOptions.Should().BeNull();
    }

    // ── Provider is called once ───────────────────────────────────────────────

    [Fact]
    public async Task GeneratePdfAsync_InvokesProvider_ExactlyOnce()
    {
        var provider = new CapturingPdfProvider();

        await provider.GeneratePdfAsync(Requests.Basic());

        provider.CallCount.Should().Be(1);
    }

    // ── Cancellation token is respected ──────────────────────────────────────

    [Fact]
    public async Task GeneratePdfAsync_CancelledToken_ThrowsOperationCanceledException()
    {
        var cts = new CancellationTokenSource();
        var provider = new CancellationAwarePdfProvider(cts);

        await cts.CancelAsync();

        Func<Task> act = () => provider.GeneratePdfAsync(Requests.Basic(), cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    // ── Error propagation ─────────────────────────────────────────────────────

    [Fact]
    public async Task GeneratePdfAsync_TemplateEngineFails_ExceptionBubblesUp()
    {
        var provider = new ThrowingEngineProvider(new InvalidOperationException("render boom"));

        Func<Task> act = () => provider.GeneratePdfAsync(Requests.Basic());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("render boom");
    }

    [Fact]
    public async Task GeneratePdfAsync_ProviderFails_ExceptionBubblesUp()
    {
        var provider = new ThrowingPdfProvider(new InvalidOperationException("provider boom"));

        Func<Task> act = () => provider.GeneratePdfAsync(Requests.Basic());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("provider boom");
    }

    // ── Generic model support ─────────────────────────────────────────────────

    [Fact]
    public async Task GeneratePdfAsync_WorksWithAnyModelType()
    {
        var provider = new CapturingPdfProvider();
        var request = new PdfGenerationRequest<EmptyModel>
        {
            Model = new EmptyModel(),
            Template = "<p>empty</p>"
        };

        var result = await provider.GeneratePdfAsync(request);

        result.Should().NotBeNull();
        result.IsBinary.Should().BeTrue();
    }
}

// ── Helpers for cancellation test ─────────────────────────────────────────────

file class CancellationAwarePdfProvider(CancellationTokenSource cts) : PdfAbstractionBase
{
    protected override TemplateEngine.Abstractions.TemplateEngine CreateTemplateEngine()
        => new CancellationAwareTemplateEngine(cts);

    protected override Task<PdfGenerationResult> RenderToPdfAsync(
        PdfRenderContext context, CancellationToken ct)
        => Task.FromResult(PdfGenerationResult.FromBytes("%PDF"u8.ToArray()));
}

file class CancellationAwareTemplateEngine(CancellationTokenSource cts)
    : TemplateEngine.Abstractions.TemplateEngine
{
    public override string Render<T>(T model, string template)
    {
        cts.Token.ThrowIfCancellationRequested();
        return "<p/>";
    }

    public override string ResolveProperty(object model, string propertyExpression)
        => string.Empty;
}