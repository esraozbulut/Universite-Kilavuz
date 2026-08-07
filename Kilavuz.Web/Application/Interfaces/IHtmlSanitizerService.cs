namespace Kilavuz.Web.Application.Interfaces;

public interface IHtmlSanitizerService
{
    string Sanitize(string htmlContent);
}
