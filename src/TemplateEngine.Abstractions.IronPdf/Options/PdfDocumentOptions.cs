namespace TemplateEngine.Abstractions.IronPdf.Options
{
    /// <summary>
    /// Controls the physical layout and rendering of the PDF document.
    /// All properties are optional — defaults are applied if not set.
    /// </summary>
    public class PdfDocumentOptions
    {
        /// <summary>Paper size. Default: A4</summary>
        public PdfPaperSize PaperSize { get; set; } = PdfPaperSize.A4;

        /// <summary>Page orientation. Default: Portrait</summary>
        public PdfOrientation Orientation { get; set; } = PdfOrientation.Portrait;

        /// <summary>Page margins in millimeters. Default: 25mm on all sides</summary>
        public PdfMargins Margins { get; set; } = PdfMargins.Default;

        /// <summary>DPI for rendering. Default: 96</summary>
        public int Dpi { get; set; } = 96;

        /// <summary>Whether to print background colors and images. Default: true</summary>
        public bool PrintBackground { get; set; } = true;

        /// <summary>Zoom factor for rendering. Default: 1.0</summary>
        public int Zoom { get; set; } = 1;

        /// <summary>
        /// Wait time in milliseconds before rendering (useful for JS-heavy templates).
        /// Default: 0
        /// </summary>
        public int RenderDelayMs { get; set; } = 0;
    }

    public class PdfMargins
    {
        public double Top { get; set; }
        public double Bottom { get; set; }
        public double Left { get; set; }
        public double Right { get; set; }

        /// <summary>25mm on all sides.</summary>
        public static PdfMargins Default => new() { Top = 25, Bottom = 25, Left = 25, Right = 25 };

        /// <summary>No margins.</summary>
        public static PdfMargins None => new() { Top = 0, Bottom = 0, Left = 0, Right = 0 };
    }

    public enum PdfPaperSize
    {
        A4,
        A3,
        A5,
        Letter,
        Legal,
        Tabloid
    }

    public enum PdfOrientation
    {
        Portrait,
        Landscape
    }
}
