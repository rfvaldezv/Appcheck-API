\# 🚀 AppCheck - Seguimiento del Proyecto



> \*\*Última actualización:\*\* 02/11/2026

> \*\*Estado:\*\* Backend en desarrollo activo. Ciclo legal completo funcional.



\---



\## 📌 ¿Qué es AppCheck?



SaaS mexicano que convierte cualquier dispositivo Android (tablet empotrable o celular de último usuario) en un reloj checador con \*\*validez legal ante la STPS\*\*, respaldo en la nube y modo offline. Resuelve la obligación del artículo 132, fracción XXXIV de la LFT (vigente a partir del 1 de enero de 2027) sin que las PYMES tengan que comprar hardware costoso.



\*\*Tagline:\*\* \*"Checa a tu equipo. Cumple con la STPS."\*



\---



\## 🏛️ Arquitectura de Negocio (Multi-Nivel)



| Nivel | Rol | Qué hace |

|-------|-----|----------|

| 1 | \*\*SuperAdmin (AppCheck)\*\* | Control total, facturación, gestión de vendedores. |

| 2 | \*\*Vendedor / Distribuidor\*\* | Gestiona su cartera, recibe comisiones (fijo + variable). |

| 3a | \*\*Cliente Corporativo\*\* | Centraliza reportes de múltiples sucursales. |

| 3b | \*\*Cliente Independiente\*\* | PYME con uno o pocos kioskos. |

| 4 | \*\*Admin (Administrativo)\*\* | Gestiona empleados y kioskos de su sucursal. |

| 5 | \*\*Contador\*\* | Solo lectura de reportes y descarga de paquetes STPS. |



\---



\## 🛡️ Modelo de Licenciamiento



\- \*\*Puntos de Lectura:\*\* Kiosko Fijo (tablet empotrada) y Punto Personal (celular del empleado - BYOD).

\- \*\*Licenciamiento:\*\* Por empleados (límite de plan) y por kiosko (licencia mensual).

\- \*\*Modo Policía:\*\* Offline-first. Si no hay pago, el kiosko entra en "Gracia" (7 días) y luego "Restringido" (checa con bandera, pero notifica al SuperAdmin).

\- \*\*Códigos de Activación:\*\* 8 caracteres, hasheados, expiración 72h, vinculados al `deviceId`.



\---



\## 📱 Funcionalidades del Producto



\- \*\*Kiosko Fijo:\*\* Modo kiosco, QR dinámico (TOTP) y Reconocimiento Facial.

\- \*\*Punto Personal (BYOD):\*\* Checada con GPS (geocerca) y selfie.

\- \*\*Modo Mix:\*\* El empleado alterna entre QR y Facial con un clic.

\- \*\*QR Antifraude:\*\* Cambia cada 30 segundos. Si lo comparten, expira.

\- \*\*Dashboard por Rol:\*\* Vista específica para cada nivel jerárquico.



\---



\## 🏗️ Arquitectura Técnica



