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





## 🚀 HITO — Deploy a Producción + End-to-End Validado (06/Nov/2026)

**Estado:** 🟢 MVP funcionando en `https://api.appcheck.com.mx`

### ✅ Validación end-to-end en producción

| Prueba | Endpoint | Resultado |
|--------|----------|-----------|
| Health check | `GET /api/health` | ✅ 200 — BD conectada |
| Login SuperAdmin | `POST /api/Auth/login` | ✅ 200 — JWT válido |
| Crear empleado con foto | `POST /api/Employee` | ✅ 200 — `tieneRostro: true` |
| Embedding facial | `POST /api/facial/embedding` | ✅ 200 — 512D, norma 1.0, 607ms |
| Compare facial (misma persona) | `POST /api/facial/compare` | ✅ 74-78% |
| Compare facial (distintas) | `POST /api/facial/compare` | ✅ 49% |
| **Checada exitosa** | `POST /api/Checada` (foto correcta) | ✅ **200 OK** — 1419ms |
| **Checada rechazada** | `POST /api/Checada` (foto incorrecta) | ✅ **401 Unauthorized** |

### 📊 Datos de prueba creados
- **Empleado:** `TEST001` — Empleado Prueba (PIN+Rostro, con rostro)
- **Kiosko:** "Kiosko de Prueba" (token `TOKEN_PRUEBA_1234567890`)
- **Checada:** 1 registro de Entrada en `RegistrosJornada`

### 🎯 Resultados en producción (idénticos a local)
| Par | Local | Producción |
|-----|-------|------------|
| rfvv ↔ vvrf | 77.98% | 77.98% ✅ |
| frvv ↔ vvfr | 74.14% | 74.14% ✅ |
| rfvv ↔ frvv | 76.37% | 76.37% ✅ |
| rfvv ↔ iav | 49.13% | 49.13% ✅ |

