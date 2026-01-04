using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StarkAid.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddYouTubeMusicCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "YouTubeMusicCaches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NormalizedQuery = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    VideoId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Channel = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DurationSeconds = table.Column<int>(type: "int", nullable: false),
                    IsLive = table.Column<bool>(type: "bit", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastUsedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    HitCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_YouTubeMusicCaches", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_YouTubeMusicCaches_NormalizedQuery",
                table: "YouTubeMusicCaches",
                column: "NormalizedQuery",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "YouTubeMusicCaches");
        }
    }
}
