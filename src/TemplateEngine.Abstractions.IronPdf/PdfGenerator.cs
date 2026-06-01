using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TemplateEngine.Abstractions.IronPdf.Providers;

namespace TemplateEngine.Abstractions.IronPdf
{
    /// <summary>
    /// The single public entry point for PDF generation.
    ///
    /// Usage:
    /// <code>
    /// var generator = new PdfGenerator();
    /// var result = await generator.GenerateAsync(new PdfGenerationRequest&lt;InvoiceModel&gt;
    /// {
    ///     Model    = invoice,
    ///     Template = "&lt;h1&gt;{{InvoiceNumber}}&lt;/h1&gt;",
    ///     // DocumentOptions, HeaderOptions, FooterOptions, OutputOptions are all optional
    /// });
    /// </code>
    /// </summary>
    public sealed class PdfGenerator
    {
        private readonly PdfAbstractionBase _provider = new IronPdfProvider();

        /// <summary>
        /// Renders <paramref name="request.Template"/> with <paramref name="request.Model"/>
        /// and converts the result to a PDF using IronPDF.
        /// </summary>
        /// <typeparam name="T">Type of the view model bound to the template.</typeparam>
        /// <param name="request">
        /// The generation request. Only <c>Model</c> and <c>Template</c> are required;
        /// all options default to sensible values when omitted.
        /// </param>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>
        /// A <see cref="PdfGenerationResult"/> containing the PDF as bytes, a stream,
        /// or a file path — depending on the <see cref="PdfOutputOptions"/> supplied.
        /// </returns>
        public Task<PdfGenerationResult> GenerateAsync<T>(
            PdfGenerationRequest<T> request,
            CancellationToken ct = default)
            => _provider.GeneratePdfAsync(request, ct);
    }
}
