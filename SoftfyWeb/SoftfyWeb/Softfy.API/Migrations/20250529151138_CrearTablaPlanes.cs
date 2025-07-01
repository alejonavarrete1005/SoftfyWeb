using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SoftfyWeb.Migrations
{
    /// <inheritdoc />
    public partial class CrearTablaPlanes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Suscripciones_AspNetUsers_UsuarioId",
                table: "Suscripciones");

            migrationBuilder.DropColumn(
                name: "Tipo",
                table: "Suscripciones");

            migrationBuilder.RenameColumn(
                name: "UsuarioId",
                table: "Suscripciones",
                newName: "UsuarioPrincipalId");

            migrationBuilder.RenameIndex(
                name: "IX_Suscripciones_UsuarioId",
                table: "Suscripciones",
                newName: "IX_Suscripciones_UsuarioPrincipalId");

            migrationBuilder.AddColumn<int>(
                name: "PlanId",
                table: "Suscripciones",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "MiembrosSuscripciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SuscripcionId = table.Column<int>(type: "integer", nullable: false),
                    UsuarioId = table.Column<string>(type: "text", nullable: false),
                    FechaAgregado = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MiembrosSuscripciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MiembrosSuscripciones_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MiembrosSuscripciones_Suscripciones_SuscripcionId",
                        column: x => x.SuscripcionId,
                        principalTable: "Suscripciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Planes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    Precio = table.Column<decimal>(type: "numeric", nullable: false),
                    MaxUsuarios = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Planes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Suscripciones_PlanId",
                table: "Suscripciones",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_MiembrosSuscripciones_SuscripcionId",
                table: "MiembrosSuscripciones",
                column: "SuscripcionId");

            migrationBuilder.CreateIndex(
                name: "IX_MiembrosSuscripciones_UsuarioId",
                table: "MiembrosSuscripciones",
                column: "UsuarioId");

            migrationBuilder.AddForeignKey(
                name: "FK_Suscripciones_AspNetUsers_UsuarioPrincipalId",
                table: "Suscripciones",
                column: "UsuarioPrincipalId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Suscripciones_Planes_PlanId",
                table: "Suscripciones",
                column: "PlanId",
                principalTable: "Planes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Suscripciones_AspNetUsers_UsuarioPrincipalId",
                table: "Suscripciones");

            migrationBuilder.DropForeignKey(
                name: "FK_Suscripciones_Planes_PlanId",
                table: "Suscripciones");

            migrationBuilder.DropTable(
                name: "MiembrosSuscripciones");

            migrationBuilder.DropTable(
                name: "Planes");

            migrationBuilder.DropIndex(
                name: "IX_Suscripciones_PlanId",
                table: "Suscripciones");

            migrationBuilder.DropColumn(
                name: "PlanId",
                table: "Suscripciones");

            migrationBuilder.RenameColumn(
                name: "UsuarioPrincipalId",
                table: "Suscripciones",
                newName: "UsuarioId");

            migrationBuilder.RenameIndex(
                name: "IX_Suscripciones_UsuarioPrincipalId",
                table: "Suscripciones",
                newName: "IX_Suscripciones_UsuarioId");

            migrationBuilder.AddColumn<string>(
                name: "Tipo",
                table: "Suscripciones",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_Suscripciones_AspNetUsers_UsuarioId",
                table: "Suscripciones",
                column: "UsuarioId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
