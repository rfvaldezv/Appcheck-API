\# APPCHECK — Estado del Proyecto



\*\*Última actualización:\*\* 06 de Noviembre de 2026, 23:00 hrs

\*\*Estado global:\*\* 🟢 MVP end-to-end funcional en local. Listo para deploy a VPS.

\*\*Hito actual:\*\* Reconocimiento facial con IA real (FaceONNX) validado con 16/16 pruebas.



\---



\## 1. ¿Qué es Appcheck?



Sistema de control de asistencia (reloj checador) que usa el \*\*celular del empleado como kiosko de lectura\*\*. Evita gastar en dispositivos biométricos dedicados aprovechando los celulares que ya tiene la empresa o el personal.



\### Componentes del ecosistema



| Componente | Tecnología | Dominio |

|------------|------------|---------|

| \*\*API\*\* | .NET 10 + SQL Server + EF Core | `https://api.appcheck.com.mx` |

| \*\*Admin\*\* | Flutter Web | `https://admin.appcheck.com.mx` |

| \*\*Kiosko\*\* | Flutter (Android APK) | Distribución directa |



\### Repositorios GitHub

\- API: https://github.com/rfvaldezv/Appcheck-API

\- Kiosko: https://github.com/rfvaldezv/Appcheck-Kiosko

\- Admin: https://github.com/rfvaldezv/Appcheck-Admin



\---



\## 2. Estado de los repos



| Repo | Rama | Último commit | Estado |

|------|------|---------------|--------|

| \*\*`Appcheck-API`\*\* | `main` | `3bf29de` — feat: reconocimiento facial con IA real | ✅ Sincronizado |

| \*\*`Appcheck-Kiosko`\*\* | `main` | `30f8afc` — fix: nombres canonicos + compresion | ✅ Sincronizado |

| \*\*`Appcheck-Admin`\*\* | `main` | `2696fd0` — fix: timeout de 120s | ✅ Sincronizado |



\---



\## 3. 🏆 HITO — Reconocimiento Facial con IA Real (06/Nov/2026)



\### ✅ Completado en esta sesión



\#### 3.1 Pipeline de reconocimiento facial (API .NET 10)

\- \*\*Detector:\*\* YOLOv5s-face (`yolov5s-face.onnx`, 30 MB) con `FaceDetectorModel.Yolov5`

\- \*\*Alineación:\*\* `Face5Landmarks.RotationAngle` + `FaceProcessingExtensions.Align()`

\- \*\*Embeddings:\*\* `recognition\_resnet27.onnx` (127 MB), input `"input"` 128x128, output 512D

\- \*\*Normalización:\*\* L2 (norma = 1.0)

\- \*\*Métrica:\*\* Coseno escalado `(dot + 1) / 2 \* 100`

\- \*\*Umbral producción:\*\* 70%

\- \*\*Tiempo por foto:\*\* 130-190 ms



\#### 3.2 Defensa en profundidad (EmployeeController)

\- Lista canónica `MetodosValidos = \[PIN, QR, PIN+Rostro, QR+Rostro, Rostro]`

\- Validación al alta y edición de empleados

\- Si la foto no detecta rostro → `400 Bad Request` (antes guardaba null silencioso)

\- Response incluye `tieneRostro` para verificar registro exitoso



\#### 3.3 Fix de nombres canónicos (Kiosko Flutter)

\- \*\*Bug crítico:\*\* `pin\_screen.dart` usaba `'PIN+Facial'` y `'Facial'` (no canónicos)

&#x20; - La API devuelve `'PIN+Rostro'` → el empleado se quedaba atorado en "Método no soportado"

&#x20; - Ahora maneja los 5 métodos canónicos correctamente

\- \*\*Bug latente:\*\* `facial\_screen.dart` default era `'Mix'` (inválido)

&#x20; - Ahora default es `'Rostro'`

\- \*\*Bug crítico:\*\* Se enviaba `'PIN+Facial'` a la API → la API NO validaba rostro

&#x20; - Ahora envía `'PIN+Rostro'` correcto



\#### 3.4 Compresión de foto (Kiosko Flutter)

\- Foto capturada se comprime a \*\*300x300 JPG q80\*\* antes de enviar

\- Antes: 2-5 MB por foto → Ahora: 30-50 KB

\- Dependencia agregada: `image: ^4.12.0`



\#### 3.5 Fix de timeout (Admin Flutter)

\- Timeout de \*\*120s\*\* en alta/edición de empleados (antes era 60s por defecto)

\- Problema: la generación de embedding tarda 1-10s según la foto

\- Resultado del bug: empleados duplicados al reintentar por timeout falso



\### 📊 Validación empírica — 16/16 pruebas correctas



\*\*Misma persona (4 fotos distintas: ropa, fondo, luz, expresión):\*\*



| Par | Similitud |

|-----|-----------|

| vvfr ↔ vvrf | 79.25% ✅ |

| frvv ↔ vvrf | 82.78% ✅ |

| rfvv ↔ vvrf | 77.98% ✅ |

| rfvv ↔ frvv | 76.37% ✅ |

| vvfr ↔ rfvv | 76.43% ✅ |

| vvfr ↔ frvv | 74.14% ✅ (el más apretado) |



\*\*Personas distintas (7 pares):\*\*



| Categoría | Rango |

|-----------|-------|

| Todas las parejas | 47.70% - 54.37% ❌ (todas rechazadas) |



\### 🎯 Estadísticas finales





