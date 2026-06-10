namespace TemplateEngine.Abstractions.IronPdf.Options
{
    /// <summary>
    /// Controls how the PDF output is returned to the caller.
    /// </summary>
    public class PdfOutputOptions
    {
        /// <summary>
        /// The format of the returned data.
        /// Default: Binary (byte[])
        /// </summary>
        public PdfDataFormat DataFormat { get; set; } = PdfDataFormat.Binary;

        /// <summary>
        /// If true, returns data as a stream instead of the resolved format type.
        /// Works with both Binary and File formats.
        /// Default: false
        /// </summary>
        public bool AsStream { get; set; } = false;

        /// <summary>
        /// Required when DataFormat is set to File.
        /// The full output path where the PDF file will be saved.
        /// </summary>
        public string? FilePath { get; set; }
        
        /// <summary>
        /// Required when DataFormat is set to File.
        /// The full output path where the PDF file will be saved.
        /// </summary>
        public string? DocumentName { get; set; }
    }

    public enum PdfDataFormat
    {
        /// <summary>Returns content as HTML.</summary>
        HtmlContent,

        /// <summary>Returns PDF as a byte array.</summary>
        Binary,

        /// <summary>Saves PDF to disk and returns the file path.</summary>
        File
    }
}
