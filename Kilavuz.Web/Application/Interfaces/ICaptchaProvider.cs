namespace Kilavuz.Web.Application.Interfaces;

public class CaptchaResult
{
    public required byte[] ImageBytes { get; set; }
    public required string CaptchaCode { get; set; }
}

public interface ICaptchaProvider
{
    CaptchaResult GenerateCaptcha();
}
