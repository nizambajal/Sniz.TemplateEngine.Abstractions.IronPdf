using FluentAssertions;
using TemplateEngine.Abstractions.IronPdf.Options;
using Xunit;

namespace PdfAbstraction.Tests;

/// <summary>
/// Tests for <see cref="PdfDocumentOptions"/> and <see cref="PdfMargins"/> defaults and values.
/// </summary>
public class PdfDocumentOptionsTests
{
    // ── Defaults ──────────────────────────────────────────────────────────────

    [Fact]
    public void DefaultDocumentOptions_HasExpectedValues()
    {
        var opts = new PdfDocumentOptions();

        opts.PaperSize.Should().Be(PdfPaperSize.A4);
        opts.Orientation.Should().Be(PdfOrientation.Portrait);
        opts.Dpi.Should().Be(96);
        opts.PrintBackground.Should().BeTrue();
        opts.Zoom.Should().Be(1);
        opts.RenderDelayMs.Should().Be(0);
    }

    [Fact]
    public void DefaultDocumentOptions_MarginsAre25mmOnAllSides()
    {
        var opts = new PdfDocumentOptions();

        opts.Margins.Top.Should().Be(25);
        opts.Margins.Bottom.Should().Be(25);
        opts.Margins.Left.Should().Be(25);
        opts.Margins.Right.Should().Be(25);
    }

    // ── PdfMargins presets ────────────────────────────────────────────────────

    [Fact]
    public void PdfMargins_Default_Is25mmOnAllSides()
    {
        var margins = PdfMargins.Default;

        margins.Top.Should().Be(25);
        margins.Bottom.Should().Be(25);
        margins.Left.Should().Be(25);
        margins.Right.Should().Be(25);
    }

    [Fact]
    public void PdfMargins_None_IsZeroOnAllSides()
    {
        var margins = PdfMargins.None;

        margins.Top.Should().Be(0);
        margins.Bottom.Should().Be(0);
        margins.Left.Should().Be(0);
        margins.Right.Should().Be(0);
    }

    [Fact]
    public void PdfMargins_CustomValues_AreStoredCorrectly()
    {
        var margins = new PdfMargins { Top = 10, Bottom = 20, Left = 5, Right = 15 };

        margins.Top.Should().Be(10);
        margins.Bottom.Should().Be(20);
        margins.Left.Should().Be(5);
        margins.Right.Should().Be(15);
    }

    // ── Paper size enum coverage ──────────────────────────────────────────────

    [Theory]
    [InlineData(PdfPaperSize.A3)]
    [InlineData(PdfPaperSize.A4)]
    [InlineData(PdfPaperSize.A5)]
    [InlineData(PdfPaperSize.Letter)]
    [InlineData(PdfPaperSize.Legal)]
    [InlineData(PdfPaperSize.Tabloid)]
    public void PdfPaperSize_AllValuesAreDefinedInEnum(PdfPaperSize size)
    {
        Enum.IsDefined(size).Should().BeTrue();
    }

    // ── Orientation enum coverage ─────────────────────────────────────────────

    [Theory]
    [InlineData(PdfOrientation.Portrait)]
    [InlineData(PdfOrientation.Landscape)]
    public void PdfOrientation_AllValuesAreDefinedInEnum(PdfOrientation orientation)
    {
        Enum.IsDefined(orientation).Should().BeTrue();
    }

    // ── Custom values round-trip ──────────────────────────────────────────────

    [Fact]
    public void CustomDocumentOptions_ValuesAreMutable()
    {
        var opts = new PdfDocumentOptions
        {
            PaperSize       = PdfPaperSize.Letter,
            Orientation     = PdfOrientation.Landscape,
            Dpi             = 300,
            PrintBackground = false,
            Zoom            = 2,
            RenderDelayMs   = 1000,
            Margins         = new PdfMargins { Top = 0, Bottom = 0, Left = 0, Right = 0 }
        };

        opts.PaperSize.Should().Be(PdfPaperSize.Letter);
        opts.Orientation.Should().Be(PdfOrientation.Landscape);
        opts.Dpi.Should().Be(300);
        opts.PrintBackground.Should().BeFalse();
        opts.Zoom.Should().Be(2);
        opts.RenderDelayMs.Should().Be(1000);
        opts.Margins.Should().BeEquivalentTo(PdfMargins.None);
    }
}
