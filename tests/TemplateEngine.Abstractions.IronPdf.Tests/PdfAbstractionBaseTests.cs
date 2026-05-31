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
        var engine   = new FakeTemplateEngine();
        var provider = new CapturingPdfProvider(engine);

        Func<Task> act = () => provider.GeneratePdfAsync<InvoiceModel>(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    // ── Template engine is called ─────────────────────────────────────────────

    [Fact]
    public async Task GeneratePdfAsync_CallsTemplateEngine_WithCorrectArguments()
    {
        var engine   = new FakeTemplateEngine();
        var provider = new CapturingPdfProvider(engine);
        var model    = new InvoiceModel("INV-042", 250m);
        var request  = new PdfGenerationRequest<InvoiceModel>
        {
            Model    = model,
            Template = "<h1>{{InvoiceNumber}}</h1>"
        };

        await provider.GeneratePdfAsync(request);

        engine.Calls.Should().HaveCount(1);
        engine.Calls[0].Template.Should().Be("<h1>{{InvoiceNumber}}</h1>");
        engine.Calls[0].Model.Should().Be(model);
    }

    [Fact]
    public async Task GeneratePdfAsync_PassesRenderedHtml_ToProvider()
    {
        const string expectedHtml = "<h1>Rendered!</h1>";
        var engine   = new FakeTemplateEngine((_, _) => expectedHtml);
        var provider = new CapturingPdfProvider(engine);

        await provider.GeneratePdfAsync(Requests.Basic());

        provider.CapturedContext!.Html.Should().Be(expectedHtml);
    }

    // ── Default options are applied when not supplied ─────────────────────────

    [Fact]
    public async Task GeneratePdfAsync_NullDocumentOptions_UsesDefaults()
    {
        var engine   = new FakeTemplateEngine();
        var provider = new CapturingPdfProvider(engine);
        var request  = new PdfGenerationRequest<InvoiceModel>
        {
            Model           = new InvoiceModel("INV-001", 1m),
            Template        = "<p/>",
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
        var engine   = new FakeTemplateEngine();
        var provider = new CapturingPdfProvider(engine);
        var request  = new PdfGenerationRequest<InvoiceModel>
        {
            Model         = new InvoiceModel("INV-001", 1m),
            Template      = "<p/>",
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
        var engine = new FakeTemplateEngine();
        var provider = new CapturingPdfProvider(engine);
        var docOpts = new PdfDocumentOptions
        {
            PaperSize       = PdfPaperSize.Letter,
            Orientation     = PdfOrientation.Landscape,
            Dpi             = 150,
            PrintBackground = false,
            Zoom            = 1,
            RenderDelayMs   = 300,
            Margins         = new PdfMargins { Top = 10, Bottom = 10, Left = 5, Right = 5 }
        };
        var request = new PdfGenerationRequest<InvoiceModel>
        {
            Model           = new InvoiceModel("INV-001", 1m),
            Template        = "<p/>",
            DocumentOptions = docOpts
        };

        await provider.GeneratePdfAsync(request);

        provider.CapturedContext!.DocumentOptions.Should().BeEquivalentTo(docOpts);
    }

    [Fact]
    public async Task GeneratePdfAsync_HeaderOptions_ForwardedToProvider()
    {
        var engine  = new FakeTemplateEngine();
        var provider = new CapturingPdfProvider(engine);
        var header  = new PdfHeaderOptions
        {
            HtmlContent = "<div>Header</div>",
            HeightInMm    = 20,
            ShowDivider = true,
            SkipOnPages = [1]
        };
        var request = new PdfGenerationRequest<InvoiceModel>
        {
            Model         = new InvoiceModel("INV-001", 1m),
            Template      = "<p/>",
            HeaderOptions = header
        };

        await provider.GeneratePdfAsync(request);

        provider.CapturedContext!.HeaderOptions.Should().BeEquivalentTo(header);
    }

    [Fact]
    public async Task GeneratePdfAsync_FooterOptions_ForwardedToProvider()
    {
        var engine   = new FakeTemplateEngine();
        var provider = new CapturingPdfProvider(engine);
        var footer   = new PdfFooterOptions
        {
            HtmlContent = "<div>Page {page} of {total-pages}</div>",
            HeightInMm    = 12,
            ShowDivider = true,
            SkipOnPages = [1]
        };
        var request = new PdfGenerationRequest<InvoiceModel>
        {
            Model         = new InvoiceModel("INV-001", 1m),
            Template      = "<p/>",
            FooterOptions = footer
        };

        await provider.GeneratePdfAsync(request);

        provider.CapturedContext!.FooterOptions.Should().BeEquivalentTo(footer);
    }

    [Fact]
    public async Task GeneratePdfAsync_NullHeaderAndFooter_PassedAsNullToProvider()
    {
        var engine   = new FakeTemplateEngine();
        var provider = new CapturingPdfProvider(engine);

        await provider.GeneratePdfAsync(Requests.Basic());

        provider.CapturedContext!.HeaderOptions.Should().BeNull();
        provider.CapturedContext!.FooterOptions.Should().BeNull();
    }

    // ── Provider is called once ───────────────────────────────────────────────

    [Fact]
    public async Task GeneratePdfAsync_InvokesProvider_ExactlyOnce()
    {
        var engine   = new FakeTemplateEngine();
        var provider = new CapturingPdfProvider(engine);

        await provider.GeneratePdfAsync(Requests.Basic());

        provider.CallCount.Should().Be(1);
    }

    // ── Cancellation token is respected ──────────────────────────────────────

    [Fact]
    public async Task GeneratePdfAsync_CancelledToken_ThrowsOperationCanceledException()
    {
        var cts    = new CancellationTokenSource();
        //var engine = new FakeTemplateEngine(async: true); // cancellation-aware overload below
        var engine = new FakeTemplateEngine(true); // cancellation-aware overload below
        // Use a template engine that honours ct
        var cancellingEngine = new CancellationAwareTemplateEngine(cts);
        var provider         = new CapturingPdfProvider(cancellingEngine);

        await cts.CancelAsync();

        Func<Task> act = () => provider.GeneratePdfAsync(Requests.Basic(), cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    // ── Error propagation ─────────────────────────────────────────────────────

    [Fact]
    public async Task GeneratePdfAsync_TemplateEngineFails_ExceptionBubblesUp()
    {
        var engine   = new ThrowingTemplateEngine(new InvalidOperationException("render boom"));
        var provider = new CapturingPdfProvider(engine);

        Func<Task> act = () => provider.GeneratePdfAsync(Requests.Basic());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("render boom");
    }

    [Fact]
    public async Task GeneratePdfAsync_ProviderFails_ExceptionBubblesUp()
    {
        var engine   = new FakeTemplateEngine();
        var provider = new ThrowingPdfProvider(engine, new InvalidOperationException("provider boom"));

        Func<Task> act = () => provider.GeneratePdfAsync(Requests.Basic());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("provider boom");
    }

    // ── Generic model support ─────────────────────────────────────────────────

    [Fact]
    public async Task GeneratePdfAsync_WorksWithAnyModelType()
    {
        var engine   = new FakeTemplateEngine();
        var provider = new CapturingPdfProvider(engine);
        var request  = new PdfGenerationRequest<EmptyModel>
        {
            Model    = new EmptyModel(),
            Template = "<p>empty</p>"
        };

        var result = await provider.GeneratePdfAsync(request);

        result.Should().NotBeNull();
        result.IsBinary.Should().BeTrue();
    }
}

// ── Helper for cancellation test ──────────────────────────────────────────────

file class CancellationAwareTemplateEngine(CancellationTokenSource cts) : IHtmlTemplateEngine
{
    public Task<string> RenderAsync<T>(string template, T model, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult("<p/>");
    }
}