### 🖥️ Infraestructura
- **API:** `https://api.appcheck.com.mx` (IIS + .NET 10 self-contained)
- **Ruta VPS:** `C:\inetpub\wwwroot\appcheck_api\`
- **BD:** SQL Server Express (`AppcheckAPI`)
- **Deploy:** RDP + ZIP de ~300 MB (con modelos ONNX)
- **Backup pre-reset:** `C:\Backups\AppcheckAPI_antes_reset_20261106.bak`

### 🐛 Bugs corregidos en esta sesión
1. **Nombres canónicos:** `'PIN+Facial'` → `'PIN+Rostro'` (Kiosko)
2. **Default de método:** `'Mix'` → `'Rostro'` (FacialScreen)
3. **Timeout en Admin:** 60s → 120s (alta/edición con foto)
4. **Validación de métodos:** lista canónica en `EmployeeController`
5. **Si foto no detecta rostro:** `400 Bad Request` (antes guardaba null)

### 🔧 Pendientes después de este hito
- [ ] **Build APK del Kiosko** (para probar desde celular real)
- [ ] **Deploy del Admin Web** (si aún no está en producción)
- [ ] **Registrar más empleados** (los 0 actuales)
- [ ] **Prueba con las 4 fotos de la misma persona** (todas deben pasar)
- [ ] **Anti-fraude:** liveness, device binding, geofence (Sprint B)

---

## 🏆 HITO — MVP End-to-End Validado en Producción (06/Nov/2026)

**Estado:** 🟢 Backend 100% funcional. Listo para instalar APK en celular.

### ✅ Validación end-to-end (9/9 pasos)

| # | Prueba | Resultado |
|---|--------|-----------|
| 1 | Health check | ✅ 200 — BD conectada |
| 2 | Login SuperAdmin | ✅ 200 — JWT válido |
| 3 | Crear empleado con foto | ✅ 200 — `tieneRostro: true` |
| 4 | Embedding facial | ✅ 200 — 512D, norma 1.0, 607ms |
| 5 | Compare misma persona | ✅ 74-78% |
| 6 | Compare distintas personas | ✅ 49% |
| 7 | Checada con kiosko real | ✅ **200 OK** — 1419ms |
| 8 | Prueba negativa (foto incorrecta) | ✅ **401 Unauthorized** |
| 9 | Alternancia Entrada/Salida | ✅ Funcionando |

### 🐛 Bugs corregidos en esta sesión

| # | Bug | Fix |
|---|-----|-----|
| 1 | `'PIN+Facial'` no canónico | → `'PIN+Rostro'` en `pin_screen.dart` |
| 2 | Default `'Mix'` inválido | → `'Rostro'` en `facial_screen.dart` |
| 3 | Timeout 60s insuficiente | → 120s en `api_service.dart` (Admin) |
| 4 | Sin validación de métodos | Lista canónica en `EmployeeController` |
| 5 | **Token del kiosko no hasheado** | **SHA-256 en `ChecadaController`** |

### 🖥️ Infraestructura en producción

- **API:** `https://api.appcheck.com.mx`
- **Ruta VPS:** `C:\inetpub\wwwroot\appcheck_api\`
- **BD:** SQL Server Express (`AppcheckAPI`)
- **Backup pre-reset:** `C:\Backups\AppcheckAPI_antes_reset_20261106.bak`
- **Modelos ONNX:** 157 MB (excluidos de Git, viven en el VPS)

### 📊 Datos de prueba en producción

| Dato | Valor |
|------|-------|
| Empleado prueba | `TEST001` — Empleado Prueba (PIN+Rostro) |
| Kiosko prueba | "Kiosko de Prueba" (token `TOKEN_PRUEBA_1234567890`) |
| Kiosko físico | "Celular Fisico" (device `S2RUBS32.51-15-3-19`) |
| Checadas registradas | 2+ (Entrada + Salida + Entrada) |

### 🎯 Pendientes para cerrar el MVP

- [ ] **Instalar APK en celular físico** (WhatsApp → instalar)
- [ ] **Verificar que recupera token automáticamente** (`/api/Kiosko/recuperar`)
- [ ] **Probar checada con cámara real** del celular
- [ ] **Verificar en BD** que se registró la checada desde celular

### 🗺️ Roadmap post-MVP

- [ ] **Admin Web deploy** (si no está en producción)
- [ ] **Anti-fraude:** liveness, device binding, geofence
- [ ] **Registrar más empleados** (los 0 actuales)
- [ ] **Migrar a ArcFace/InsightFace** (opcional, si se necesita mejor precisión)

---


## 🏆 HITO FINAL — MVP 100% Validado en Celular Real (06/Nov/2026)

**Estado:** 🟢 MVP completo y funcional en producción. Listo para piloto.

### ✅ Validación final (última checada desde celular físico)


**La checada se registró desde el celular físico "Celular Fisico" (deviceId S2RUBS32.51-15-3-19).**

### ✅ Historial completo del día (4 checadas)

| # | Timestamp (UTC) | Tipo | Método |
|---|-----------------|------|--------|
| 4 | 20:11:34 | Salida | PIN+Rostro (celular físico) |
| 3 | 20:00:27 | Entrada | PIN+Rostro |
| 2 | 19:44:24 | Salida | PIN+Rostro |
| 1 | 19:23:55 | Entrada | PIN+Rostro |

**Alternancia correcta: Entrada → Salida → Entrada → Salida** ✅

### 🐛 Bug adicional corregido

| Bug | Fix |
|-----|-----|
| `keyboardType: number` bloqueaba IDs alfanuméricos (`TEST001`, `AIM003`, etc.) | → `keyboardType: text` en `pin_screen.dart` línea 159 |

### 🎯 Flujo completo validado (13 pasos)

1. App arranca en celular físico
2. Llama a `/api/Kiosko/recuperar` con `deviceId` registrado
3. Backend genera token nuevo + actualiza hash
4. App guarda token en `flutter_secure_storage`
5. Va al scanner (sin pedir código)
6. Toca "Usar PIN"
7. Ingresa `TEST001` + PIN `1234`
8. App valida PIN (`/api/Employee/validar-pin`)
9. Abre `FacialScreen` con `metodoUsado = 'PIN+Rostro'`
10. Cámara captura foto → comprime a 300x300 JPG
11. Envía a `/api/Checada` con `fotoBase64`
12. Backend: valida token → detecta rostro → genera embedding → compara (>70%) → registra
13. App muestra "Salida exitosa" (pantalla amarilla)

### 📊 Estado del ecosistema

| Componente | Estado |
|------------|--------|
| **API** | ✅ `https://api.appcheck.com.mx` |
| **BD** | ✅ SQL Express (`AppcheckAPI`) |
| **Facial** | ✅ FaceONNX + ResNet-27 (600ms/foto) |
| **Empleados** | ✅ TEST001 con rostro registrado |
| **Kioskos** | ✅ 4 registrados (1 en uso en celular físico) |
| **APK** | ✅ Instalado en celular físico |
| **Checadas** | ✅ 4 registradas (alternancia correcta) |

