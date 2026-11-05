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


### Sesión 4 (02/11/2026) - Módulo de Facturación y Comisiones
- [x] **Backend:** Tablas `Planes`, `Suscripciones`, `Facturas`, `ConfiguracionesComision`, `Liquidaciones` creadas.
- [x] **Backend:** Endpoints de planes, suscripciones, facturas y comisiones funcionando.
- [x] **Frontend Admin:**
  - Vista de "Mi Suscripción" conectada (muestra plan PYME Básico).
  - Vista de "Facturas" con generación mensual y acciones (Marcar como pagada).
- [x] **Datos semilla:** 3 planes (Básico, Plus, Corporativo) insertados.
- [x] **Pruebas exitosas:** Asignación de plan, generación de factura de noviembre ($346.84 con IVA), listado de facturas.

**Siguientes pasos:**
1. Vista de Comisiones (para rol Vendedor).
2. Cámara real en AppCheck_Kiosko (Flutter).
3. AppCheck_Employee (BYOD) con GPS y selfie.
4. QR Dinámico (TOTP) antifraude.
5. Despliegue a VPS.

### Sesión 5 (02/11/2026) - Módulo de Facturación 100%
- [x] **Backend:** Endpoints de planes, suscripciones, facturas y comisiones funcionando.
- [x] **Frontend Admin:**
  - Vista de "Mi Suscripción" (plan PYME Básico activo).
  - Vista de "Facturas" (generación y marcado de pago).
  - Vista de "Comisiones" (solo para Vendedor, con desglose y total estimado).
- [x] **Fix crítico:** Configuración de JWT con `MapInboundClaims = false` para .NET 10.
- [x] **Pruebas exitosas:** Login de Vendedor, endpoint de comisiones y vista en el Admin.

**Siguientes pasos:**
1. Cámara Real en AppCheck_Kiosko (Flutter).
2. AppCheck_Employee (BYOD) con GPS y selfie.
3. QR Dinámico (TOTP) antifraude.
4. Despliegue a VPS.

### Sesión 6 (02/11/2026) - Kiosko Funcional y Fin de Sesión
- [x] **AppCheck_Kiosko (Flutter):**
  - Pantalla de activación con formateo automático de código y botón de pegar.
  - Cámara activada con `mobile_scanner` y overlay de escaneo.
  - Conexión exitosa a la API a través de `adb reverse` (túnel de emulador).
  - Manejo de errores robusto (sin FormatException).
- [x] **Backend:** Desactivada la redirección a HTTPS en entorno de desarrollo para permitir HTTP desde el emulador.
- [x] **Flujo de activación completo:** Generación de código -> Activación en Kiosko -> Token guardado.
- [x] **Pendiente para próxima sesión:** Prueba de escaneo real de QR en dispositivo físico.
- [x] **Estado del proyecto:** Backend, Admin y Kiosko (base) funcionando. Listo para pruebas en hardware real.

**Siguientes pasos:**
1. **Probar en celular físico:** Escaneo de QR real y flujo completo de checada.
2. Implementar AppCheck_Employee (BYOD) con GPS y selfie.
3. Implementar QR Dinámico (TOTP) para antifraude.
4. Despliegue a VPS (configuración de producción).

# 🚀 AppCheck - Seguimiento del Proyecto

> **Última actualización:** 03/11/2026
> **Estado:** SaaS en Producción (API, Admin, Landing) + Kiosko con QR y Facial funcionando.

---

## 📌 ¿Qué es AppCheck?

SaaS mexicano que convierte cualquier dispositivo Android (tablet empotrable o celular de último usuario) en un reloj checador con **validez legal ante la STPS**, respaldo en la nube y modo offline. Resuelve la obligación del artículo 132, fracción XXXIV de la LFT (vigente a partir del 1 de enero de 2027) sin que las PYMES tengan que comprar hardware costoso.

**Tagline:** *"Checa a tu equipo. Cumple con la STPS."*

---

## 🌍 Estado de Producción (Dominios y URLs)

- **Landing Page:** `https://appcheck.com.mx` (en vivo con SEO, branding y AIMe).
- **API (Backend):** `https://api.appcheck.com.mx` (en vivo, .NET 10, conectada a SQL Server en VPS).
- **Admin (Flutter Web):** `https://admin.appcheck.com.mx` (en vivo, responsivo para móvil).
- **Kiosko (Flutter Android):** APK generado e instalado en celular físico, conectado a producción.

---

## 🏛️ Arquitectura de Negocio (Multi-Nivel)

