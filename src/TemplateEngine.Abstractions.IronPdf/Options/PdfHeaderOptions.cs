namespace TemplateEngine.Abstractions.IronPdf.Options
{
    /// <summary>
    /// Options for rendering an HTML header on every page.
    /// If null, no header is rendered.
    /// </summary>
    public class PdfHeaderOptions
    {
        /// <summary>
        /// Inline HTML string for the header content.
        /// Supports the same template variables as the main body.
        /// </summary>
        public string? HtmlContent { get; set; }

        /// <summary>Height of the header area in millimeters. Default: 15mm</summary>
        public int? HeightInMm { get; set; } = 15;

        /// <summary>Whether to draw a dividing line below the header. Default: false</summary>
        public bool ShowDivider { get; set; } = false;

        /// <summary>Pages to skip rendering the header on (e.g., [1] to skip the cover). Default: empty</summary>
        public IReadOnlyList<int> SkipOnPages { get; set; } = [];
    }
}
