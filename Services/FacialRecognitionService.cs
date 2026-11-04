using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace AppCheck.API.Services;

public class FacialRecognitionService
{
    // Compara dos imágenes en Base64 y devuelve un porcentaje de similitud (0 a 100)
    public double CompararRostros(string fotoBase64Guardada, string fotoBase64EnVivo)
    {
        try
        {
            // Limpiar el prefijo "data:image/jpeg;base64," si existe
            var base64Guardada = LimpiarBase64(fotoBase64Guardada);
            var base64EnVivo = LimpiarBase64(fotoBase64EnVivo);

            if (string.IsNullOrEmpty(base64Guardada) || string.IsNullOrEmpty(base64EnVivo))
                return 0;

            byte[] bytesGuardada = Convert.FromBase64String(base64Guardada);
            byte[] bytesEnVivo = Convert.FromBase64String(base64EnVivo);

            using var imageGuardada = Image.Load<Rgb24>(bytesGuardada);
            using var imageEnVivo = Image.Load<Rgb24>(bytesEnVivo);

            // Redimensionar ambas imágenes al mismo tamaño para comparar pixel a pixel (ej. 100x100)
            imageGuardada.Mutate(x => x.Resize(100, 100));
            imageEnVivo.Mutate(x => x.Resize(100, 100));

            int coincidencias = 0;
            int totalPixeles = 100 * 100;

            // Comparar cada píxel
            for (int y = 0; y < 100; y++)
            {
                for (int x = 0; x < 100; x++)
                {
                    var pixelGuardado = imageGuardada[x, y];
                    var pixelEnVivo = imageEnVivo[x, y];

                    // Calcular la diferencia de color (distancia euclidiana simple)
                    int diffR = Math.Abs(pixelGuardado.R - pixelEnVivo.R);
                    int diffG = Math.Abs(pixelGuardado.G - pixelEnVivo.G);
                    int diffB = Math.Abs(pixelGuardado.B - pixelEnVivo.B);

                    // Si la diferencia es menor a un umbral (ej. 50), consideramos que el píxel coincide
                    if (diffR < 50 && diffG < 50 && diffB < 50)
                    {
                        coincidencias++;
                    }
                }
            }

            // Calcular el porcentaje de similitud
            double similitud = (double)coincidencias / totalPixeles * 100;
            return Math.Round(similitud, 2);
        }
        catch
        {
            return 0; // Si hay error al procesar la imagen, devolvemos 0
        }
    }

    private string LimpiarBase64(string base64)
    {
        if (string.IsNullOrEmpty(base64)) return string.Empty;
        // Remover el prefijo "data:image/...;base64,"
        var commaIndex = base64.IndexOf(',');
        if (commaIndex >= 0)
            return base64.Substring(commaIndex + 1);
        return base64;
    }
}
