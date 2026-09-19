using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EducationalPlataform.Migrations
{
    /// <inheritdoc />
    public partial class FixAnnouncementShadowForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE Announcements
                SET CourseId = CourseId1
                WHERE CourseId IS NULL AND CourseId1 IS NOT NULL;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_Announcements_Courses_CourseId1",
                table: "Announcements");

            migrationBuilder.DropForeignKey(
                name: "FK_Announcements_Users_UserId",
                table: "Announcements");

            migrationBuilder.DropIndex(
                name: "IX_Announcements_CourseId1",
                table: "Announcements");

            migrationBuilder.DropIndex(
                name: "IX_Announcements_UserId",
                table: "Announcements");

            migrationBuilder.DropColumn(
                name: "CourseId1",
                table: "Announcements");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Announcements");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CourseId1",
                table: "Announcements",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Announcements",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Announcements_CourseId1",
                table: "Announcements",
                column: "CourseId1");

            migrationBuilder.CreateIndex(
                name: "IX_Announcements_UserId",
                table: "Announcements",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Announcements_Courses_CourseId1",
                table: "Announcements",
                column: "CourseId1",
                principalTable: "Courses",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Announcements_Users_UserId",
                table: "Announcements",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
