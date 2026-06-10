using TemplateEngine.Abstractions.IronPdf.Options;

namespace TemplateEngine.Abstractions.IronPdf
{
    /// <summary>
    /// Provider-agnostic abstraction layer for HTML-template → PDF generation.
    ///
    /// Flow:
    ///   1. Caller provides a <see cref="PdfGenerationRequest{T}"/> (model, template, options).
    ///   2. <see cref="GeneratePdfAsync{T}"/> renders the template to HTML via the
    ///      built-in <see cref="TemplateEngine"/> instance, then delegates to the provider.
    ///   3. The provider produces the PDF and returns a <see cref="PdfGenerationResult"/>
    ///      shaped by the caller's <see cref="PdfOutputOptions"/>.
    ///
    /// The template engine is an internal detail — callers only interact with
    /// <see cref="GeneratePdfAsync{T}"/> and the options DTOs.
    /// </summary>
    internal abstract class PdfAbstractionBase
    {
        // Lazy: resolved on first call to GeneratePdfAsync, not in the constructor.
        // This avoids the classic C# pitfall where a virtual method is called from base()
        // before the subclass constructor has run and assigned its fields.
        private TemplateEngine? _templateEngine;
        private TemplateEngine TemplateEngine => _templateEngine ??= CreateTemplateEngine();

        protected PdfAbstractionBase() { }

        /// <summary>
        /// Factory for the template engine.
        /// Returns <see cref="HtmlTemplateEngine"/> by default.
        /// Override in tests to substitute a fake — called once, on first use.
        /// </summary>
        protected virtual TemplateEngine CreateTemplateEngine() => new HtmlTemplateEngine();

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Renders <paramref name="request.Template"/> with <paramref name="request.Model"/>,
        /// converts the resulting HTML to a PDF, and returns the result in the format
        /// specified by <see cref="PdfOutputOptions"/>.
        /// </summary>
        public async Task<PdfGenerationResult> GeneratePdfAsync<T>(
            PdfGenerationRequest<T> request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            // Step 1 — Render template → HTML (synchronous, CPU-bound)
            string html = TemplateEngine.Render(request.Model, request.Template);

            // Step 2 — Resolve effective options (apply defaults for nulls)
            var documentOptions = request.DocumentOptions ?? new PdfDocumentOptions();
            var outputOptions = request.OutputOptions ?? new PdfOutputOptions();

            // Return html content immediately if the caller only wants that (e.g. for debugging).
            if (outputOptions.DataFormat == PdfDataFormat.HtmlContent)
            {
                return PdfGenerationResult.FromHtml(html);
            }

            // Step 3 — Delegate to provider (async, real I/O)
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
        /// Implemented by each concrete provider (e.g. IronPDF, Puppeteer, wkhtmltopdf).
        /// Receives fully-resolved HTML and options; must return a <see cref="PdfGenerationResult"/>
        /// matching the <see cref="PdfRenderContext.OutputOptions"/> format.
        /// </summary>
        protected abstract Task<PdfGenerationResult> RenderToPdfAsync(
            PdfRenderContext context,
            CancellationToken ct);
    }

    /// <summary>
    /// All resolved inputs passed down to a provider's <c>RenderToPdfAsync</c>.
    /// </summary>
    internal sealed record PdfRenderContext(
        string Html,
        PdfDocumentOptions DocumentOptions,
        PdfHeaderOptions? HeaderOptions,
        PdfFooterOptions? FooterOptions,
        PdfOutputOptions OutputOptions);
}
