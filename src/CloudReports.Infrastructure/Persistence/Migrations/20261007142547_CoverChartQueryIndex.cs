using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloudReports.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CoverChartQueryIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WeatherReadings_FetchedAtUtc_City",
                table: "WeatherReadings");

            migrationBuilder.CreateIndex(
                name: "IX_WeatherReadings_FetchedAtUtc_City",
                table: "WeatherReadings",
                columns: new[] { "FetchedAtUtc", "City" })
                .Annotation("SqlServer:Include", new[] { "Country", "ObservedAtUtc", "Temperature", "TemperatureMin", "TemperatureMax" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WeatherReadings_FetchedAtUtc_City",
                table: "WeatherReadings");

            migrationBuilder.CreateIndex(
                name: "IX_WeatherReadings_FetchedAtUtc_City",
                table: "WeatherReadings",
                columns: new[] { "FetchedAtUtc", "City" });
        }
    }
}
