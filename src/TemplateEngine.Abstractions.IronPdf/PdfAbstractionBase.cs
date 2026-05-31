using TemplateEngine.Abstractions.IronPdf.Options;

namespace TemplateEngine.Abstractions.IronPdf
{
    /// <summary>
    /// Provider-agnostic abstraction layer for HTML-template → PDF generation.
    ///
    /// Flow:
    ///   1. Caller provides a <see cref="PdfGenerationRequest{T}"/> (model, template, options).
    ///   2. <see cref="GeneratePdfAsync{T}"/> renders the template to HTML via the
    ///      injected template engine, then delegates to the provider implementation.
    ///   3. The provider produces the PDF and returns a <see cref="PdfGenerationResult"/>
    ///      shaped by the caller's <see cref="PdfOutputOptions"/>.
    ///
    /// Subclasses must implement:
    ///   - <see cref="RenderToPdfAsync"/> — provider-specific HTML → PDF conversion.
    /// </summary>
    public abstract class PdfAbstractionBase
    {
        private readonly IHtmlTemplateEngine _templateEngine;

        protected PdfAbstractionBase(IHtmlTemplateEngine templateEngine)
        {
            _templateEngine = templateEngine;
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Renders <paramref name="request.Template"/> with <paramref name="request.Model"/>,
        /// converts the resulting HTML to a PDF using the configured provider,
        /// and returns the result in the format specified by <see cref="PdfOutputOptions"/>.
        /// </summary>
        public async Task<PdfGenerationResult> GeneratePdfAsync<T>(
            PdfGenerationRequest<T> request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            // Step 1 — Render template → HTML
            string html = await _templateEngine.RenderAsync(request.Template, request.Model, ct);

            // Step 2 — Resolve effective options (apply defaults for nulls)
            var documentOptions = request.DocumentOptions ?? new PdfDocumentOptions();
            var outputOptions = request.OutputOptions ?? new PdfOutputOptions();

            // Step 3 — Delegate to provider
            var context = new PdfRenderContext(
                Html: html,
                DocumentOptions: documentOptions,
                HeaderOptions: request.HeaderOptions,
                FooterOptions: request.FooterOptions,
                OutputOptions: outputOptions);

            return await RenderToPdfAsync(context, ct);
        }

        // ── Provider contract ─────────────────────────────────────────────────────

        /// <summary>
        /// Implemented by each concrete provider (e.g. IronPdf, Puppeteer, wkhtmltopdf).
        /// Receives fully-resolved HTML and options; must return a <see cref="PdfGenerationResult"/>
        /// that matches the <see cref="PdfRenderContext.OutputOptions"/> format.
        /// </summary>
        protected abstract Task<PdfGenerationResult> RenderToPdfAsync(
            PdfRenderContext context,
            CancellationToken ct);
    }

    /// <summary>
    /// All resolved inputs passed down to a provider's <c>RenderToPdfAsync</c>.
    /// </summary>
    public sealed record PdfRenderContext(
        string Html,
        PdfDocumentOptions DocumentOptions,
        PdfHeaderOptions? HeaderOptions,
        PdfFooterOptions? FooterOptions,
        PdfOutputOptions OutputOptions);
}
