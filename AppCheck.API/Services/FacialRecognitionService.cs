using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SkiaSharp;

namespace AppCheck.API.Services
{
    public class FacialRecognitionService
    {
        private readonly string _detectorModelPath;
        private readonly string _recognitionModelPath;

        // Ajusta las rutas según tu proyecto
        private const string DetectorModel = "yolov5s-face.onnx";
        private const string RecognitionModel = "recognition_resnet27.onnx";

        public FacialRecognitionService()
        {
            _detectorModelPath = Path.Combine(AppContext.BaseDirectory, "Models", DetectorModel);
            _recognitionModelPath = Path.Combine(AppContext.BaseDirectory, "Models", RecognitionModel);
        }

        /// <summary>
        /// Genera un embedding a partir de UNA foto Base64.
        /// </summary>
        public float[] GenerarEmbedding(string fotoBase64)
        {
            try
            {
                var bytes = Convert.FromBase64String(fotoBase64);
                using var original = SKBitmap.Decode(bytes);
                if (original == null) return Array.Empty<float>();

                // 1. Detectar rostro (simplificado: usar la imagen completa si no hay detector activo)
                // Si tienes un detector YOLO funcionando, aquí lo llamarías.
                // Por ahora, redimensionamos la imagen completa a 128x128 para el reconocimiento.
                using var rostro128 = RedimensionarYRecortar(original, 128, 128);

                // 2. Generar embedding
                return GenerarEmbeddingDesdeRostro(rostro128);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FacialRecognition] Error: {ex.Message}");
                return Array.Empty<float>();
            }
        }

        /// <summary>
        /// 👇 NUEVO: Genera un embedding promediado a partir de MÚLTIPLES fotos.
        /// </summary>
        public float[] GenerarEmbeddingPromediado(List<string> fotosBase64)
        {
            if (fotosBase64 == null || fotosBase64.Count == 0)
                return Array.Empty<float>();

            var embeddings = new List<float[]>();

            foreach (var foto in fotosBase64)
            {
                if (string.IsNullOrWhiteSpace(foto)) continue;
                var emb = GenerarEmbedding(foto);
                if (emb.Length > 0)
                    embeddings.Add(emb);
            }

            if (embeddings.Count == 0) return Array.Empty<float>();
            if (embeddings.Count == 1) return embeddings[0];

            // Promediar cada dimensión
            var dimension = embeddings[0].Length;
            var promedio = new float[dimension];

            foreach (var emb in embeddings)
            {
                for (int i = 0; i < dimension; i++)
                {
                    promedio[i] += emb[i];
                }
            }

            for (int i = 0; i < dimension; i++)
            {
                promedio[i] /= embeddings.Count;
            }

            // Normalizar L2 (importante para similitud coseno)
            var norma = Math.Sqrt(promedio.Sum(x => x * x));
            if (norma > 0)
            {
                for (int i = 0; i < dimension; i++)
                {
                    promedio[i] /= (float)norma;
                }
            }

            return promedio;
        }

        // --- Métodos auxiliares ---
        private SKBitmap RedimensionarYRecortar(SKBitmap original, int ancho, int alto)
        {
            var result = new SKBitmap(ancho, alto);
            using var canvas = new SKCanvas(result);
            using var paint = new SKPaint();
            paint.IsAntialias = true;
            // Eliminamos FilterQuality (ya no existe en esta versión de SkiaSharp)
            canvas.DrawBitmap(original, new SKRect(0, 0, ancho, alto), paint);
            canvas.Flush();
            return result;
        }

        private float[] GenerarEmbeddingDesdeRostro(SKBitmap rostro)
        {
            // Convertir SKBitmap a Tensor<float>
            var input = new DenseTensor<float>(new[] { 1, 3, 128, 128 });
            for (int y = 0; y < 128; y++)
            {
                for (int x = 0; x < 128; x++)
                {
                    var color = rostro.GetPixel(x, y);
                    input[0, 0, y, x] = color.Red / 255f;
                    input[0, 1, y, x] = color.Green / 255f;
                    input[0, 2, y, x] = color.Blue / 255f;
                }
            }

            try
            {
                using var session = new InferenceSession(_recognitionModelPath);
                var inputs = new List<NamedOnnxValue>
                {
                    NamedOnnxValue.CreateFromTensor("input", input)
                };
                using var results = session.Run(inputs);
                var output = results.First().AsTensor<float>();
                return output.ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FacialRecognition] Error en ONNX: {ex.Message}");
                return Array.Empty<float>();
            }
        }

        /// <summary>
        /// Compara dos embeddings y devuelve un porcentaje de similitud (0-100).
        /// </summary>
        public float CompararRostros(float[] emb1, float[] emb2)
        {
            if (emb1 == null || emb2 == null || emb1.Length == 0 || emb2.Length == 0) 
                return 0;

            float dot = 0;
            for (int i = 0; i < emb1.Length; i++)
            {
                dot += emb1[i] * emb2[i];
            }
            // Similitud coseno escalada a 0-100
            return (dot + 1) / 2 * 100;
        }

    }
}
