# Sniz.TemplateEngine.Abstractions.IronPdf

Overview
--------

Sniz.TemplateEngine.Abstractions.IronPdf provides lightweight abstractions to integrate an HTML template engine with IronPdf-based PDF generation. The project defines interfaces and contracts so applications can plug in different template rendering engines and produce PDFs using IronPdf with minimal coupling.

Key points
- Small, focused abstraction layer for HTML -> PDF workflows
- Designed for .NET 8 projects
- Keeps PDF generation and template rendering concerns separated

Features
- Interfaces for template rendering and PDF creation
- Extension points to implement custom template engines or PDF configuration
- Minimal dependencies — intended to be composed into larger applications

Prerequisites
- .NET 8 SDK
- IronPdf library (commercial license may be required for production)

Installation

Add the project or reference the NuGet package (if published) to your solution.

- Project reference:
  - Add the project to your solution and reference it from consumer projects

- NuGet (example):
  - dotnet add package Sniz.TemplateEngine.Abstractions.IronPdf

Usage example (conceptual)

- Implement a template renderer that produces HTML from a model:

  ```
  var html = await myTemplateRenderer.RenderAsync("Invoice", model);
  ```

- Use IronPdf adapter that implements the abstraction to convert HTML -> PDF:

  ```
  var pdfBytes = myIronPdfAdapter.GeneratePdfFromHtml(html, pdfOptions);
  ```

- Persist or return the generated PDF bytes as needed.

Configuration

- Configure HTML rendering and PDF options in the consuming application.
- Keep IronPdf-specific configuration inside the IronPdf adapter implementation so callers remain implementation-agnostic.

Contributing

Contributions are welcome. Open issues and pull requests should follow the repository's contribution guidelines. Keep changes focused and add tests for behavior changes.

License

See the repository LICENSE file for licensing details.

Contact

Repository: https://github.com/nizambajal/Sniz.TemplateEngine.Abstractions.IronPdf

---

## Support Development

Sniz.TemplateEngine.Abstractions.IronPdf is maintained in my spare time.

If it saves you development time, consider supporting the project:

☕ Ko-fi: https://ko-fi.com/nizambajal

Every contribution helps improve the package and fund future features.