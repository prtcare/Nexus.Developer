using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexus.Developer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDevelopmentRunExecutionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CompletedAt",
                schema: "dev",
                table: "DevelopmentRun",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResultSummary",
                schema: "dev",
                table: "DevelopmentRun",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StartedAt",
                schema: "dev",
                table: "DevelopmentRun",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkerId",
                schema: "dev",
                table: "DevelopmentRun",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkerType",
                schema: "dev",
                table: "DevelopmentRun",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletedAt",
                schema: "dev",
                table: "DevelopmentRun");

            migrationBuilder.DropColumn(
                name: "ResultSummary",
                schema: "dev",
                table: "DevelopmentRun");

            migrationBuilder.DropColumn(
                name: "StartedAt",
                schema: "dev",
                table: "DevelopmentRun");

            migrationBuilder.DropColumn(
                name: "WorkerId",
                schema: "dev",
                table: "DevelopmentRun");

            migrationBuilder.DropColumn(
                name: "WorkerType",
                schema: "dev",
                table: "DevelopmentRun");
        }
    }
}
