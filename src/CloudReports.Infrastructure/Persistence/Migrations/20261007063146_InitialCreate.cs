using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloudReports.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WeatherReadings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Country = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Temperature = table.Column<double>(type: "float", nullable: false),
                    TemperatureMin = table.Column<double>(type: "float", nullable: false),
                    TemperatureMax = table.Column<double>(type: "float", nullable: false),
                    ObservedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FetchedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RawPayload = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeatherReadings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FetchLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AttemptedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false),
                    HttpStatusCode = table.Column<int>(type: "int", nullable: true),
                    DurationMs = table.Column<long>(type: "bigint", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    WeatherReadingId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FetchLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FetchLogs_WeatherReadings_WeatherReadingId",
                        column: x => x.WeatherReadingId,
                        principalTable: "WeatherReadings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FetchLogs_AttemptedAtUtc",
                table: "FetchLogs",
                column: "AttemptedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_FetchLogs_City_AttemptedAtUtc",
                table: "FetchLogs",
                columns: new[] { "City", "AttemptedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_FetchLogs_WeatherReadingId",
                table: "FetchLogs",
                column: "WeatherReadingId");

            migrationBuilder.CreateIndex(
                name: "IX_WeatherReadings_FetchedAtUtc_City",
                table: "WeatherReadings",
                columns: new[] { "FetchedAtUtc", "City" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FetchLogs");

            migrationBuilder.DropTable(
                name: "WeatherReadings");
        }
    }
}
