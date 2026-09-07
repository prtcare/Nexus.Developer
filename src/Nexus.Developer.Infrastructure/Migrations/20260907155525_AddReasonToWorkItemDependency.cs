using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexus.Developer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReasonToWorkItemDependency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Reason",
                schema: "dev",
                table: "WorkItemDependency",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Reason",
                schema: "dev",
                table: "WorkItemDependency");
        }
    }
}
