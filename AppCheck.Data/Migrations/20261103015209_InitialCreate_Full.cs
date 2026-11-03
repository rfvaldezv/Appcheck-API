using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppCheck.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate_Full : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BitacoraAuditoria",
                columns: table => new
                {
                    LogId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CuentaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Accion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Detalle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Fecha = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BitacoraAuditoria", x => x.LogId);
                });

            migrationBuilder.CreateTable(
                name: "Cuentas",
                columns: table => new
                {
                    CuentaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CuentaPadreId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VendedorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TipoCuenta = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NombreComercial = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RFC = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModoConfianza = table.Column<bool>(type: "bit", nullable: false),
                    SaldoActual = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cuentas", x => x.CuentaId);
                    table.ForeignKey(
                        name: "FK_Cuentas_Cuentas_CuentaPadreId",
                        column: x => x.CuentaPadreId,
                        principalTable: "Cuentas",
                        principalColumn: "CuentaId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Planes",
                columns: table => new
                {
                    PlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PrecioBase = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LimiteEmpleados = table.Column<int>(type: "int", nullable: false),
                    LimiteKioskos = table.Column<int>(type: "int", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Planes", x => x.PlanId);
                });

            migrationBuilder.CreateTable(
                name: "Empleados",
                columns: table => new
                {
                    EmpleadoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CuentaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NumeroEmpleado = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NombreCompleto = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PinRespaldo = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    MetodoLecturaPreferido = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FaceTemplateHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Empleados", x => x.EmpleadoId);
                    table.ForeignKey(
                        name: "FK_Empleados_Cuentas_CuentaId",
                        column: x => x.CuentaId,
                        principalTable: "Cuentas",
                        principalColumn: "CuentaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PuntosLectura",
                columns: table => new
                {
                    PuntoLecturaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CuentaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DeviceId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TokenHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EstadoLicencia = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FechaExpiracionLicencia = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UltimoHeartbeat = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PuntosLectura", x => x.PuntoLecturaId);
                    table.ForeignKey(
                        name: "FK_PuntosLectura_Cuentas_CuentaId",
                        column: x => x.CuentaId,
                        principalTable: "Cuentas",
                        principalColumn: "CuentaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CuentaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Rol = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.UsuarioId);
                    table.ForeignKey(
                        name: "FK_Usuarios_Cuentas_CuentaId",
                        column: x => x.CuentaId,
                        principalTable: "Cuentas",
                        principalColumn: "CuentaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Suscripciones",
                columns: table => new
                {
                    SuscripcionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CuentaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FechaInicio = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FechaFin = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suscripciones", x => x.SuscripcionId);
                    table.ForeignKey(
                        name: "FK_Suscripciones_Cuentas_CuentaId",
                        column: x => x.CuentaId,
                        principalTable: "Cuentas",
                        principalColumn: "CuentaId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Suscripciones_Planes_PlanId",
                        column: x => x.PlanId,
                        principalTable: "Planes",
                        principalColumn: "PlanId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AcuerdosFirmados",
                columns: table => new
                {
                    AcuerdoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmpleadoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentoUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FechaFirma = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcuerdosFirmados", x => x.AcuerdoId);
                    table.ForeignKey(
                        name: "FK_AcuerdosFirmados_Empleados_EmpleadoId",
                        column: x => x.EmpleadoId,
                        principalTable: "Empleados",
                        principalColumn: "EmpleadoId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CodigosActivacion",
                columns: table => new
                {
                    CodigoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CuentaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PuntoLecturaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CodigoHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ExpiraEn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodigosActivacion", x => x.CodigoId);
                    table.ForeignKey(
                        name: "FK_CodigosActivacion_Cuentas_CuentaId",
                        column: x => x.CuentaId,
                        principalTable: "Cuentas",
                        principalColumn: "CuentaId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CodigosActivacion_PuntosLectura_PuntoLecturaId",
                        column: x => x.PuntoLecturaId,
                        principalTable: "PuntosLectura",
                        principalColumn: "PuntoLecturaId");
                });

            migrationBuilder.CreateTable(
                name: "RegistrosJornada",
                columns: table => new
                {
                    RegistroId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmpleadoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PuntoLecturaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TipoChecada = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    TimestampServidor = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TimestampLocal = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Latitud = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    Longitud = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    MetodoUsado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    HashRegistro = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EsOffline = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrosJornada", x => x.RegistroId);
                    table.ForeignKey(
                        name: "FK_RegistrosJornada_Empleados_EmpleadoId",
                        column: x => x.EmpleadoId,
                        principalTable: "Empleados",
                        principalColumn: "EmpleadoId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RegistrosJornada_PuntosLectura_PuntoLecturaId",
                        column: x => x.PuntoLecturaId,
                        principalTable: "PuntosLectura",
                        principalColumn: "PuntoLecturaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConfiguracionesComision",
                columns: table => new
                {
                    ConfiguracionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendedorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SueldoBaseMensual = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    PorcentajeComisionRecurrente = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    BonoPorNuevoCliente = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    BonoPorMeta = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    MetaClientesMes = table.Column<int>(type: "int", nullable: true),
                    VigenteDesde = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    VigenteHasta = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracionesComision", x => x.ConfiguracionId);
                    table.ForeignKey(
                        name: "FK_ConfiguracionesComision_Usuarios_VendedorId",
                        column: x => x.VendedorId,
                        principalTable: "Usuarios",
                        principalColumn: "UsuarioId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Liquidaciones",
                columns: table => new
                {
                    LiquidacionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendedorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Periodo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MRRCobrado = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ComisionRecurrente = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Bonos = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SueldoBase = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAPagar = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FechaCalculo = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FechaPago = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Liquidaciones", x => x.LiquidacionId);
                    table.ForeignKey(
                        name: "FK_Liquidaciones_Usuarios_VendedorId",
                        column: x => x.VendedorId,
                        principalTable: "Usuarios",
                        principalColumn: "UsuarioId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Facturas",
                columns: table => new
                {
                    FacturaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CuentaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SuscripcionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PeriodoInicio = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PeriodoFin = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IVA = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UUID_CFDI = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FechaEmision = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FechaPago = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    URL_PDF = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    URL_XML = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Facturas", x => x.FacturaId);
                    table.ForeignKey(
                        name: "FK_Facturas_Cuentas_CuentaId",
                        column: x => x.CuentaId,
                        principalTable: "Cuentas",
                        principalColumn: "CuentaId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Facturas_Suscripciones_SuscripcionId",
                        column: x => x.SuscripcionId,
                        principalTable: "Suscripciones",
                        principalColumn: "SuscripcionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AcuerdosFirmados_EmpleadoId",
                table: "AcuerdosFirmados",
                column: "EmpleadoId");

            migrationBuilder.CreateIndex(
                name: "IX_CodigosActivacion_CuentaId",
                table: "CodigosActivacion",
                column: "CuentaId");

            migrationBuilder.CreateIndex(
                name: "IX_CodigosActivacion_PuntoLecturaId",
                table: "CodigosActivacion",
                column: "PuntoLecturaId");

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracionesComision_VendedorId",
                table: "ConfiguracionesComision",
                column: "VendedorId");

            migrationBuilder.CreateIndex(
                name: "IX_Cuentas_CuentaPadreId",
                table: "Cuentas",
                column: "CuentaPadreId");

            migrationBuilder.CreateIndex(
                name: "IX_Cuentas_RFC",
                table: "Cuentas",
                column: "RFC",
                unique: true,
                filter: "[RFC] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Empleados_CuentaId",
                table: "Empleados",
                column: "CuentaId");

            migrationBuilder.CreateIndex(
                name: "IX_Facturas_CuentaId",
                table: "Facturas",
                column: "CuentaId");

            migrationBuilder.CreateIndex(
                name: "IX_Facturas_SuscripcionId",
                table: "Facturas",
                column: "SuscripcionId");

            migrationBuilder.CreateIndex(
                name: "IX_Liquidaciones_VendedorId",
                table: "Liquidaciones",
                column: "VendedorId");

            migrationBuilder.CreateIndex(
                name: "IX_PuntosLectura_CuentaId",
                table: "PuntosLectura",
                column: "CuentaId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosJornada_EmpleadoId",
                table: "RegistrosJornada",
                column: "EmpleadoId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosJornada_PuntoLecturaId",
                table: "RegistrosJornada",
                column: "PuntoLecturaId");

            migrationBuilder.CreateIndex(
                name: "IX_Suscripciones_CuentaId",
                table: "Suscripciones",
                column: "CuentaId");

            migrationBuilder.CreateIndex(
                name: "IX_Suscripciones_PlanId",
                table: "Suscripciones",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_CuentaId",
                table: "Usuarios",
                column: "CuentaId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Email",
                table: "Usuarios",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AcuerdosFirmados");

            migrationBuilder.DropTable(
                name: "BitacoraAuditoria");

            migrationBuilder.DropTable(
                name: "CodigosActivacion");

            migrationBuilder.DropTable(
                name: "ConfiguracionesComision");

            migrationBuilder.DropTable(
                name: "Facturas");

            migrationBuilder.DropTable(
                name: "Liquidaciones");

            migrationBuilder.DropTable(
                name: "RegistrosJornada");

            migrationBuilder.DropTable(
                name: "Suscripciones");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropTable(
                name: "Empleados");

            migrationBuilder.DropTable(
                name: "PuntosLectura");

            migrationBuilder.DropTable(
                name: "Planes");

            migrationBuilder.DropTable(
                name: "Cuentas");
        }
    }
}
