using QRCoder;
using System.Globalization;

namespace EducationalPlataform.Services
{
    public static class PixQrHelper
    {
        public static string BuildCopyPaste(int paymentId, decimal amount, string? userName)
        {
            var name = (userName ?? "ALUNO").Replace(" ", "").ToUpperInvariant();
            if (name.Length > 25) name = name[..25];
            return $"00020126580014BR.GOV.BCB.PIX0136{paymentId}520400005303986540{amount.ToString("0.00", CultureInfo.InvariantCulture)}5802BR5925{name}";
        }

        public static string ToBase64Png(string pixCode)
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(pixCode, QRCodeGenerator.ECCLevel.Q);
            var png = new PngByteQRCode(qrCodeData).GetGraphic(20);
            return Convert.ToBase64String(png);
        }
    }
}
