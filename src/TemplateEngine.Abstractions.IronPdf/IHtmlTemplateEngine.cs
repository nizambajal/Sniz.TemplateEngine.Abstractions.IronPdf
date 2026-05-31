namespace TemplateEngine.Abstractions.IronPdf
{
    /// <summary>
    /// Contract for the HTML template engine already built by the caller.
    /// Implement this interface to plug in any engine (Handlebars, Scriban, RazorLight, etc.)
    /// </summary>
    public interface IHtmlTemplateEngine
    {
        /// <summary>
        /// Renders <paramref name="template"/> by binding <paramref name="model"/> to it
        /// and returns the resulting HTML string.
        /// </summary>
        /// <typeparam name="T">Type of the view model.</typeparam>
        /// <param name="template">Raw template string.</param>
        /// <param name="model">Data bound into the template.</param>
        /// <param name="ct">Cancellation token.</param>
        Task<string> RenderAsync<T>(string template, T model, CancellationToken ct = default);
    }
}
