using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoftfyWeb.Migrations
{
    /// <inheritdoc />
    public partial class AgregarDuracionACancion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeSpan>(
                name: "Duracion",
                table: "Canciones",
                type: "interval",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Duracion",
                table: "Canciones");
        }
    }
}