### 🗺️ Roadmap post-MVP

#### Corto plazo (próxima sesión)
- [ ] Registrar más empleados (reales)
- [ ] Deploy del Admin Web (`admin.appcheck.com.mx`) si no está
- [ ] Probar los 5 métodos de checada en celular real
- [ ] Actualizar `PROJECT_STATUS.md` con cada método validado

#### Mediano plazo (Sprint B — Anti-fraude)
- [ ] **Liveness detection** (detectar foto impresa/video)
- [ ] **Device binding** (un empleado = un dispositivo)
- [ ] **Geofence** (validar ubicación para oficina)
- [ ] **Regla "Rostro solo prohibido en remoto"** en `ChecadaController`

#### Largo plazo (Sprint C — Escala)
- [ ] Múltiples embeddings por empleado (3-5 fotos, promedio)
- [ ] Migrar a ArcFace/InsightFace (si se necesita más precisión)
- [ ] Modo offline con cola local
- [ ] Panel admin con reportes y analytics

### 🏆 Hitos del día (06/Nov/2026)

1. ✅ Reconocimiento facial con IA real (FaceONNX)
2. ✅ 16/16 pruebas de validación (misma persona 74-83%, distintas 48-54%)
3. ✅ 5 bugs corregidos (detector, alineación, input name, dimensiones, métrica)
4. ✅ Defensa en profundidad (validación de métodos canónicos)
5. ✅ Deploy a producción (`api.appcheck.com.mx`)
6. ✅ BD limpia + backup pre-reset
7. ✅ Empleado TEST001 con rostro registrado
8. ✅ Checada end-to-end desde PowerShell (1419ms)
9. ✅ Prueba negativa (foto incorrecta → 401)
10. ✅ Bug del token hasheado corregido y desplegado
11. ✅ APK instalado en celular físico
12. ✅ Bug del `keyboardType` corregido
13. ✅ **Checada real desde celular físico (pantalla amarilla de éxito)**

**MVP cerrado. El producto está listo para piloto real.** 🎯

---


## 🏆 HITO FINAL — MVP 100% Funcional en Celular Real (06-07/Nov/2026)

**Estado:** 🟢 **MVP COMPLETADO Y VALIDADO EN PRODUCCIÓN.**

### ✅ Métodos validados end-to-end en celular físico real

| Método | Probado | Notas |
|--------|---------|-------|
| **ID + Rostro** | ✅ | Ingresando número de empleado |
| **QR + Rostro** | ✅ | Escaneando gafete (con lentes puestos) |
| **QR** | ⏳ | Pendiente de probar |
| **ID + PIN** | ⏳ | Pendiente de probar |
| **Rostro solo** | ⏳ | Pendiente (requiere búsqueda 1-N) |

### 📊 Checadas registradas en producción (5)


## 🏆 HITO FINAL — GPS en APK + PWA, Ciclo Completo Cerrado (09/Nov/2026)