| Nivel | Rol | Qué hace |
|-------|-----|----------|
| 1 | **SuperAdmin (AppCheck)** | Control total, facturación, gestión de vendedores. |
| 2 | **Vendedor / Distribuidor** | Gestiona su cartera, recibe comisiones (fijo + variable). |
| 3a | **Cliente Corporativo** | Centraliza reportes de múltiples sucursales. |
| 3b | **Cliente Independiente** | PYME con uno o pocos kioskos. |
| 4 | **Admin (Administrativo)** | Gestiona empleados y kioskos de su sucursal. |
| 5 | **Contador** | Solo lectura de reportes y descarga de paquetes STPS. |

---

## 🛡️ Modelo de Licenciamiento

- **Puntos de Lectura:** Kiosko Fijo (tablet empotrada) y Punto Personal (celular del empleado - BYOD).
- **Licenciamiento:** Por empleados (límite de plan) y por kiosko (licencia mensual).
- **Modo Policía:** Offline-first. Si no hay pago, el kiosko entra en "Gracia" (7 días) y luego "Restringido" (checa con bandera, pero notifica al SuperAdmin).
- **Códigos de Activación:** 8 caracteres, hasheados, expiración 72h, vinculados al `deviceId`.

---

## 🏗️ Arquitectura Técnica

