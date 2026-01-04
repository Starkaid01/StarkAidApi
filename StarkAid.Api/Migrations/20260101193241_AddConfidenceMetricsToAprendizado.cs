using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StarkAid.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddConfidenceMetricsToAprendizado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ConfidenceScore",
                table: "Aprendizados",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "HitCount",
                table: "Aprendizados",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastUsedAt",
                table: "Aprendizados",
                type: "datetimeoffset",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConfidenceScore",
                table: "Aprendizados");

            migrationBuilder.DropColumn(
                name: "HitCount",
                table: "Aprendizados");

            migrationBuilder.DropColumn(
                name: "LastUsedAt",
                table: "Aprendizados");
        }
    }
}
