# 📘 Manual del Vendedor — AppCheck

**Versión:** 2.0
**Última actualización:** 11/Nov/2026
**Audiencia:** Vendedores, Distribuidores, Partners

---

## 📌 Índice

1. [Qué es AppCheck (Pitch de 30 seg)](#1-qué-es-appcheck-pitch-de-30-seg)
2. [Cómo funciona (Arquitectura técnica)](#2-cómo-funciona-arquitectura-técnica)
3. [Los 6 métodos de checada](#3-los-6-métodos-de-checada)
4. [Cómo configurar un cliente nuevo (Onboarding)](#4-cómo-configurar-un-cliente-nuevo-onboarding)
5. [Cómo dar soporte](#5-cómo-dar-soporte)
6. [Cómo probar el producto (Demos)](#6-cómo-probar-el-producto-demos)
7. [Precios y comisiones](#7-precios-y-comisiones)
8. [Contactos de escalación](#8-contactos-de-escalación)
9. [FAQ para clientes](#9-faq-para-clientes)

---

## 1. Qué es AppCheck (Pitch de 30 seg)

### Pitch principal

> **"AppCheck es el reloj checador que corre en cualquier celular. Sin hardware, sin App Store, sin fricción. Con IA facial, GPS y 6 métodos de checada. Cumple con la STPS y se configura en 5 minutos."**

### Versión corta (para WhatsApp)

> **"¿Todavía usas reloj checador físico? AppCheck convierte cualquier celular en un checador con IA facial, GPS y validez legal STPS. Sin hardware, sin instalación. ¿Te muestro una demo?"**

### Versión larga (para presentación)

> **"AppCheck es un SaaS de control de asistencia que reemplaza los relojes checadores físicos. El empleado usa su propio celular (o un celular de la empresa) para checar con IA facial + GPS. El RH gestiona todo desde un panel web. No requiere hardware, no requiere App Store, y cumple con el Artículo 132 de la LFT (STPS)."**

---

## 2. Cómo funciona (Arquitectura técnica)

### Diagrama simple

### Diagrama de Flujo

1.  **Empleado:** Abre la App (APK o PWA) en su celular.
2.  **Checada:** Selecciona su método (QR, Rostro, PIN) y captura su GPS.
3.  **API (Nube):** Recibe los datos, valida el rostro, la distancia y guarda el registro.
4.  **Admin Web (RH):** Consulta los reportes, crea turnos y audita las checadas.

**Flujo visual:**

```text
[ Empleado ] 
    │ (Checa desde su celular)
    ▼
[ AppCheck APK / PWA ] 
    │ (Envía datos a la nube)
    ▼
[ API en la Nube ] ◄───► [ Base de Datos ]
    │ (Procesa y guarda)
    ▼
[ Panel Admin Web (RH) ] 
    (Reportes, KPIs, Excel, PDF)