- **Backend:** .NET 10 (C#), Entity Framework Core, SQL Server.
- **Frontend Kiosko:** Flutter / Dart (Android only).
- **Frontend Admin:** Flutter Web (Responsive: Sidebar en PC, Drawer en móvil).
- **Base de Datos:** SQL Server (local en `C:\Aplica` y VPS de producción).
- **Caché Local (Kiosko):** Drift (SQLite) con sincronización vía WorkManager (pendiente).
- **Repositorios:**
  - API: https://github.com/rfvaldezv/Appcheck-API.git
  - Admin: https://github.com/rfvaldezv/Appcheck-Admin.git
  - Kiosko: https://github.com/rfvaldezv/Appcheck-Kiosko.git

---

## ✅ Lo que YA está hecho (Hitos Alcanzados)

### 1. Backend (API)
- [x] Autenticación JWT (Login, Seed de SuperAdmin).
- [x] Gestión de Empleados (Alta con acuse legal, Edición, Eliminación).
- [x] Registro de Checadas (Hash SHA-256, detección automática Entrada/Salida).
- [x] Gestión de Kioskos (Generar código, Activar, Regenerar, Revocar, Eliminar).
- [x] Facturación (Planes, Suscripciones, Generar Factura, Marcar Pagada).
- [x] Comisiones (Cálculo de MRR y comisión para Vendedor).
- [x] CORS configurado y `MapInboundClaims = false` para JWT en .NET 10.

### 2. Admin (Flutter Web)
- [x] Login funcional con "ojito" para ver contraseña.
- [x] Dashboard con Sidebar (PC) y Drawer (Móvil) para diseño responsivo.
- [x] CRUD de Kioskos (Alta Directa, Generar Código, Editar, Revocar, Eliminar, Buscador).
- [x] CRUD de Empleados (Alta con captura de foto facial opcional, Editar, Eliminar).
- [x] Reportes STPS (Filtro por fechas, descarga de CSV).
- [x] Vista de Mi Suscripción y Facturas.
- [x] Vista de Comisiones (para rol Vendedor).
- [x] **Registro Biométrico:** Botones de "Tomar Foto" y "Subir Foto" al dar de alta empleado.

### 3. Kiosko (Flutter Android)
- [x] Activación con código de 8 caracteres (con formateo automático y botón de pegar).
- [x] Cámara QR funcional (`mobile_scanner`).
- [x] Flujo de Reconocimiento Facial (Doble Check: escanea QR -> abre cámara frontal -> registra checada).
- [x] Conexión a API de producción (`https://api.appcheck.com.mx`).
- [x] Generación de APK de Producción.

### 4. Landing Page (HTML/CSS)
- [x] En vivo en `appcheck.com.mx`.
- [x] SEO optimizado (Meta Tags, Schema.org, Open Graph, Keywords).
- [x] Branding completo (Logo, Favicon, Paleta de colores, Tipografía Inter).
- [x] Secciones: Hero, Características, Precios, FAQ, Footer con "AIMe".

---

## 🚧 Lo que FALTA (Backlog Priorizado)

### Fase 1: Blindaje del Producto (Corto Plazo)
- [ ] **Validación Facial Real:** Implementar la comparación biométrica en la API (comparar la foto del QR con la foto del expediente guardada en `FaceTemplateHash`).
- [ ] **Modo "Index Facial" (1:N):** Que el Kiosko reconozca al empleado sin QR (ideal para oficinas pequeñas).
- [ ] **Acuse en PDF:** Generar el PDF del acuse de acuerdo legal con QuestPDF.
- [ ] **Modo Offline en Kiosko:** Implementar Drift (SQLite) para que el kiosko funcione sin internet y sincronice después.

### Fase 2: Crecimiento (Mediano Plazo)
- [ ] **AppCheck_Employee (BYOD):** App móvil para que el empleado chequee desde su celular con GPS (geocercas).
- [ ] **QR Dinámico (TOTP):** Implementar el QR antifraude que cambia cada 30 segundos.
- [ ] **Facturación Real (CFDI):** Integrar un PAC para generar facturas fiscales reales.

### Fase 3: Hardening (Largo Plazo)
- [ ] **Contraseñas Hasheadas:** Migrar de texto plano a BCrypt.
- [ ] **Variables de Entorno:** Mover secretos (JWT Key, Connection String) a Azure Key Vault o variables del sistema en IIS.
- [ ] **Pruebas de Carga:** Validar que la API soporte 50+ checadas simultáneas.

---

## 🔄 Cómo retomar el proyecto en 2 minutos

### 1. Verificar el VPS y la Base de Datos
- Conéctate por RDP al VPS.
- Abre SSMS y verifica que la base de datos `AppcheckAPI` esté en línea.

### 2. Verificar que la API esté en línea
Abre en el navegador: `https://api.appcheck.com.mx/api/health`
Debería devolver: `{"status":"AppCheck API funcionando",...}`

### 3. Probar el Admin en producción
Abre: `https://admin.appcheck.com.mx`
- Email: `admin@appcheck.com.mx`
- Password: `Admin123!`

### 4. Para Desarrollo Local (si es necesario)
```powershell
# API Local (para desarrollo)
Set-Location "C:\Aplica\Appcheck_API\AppCheck.API"
dotnet run --project "C:\Aplica\Appcheck_API\AppCheck.API\AppCheck.API.csproj" --urls "http://0.0.0.0:5123;https://localhost:7123"

# Admin Local
Set-Location "C:\Aplica\Appcheck_Admin"
flutter run -d chrome --dart-define=API_URL=https://api.appcheck.com.mx

# Kiosko Local (en emulador)
Set-Location "C:\Aplica\Appcheck_Kiosko"
flutter run -d emulator-5554 --dart-define=API_URL=https://api.appcheck.com.mx

# 🚀 AppCheck - Seguimiento del Proyecto

> **Última actualización:** 03/11/2026
> **Estado:** SaaS en Producción + Registro Biométrico Dual (Cámara/Archivo) + Admin Responsivo.

---

## 📌 ¿Qué es AppCheck?

SaaS mexicano que convierte cualquier dispositivo Android (tablet empotrable o celular de último usuario) en un reloj checador con **validez legal ante la STPS**, respaldo en la nube y modo offline. Resuelve la obligación del artículo 132, fracción XXXIV de la LFT (vigente a partir del 1 de enero de 2027) sin que las PYMES tengan que comprar hardware costoso.

**Tagline:** *"Checa a tu equipo. Cumple con la STPS."*

---

## 🌍 Estado de Producción (Dominios y URLs)

- **Landing Page:** `https://appcheck.com.mx` (en vivo con SEO, branding y AIMe).
- **API (Backend):** `https://api.appcheck.com.mx` (en vivo, .NET 10, conectada a SQL Server en VPS).
- **Admin (Flutter Web):** `https://admin.appcheck.com.mx` (en vivo, responsivo para móvil, con registro biométrico).
- **Kiosko (Flutter Android):** APK generado e instalado en celular físico, con QR y flujo Facial.

---

## 🏛️ Arquitectura de Negocio (Multi-Nivel)

| Nivel | Rol | Qué hace |
|-------|-----|----------|
| 1 | **SuperAdmin (AppCheck)** | Control total, facturación, gestión de vendedores. |
| 2 | **Vendedor / Distribuidor** | Gestiona su cartera, recibe comisiones (fijo + variable). |
| 3a | **Cliente Corporativo** | Centraliza reportes de múltiples sucursales. |
| 3b | **Cliente Independiente** | PYME con uno o pocos kioskos. |
| 4 | **Admin (Administrativo)** | Gestiona empleados y kioskos de su sucursal. |
| 5 | **Contador** | Solo lectura de reportes y descarga de paquetes STPS. |

---

## 🛡️ Modelo de Licenciamiento

- **Puntos de Lectura:** Kiosko Fijo (tablet empotrada) y Punto Personal (celular del empleado - BYOD).
- **Licenciamiento:** Por empleados (límite de plan) y por kiosko (licencia mensual).
- **Modo Policía:** Offline-first. Si no hay pago, el kiosko entra en "Gracia" (7 días) y luego "Restringido" (checa con bandera, pero notifica al SuperAdmin).
- **Códigos de Activación:** 8 caracteres, hasheados, expiración 72h, vinculados al `deviceId`.

---

## 🏗️ Arquitectura Técnica

- **Backend:** .NET 10 (C#), Entity Framework Core, SQL Server.
- **Frontend Kiosko:** Flutter / Dart (Android only).
- **Frontend Admin:** Flutter Web (Responsive: Sidebar en PC, Drawer en móvil).
- **Base de Datos:** SQL Server (local en `C:\Aplica` y VPS de producción).
- **Repositorios:**
  - API: https://github.com/rfvaldezv/Appcheck-API.git
  - Admin: https://github.com/rfvaldezv/Appcheck-Admin.git
  - Kiosko: https://github.com/rfvaldezv/Appcheck-Kiosko.git

---

## ✅ Lo que YA está hecho (Hitos Alcanzados)

### 1. Backend (API)
- [x] Autenticación JWT (Login, Seed de SuperAdmin).
- [x] Gestión de Empleados (Alta con acuse legal, foto facial en Base64, Edición, Eliminación).
- [x] Registro de Checadas (Hash SHA-256, detección automática Entrada/Salida).
- [x] Gestión de Kioskos (Generar código, Activar, Regenerar, Revocar, Eliminar).
- [x] Facturación (Planes, Suscripciones, Generar Factura, Marcar Pagada).
- [x] Comisiones (Cálculo de MRR y comisión para Vendedor).

### 2. Admin (Flutter Web)
- [x] Login funcional con "ojito" para ver contraseña.
- [x] **Diseño 100% Responsivo:** Sidebar en PC y Menú Hamburguesa (Drawer) en móvil.
- [x] **Diálogos Scrollables:** El alta de empleado y otros diálogos ya no se enciman en celular.
- [x] CRUD de Kioskos (Alta Directa, Generar Código, Editar, Revocar, Eliminar, Buscador).
- [x] CRUD de Empleados (Alta con **Registro Biométrico Dual**: Tomar Foto o Subir Archivo).
- [x] Reportes STPS (Filtro por fechas, descarga de CSV).
- [x] Vista de Mi Suscripción y Facturas.
- [x] Vista de Comisiones (para rol Vendedor).

### 3. Kiosko (Flutter Android)
- [x] Activación con código de 8 caracteres (con formateo automático y botón de pegar).
- [x] Cámara QR funcional (`mobile_scanner`).
- [x] **Flujo de Doble Check:** Escanea QR -> Abre cámara frontal -> Detecta rostro -> Registra checada (método "Facial").
- [x] Conexión a API de producción (`https://api.appcheck.com.mx`).
- [x] Generación de APK de Producción.

### 4. Landing Page (HTML/CSS)
- [x] En vivo en `appcheck.com.mx`.
- [x] SEO optimizado (Meta Tags, Schema.org, Open Graph, Keywords).
- [x] Branding completo (Logo, Favicon, Paleta de colores, Tipografía Inter).
- [x] Secciones: Hero, Características, Precios, FAQ, Footer con "AIMe".

---

## 🚧 Lo que FALTA (Backlog Priorizado)

### Fase 1: Blindaje del Producto (Corto Plazo)
- [ ] **Validación Facial Real (1:1):** Implementar la comparación biométrica en la API (comparar la foto en vivo del Kiosko con la foto Base64 guardada en `FaceTemplateHash`).
- [ ] **Modo "Index Facial" (1:N):** Que el Kiosko reconozca al empleado sin QR.
- [ ] **Acuse en PDF:** Generar el PDF del acuse de acuerdo legal con QuestPDF.
- [ ] **Modo Offline en Kiosko:** Implementar Drift (SQLite) para que funcione sin internet.

### Fase 2: Crecimiento (Mediano Plazo)
- [ ] **AppCheck_Employee (BYOD):** App móvil para checar desde el celular personal con GPS.
- [ ] **QR Dinámico (TOTP):** Implementar el QR antifraude que cambia cada 30 segundos.
- [ ] **Facturación Real (CFDI):** Integrar un PAC para generar facturas fiscales reales.

### Fase 3: Hardening (Largo Plazo)
- [ ] **Contraseñas Hasheadas:** Migrar de texto plano a BCrypt.
- [ ] **Variables de Entorno:** Mover secretos (JWT Key, Connection String) a variables del sistema en IIS.
- [ ] **Pruebas de Carga:** Validar que la API soporte 50+ checadas simultáneas.

---

## 🔄 Cómo retomar el proyecto en 2 minutos

### 1. Verificar el VPS y la Base de Datos
- Conéctate por RDP al VPS.
- Abre SSMS y verifica que la base de datos `AppcheckAPI` esté en línea.

### 2. Verificar que la API esté en línea
Abre en el navegador: `https://api.appcheck.com.mx/api/health`
Debería devolver: `{"status":"AppCheck API funcionando",...}`

### 3. Probar el Admin en producción
Abre: `https://admin.appcheck.com.mx`
- Email: `admin@appcheck.com.mx`
- Password: `Admin123!`

### 4. Para Desarrollo Local (si es necesario)
```powershell
# API Local
Set-Location "C:\Aplica\Appcheck_API\AppCheck.API"
dotnet run --project "C:\Aplica\Appcheck_API\AppCheck.API\AppCheck.API.csproj" --urls "http://0.0.0.0:5123;https://localhost:7123"

# Admin Local
Set-Location "C:\Aplica\Appcheck_Admin"
flutter run -d chrome --dart-define=API_URL=https://api.appcheck.com.mx

# Kiosko Local (en emulador)
Set-Location "C:\Aplica\Appcheck_Kiosko"
flutter run -d emulator-5554 --dart-define=API_URL=https://api.appcheck.com.mx

# 🚀 AppCheck - Seguimiento del Proyecto

> **Última actualización:** 03/11/2026
> **Estado:** SaaS en Producción con Validación Facial Real (1:1) y Registro Biométrico Dual.

---

## 📌 ¿Qué es AppCheck?

SaaS mexicano que convierte cualquier dispositivo Android (tablet empotrable o celular de último usuario) en un reloj checador con **validez legal ante la STPS**, respaldo en la nube y modo offline. Resuelve la obligación del artículo 132, fracción XXXIV de la LFT (vigente a partir del 1 de enero de 2027) sin que las PYMES tengan que comprar hardware costoso.

**Tagline:** *"Checa a tu equipo. Cumple con la STPS."*

---

## 🌍 Estado de Producción (Dominios y URLs)

- **Landing Page:** `https://appcheck.com.mx` (en vivo con SEO, branding y AIMe).
- **API (Backend):** `https://api.appcheck.com.mx` (en vivo, .NET 10, conectada a SQL Server en VPS, con validación facial real).
- **Admin (Flutter Web):** `https://admin.appcheck.com.mx` (en vivo, responsivo para móvil, con registro biométrico dual).
- **Kiosko (Flutter Android):** APK de producción en celular físico, con QR y Validación Facial real.

---

## 🏛️ Arquitectura de Negocio (Multi-Nivel)

| Nivel | Rol | Qué hace |
|-------|-----|----------|
| 1 | **SuperAdmin (AppCheck)** | Control total, facturación, gestión de vendedores. |
| 2 | **Vendedor / Distribuidor** | Gestiona su cartera, recibe comisiones (fijo + variable). |
| 3a | **Cliente Corporativo** | Centraliza reportes de múltiples sucursales. |
| 3b | **Cliente Independiente** | PYME con uno o pocos kioskos. |
| 4 | **Admin (Administrativo)** | Gestiona empleados y kioskos de su sucursal. |
| 5 | **Contador** | Solo lectura de reportes y descarga de paquetes STPS. |

---

## 🛡️ Modelo de Licenciamiento

- **Puntos de Lectura:** Kiosko Fijo (tablet empotrada) y Punto Personal (celular del empleado - BYOD).
- **Licenciamiento:** Por empleados (límite de plan) y por kiosko (licencia mensual).
- **Modo Policía:** Offline-first. Si no hay pago, el kiosko entra en "Gracia" (7 días) y luego "Restringido".
- **Códigos de Activación:** 8 caracteres, hasheados, expiración 72h, vinculados al `deviceId`.

---

## 🏗️ Arquitectura Técnica

- **Backend:** .NET 10 (C#), Entity Framework Core, SQL Server.
- **Validación Facial:** SkiaSharp (comparación pixel a pixel, sin licencias, seguro y libre de vulnerabilidades).
- **Frontend Kiosko:** Flutter / Dart (Android only).
- **Frontend Admin:** Flutter Web (Responsive: Sidebar en PC, Drawer en móvil).
- **Base de Datos:** SQL Server (local en `C:\Aplica` y VPS de producción).
- **Repositorios:**
  - API: https://github.com/rfvaldezv/Appcheck-API.git
  - Admin: https://github.com/rfvaldezv/Appcheck-Admin.git
  - Kiosko: https://github.com/rfvaldezv/Appcheck-Kiosko.git

---

## ✅ Lo que YA está hecho (Hitos Alcanzados)

### 1. Backend (API)
- [x] Autenticación JWT.
- [x] Gestión de Empleados (Alta con acuse legal, foto facial en Base64, Edición con foto, Eliminación).
- [x] Registro de Checadas (Hash SHA-256, detección automática Entrada/Salida).
- [x] **Validación Facial Real:** Comparación pixel a pixel con SkiaSharp. Umbral del 30% para el MVP (ajustado para tolerar iluminación y ángulos).
- [x] Gestión de Kioskos (Generar código, Activar, Regenerar, Revocar, Eliminar).
- [x] Facturación (Planes, Suscripciones, Generar Factura, Marcar Pagada).
- [x] Comisiones (Cálculo de MRR y comisión para Vendedor).

### 2. Admin (Flutter Web)
- [x] Login funcional con "ojito" para ver contraseña.
- [x] **Diseño 100% Responsivo:** Sidebar en PC y Menú Hamburguesa (Drawer) en móvil.
- [x] **Diálogos Scrollables:** El alta y edición de empleado no se enciman en celular.
- [x] CRUD de Kioskos (Alta Directa, Generar Código, Editar, Revocar, Eliminar, Buscador).
- [x] CRUD de Empleados (Alta y Edición con **Registro Biométrico Dual**: Tomar Foto o Subir Archivo).
- [x] Reportes STPS (Filtro por fechas, descarga de CSV).
- [x] Vista de Mi Suscripción, Facturas y Comisiones.

### 3. Kiosko (Flutter Android)
- [x] Activación con código de 8 caracteres (con formateo automático y botón de pegar).
- [x] Cámara QR funcional (`mobile_scanner`).
- [x] **Flujo de Doble Check:** Escanea QR -> Abre cámara frontal -> Captura foto -> Envía a la API -> Valida rostro -> Registra checada.
- [x] **Manejo de Errores:** Si la similitud es baja, muestra el porcentaje y permite regresar a escanear otro QR (con reinicio de cámara).
- [x] Generación de APK de Producción.

### 4. Landing Page (HTML/CSS)
- [x] En vivo en `appcheck.com.mx`.
- [x] SEO optimizado (Meta Tags, Schema.org, Open Graph, Keywords).
- [x] Branding completo (Logo, Favicon, Paleta de colores, Tipografía Inter).
- [x] Secciones: Hero, Características, Precios, FAQ, Footer con "AIMe".

---

## 🚧 Lo que FALTA (Backlog Priorizado)

### Fase 1: Blindaje del Producto (Corto Plazo)
- [ ] **Modo Offline en Kiosko:** Implementar Drift (SQLite) para que funcione sin internet y sincronice después.
- [ ] **Index Facial (1:N):** Que el Kiosko reconozca al empleado sin QR (ideal para oficinas pequeñas).
- [ ] **Acuse en PDF:** Generar el PDF del acuse de acuerdo legal con QuestPDF.
- [ ] **Mejorar el algoritmo facial:** Migrar de comparación pixel a pixel a un modelo de IA (FaceNet) para mayor precisión.

### Fase 2: Crecimiento (Mediano Plazo)
- [ ] **AppCheck_Employee (BYOD):** App móvil para checar desde el celular personal con GPS.
- [ ] **QR Dinámico (TOTP):** Implementar el QR antifraude que cambia cada 30 segundos.
- [ ] **Facturación Real (CFDI):** Integrar un PAC para generar facturas fiscales reales.

### Fase 3: Hardening (Largo Plazo)
- [ ] **Contraseñas Hasheadas:** Migrar de texto plano a BCrypt.
- [ ] **Variables de Entorno:** Mover secretos a variables del sistema en IIS.
- [ ] **Pruebas de Carga:** Validar que la API soporte 50+ checadas simultáneas.

---

## 🔄 Cómo retomar el proyecto en 2 minutos

### 1. Verificar el VPS y la Base de Datos
- Conéctate por RDP al VPS.
- Abre SSMS y verifica que la base de datos `AppcheckAPI` esté en línea.

### 2. Verificar que la API esté en línea
Abre en el navegador: `https://api.appcheck.com.mx/api/health`
Debería devolver: `{"status":"AppCheck API funcionando",...}`

### 3. Probar el Admin en producción
Abre: `https://admin.appcheck.com.mx`
- Email: `admin@appcheck.com.mx`
- Password: `Admin123!`

### 4. Para Desarrollo Local (si es necesario)
```powershell
# API Local
Set-Location "C:\Aplica\Appcheck_API\AppCheck.API"
dotnet run --project "C:\Aplica\Appcheck_API\AppCheck.API\AppCheck.API.csproj" --urls "http://0.0.0.0:5123;https://localhost:7123"

# Admin Local
Set-Location "C:\Aplica\Appcheck_Admin"
flutter run -d chrome --dart-define=API_URL=https://api.appcheck.com.mx

# Kiosko Local (en emulador)
Set-Location "C:\Aplica\Appcheck_Kiosko"
flutter run -d emulator-5554 --dart-define=API_URL=https://api.appcheck.com.mx

# 🚀 AppCheck - Seguimiento del Proyecto

> **Última actualización:** 05/11/2026
> **Estado:** SaaS en Producción - 5 Modalidades de Checada + Recuperación Automática de Token.

---

## 📌 ¿Qué es AppCheck?

SaaS mexicano que convierte cualquier dispositivo Android (tablet empotrable o celular de último usuario) en un reloj checador con **validez legal ante la STPS**, respaldo en la nube y modo offline. Resuelve la obligación del artículo 132, fracción XXXIV de la LFT (vigente a partir del 1 de enero de 2027) sin que las PYMES tengan que comprar hardware costoso.

**Tagline:** *"Checa a tu equipo. Cumple con la STPS."*

---

## 🌍 Estado de Producción (Dominios y URLs)

- **Landing Page:** `https://appcheck.com.mx` (en vivo con SEO, branding y AIMe).
- **API (Backend):** `https://api.appcheck.com.mx` (en vivo, .NET 10, SQL Server en VPS).
- **Admin (Flutter Web):** `https://admin.appcheck.com.mx` (en vivo, responsivo, con filtros y PIN).
- **Kiosko (Flutter Android):** APK de producción en celular físico, con 5 modalidades y recuperación automática.

---

## 🏛️ Arquitectura de Negocio (Multi-Nivel)

| Nivel | Rol | Qué hace |
|-------|-----|----------|
| 1 | **SuperAdmin (AppCheck)** | Control total, facturación, gestión de vendedores. |
| 2 | **Vendedor / Distribuidor** | Gestiona su cartera, recibe comisiones (fijo + variable). |
| 3a | **Cliente Corporativo** | Centraliza reportes de múltiples sucursales. |
| 3b | **Cliente Independiente** | PYME con uno o pocos kioskos. |
| 4 | **Admin (Administrativo)** | Gestiona empleados y kioskos de su sucursal. |
| 5 | **Contador** | Solo lectura de reportes y descarga de paquetes STPS. |

---

## 🛡️ Modelo de Licenciamiento

- **Puntos de Lectura:** Kiosko Fijo (tablet empotrada) y Punto Personal (celular del empleado - BYOD).
- **Licenciamiento:** Por empleados (límite de plan) y por kiosko (licencia mensual).
- **Modo Policía:** Offline-first. Si no hay pago, el kiosko entra en "Gracia" (7 días) y luego "Restringido".
- **Códigos de Activación:** 8 caracteres, hasheados, expiración 72h, vinculados al `deviceId`.
- **Recuperación Automática:** Si el kiosko pierde el token, lo recupera con su `DeviceId` (androidId).

---

## 🏗️ Arquitectura Técnica

- **Backend:** .NET 10 (C#), Entity Framework Core, SQL Server.
- **Validación Facial:** SkiaSharp (comparación pixel a pixel, umbral del 30% para MVP).
- **Frontend Kiosko:** Flutter / Dart (Android only).
- **Frontend Admin:** Flutter Web (Responsive: Sidebar en PC, Drawer en móvil).
- **Base de Datos:** SQL Server (local en `C:\Aplica` y VPS de producción).
- **Repositorios:**
  - API: https://github.com/rfvaldezv/Appcheck-API.git
  - Admin: https://github.com/rfvaldezv/Appcheck-Admin.git
  - Kiosko: https://github.com/rfvaldezv/Appcheck-Kiosko.git

---

## ✅ Lo que YA está hecho (Hitos Alcanzados)

### 1. Backend (API)
- [x] Autenticación JWT.
- [x] Gestión de Empleados (Alta con acuse legal, foto facial en Base64, Edición, Eliminación).
- [x] Registro de Checadas (Hash SHA-256, detección automática Entrada/Salida).
- [x] **5 Modalidades de Checada:**
  - **QR:** Escanea y checa.
  - **QR + Rostro:** Escanea, valida rostro, checa.
  - **PIN:** Teclea número y PIN, checa.
  - **PIN + Rostro:** Teclea número y PIN, valida rostro, checa.
  - **Solo Rostro:** (Pendiente IA real).
- [x] **Recuperación Automática de Token:** Endpoint `POST /api/kiosko/recuperar` que devuelve el token si el `DeviceId` ya existe.
- [x] **Limpieza de Kioskos Inactivos:** Endpoint `POST /api/kiosko/limpiar-inactivos` que elimina kioskos sin heartbeat en 30 días.
- [x] Gestión de Kioskos (Generar código, Activar, Regenerar, Revocar, Eliminar).
- [x] Facturación (Planes, Suscripciones, Generar Factura, Marcar Pagada).
- [x] Comisiones (Cálculo de MRR y comisión para Vendedor).

### 2. Admin (Flutter Web)
- [x] Login funcional con "ojito" para ver contraseña.
- [x] **Diseño 100% Responsivo:** Sidebar en PC y Menú Hamburguesa (Drawer) en móvil.
- [x] **Diálogos Scrollables:** El alta y edición de empleado no se enciman en celular.
- [x] **5 Métodos de Lectura:** QR, PIN, QR+Rostro, PIN+Rostro, Rostro.
- [x] **Registro Biométrico Dual:** Tomar Foto o Subir Archivo.
- [x] **Filtros de Empleados:** Por nombre/número, estado, empresa y vendedor.
- [x] **Buscador de Kioskos:** Por nombre.
- [x] **Botón de Limpieza:** Limpiar kioskos inactivos.
- [x] **Refresco Automático:** Al cerrar diálogos de activación/edición.
- [x] Reportes STPS (Filtro por fechas, descarga de CSV).
- [x] Vista de Mi Suscripción, Facturas y Comisiones.

### 3. Kiosko (Flutter Android)
- [x] Activación con código de 8 caracteres (formateo automático y botón de pegar).
- [x] **Recuperación Automática de Token:** Al abrir la app, recupera el token con el `DeviceId` sin pedir código.
- [x] **DeviceId Real:** Usa el `androidId` (inmutable) en lugar de un timestamp.
- [x] Cámara QR funcional (`mobile_scanner`).
- [x] **5 Modalidades:** QR, PIN, QR+Rostro, PIN+Rostro, Rostro.
- [x] Manejo de Errores y reinicio de cámara al regresar.
- [x] Generación de APK de Producción.

### 4. Landing Page (HTML/CSS)
- [x] En vivo en `appcheck.com.mx`.
- [x] SEO optimizado (Meta Tags, Schema.org, Open Graph, Keywords).
- [x] Branding completo (Logo, Favicon, Paleta de colores, Tipografía Inter).
- [x] Secciones: Hero, Características, Precios, FAQ, Footer con "AIMe".

---

## 🚧 Lo que FALTA (Backlog Priorizado)

### Fase 1: Blindaje del Producto (Corto Plazo)
- [ ] **Reconocimiento Facial con IA Real:** Implementar `FaceNet` o `ONNX Runtime` para que el "Solo Rostro" sea preciso.
- [ ] **Modo Offline en Kiosko:** Implementar Drift (SQLite) para que funcione sin internet y sincronice después.
- [ ] **Acuse en PDF:** Generar el PDF del acuse de acuerdo legal con QuestPDF.
- [ ] **Mejorar el algoritmo facial:** Migrar de comparación pixel a pixel a un modelo de IA (FaceNet) para mayor precisión.

### Fase 2: Crecimiento (Mediano Plazo)
- [ ] **AppCheck_Employee (BYOD):** App móvil para checar desde el celular personal con GPS.
- [ ] **QR Dinámico (TOTP):** Implementar el QR antifraude que cambia cada 30 segundos.
- [ ] **Facturación Real (CFDI):** Integrar un PAC para generar facturas fiscales reales.

### Fase 3: Hardening (Largo Plazo)
- [ ] **Contraseñas Hasheadas:** Migrar de texto plano a BCrypt.
- [ ] **Variables de Entorno:** Mover secretos a variables del sistema en IIS.
- [ ] **Pruebas de Carga:** Validar que la API soporte 50+ checadas simultáneas.

---

## 🔄 Cómo retomar el proyecto en 2 minutos

### 1. Verificar el VPS y la Base de Datos
- Conéctate por RDP al VPS.
- Abre SSMS y verifica que la base de datos `AppcheckAPI` esté en línea.

### 2. Verificar que la API esté en línea
Abre en el navegador: `https://api.appcheck.com.mx/api/health`
Debería devolver: `{"status":"AppCheck API funcionando",...}`

### 3. Probar el Admin en producción
Abre: `https://admin.appcheck.com.mx`
- Email: `admin@appcheck.com.mx`
- Password: `Admin123!`

### 4. Para Desarrollo Local (si es necesario)
```powershell
# API Local
Set-Location "C:\Aplica\Appcheck_API\AppCheck.API"
dotnet run --project "C:\Aplica\Appcheck_API\AppCheck.API\AppCheck.API.csproj" --urls "http://0.0.0.0:5123;https://localhost:7123"

# Admin Local
Set-Location "C:\Aplica\Appcheck_Admin"
flutter run -d chrome --dart-define=API_URL=https://api.appcheck.com.mx

# Kiosko Local (en emulador)
Set-Location "C:\Aplica\Appcheck_Kiosko"
flutter run -d emulator-5554 --dart-define=API_URL=https://api.appcheck.com.mx
