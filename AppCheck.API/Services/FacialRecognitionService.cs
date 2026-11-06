using FaceONNX;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SkiaSharp;

namespace AppCheck.API.Services;

/// <summary>
/// Reconocimiento facial con IA real:
///  - Detección: YOLOv5s-face (FaceONNX)
///  - Alineación: Face5Landmarks.RotationAngle + FaceProcessingExtensions.Align
///  - Embedding: ResNet-27 (recognition_resnet27.onnx) vía ONNX Runtime
///  - Comparación: Similitud de coseno sobre embeddings normalizados L2
/// </summary>
public class FacialRecognitionService
{
    private const float UMBRAL_SIMILITUD_PRODUCCION = 70f; // % mínimo para aceptar match
    private const float UMBRAL_ESTRICTO = 80f;             // % para RH crítico
    // El modelo recognition_resnet27.onnx espera 128x128 (no 112x112)
    private const int TAMANO_ENTRADA = 128;

    private readonly FaceDetector _detector;
    private readonly InferenceSession _recognizerSession;

    public FacialRecognitionService(IWebHostEnvironment env)
    {
        var modelsPath = Path.Combine(env.ContentRootPath, "Models");
        var recognizerPath = Path.Combine(modelsPath, "recognition_resnet27.onnx");

        if (!File.Exists(recognizerPath))
            throw new FileNotFoundException($"Modelo de reconocimiento no encontrado: {recognizerPath}");

        // 🔧 FIX Bug #1: Especificar FaceDetectorModel.Yolov5 explícitamente.
        // El fork tiene: FaceOnnx=0, Yolov5=1, Yolo26=2.
        // Antes: new FaceDetector(0.5f) → probablemente usaba FaceOnnx (débil).
        _detector = new FaceDetector(
            detectionThreshold: 0.5f,
            confidenceThreshold: 0.5f,
            nmsThreshold: 0.45f,
            model: FaceDetectorModel.Yolov5
        );

        // Reconocedor: ONNX Runtime directo (el fork NO trae wrapper FaceRecognizer)
        _recognizerSession = new InferenceSession(recognizerPath);
    }