\- \*\*Backend:\*\* .NET 10 (C#), Entity Framework Core, SQL Server.

\- \*\*Frontend Kiosko:\*\* Flutter / Dart (Android only).

\- \*\*Frontend Admin:\*\* Flutter Web o React (por definir).

\- \*\*Base de Datos:\*\* SQL Server (local en `C:\\Aplica` y VPS).

\- \*\*Caché Local (Kiosko):\*\* Drift (SQLite) con sincronización vía WorkManager.

\- \*\*Repositorios:\*\*

&#x20; - API: https://github.com/rfvaldezv/Appcheck-API.git

&#x20; - Admin: https://github.com/rfvaldezv/Appcheck-Admin.git

&#x20; - Kiosko: https://github.com/rfvaldezv/Appcheck-Kiosko.git



\---



\## 🗄️ Estado de la Base de Datos



\- \*\*Nombre:\*\* `AppcheckAPI`

\- \*\*Usuario de conexión:\*\* `sa` (configurado en `appsettings.json`).

\- \*\*Tablas creadas:\*\* `Cuentas`, `Usuarios`, `Empleados`, `PuntosLectura`, `RegistrosJornada`, `AcuerdosFirmados`, `CodigosActivacion`, `BitacoraAuditoria`.

\- \*\*Script de migración:\*\* `C:\\Aplica\\Appcheck\_API\\InitialCreate.sql` (ejecutado en SSMS).

\- \*\*Datos semilla:\*\*

&#x20; - Cuenta: `AppCheck SA de CV` (CuentaId: `d5a470d4-a034-4efa-bfb5-9dfd4ff09a03`)

&#x20; - SuperAdmin: `admin@appcheck.com.mx` / `Admin123!` (UsuarioId: `7e769c96-2747-4cd4-9562-27515e10a6fa`)

&#x20; - Kiosko de prueba: `TOKEN\_DE\_PRUEBA\_12345` (PuntoLecturaId: `0c564018-232c-4b21-a3fd-ce3595a056b5`)



\---



\## ✅ Lo que YA está hecho (Backend)



\### Capa de Datos (`AppCheck.Data`)

\- \[x] 8 entidades creadas: `Cuenta`, `Usuario`, `Empleado`, `PuntoLectura`, `RegistroJornada`, `AcuerdoFirmado`, `CodigoActivacion`, `BitacoraAuditoria`.

\- \[x] `AppCheckDbContext` con configuración de relaciones, índices y precisiones decimales.

\- \[x] Migración inicial generada y aplicada a `AppcheckAPI`.



\### API (`AppCheck.API`)

\- \[x] \*\*Autenticación JWT\*\* (`AuthController`):

&#x20; - `POST /api/auth/login` → Devuelve token JWT.

&#x20; - `POST /api/auth/seed` → Crea el SuperAdmin y su cuenta.

\- \[x] \*\*Alta de Empleados\*\* (`EmployeeController`):

&#x20; - `POST /api/Employee` → Crea empleado + acuse legal firmado.

&#x20; - `GET /api/Employee` → Lista empleados de la cuenta.

\- \[x] \*\*Registro de Checadas\*\* (`ChecadaController`):

&#x20; - `POST /api/checada` → Registra checada con hash SHA-256, detecta Entrada/Salida automáticamente, valida token del kiosko.

&#x20; - `GET /api/checada/historial/{empleadoId}` → Historial de checadas (protegido con JWT).

\- \[x] \*\*Swagger con seguridad JWT\*\* (candado verde funcional).

\- \[x] \*\*Health check:\*\* `GET /api/health` → Valida conexión a la BD.



\### Pruebas exitosas (02/11/2026)

\- \[x] Login con `admin@appcheck.com.mx` → Token JWT.

\- \[x] Alta de empleado `EMP001` (Xavier López) → Acuse legal generado.

\- \[x] Primera checada → \*\*Entrada\*\* registrada con hash `CDC6EBA5...`

\- \[x] Segunda checada → \*\*Salida\*\* detectada automáticamente con hash `AAEC9ED2...`

\- \[x] Build limpio: 0 errores, 0 advertencias (nullable resuelto en ChecadaController).



\---



\## 🚧 Lo que FALTA (Backend)



\### `KioskoController` (Siguiente paso inmediato)

\- \[ ] `POST /api/kiosko/generar-codigo` → Genera código de 8 caracteres para activar un kiosko (protegido con JWT).

\- \[ ] `POST /api/kiosko/activar` → El kiosko se activa con el código y recibe su `TokenPuntoLectura` (público).

\- \[ ] `GET /api/kiosko` → Lista kioskos del Propietario.

\- \[ ] `POST /api/kiosko/heartbeat` → El kiosko reporta que está vivo (para el modo policía).



\### Pendientes de Backend (post-MVP)

\- \[ ] `FacturacionController` (CFDI, planes, suscripciones).

\- \[ ] `ComisionesController` (liquidación de vendedores).

\- \[ ] `DashboardController` (métricas por rol).

\- \[ ] Subida de PDFs de acuse a Azure Blob Storage (actualmente es texto plano).

\- \[ ] QR dinámico TOTP (validación antifraude).

\- \[ ] Notificaciones push (Firebase).

\- \[ ] Variables de entorno para secretos en producción (Azure Key Vault o similar).



\---



\## 🎨 Lo que FALTA (Frontend)



\### `Appcheck-Kiosko` (Flutter Android)

\- \[ ] Pantalla de activación con código de 8 caracteres.

\- \[ ] Pantalla de escaneo QR con `mobile\_scanner`.

\- \[ ] Pantalla de reconocimiento facial (paquete por definir).

\- \[ ] Modo kiosco nativo (LOCK\_TASK\_MODE).

\- \[ ] Caché local con Drift (SQLite).

\- \[ ] Sincronización con WorkManager (offline-first).

\- \[ ] Botón para alternar QR/Facial (Mix).



\### `Appcheck-Admin` (Flutter Web o React)

\- \[ ] Login.

\- \[ ] Dashboard por rol (SuperAdmin, Vendedor, Propietario, Admin, Contador).

\- \[ ] Alta de empleados (con acuse PDF).

\- \[ ] Gestión de kioskos (activar, revocar, ver estado).

\- \[ ] Reportes de jornada (exportable a Excel/PDF para STPS).

\- \[ ] Facturación y comisiones.



\---



\## 🔄 Cómo retomar el proyecto en 2 minutos



\### 1. Verificar que la BD esté corriendo

```powershell

\# Abrir SSMS y verificar:

SELECT COUNT(\*) FROM AppcheckAPI.dbo.Cuentas; -- Debe ser >= 1



### Sesión 3 (02/11/2026) - Hito: Ecosistema Funcional
- [x] **Backend:** Completado el ciclo legal (JWT, alta de empleados, checadas con hash, activación de kioskos).
- [x] **AppCheck_Admin (Flutter Web):**
  - Dashboard con sidebar (Resumen, Kioskos, Empleados, Reportes).
  - CRUD completo de Kioskos (Alta directa, generar código, editar, revocar, eliminar).
  - CRUD completo de Empleados (Alta con acuse legal, editar, eliminar).
  - Login con funcionalidad "ojito" para mostrar/ocultar contraseña.
- [x] **AppCheck_Kiosko (Flutter Android):**
  - Pantalla de activación con código de 8 caracteres.
  - Almacenamiento seguro del token con `flutter_secure_storage`.
  - Navegación a pantalla de escáner (pendiente integración real de cámara).
- [x] **Base de Datos:** Migraciones aplicadas, usuarios semilla creados, kioskos y empleados de prueba funcionando.
- [x] **GitHub:** Backend y Admin resguardados en repositorios independientes.

**Siguientes pasos:**
1. **Módulo de Facturación y Comisiones** (backend y frontend).
2. **Dashboard de Reportes STPS** (descarga de PDF consolidado).
3. **AppCheck_Kiosko:** Integración real de la cámara (`mobile_scanner`) y modo kiosco.
4. **AppCheck_Employee (BYOD):** App móvil para checadas con GPS y selfie.
5. **QR Dinámico (TOTP):** Implementación antifraude.
6. **Reconocimiento Facial:** Integración con paquete de Flutter.
7. **Despliegue a VPS:** Configuración de producción con variables de entorno.
