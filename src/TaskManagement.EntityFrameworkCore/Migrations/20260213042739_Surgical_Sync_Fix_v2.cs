using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskManagement.Migrations
{
    /// <inheritdoc />
    public partial class Surgical_Sync_Fix_v2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppTasks_AppProjects_ProjectId",
                table: "AppTasks");

            migrationBuilder.AddForeignKey(
                name: "FK_AppTasks_AppProjects_ProjectId",
                table: "AppTasks",
                column: "ProjectId",
                principalTable: "AppProjects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppTasks_AppProjects_ProjectId",
                table: "AppTasks");

            migrationBuilder.AddForeignKey(
                name: "FK_AppTasks_AppProjects_ProjectId",
                table: "AppTasks",
                column: "ProjectId",
                principalTable: "AppProjects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
