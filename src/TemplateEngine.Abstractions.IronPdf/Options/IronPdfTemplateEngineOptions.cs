using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TemplateEngine.Abstractions.IronPdf.Options
{
    /// <summary>
    /// Configuration options for the IronPDF template engine integration.
    /// </summary>
    public sealed class IronPdfTemplateEngineOptions
    {
        /// <summary>
        /// Gets or sets the IronPDF license key.
        /// </summary>
        /// <remarks>
        /// If specified, the license key is assigned to IronPDF during service
        /// registration. If not provided, IronPDF operates according to its
        /// default licensing behavior.
        /// </remarks>
        public string? LicenseKey { get; set; }
    }
}
