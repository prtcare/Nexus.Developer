using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexus.Developer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFeatureParentFeatureId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ParentFeatureId",
                schema: "dev",
                table: "Feature",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Feature_ParentFeatureId",
                schema: "dev",
                table: "Feature",
                column: "ParentFeatureId");

            migrationBuilder.AddForeignKey(
                name: "FK_Feature_ParentFeature",
                schema: "dev",
                table: "Feature",
                column: "ParentFeatureId",
                principalSchema: "dev",
                principalTable: "Feature",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Feature_ParentFeature",
                schema: "dev",
                table: "Feature");

            migrationBuilder.DropIndex(
                name: "IX_Feature_ParentFeatureId",
                schema: "dev",
                table: "Feature");

            migrationBuilder.DropColumn(
                name: "ParentFeatureId",
                schema: "dev",
                table: "Feature");
        }
    }
}
