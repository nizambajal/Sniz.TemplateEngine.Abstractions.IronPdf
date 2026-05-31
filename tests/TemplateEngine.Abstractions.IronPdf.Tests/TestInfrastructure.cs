using TemplateEngine.Abstractions.IronPdf;
using TemplateEngine.Abstractions.IronPdf.Options;

namespace PdfAbstraction.Tests.Infrastructure;

// ── Fake models ───────────────────────────────────────────────────────────────

public record InvoiceModel(string InvoiceNumber, decimal Total);
public record EmptyModel();

// ── Fake template engine ──────────────────────────────────────────────────────

/// <summary>
/// Returns a predictable HTML string so tests are fully deterministic.
/// By default renders "<p>{model}</p>"; can be overridden per-test.
/// </summary>
public class FakeTemplateEngine : IHtmlTemplateEngine
{
    private readonly Func<object?, string, string>? _renderer;

    public FakeTemplateEngine(Func<object?, string, string>? renderer = null)
        => _renderer = renderer;

    /// <summary>Overload used by cancellation tests — the bool param is unused.</summary>
    public FakeTemplateEngine(bool cancellationAware)
        => _renderer = null;

    public List<(string Template, object? Model)> Calls { get; } = [];

    public Task<string> RenderAsync<T>(string template, T model, CancellationToken ct = default)
    {
        Calls.Add((template, model));
        var html = _renderer is not null
            ? _renderer(model, template)
            : $"<p>{model}</p>";
        return Task.FromResult(html);
    }
}

/// <summary>
/// Template engine that throws, used to verify error propagation.
/// </summary>
public class ThrowingTemplateEngine : IHtmlTemplateEngine
{
    private readonly Exception _exception;
    public ThrowingTemplateEngine(Exception? ex = null)
        => _exception = ex ?? new InvalidOperationException("Template engine failed.");

    public Task<string> RenderAsync<T>(string template, T model, CancellationToken ct = default)
        => throw _exception;
}

// ── Fake PDF provider ─────────────────────────────────────────────────────────

/// <summary>
/// Captures the PdfRenderContext it receives and returns a pre-canned result.
/// Lets us assert on exactly what the base class passed down.
/// </summary>
public class CapturingPdfProvider : PdfAbstractionBase
{
    private readonly Func<PdfRenderContext, PdfGenerationResult> _resultFactory;

    public CapturingPdfProvider(
        IHtmlTemplateEngine engine,
        Func<PdfRenderContext, PdfGenerationResult>? resultFactory = null)
        : base(engine)
    {
        _resultFactory = resultFactory
            ?? (_ => PdfGenerationResult.FromBytes(FakePdfBytes));
    }

    public PdfRenderContext? CapturedContext { get; private set; }
    public int CallCount { get; private set; }

    /// <summary>Canonical fake PDF payload used across all tests.</summary>
    public static readonly byte[] FakePdfBytes = "%PDF-fake"u8.ToArray();

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
/// Provider whose RenderToPdfAsync throws, used to verify error propagation.
/// </summary>
public class ThrowingPdfProvider : PdfAbstractionBase
{
    private readonly Exception _exception;

    public ThrowingPdfProvider(IHtmlTemplateEngine engine, Exception? ex = null)
        : base(engine)
        => _exception = ex ?? new InvalidOperationException("Provider failed.");

    protected override Task<PdfGenerationResult> RenderToPdfAsync(
        PdfRenderContext context,
        CancellationToken ct)
        => throw _exception;
}

// ── Builder helpers ───────────────────────────────────────────────────────────

public static class Requests
{
    public static PdfGenerationRequest<InvoiceModel> Basic(
        string template = "<h1>Invoice</h1>",
        InvoiceModel? model = null) =>
        new()
        {
            Template = template,
            Model    = model ?? new InvoiceModel("INV-001", 99.99m)
        };

    public static PdfGenerationRequest<InvoiceModel> WithOutput(
        PdfOutputOptions output,
        string template = "<h1>Invoice</h1>") =>
        new()
        {
            Template      = template,
            Model         = new InvoiceModel("INV-001", 99.99m),
            OutputOptions = output
        };
}
