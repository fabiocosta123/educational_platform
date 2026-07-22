using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EducationalPlataform.Migrations
{
    /// <inheritdoc />
    public partial class AddUserCourseEnrollments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserId1",
                table: "CourseEnrollments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseEnrollments_UserId1",
                table: "CourseEnrollments",
                column: "UserId1");

            migrationBuilder.AddForeignKey(
                name: "FK_CourseEnrollments_Users_UserId1",
                table: "CourseEnrollments",
                column: "UserId1",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CourseEnrollments_Users_UserId1",
                table: "CourseEnrollments");

            migrationBuilder.DropIndex(
                name: "IX_CourseEnrollments_UserId1",
                table: "CourseEnrollments");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "CourseEnrollments");
        }
    }
}
