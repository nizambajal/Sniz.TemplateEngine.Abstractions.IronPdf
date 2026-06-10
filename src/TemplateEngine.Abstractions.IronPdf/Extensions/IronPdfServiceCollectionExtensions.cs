using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TemplateEngine.Abstractions.IronPdf.Options;
using IronPdf;

namespace TemplateEngine.Abstractions.IronPdf.Extensions
{
    /// <summary>
    /// Extension methods for registering IronPDF template engine services.
    /// </summary>
    public static class IronPdfServiceCollectionExtensions
    {
        /// <summary>
        /// Registers the IronPDF template engine and configures IronPDF licensing
        /// using the specified configuration section.
        /// </summary>
        /// <param name="services">
        /// The service collection to add the IronPDF template engine services to.
        /// </param>
        /// <param name="section">
        /// The configuration section containing <see cref="IronPdfTemplateEngineOptions"/>
        /// settings, including the optional IronPDF license key.
        /// </param>
        /// <returns>
        /// The same <see cref="IServiceCollection"/> instance so that additional
        /// service registrations can be chained.
        /// </returns>
        /// <remarks>
        /// If a valid license key is provided, it is assigned to
        /// <see cref="License.LicenseKey"/> during application startup.
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddIronPdfTemplateEngine(
        ///     configuration.GetSection("IronPdf"));
        /// </code>
        /// </example>
        public static IServiceCollection AddIronPdfTemplateEngine(
            this IServiceCollection services,
            IConfigurationSection section)
        {
            services.AddScoped<PdfGenerator>();

            var options = section.Get<IronPdfTemplateEngineOptions>();

            if (!string.IsNullOrWhiteSpace(options?.LicenseKey))
            {
                License.LicenseKey = options.LicenseKey;
            }

            return services;
        }
    }
}
