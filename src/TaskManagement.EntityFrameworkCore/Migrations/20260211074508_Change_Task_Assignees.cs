using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskManagement.Migrations
{
    /// <inheritdoc />
    public partial class Change_Task_Assignees : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_AppTaskItems",
                table: "AppTaskItems");

            migrationBuilder.DropColumn(
                name: "AssignedUserId",
                table: "AppTaskItems");

            migrationBuilder.RenameTable(
                name: "AppTaskItems",
                newName: "AppTasks");

            migrationBuilder.RenameIndex(
                name: "IX_AppTaskItems_Status",
                table: "AppTasks",
                newName: "IX_AppTasks_Status");

            migrationBuilder.AddColumn<DateTime>(
                name: "DueDate",
                table: "AppTasks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_AppTasks",
                table: "AppTasks",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "TaskAssignees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskAssignees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskAssignees_AppTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "AppTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaskAssignees_TaskId",
                table: "TaskAssignees",
                column: "TaskId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaskAssignees");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AppTasks",
                table: "AppTasks");

            migrationBuilder.DropColumn(
                name: "DueDate",
                table: "AppTasks");

            migrationBuilder.RenameTable(
                name: "AppTasks",
                newName: "AppTaskItems");

            migrationBuilder.RenameIndex(
                name: "IX_AppTasks_Status",
                table: "AppTaskItems",
                newName: "IX_AppTaskItems_Status");

            migrationBuilder.AddColumn<Guid>(
                name: "AssignedUserId",
                table: "AppTaskItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_AppTaskItems",
                table: "AppTaskItems",
                column: "Id");
        }
    }
}
