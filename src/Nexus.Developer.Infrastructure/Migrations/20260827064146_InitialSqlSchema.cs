using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexus.Developer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialSqlSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dev");

            migrationBuilder.CreateTable(
                name: "DevelopmentRun",
                schema: "dev",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetType = table.Column<int>(type: "int", nullable: false),
                    TargetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Ref = table.Column<string>(type: "nvarchar(450)", nullable: false, computedColumnSql: "('RUN-' + RIGHT('000000' + CAST([Seq] AS varchar(6)), 6))", stored: true),
                    PlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PromptId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResultId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CheckSetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VerificationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Seq = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DevelopmentRun", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Feature",
                schema: "dev",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubprojectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Ref = table.Column<string>(type: "nvarchar(450)", nullable: false, computedColumnSql: "('FEA-' + RIGHT('00000000' + CAST([Seq] AS varchar(8)), 8))", stored: true),
                    Seq = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Feature", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Issue",
                schema: "dev",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Ref = table.Column<string>(type: "nvarchar(450)", nullable: false, computedColumnSql: "('ISS-' + RIGHT('00000000' + CAST([Seq] AS varchar(8)), 8))", stored: true),
                    Seq = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Issue", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Milestone",
                schema: "dev",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubprojectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TargetDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Ref = table.Column<string>(type: "nvarchar(450)", nullable: false, computedColumnSql: "('MIL-' + RIGHT('00000000' + CAST([Seq] AS varchar(8)), 8))", stored: true),
                    Seq = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Milestone", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ObjectChatLink",
                schema: "dev",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MessageRangeStart = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MessageRangeEnd = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TargetType = table.Column<int>(type: "int", nullable: false),
                    TargetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LinkedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LinkedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObjectChatLink", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Task",
                schema: "dev",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FeatureId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Ref = table.Column<string>(type: "nvarchar(450)", nullable: false, computedColumnSql: "('TSK-' + RIGHT('00000000' + CAST([Seq] AS varchar(8)), 8))", stored: true),
                    MigratedFromWorkItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Seq = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Task", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Task_Feature",
                        column: x => x.FeatureId,
                        principalSchema: "dev",
                        principalTable: "Feature",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IssueLink",
                schema: "dev",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IssueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetType = table.Column<int>(type: "int", nullable: false),
                    TargetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LinkedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LinkedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IssueLink", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IssueLink_Issue",
                        column: x => x.IssueId,
                        principalSchema: "dev",
                        principalTable: "Issue",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MilestoneLink",
                schema: "dev",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MilestoneId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetType = table.Column<int>(type: "int", nullable: false),
                    TargetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LinkedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LinkedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MilestoneLink", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MilestoneLink_Milestone",
                        column: x => x.MilestoneId,
                        principalSchema: "dev",
                        principalTable: "Milestone",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Subtask",
                schema: "dev",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Ref = table.Column<string>(type: "nvarchar(450)", nullable: false, computedColumnSql: "('SUB-' + RIGHT('00000000' + CAST([Seq] AS varchar(8)), 8))", stored: true),
                    Seq = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subtask", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Subtask_Task",
                        column: x => x.TaskId,
                        principalSchema: "dev",
                        principalTable: "Task",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DevelopmentRun_Target",
                schema: "dev",
                table: "DevelopmentRun",
                columns: new[] { "TargetType", "TargetId" });

            migrationBuilder.CreateIndex(
                name: "UQ_DevelopmentRun_Ref",
                schema: "dev",
                table: "DevelopmentRun",
                column: "Ref",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Feature_SubprojectId",
                schema: "dev",
                table: "Feature",
                column: "SubprojectId");

            migrationBuilder.CreateIndex(
                name: "UQ_Feature_Ref",
                schema: "dev",
                table: "Feature",
                column: "Ref",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Issue_Ref",
                schema: "dev",
                table: "Issue",
                column: "Ref",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IssueLink_IssueId",
                schema: "dev",
                table: "IssueLink",
                column: "IssueId");

            migrationBuilder.CreateIndex(
                name: "IX_IssueLink_Target",
                schema: "dev",
                table: "IssueLink",
                columns: new[] { "TargetType", "TargetId" });

            migrationBuilder.CreateIndex(
                name: "IX_Milestone_SubprojectId",
                schema: "dev",
                table: "Milestone",
                column: "SubprojectId");

            migrationBuilder.CreateIndex(
                name: "UQ_Milestone_Ref",
                schema: "dev",
                table: "Milestone",
                column: "Ref",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MilestoneLink_MilestoneId",
                schema: "dev",
                table: "MilestoneLink",
                column: "MilestoneId");

            migrationBuilder.CreateIndex(
                name: "IX_MilestoneLink_Target",
                schema: "dev",
                table: "MilestoneLink",
                columns: new[] { "TargetType", "TargetId" });

            migrationBuilder.CreateIndex(
                name: "IX_ObjectChatLink_ConversationId",
                schema: "dev",
                table: "ObjectChatLink",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_ObjectChatLink_Target",
                schema: "dev",
                table: "ObjectChatLink",
                columns: new[] { "TargetType", "TargetId" });

            migrationBuilder.CreateIndex(
                name: "IX_Subtask_TaskId",
                schema: "dev",
                table: "Subtask",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "UQ_Subtask_Ref",
                schema: "dev",
                table: "Subtask",
                column: "Ref",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Task_FeatureId",
                schema: "dev",
                table: "Task",
                column: "FeatureId");

            migrationBuilder.CreateIndex(
                name: "IX_Task_MigratedFromWorkItemId",
                schema: "dev",
                table: "Task",
                column: "MigratedFromWorkItemId");

            migrationBuilder.CreateIndex(
                name: "UQ_Task_Ref",
                schema: "dev",
                table: "Task",
                column: "Ref",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DevelopmentRun",
                schema: "dev");

            migrationBuilder.DropTable(
                name: "IssueLink",
                schema: "dev");

            migrationBuilder.DropTable(
                name: "MilestoneLink",
                schema: "dev");

            migrationBuilder.DropTable(
                name: "ObjectChatLink",
                schema: "dev");

            migrationBuilder.DropTable(
                name: "Subtask",
                schema: "dev");

            migrationBuilder.DropTable(
                name: "Issue",
                schema: "dev");

            migrationBuilder.DropTable(
                name: "Milestone",
                schema: "dev");

            migrationBuilder.DropTable(
                name: "Task",
                schema: "dev");

            migrationBuilder.DropTable(
                name: "Feature",
                schema: "dev");
        }
    }
}