    /// <summary>
    /// Genera un embedding L2-normalizado (norma = 1.0) a partir de una foto Base64.
    /// Retorna array vacío si no hay rostro o hay error.
    /// </summary>
    public float[] GenerarEmbedding(string fotoBase64)
    {
        try
        {
            using var bitmap = DecodificarBase64(fotoBase64);
            if (bitmap == null) return Array.Empty<float>();

            // 1. Detectar rostros
            var detecciones = _detector.ForwardDetection(bitmap);
            if (detecciones == null || detecciones.Length == 0)
            {
                Console.WriteLine("[Facial] No se detectaron rostros.");
                return Array.Empty<float>();
            }

            // 2. Tomar el rostro más grande (más probable que sea el sujeto principal)
            var mejor = detecciones
                .OrderByDescending(d => (long)d.Rectangle.Width * d.Rectangle.Height)
                .First();

            Console.WriteLine($"[Facial] Rostro detectado: {mejor.Rectangle.Width}x{mejor.Rectangle.Height}, " +
                              $"score={mejor.Score:F2}, rotation={mejor.Points?.RotationAngle:F1}°");

            // 3. 🎯 FIX Bug #2: ALINEAR usando el RotationAngle que ya calculó FaceONNX.
            //    Rotamos toda la imagen para que los ojos queden horizontales.
            float angulo = mejor.Points?.RotationAngle ?? 0f;
            using var bitmapAlineado = Math.Abs(angulo) > 0.5f
                ? bitmap.Align(angulo)
                : bitmap.Copy();

            // 4. Recortar el rostro alineado (con margen del 20% para incluir mentón y frente)
            using var rostroRecortado = RecortarConMargen(bitmapAlineado, mejor.Rectangle, margen: 0.20f);
            if (rostroRecortado == null) return Array.Empty<float>();

	    // 5. Redimensionar a 128x128 (entrada real del recognition_resnet27.onnx)
	    var sampling = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear);
	    using var rostroRedimensionado = rostroRecortado.Resize(new SKSizeI(TAMANO_ENTRADA, TAMANO_ENTRADA), sampling);
	    if (rostroRedimensionado == null) return Array.Empty<float>();

            // 6. Convertir a tensor CHW normalizado [-1..1]
	    var tensor = ConvertirSKBitmapATensor(rostroRedimensionado);

            // 7. Inferencia
	    // El modelo usa "input" como nombre (verificado con inspección del .onnx)
	    var inputName = _recognizerSession.InputMetadata.Keys.First();
	    var inputs = new List<NamedOnnxValue>
	    {
	        NamedOnnxValue.CreateFromTensor(inputName, tensor)
	    };

	    using var results = _recognizerSession.Run(inputs);
	    var output = results.First().AsTensor<float>();
	    var embedding = output.ToArray();

	    // Validación: el modelo debe devolver 512 dimensiones
	    if (embedding.Length != 512)
	    {
	        Console.WriteLine($"[Facial] ⚠️ Embedding inesperado: {embedding.Length} dims (se esperaban 512)");
	        return Array.Empty<float>();
	    }

            // 8. 🔧 FIX Bug #3: L2-normalizar el embedding.
            //    Con esto, la similitud de coseno = dot product, y umbrales como 70% son estables.
            NormalizarL2(embedding);

            Console.WriteLine($"[Facial] Embedding generado: dim={embedding.Length}, norma={CalcularNorma(embedding):F4}");
            return embedding;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Facial] Error: {ex.Message}");
            return Array.Empty<float>();
        }
    }

    /// <summary>
    /// Compara dos embeddings L2-normalizados. Retorna similitud % [0..100].
    /// Interpretación:
    ///   >90 → match fuerte
    ///   75-90 → probable
    ///   60-75 → zona gris
    ///   &lt;60 → distintas personas
    /// </summary>
    public double CompararRostros(float[] a, float[] b)
    {
        if (a == null || b == null || a.Length == 0 || b.Length == 0) return 0;
        if (a.Length != b.Length) return 0;

        double dot = 0;
        for (int i = 0; i < a.Length; i++) dot += a[i] * b[i];

        // 🎯 FIX CALIBRACIÓN: el modelo recognition_resnet27.onnx está calibrado
        // para el rango natural del coseno [-1, 1], no para [0, 1].
        // Mapeamos a [0, 100] con: (dot + 1) / 2 * 100.
        // Resultados empíricos:
        //   - Misma persona: ~81%
        //   - Personas distintas: ~49%
        //   - Umbral recomendado: 70%
        double sim = (Math.Clamp(dot, -1.0, 1.0) + 1.0) / 2.0 * 100.0;
        return Math.Round(sim, 2);
    }

    public float UmbralProduccion => UMBRAL_SIMILITUD_PRODUCCION;
    public float UmbralEstricto => UMBRAL_ESTRICTO;

    // ─────────────────────────────────────────────────────────────────────
    // Helpers privados
    // ─────────────────────────────────────────────────────────────────────

    private static SKBitmap? DecodificarBase64(string base64)
    {
        if (string.IsNullOrWhiteSpace(base64)) return null;
        var limpio = base64;
        var idx = limpio.IndexOf(',');
        if (idx >= 0) limpio = limpio[(idx + 1)..];
        try
        {
            var bytes = Convert.FromBase64String(limpio);
            return SKBitmap.Decode(bytes);
        }
        catch { return null; }
    }

    private static SKBitmap? RecortarConMargen(SKBitmap source, SKRectI rect, float margen)
    {
        int mx = (int)(rect.Width * margen);
        int my = (int)(rect.Height * margen);

        int left   = Math.Max(0, rect.Left - mx);
        int top    = Math.Max(0, rect.Top - my);
        int right  = Math.Min(source.Width,  rect.Right + mx);
        int bottom = Math.Min(source.Height, rect.Bottom + my);

        int w = right - left;
        int h = bottom - top;
        if (w <= 0 || h <= 0) return null;

        var dst = new SKBitmap(w, h);
        using var canvas = new SKCanvas(dst);
        canvas.DrawBitmap(source,
            new SKRect(left, top, right, bottom),
            new SKRect(0, 0, w, h));
        return dst;
    }

    private static DenseTensor<float> ConvertirSKBitmapATensor(SKBitmap bitmap)
          {
              // Entrada: 1x3x128x128, normalización [-1..1] (típica de ResNet facial)
              var tensor = new DenseTensor<float>(new[] { 1, 3, TAMANO_ENTRADA, TAMANO_ENTRADA });
             for (int y = 0; y < TAMANO_ENTRADA; y++)
             {
                   for (int x = 0; x < TAMANO_ENTRADA; x++)
                  {
                      var c = bitmap.GetPixel(x, y);
                     tensor[0, 0, y, x] = (c.Red   / 255f) * 2f - 1f;
                     tensor[0, 1, y, x] = (c.Green / 255f) * 2f - 1f;
                     tensor[0, 2, y, x] = (c.Blue  / 255f) * 2f - 1f;
                 }
             }
              return tensor;
          }

    private static void NormalizarL2(float[] v)
    {
        double norm = 0;
        for (int i = 0; i < v.Length; i++) norm += (double)v[i] * v[i];
        norm = Math.Sqrt(norm);
        if (norm < 1e-9) return;
        for (int i = 0; i < v.Length; i++) v[i] = (float)(v[i] / norm);
    }

    private static double CalcularNorma(float[] v)
    {
        double n = 0;
        for (int i = 0; i < v.Length; i++) n += (double)v[i] * v[i];
        return Math.Sqrt(n);
    }
}
