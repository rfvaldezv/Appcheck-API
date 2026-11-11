using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppCheck.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGeofenceToPuntoLectura : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RegistrosJornada_PuntosLectura_PuntoLecturaId",
                table: "RegistrosJornada");

            migrationBuilder.AlterColumn<Guid>(
                name: "PuntoLecturaId",
                table: "RegistrosJornada",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<decimal>(
                name: "Latitud",
                table: "PuntosLectura",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Longitud",
                table: "PuntosLectura",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RadioMetros",
                table: "PuntosLectura",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "PuedeUsarHuella",
                table: "Empleados",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddForeignKey(
                name: "FK_RegistrosJornada_PuntosLectura_PuntoLecturaId",
                table: "RegistrosJornada",
                column: "PuntoLecturaId",
                principalTable: "PuntosLectura",
                principalColumn: "PuntoLecturaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RegistrosJornada_PuntosLectura_PuntoLecturaId",
                table: "RegistrosJornada");

            migrationBuilder.DropColumn(
                name: "Latitud",
                table: "PuntosLectura");

            migrationBuilder.DropColumn(
                name: "Longitud",
                table: "PuntosLectura");

            migrationBuilder.DropColumn(
                name: "RadioMetros",
                table: "PuntosLectura");

            migrationBuilder.DropColumn(
                name: "PuedeUsarHuella",
                table: "Empleados");

            migrationBuilder.AlterColumn<Guid>(
                name: "PuntoLecturaId",
                table: "RegistrosJornada",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_RegistrosJornada_PuntosLectura_PuntoLecturaId",
                table: "RegistrosJornada",
                column: "PuntoLecturaId",
                principalTable: "PuntosLectura",
                principalColumn: "PuntoLecturaId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
