using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UrlShortener.WriteApi.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSequenceToStartAtOne : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RestartSequence(
                name: "UrlCounter",
                startValue: 1L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RestartSequence(
                name: "UrlCounter",
                startValue: 100000000000L);
        }
    }
}
