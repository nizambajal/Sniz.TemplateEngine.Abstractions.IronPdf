using IronPdf;
using IronPdf.Rendering;
using TemplateEngine.Abstractions.IronPdf.Options;
using PdfPaperSize2 = IronPdf.Rendering.PdfPaperSize;
using PdfPaperSize = TemplateEngine.Abstractions.IronPdf.Options.PdfPaperSize;

namespace TemplateEngine.Abstractions.IronPdf.Providers
{
    /// <summary>
    /// IronPDF provider implementation of <see cref="PdfAbstractionBase"/>.
    /// Translates the provider-agnostic <see cref="PdfRenderContext"/> into IronPDF API calls.
    /// </summary>
    internal sealed class IronPdfProvider : PdfAbstractionBase
    {
        public IronPdfProvider() : base() { }

        protected override async Task<PdfGenerationResult> RenderToPdfAsync(
            PdfRenderContext context,
            CancellationToken ct)
        {
            var renderer = BuildRenderer(context.DocumentOptions);

            ApplyHeader(renderer, context.HeaderOptions);
            ApplyFooter(renderer, context.FooterOptions);

            // IronPDF's async render
            PdfDocument pdf = await renderer.RenderHtmlAsPdfAsync(context.Html);

            return await BuildResultAsync(pdf, context.OutputOptions, ct);
        }

        // ── Renderer setup ────────────────────────────────────────────────────────

        private static ChromePdfRenderer BuildRenderer(PdfDocumentOptions opts)
        {
            var renderer = new ChromePdfRenderer();

            renderer.RenderingOptions.PaperSize = MapPaperSize(opts.PaperSize);
            renderer.RenderingOptions.PaperOrientation = MapOrientation(opts.Orientation);
            //renderer.RenderingOptions.Dpi = opts.Dpi;
            renderer.RenderingOptions.PrintHtmlBackgrounds = opts.PrintBackground;
            renderer.RenderingOptions.Zoom = opts.Zoom;

            if (opts.RenderDelayMs > 0)
                renderer.RenderingOptions.WaitFor.RenderDelay(opts.RenderDelayMs);

            renderer.RenderingOptions.MarginTop = opts.Margins.Top;
            renderer.RenderingOptions.MarginBottom = opts.Margins.Bottom;
            renderer.RenderingOptions.MarginLeft = opts.Margins.Left;
            renderer.RenderingOptions.MarginRight = opts.Margins.Right;

            return renderer;
        }

        private static void ApplyHeader(ChromePdfRenderer renderer, PdfHeaderOptions? opts)
        {
            if (opts?.HtmlContent is null) return;

            renderer.RenderingOptions.HtmlHeader = new HtmlHeaderFooter
            {
                HtmlFragment = opts.HtmlContent,
                MaxHeight = opts.HeightInMm,
                DrawDividerLine = opts.ShowDivider
            };
        }

        private static void ApplyFooter(ChromePdfRenderer renderer, PdfFooterOptions? opts)
        {
            if (opts?.HtmlContent is null) return;

            renderer.RenderingOptions.HtmlFooter = new HtmlHeaderFooter
            {
                HtmlFragment = opts.HtmlContent,
                MaxHeight = opts.HeightInMm,
                DrawDividerLine = opts.ShowDivider
            };
        }

        // ── Result packaging ──────────────────────────────────────────────────────

        private static async Task<PdfGenerationResult> BuildResultAsync(
            PdfDocument pdf,
            PdfOutputOptions opts,
            CancellationToken ct)
        {
            if (opts.AsStream)
            {
                // IronPDF → MemoryStream, caller reads/disposes it
                var ms = new MemoryStream(pdf.BinaryData);
                return PdfGenerationResult.FromStream(ms);
            }

            switch (opts.DataFormat)
            {
                case PdfDataFormat.File:
                    {
                        if (string.IsNullOrWhiteSpace(opts.FilePath))
                            throw new InvalidOperationException(
                                $"{nameof(PdfOutputOptions.FilePath)} must be set when {nameof(PdfDataFormat.File)} is selected.");

                        await File.WriteAllBytesAsync(opts.FilePath, pdf.BinaryData, ct);
                        return PdfGenerationResult.FromFile(opts.FilePath);
                    }

                case PdfDataFormat.Binary:
                default:
                    return PdfGenerationResult.FromBytes(pdf.BinaryData);
            }
        }

        // ── Enum mappings ─────────────────────────────────────────────────────────

        private static PdfPaperSize2 MapPaperSize(PdfPaperSize size) => size switch
        {
            PdfPaperSize.A3 => PdfPaperSize2.A3,
            PdfPaperSize.A4 => PdfPaperSize2.A4,
            PdfPaperSize.A5 => PdfPaperSize2.A5,
            PdfPaperSize.Letter => PdfPaperSize2.Letter,
            PdfPaperSize.Legal => PdfPaperSize2.Legal,
            PdfPaperSize.Tabloid => PdfPaperSize2.Tabloid,
            _ => PdfPaperSize2.A4
        };

        private static PdfPaperOrientation MapOrientation(PdfOrientation orientation) =>
            orientation == PdfOrientation.Landscape
                ? PdfPaperOrientation.Landscape
                : PdfPaperOrientation.Portrait;
    }
}
