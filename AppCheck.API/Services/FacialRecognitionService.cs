using SkiaSharp;

namespace AppCheck.API.Services;

public class FacialRecognitionService
{
    public double CompararRostros(string fotoBase64Guardada, string fotoBase64EnVivo)
    {
        try
        {
            var base64Guardada = LimpiarBase64(fotoBase64Guardada);
            var base64EnVivo = LimpiarBase64(fotoBase64EnVivo);

            if (string.IsNullOrEmpty(base64Guardada) || string.IsNullOrEmpty(base64EnVivo))
                return 0;

            byte[] bytesGuardada = Convert.FromBase64String(base64Guardada);
            byte[] bytesEnVivo = Convert.FromBase64String(base64EnVivo);

            using var bitmapGuardado = SKBitmap.Decode(bytesGuardada);
            using var bitmapEnVivo = SKBitmap.Decode(bytesEnVivo);

            if (bitmapGuardado == null || bitmapEnVivo == null)
                return 0;

            // Redimensionar a un tamaño estándar (100x100) para comparar
	    // En SkiaSharp 3.x, se usa SKSamplingOptions en lugar de SKFilterQuality
	    var samplingOptions = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear);
	    using var resizedGuardado = bitmapGuardado.Resize(new SKImageInfo(100, 100), samplingOptions);
	    using var resizedEnVivo = bitmapEnVivo.Resize(new SKImageInfo(100, 100), samplingOptions);

            if (resizedGuardado == null || resizedEnVivo == null)
                return 0;

            int coincidencias = 0;
            int totalPixeles = 100 * 100;

            for (int y = 0; y < 100; y++)
            {
                for (int x = 0; x < 100; x++)
                {
                    var pixelGuardado = resizedGuardado.GetPixel(x, y);
                    var pixelEnVivo = resizedEnVivo.GetPixel(x, y);

                    // Diferencia de color (distancia euclidiana simple)
                    int diffR = Math.Abs(pixelGuardado.Red - pixelEnVivo.Red);
                    int diffG = Math.Abs(pixelGuardado.Green - pixelEnVivo.Green);
                    int diffB = Math.Abs(pixelGuardado.Blue - pixelEnVivo.Blue);

                    if (diffR < 50 && diffG < 50 && diffB < 50)
                    {
                        coincidencias++;
                    }
                }
            }

            double similitud = (double)coincidencias / totalPixeles * 100;
            return Math.Round(similitud, 2);
        }
        catch
        {
            return 0;
        }
    }

    private string LimpiarBase64(string base64)
    {
        if (string.IsNullOrEmpty(base64)) return string.Empty;
        var commaIndex = base64.IndexOf(',');
        if (commaIndex >= 0)
            return base64.Substring(commaIndex + 1);
        return base64;
    }
}