**Estado:** 🟢 **MVP MULTIPLATAFORMA COMPLETO CON GPS.**

### ✅ Validación end-to-end en producción

| Origen | GPS | Checada |
|--------|-----|---------|
| **APK Android** (Kiosko) | ✅ 20.634066, -103.419420 | ✅ Registrada |
| **PWA iPhone** (Safari) | ✅ 20.634245, -103.419400 | ✅ Registrada |
| **PWA Android** (Chrome) | ✅ | ✅ Registrada |
| **Chrome desktop** | ⚠️ (sin GPS en desktop) | ✅ Registrada |

**5 checadas registradas hoy:**
- 4 desde PWA (iPhone/Android)
- 1 desde APK (Kiosko físico)
- 2 con GPS capturado
- 3 sin GPS (permiso denegado, pero checada OK)

### 🎯 Lo que se logró

- **Backend:** endpoint público `/api/Checada/empleado` + GPS en `/api/Checada`
- **APK Kiosko:** captura GPS con timeout de 8s
- **PWA Empleado:** captura GPS con timeout de 8s (iOS Safari bloquea requestPermission)
- **BD:** `PuntoLecturaId` nullable, `Latitud`/`Longitud` opcionales
- **Seguridad:** par `(NumeroEmpleado + PIN)` único global

### 🐛 Bugs corregidos en esta sesión

| # | Bug | Fix |
|---|-----|-----|
| 1 | `AndroidManifest.xml` con permisos dentro de `<application>` | Movidos antes de `<application>` |
| 2 | `StorageService.borrarToken` no existía en Kiosko | Agregado |
| 3 | Warning `unnecessary_null_comparison` | Removido `deviceId != null` |
| 4 | `PuntoLecturaId` no aceptaba NULL | `ALTER COLUMN ... NULL` en BD |
| 5 | `RegistroJornada.PuntoLecturaId` no era nullable en C# | `Guid?` |

### 🖥️ Infraestructura completa

| Componente | URL |
|------------|-----|
| API | `https://api.appcheck.com.mx` |
| PWA Empleado | `https://app.appcheck.com.mx` |
| Admin Web | `https://admin.appcheck.com.mx` |
| APK Kiosko | Distribución directa (WhatsApp) |

### 📊 Repos sincronizados

| Repo | Último commit |
|------|---------------|
| `Appcheck-API` | (pendiente push) |
| `Appcheck-Kiosko` | (pendiente push) |
| `Appcheck-Admin` | `f28f0d1` |
| `Appcheck-App` | (primer commit) |

### 🗺️ Roadmap post-MVP

#### Corto plazo
- [ ] Probar los 3 métodos faltantes en PWA (`QR`, `ID+PIN`, `Rostro`)
- [ ] Verificar Admin Web en producción
- [ ] Registrar empleados reales
- [ ] Documentar instalación (APK + PWA)

#### Mediano plazo (Sprint B — Anti-fraude)
- [ ] Liveness detection
- [ ] Device binding
- [ ] Geofence (validar ubicación)
- [ ] Regla "Rostro solo prohibido en remoto"

#### Largo plazo (Sprint C — Escala)
- [ ] Múltiples embeddings por empleado
- [ ] Migrar a ArcFace/InsightFace
- [ ] Reportes y analytics
- [ ] Notificaciones push

---


## 🏆 HITO FINAL — 6 Métodos de Checada (09/Nov/2026)

**Estado:** 🟢 **MVP COMPLETO con 6 métodos.**

### ✅ Métodos soportados

| # | Método | Kiosko (APK) | PWA |
|---|--------|--------------|-----|
| 1 | **QR** | ✅ | ✅ |
| 2 | **QR + Rostro** | ✅ | ✅ |
| 3 | **ID + PIN** | ✅ | ✅ |
| 4 | **ID + Rostro** | ✅ | ✅ |
| 5 | **Rostro** | ✅ | ✅ |
| 6 | **Huella** | ✅ (local_auth) | ⚠️ Próximamente (WebAuthn) |

