using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StarkAid.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSpotifyFieldsToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SpotifyAccessToken",
                table: "Users",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SpotifyRefreshToken",
                table: "Users",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SpotifyTokenExpiresAt",
                table: "Users",
                type: "datetimeoffset",
                nullable: true);

            
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SpotifyAccessToken",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SpotifyRefreshToken",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SpotifyTokenExpiresAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Comando",
                table: "Devices");
        }
    }
}
