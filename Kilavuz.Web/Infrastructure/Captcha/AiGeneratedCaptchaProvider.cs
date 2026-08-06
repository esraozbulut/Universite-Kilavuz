using System;
using System.IO;
using System.Linq;
using Kilavuz.Web.Application.Interfaces;
using SkiaSharp;

namespace Kilavuz.Web.Infrastructure.Captcha;

public class AiGeneratedCaptchaProvider : ICaptchaProvider
{
    private const string Chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Okunması kolay karakterler
    private const int Width = 180;
    private const int Height = 60;
    private const int Length = 5;

    public CaptchaResult GenerateCaptcha()
    {
        var random = new Random();
        var captchaCode = new string(Enumerable.Repeat(Chars, Length)
            .Select(s => s[random.Next(s.Length)]).ToArray());

        var info = new SKImageInfo(Width, Height);
        using var surface = SKSurface.Create(info);
        var canvas = surface.Canvas;

        // Arka plan rengi
        canvas.Clear(SKColors.WhiteSmoke);

        // Gürültü çizgileri çiz
        using var linePaint = new SKPaint
        {
            IsAntialias = true,
            StrokeWidth = 2,
            Style = SKPaintStyle.Stroke
        };

        for (int i = 0; i < 10; i++)
        {
            linePaint.Color = new SKColor(
                (byte)random.Next(150, 220), 
                (byte)random.Next(150, 220), 
                (byte)random.Next(150, 220));

            var x0 = random.Next(0, Width);
            var y0 = random.Next(0, Height);
            var x1 = random.Next(0, Width);
            var y1 = random.Next(0, Height);

            canvas.DrawLine(x0, y0, x1, y1, linePaint);
        }

        // Metin çiz
        using var textPaint = new SKPaint
        {
            IsAntialias = true
        };
        using var font = new SKFont(SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold) ?? SKTypeface.Default, 36);

        for (int i = 0; i < captchaCode.Length; i++)
        {
            var charText = captchaCode[i].ToString();
            textPaint.Color = new SKColor(
                (byte)random.Next(0, 100), 
                (byte)random.Next(0, 100), 
                (byte)random.Next(0, 100));

            // Harf rotasyonu için canvas'ı kaydırıp döndürüyoruz
            var x = 20 + (i * 30);
            var y = random.Next(40, 50);
            var rotation = random.Next(-20, 20);

            canvas.Save();
            canvas.Translate(x, y);
            canvas.RotateDegrees(rotation);
            canvas.DrawText(charText, 0, 0, SKTextAlign.Left, font, textPaint);
            canvas.Restore();
        }

        // Noktalı gürültü ekle
        using var pointPaint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };

        for (int i = 0; i < 100; i++)
        {
            pointPaint.Color = new SKColor(
                (byte)random.Next(100, 200), 
                (byte)random.Next(100, 200), 
                (byte)random.Next(100, 200));

            var cx = random.Next(0, Width);
            var cy = random.Next(0, Height);
            var radius = random.Next(1, 3);
            canvas.DrawCircle(cx, cy, radius, pointPaint);
        }

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        
        return new CaptchaResult
        {
            CaptchaCode = captchaCode,
            ImageBytes = data.ToArray()
        };
    }
}