### ✅ Validación end-to-end

**APK (Kiosko):**
- 6 métodos funcionando
- Huella con `local_auth` (requiere huella registrada en el celular)
- Backend valida `PuedeUsarHuella` por empleado

**PWA (iPhone/Android):**
- 5 métodos funcionando
- Huella muestra "Próximamente" (WebAuthn pendiente Sprint C)
- GPS capturado (con timeout de 8s para iOS Safari)

### 🐛 Bugs corregidos en esta sesión

| # | Bug | Fix |
|---|-----|-----|
| 1 | Falta coma en lista de métodos | `"Rostro",` |
| 2 | `PuedeUsarHuella` duplicado en BD | Verificar antes de agregar |
| 3 | `AppTheme` no existe en Kiosko | Crear `lib/theme/app_theme.dart` |
| 4 | `AuthenticationOptions` no existe en `local_auth` 3.x | Quitar `options` de `authenticate` |
| 5 | `MainActivity.kt` no era `FlutterFragmentActivity` | Cambiar (requerido por `local_auth`) |
| 6 | Permisos `USE_BIOMETRIC` faltaban | Agregar en `AndroidManifest.xml` |

### 🗺️ Deuda técnica (Sprint C)

- [ ] **WebAuthn completo** para huella en PWA (backend con `Fido2NetLib` + tabla `EmpleadoCredenciales`)
- [ ] **Liveness detection** (anti-spoofing)
- [ ] **Device binding** (1 empleado = 1 celular)
- [ ] **Actualizar web pública** (`www.appcheck.com.mx`)

---


## 🏆 HITO — GPS en TODOS los Métodos (09/Nov/2026)

**Estado:** 🟢 **MVP COMPLETO con GPS en todos los métodos.**

### ✅ Validación end-to-end

| Método | GPS capturado | PuntoLecturaId |
|--------|---------------|----------------|
| **ID+PIN** | ✅ 20.634245, -103.419400 | NULL (PWA) |
| **ID+Rostro** | ✅ 20.634202, -103.419369 | NULL (PWA) |
| **Rostro** | ✅ 20.634210, -103.419378 | NULL (PWA) |
| **QR** | ❌ (no aplica en PWA) | — |
| **QR+Rostro** | ❌ (no aplica en PWA) | — |
| **Huella** | ⏳ (WebAuthn pendiente) | — |

### 🐛 Bugs corregidos en esta sesión

| # | Bug | Fix |
|---|-----|-----|
| 1 | GPS solo se capturaba en `CameraScreen` | Capturar en `HomeScreen` (antes de decidir método) |
| 2 | `ID+PIN` no enviaba GPS | Agregar `latitud`/`longitud` al `registrarChecadaEmpleado` |
| 3 | `CameraScreen` capturaba GPS 2 veces | Recibir GPS del `HomeScreen` (no capturar de nuevo) |
| 4 | Debug log visible en producción | Eliminar el cuadro negro de logs |
| 5 | Falta import de `geolocator` en `home_screen.dart` | Agregado |

### 🎯 Arquitectura del GPS


## 🏆 HITO — Filtros de Reportes STPS (10/Nov/2026)

**Estado:** 🟢 **Admin Web 100% funcional con reportes filtrables.**

### ✅ Filtros implementados

| # | Filtro | Backend | Frontend |
|---|--------|---------|----------|
| 1 | **Fecha inicio** | ✅ | ✅ |
| 2 | **Fecha fin** | ✅ | ✅ |
| 3 | **Empresa** (solo SuperAdmin) | ✅ | ✅ |
| 4 | **Vendedor** (solo SuperAdmin) | ✅ | ✅ |
| 5 | **Método** (QR, Rostro, PIN, etc.) | ✅ | ✅ |
| 6 | **Empleado** (opcional) | ✅ | ⏳ |

### 🆕 Endpoints nuevos

