using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexus.Developer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkItemDependency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkItemDependency",
                schema: "dev",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpstreamType = table.Column<int>(type: "int", nullable: false),
                    UpstreamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DownstreamType = table.Column<int>(type: "int", nullable: false),
                    DownstreamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    RequiredState = table.Column<int>(type: "int", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkItemDependency", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkItemDependency_Downstream",
                schema: "dev",
                table: "WorkItemDependency",
                columns: new[] { "DownstreamType", "DownstreamId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkItemDependency_Upstream",
                schema: "dev",
                table: "WorkItemDependency",
                columns: new[] { "UpstreamType", "UpstreamId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkItemDependency",
                schema: "dev");
        }
    }
}
