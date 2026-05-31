using TemplateEngine.Abstractions.IronPdf.Options;

namespace TemplateEngine.Abstractions.IronPdf
{
    /// <summary>
    /// The full request handed to the abstraction layer to produce a PDF.
    /// </summary>
    /// <typeparam name="T">Type of the model bound to the HTML template.</typeparam>
    public class PdfGenerationRequest<T>
    {
        /// <summary>The model whose properties are bound into the template. Required.</summary>
        public required T Model { get; init; }

        /// <summary>The HTML template string (supports your existing template engine syntax). Required.</summary>
        public required string Template { get; init; }

        /// <summary>
        /// PDF document layout/rendering options.
        /// Falls back to <see cref="PdfDocumentOptions"/> defaults when null.
        /// </summary>
        public PdfDocumentOptions? DocumentOptions { get; init; }

        /// <summary>
        /// Header rendered on every page.
        /// No header is added when null.
        /// </summary>
        public PdfHeaderOptions? HeaderOptions { get; init; }

        /// <summary>
        /// Footer rendered on every page.
        /// No footer is added when null.
        /// </summary>
        public PdfFooterOptions? FooterOptions { get; init; }

        /// <summary>
        /// Controls output format (binary / file) and streaming.
        /// Falls back to binary output when null.
        /// </summary>
        public PdfOutputOptions? OutputOptions { get; init; }
    }
}