**`GET /api/admin/empresas`**
- Devuelve todas las cuentas (para dropdown de filtro)
- Solo SuperAdmin

**`GET /api/admin/vendedores`**
- Devuelve todos los usuarios con rol Vendedor
- Solo SuperAdmin

**`GET /api/checada/reporte` (actualizado)**
- Acepta query params: `fechaInicio`, `fechaFin`, `cuentaId`, `vendedorId`, `metodoUsado`, `empleadoId`
- Filtra por los parámetros proporcionados

### 🎯 Validación en producción

- ✅ Filtro por fechas: funciona
- ✅ Filtro por empresa: funciona
- ✅ Filtro por vendedor: funciona (después de asignar vendedor a la cuenta)
- ✅ Filtro por método: funciona (QR, Rostro, ID+Rostro, Huella)
- ✅ Combinaciones de filtros: funcionan
- ✅ Exportación CSV: incluye filtros aplicados
- ✅ Exportación Excel: incluye filtros aplicados

### 🐛 Bugs corregidos

| # | Bug | Fix |
|---|-----|-----|
| 1 | `null: type 'String'` en `listarKioskos()` | Manejar `{value: [...], Count: N}` |
| 2 | `listarEmpleados()` duplicado | Eliminar duplicado |
| 3 | `DropdownMenuItem<String?>` no compatible | Cambiar a `<String>` |
| 4 | Fechas perdidas al agregar filtros | Reestructurar `_buildReportes()` |

### 🗺️ Modelo de negocio

- **SuperAdmin** → ve todas las empresas, filtra por vendedor
- **TenantAdmin** → ve solo su empresa
- **Vendedor** → ve sus empresas asignadas
- **Cada cuenta** tiene un vendedor asignado (`VendedorId`)

### 🗺️ Roadmap post-MVP

- [ ] **FASE 2:** Registrar 3-5 empleados reales
- [ ] **FASE 3:** Documentar instalación (APK + PWA)
- [ ] **Sprint C:** WebAuthn (huella en PWA), liveness, device binding, geofence
- [ ] **Salir a vender**

---


## 🏆 HITO — Dashboard con KPIs y Gráficos (10/Nov/2026)

**Estado:** 🟢 **Admin Web con Dashboard analítico.**

### ✅ Dashboard con 8 KPIs

| # | KPI | Descripción |
|---|-----|-------------|
| 1 | **Total Checadas** | Checadas del período |
| 2 | **Total Empleados** | Empleados activos |
| 3 | **Empleados con Checada** | Empleados que checaron al menos 1 vez |
| 4 | **% Puntuales** | Checadas antes de las 9:00 AM |
| 5 | **% Retardos** | Checadas después de las 9:00 AM |
| 6 | **Horas Extra** | Checadas de salida después de las 6:00 PM |
| 7 | **Ausencias** | Empleados sin checada |
| 8 | **Kioskos** | Kioskos activos |

### 📊 Gráficos implementados

| Gráfico | Tipo | Descripción |
|---------|------|-------------|
| **Checadas por Día** | Líneas | Tendencia día a día con tooltip |
| **Métodos Usados** | Pastel | Distribución de métodos con leyenda |

### 🆕 Endpoint nuevo

**`GET /api/analytics/dashboard`**
- Devuelve los 8 KPIs + 2 gráficos
- Filtra por fechaInicio y fechaFin (default: mes actual)
- SuperAdmin ve todo, TenantAdmin solo su cuenta

### 🐛 Bugs corregidos

| # | Bug | Fix |
|---|-----|-----|
| 1 | Gráfica de líneas sin valores visibles | Tooltip + puntos más grandes |
| 2 | Gráfico de pastel sin leyenda | Leyenda al lado (color + método + total) |
| 3 | `tooltipRoundedRadius` no existe en `fl_chart 1.3.1` | Quitado |

### 🗺️ Roadmap post-dashboard

