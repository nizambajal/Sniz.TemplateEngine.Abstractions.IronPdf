namespace TemplateEngine.Abstractions.IronPdf.Options
{
    /// <summary>
    /// Options for rendering an HTML footer on every page.
    /// If null, no footer is rendered.
    /// </summary>
    public class PdfFooterOptions
    {
        /// <summary>
        /// Inline HTML string for the footer content.
        /// You may use {page} and {total-pages} as placeholder tokens.
        /// </summary>
        public string? HtmlContent { get; set; }

        /// <summary>Height of the footer area in millimeters. Default: 15mm</summary>
        public int? HeightInMm { get; set; } = 15;

        /// <summary>Whether to draw a dividing line above the footer. Default: false</summary>
        public bool ShowDivider { get; set; } = false;

        /// <summary>Pages to skip rendering the footer on (e.g., [1] to skip the cover). Default: empty</summary>
        public IReadOnlyList<int> SkipOnPages { get; set; } = [];
    }
}
