using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexus.Developer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSourceRoadmapNodeIdToFeatureAndTask : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SourceRoadmapNodeId",
                schema: "dev",
                table: "Task",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceRoadmapNodeId",
                schema: "dev",
                table: "Feature",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SourceRoadmapNodeId",
                schema: "dev",
                table: "Task");

            migrationBuilder.DropColumn(
                name: "SourceRoadmapNodeId",
                schema: "dev",
                table: "Feature");
        }
    }
}
