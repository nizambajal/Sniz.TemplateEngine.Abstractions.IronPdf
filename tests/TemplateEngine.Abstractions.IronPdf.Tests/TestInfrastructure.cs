using TemplateEngine.Abstractions.IronPdf;
using TemplateEngine.Abstractions.IronPdf.Options;

namespace PdfAbstraction.Tests.Infrastructure;

// ── Fake models ───────────────────────────────────────────────────────────────

public record InvoiceModel(string InvoiceNumber, decimal Total);
public record EmptyModel();

// ── Fake template engines ─────────────────────────────────────────────────────

/// <summary>
/// Test double for <see cref="TemplateEngine.Abstractions.TemplateEngine"/>.
/// Records every Render call so tests can assert on what was passed in.
/// Default output: "&lt;p&gt;{model}&lt;/p&gt;". Pass a factory func to customise per-test.
/// </summary>
public class FakeTemplateEngine : TemplateEngine.Abstractions.TemplateEngine
{
    private readonly Func<object?, string, string>? _renderer;

    public FakeTemplateEngine(Func<object?, string, string>? renderer = null)
        => _renderer = renderer;

    public List<(string Template, object? Model)> Calls { get; } = [];

    public override string Render<T>(T model, string template)
    {
        Calls.Add((template, model));
        return _renderer is not null
            ? _renderer(model, template)
            : $"<p>{model}</p>";
    }

    public override string ResolveProperty(object model, string propertyExpression)
        => string.Empty; // not exercised in PDF generation tests
}

/// <summary>
/// Template engine that always throws from <see cref="Render{T}"/>.
/// Used to verify that rendering errors bubble up through the pipeline.
/// </summary>
public class ThrowingTemplateEngine : TemplateEngine.Abstractions.TemplateEngine
{
    private readonly Exception _exception;

    public ThrowingTemplateEngine(Exception? ex = null)
        => _exception = ex ?? new InvalidOperationException("Template engine failed.");

    public override string Render<T>(T model, string template) => throw _exception;

    public override string ResolveProperty(object model, string propertyExpression)
        => throw _exception;
}

// ── Fake PDF providers ────────────────────────────────────────────────────────

/// <summary>
/// Captures the <see cref="PdfRenderContext"/> passed down from the base class
/// and returns a pre-canned <see cref="PdfGenerationResult"/>.
/// Substitutes the real template engine via <c>CreateTemplateEngine()</c> override.
/// </summary>
internal class CapturingPdfProvider : PdfAbstractionBase
{
    private readonly FakeTemplateEngine _engine;
    private readonly Func<PdfRenderContext, PdfGenerationResult> _resultFactory;

    public CapturingPdfProvider(
        FakeTemplateEngine? engine = null,
        Func<PdfRenderContext, PdfGenerationResult>? resultFactory = null)
    {
        _engine = engine ?? new FakeTemplateEngine();
        _resultFactory = resultFactory ?? (_ => PdfGenerationResult.FromBytes(FakePdfBytes));
    }

    /// <summary>Exposes the fake engine so tests can inspect its Calls list.</summary>
    public FakeTemplateEngine Engine => _engine;

    public PdfRenderContext? CapturedContext { get; private set; }
    public int CallCount { get; private set; }

    public static readonly byte[] FakePdfBytes = "%PDF-fake"u8.ToArray();

    protected override TemplateEngine.Abstractions.TemplateEngine CreateTemplateEngine() => _engine;

    protected override Task<PdfGenerationResult> RenderToPdfAsync(
        PdfRenderContext context,
        CancellationToken ct)
    {
        CapturedContext = context;
        CallCount++;
        return Task.FromResult(_resultFactory(context));
    }
}

/// <summary>
/// Provider that installs a <see cref="ThrowingTemplateEngine"/> — verifies that
/// template rendering errors propagate out of <c>GenerateAsync</c>.
/// </summary>
internal class ThrowingEngineProvider : PdfAbstractionBase
{
    private readonly ThrowingTemplateEngine _engine;

    public ThrowingEngineProvider(Exception? ex = null)
        => _engine = new ThrowingTemplateEngine(ex);

    protected override TemplateEngine.Abstractions.TemplateEngine CreateTemplateEngine() => _engine;

    protected override Task<PdfGenerationResult> RenderToPdfAsync(
        PdfRenderContext context, CancellationToken ct)
        => Task.FromResult(PdfGenerationResult.FromBytes("%PDF"u8.ToArray()));
}

/// <summary>
/// Provider whose <c>RenderToPdfAsync</c> throws — verifies that provider errors
/// propagate correctly after a successful template render.
/// </summary>
internal class ThrowingPdfProvider : PdfAbstractionBase
{
    private readonly Exception _exception;

    public ThrowingPdfProvider(Exception? ex = null)
        => _exception = ex ?? new InvalidOperationException("Provider failed.");

    protected override TemplateEngine.Abstractions.TemplateEngine CreateTemplateEngine()
        => new FakeTemplateEngine();

    protected override Task<PdfGenerationResult> RenderToPdfAsync(
        PdfRenderContext context, CancellationToken ct)
        => throw _exception;
}

// ── Request builder helpers ───────────────────────────────────────────────────

public static class Requests
{
    public static PdfGenerationRequest<InvoiceModel> Basic(
        string template = "<h1>Invoice</h1>",
        InvoiceModel? model = null) =>
        new()
        {
            Template = template,
            Model = model ?? new InvoiceModel("INV-001", 99.99m)
        };

    public static PdfGenerationRequest<InvoiceModel> WithOutput(
        PdfOutputOptions output,
        string template = "<h1>Invoice</h1>") =>
        new()
        {
            Template = template,
            Model = new InvoiceModel("INV-001", 99.99m),
            OutputOptions = output
        };
}