- [ ] **Reportes adicionales:** comparativo mes anterior, histórico del año, por empleado, por sucursal, horas extra, retardos, ausencias
- [ ] **Sucursales:** CRUD de sucursales (multi-ubicación)
- [ ] **Salir a vender**

---


## 🏆 HITO — CRUD de Checadas desde Admin + Fix IIS WebDAV (10/Nov/2026)

**Estado:** 🟢 Admin Web 100% funcional para crear y eliminar checadas.

### ✅ Lo que se logró
- **Backend:** Endpoint `POST /api/checada/manual` y `DELETE /api/checada/{id}` probados y funcionando en producción.
- **Admin Web:** Modal "Nueva Checada Manual" y botón de eliminar checadas con confirmación, validados end-to-end.
- **Infraestructura:** Se deshabilitó **WebDAV** en el IIS del VPS, lo cual estaba bloqueando las peticiones POST/DELETE con error 405 (Method Not Allowed).

### 🐛 Bugs corregidos
| # | Bug | Fix |
|---|-----|-----|
| 1 | Error 405 en `/api/checada/manual` | Se removió el módulo WebDAV de IIS (`web.config` y módulos globales). |
| 2 | `FormatException: Unexpected end of JSON` en Admin | Causado por el 405 previo. Resuelto al habilitar el endpoint. |
| 3 | Envío incorrecto de parámetro | El Admin ahora envía el `empleadoId` (GUID) en lugar del `numeroEmpleado` para evitar el 404. |
| 4 | Limpieza de empleado duplicado | Se borraron registros en `AcuerdosFirmados`, `RegistrosJornada` y `EmpleadoCredenciales` antes de eliminar el empleado. |

### 🗺️ Deuda Técnica (Sprint C)
- [ ] **Fix de acentos (Mojibake):** Los textos en el Admin Web muestran `Ã³` en lugar de `ó`. Pendiente ajustar la codificación UTF-8 en el compilador de Flutter o en los archivos fuente locales.
- [ ] **Liveness detection** (anti-spoofing).
- [ ] **Device binding** (1 empleado = 1 celular).
- [ ] **WebAuthn** para huella en PWA.

### 🎯 Próximo paso inmediato
- [ ] Registrar empleados reales (FASE 2).
- [ ] Documentar instalación de APK + PWA para clientes.

## 🏆 HITO — Backend Multi-Sucursal y Multi-Turno (10/Nov/2026)

**Estado:** 🟢 Backend listo y probado en producción.

### ✅ Lo que se logró
- **Base de Datos:** Creación de las tablas `Sucursales`, `Turnos`, `TurnoDetalle` y `EmpleadoTurnos`.
- **API Endpoints Nuevos:**
  - `GET/POST /api/Sucursal` (Gestión de sucursales).
  - `GET/POST /api/Turno` (Gestión de turnos con detalle por día de la semana).
  - `POST /api/Turno/asignar` (Asignación de turnos a empleados, con soporte para rotación por fechas).
- **Validación:** Todos los endpoints probados exitosamente vía PowerShell y registros verificados en SQL Server.

### 🐛 Bugs corregidos / Mejoras
| # | Detalle | Fix |
|---|---------|-----|
| 1 | Error de Namespaces en Controladores | Se corrigieron los `using` a `AppCheck.Data` y `AppCheck.Data.Entities`. |
| 2 | Endpoint GET de Sucursal devolvía un objeto | Se ajustará en el próximo sprint para que devuelva un `List` siempre. |

### 🗺️ Roadmap Actualizado
- [ ] **Fase B (Frontend):** Crear la interfaz en el Admin Web para gestionar Sucursales y Turnos (CRUD visual).
- [ ] **Fase B (Reportes):** Asociar las checadas al `TurnoId` para calcular retardos exactos según el horario del empleado.
- [ ] **Fase C (Geofence):** Validar que el GPS de la checada esté dentro del `RadioMetros` de la Sucursal.
- [ ] **Deuda Técnica:** Fix de acentos (Mojibake) en el Admin Web.
