namespace TemplateEngine.Abstractions.IronPdf
{
    /// <summary>
    /// The result of a PDF generation call.
    /// Exactly one of the payload properties will be non-null,
    /// depending on the <see cref="Options.PdfOutputOptions"/> supplied.
    /// </summary>
    public sealed class PdfGenerationResult : IAsyncDisposable, IDisposable
    {
        // ── Payloads (mutually exclusive) ────────────────────────────────────────

        /// <summary>Raw PDF bytes. Set when DataFormat = Binary and AsStream = false.</summary>
        public byte[]? Bytes { get; private init; }

        /// <summary>Readable PDF stream. Set when AsStream = true (any DataFormat).</summary>
        public Stream? Stream { get; private init; }

        /// <summary>Absolute path to the written PDF file. Set when DataFormat = File and AsStream = false.</summary>
        public string? FilePath { get; private init; }

        /// <summary>HTML content. Set when DataFormat = HtmlContent and AsStream = false.</summary>
        public string? HtmlContent { get; private init; }

        // ── Factories ─────────────────────────────────────────────────────────────

        public static PdfGenerationResult FromBytes(byte[] bytes) =>
            new() { Bytes = bytes };

        public static PdfGenerationResult FromStream(Stream stream) =>
            new() { Stream = stream };

        public static PdfGenerationResult FromFile(string filePath) =>
            new() { FilePath = filePath };

        public static PdfGenerationResult FromHtml(string htmlContent) =>
            new() { HtmlContent = htmlContent };

        // ── Helpers ───────────────────────────────────────────────────────────────

        /// <summary>True when this result carries raw binary data.</summary>
        public bool IsBinary => Bytes is not null;

        /// <summary>True when this result carries a stream.</summary>
        public bool IsStream => Stream is not null;

        /// <summary>True when this result refers to a saved file.</summary>
        public bool IsFile => FilePath is not null;

        /// <summary>True when this result carries HTML content.</summary>
        public bool IsHtmlContent => HtmlContent is not null;

        /// <summary>
        /// Coerces any result variant to a byte array.
        /// Reads the stream to end if necessary (does not reset position).
        /// </summary>
        public async Task<byte[]> ToBytesAsync(CancellationToken ct = default)
        {
            if (Bytes is not null) return Bytes;

            if (Stream is not null)
            {
                using var ms = new MemoryStream();
                await Stream.CopyToAsync(ms, ct);
                return ms.ToArray();
            }

            if (FilePath is not null)
                return await File.ReadAllBytesAsync(FilePath, ct);

            if (HtmlContent is not null)
                return System.Text.Encoding.UTF8.GetBytes(HtmlContent);

            throw new InvalidOperationException("PdfGenerationResult has no payload.");
        }

        // ── Disposal ──────────────────────────────────────────────────────────────

        public void Dispose() => Stream?.Dispose();

        public async ValueTask DisposeAsync()
        {
            if (Stream is not null)
                await Stream.DisposeAsync();
        }
    }
}
