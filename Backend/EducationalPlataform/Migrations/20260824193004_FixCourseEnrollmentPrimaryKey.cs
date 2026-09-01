using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EducationalPlataform.Migrations
{
    /// <inheritdoc />
    public partial class FixCourseEnrollmentPrimaryKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remove a chave primária composta atual
            migrationBuilder.DropPrimaryKey(
                name: "PK_CourseEnrollments",
                table: "CourseEnrollments");

            // Remove a coluna Id antiga.
            // Ela atualmente não é Identity e todos os registros existentes
            // possuem valor 0.
            migrationBuilder.DropColumn(
                name: "Id",
                table: "CourseEnrollments");

            // Cria uma nova coluna Id como Identity.
            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "CourseEnrollments",
                type: "int",
                nullable: false)
                .Annotation("SqlServer:Identity", "1, 1");

            // Define Id como nova chave primária
            migrationBuilder.AddPrimaryKey(
                name: "PK_CourseEnrollments",
                table: "CourseEnrollments",
                column: "Id");

            // Garante que um usuário não possa possuir
            // duas matrículas no mesmo curso.
            migrationBuilder.CreateIndex(
                name: "IX_CourseEnrollments_UserId_CourseId",
                table: "CourseEnrollments",
                columns: new[] { "UserId", "CourseId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove a nova chave primária
            migrationBuilder.DropPrimaryKey(
                name: "PK_CourseEnrollments",
                table: "CourseEnrollments");

            // Remove o índice único
            migrationBuilder.DropIndex(
                name: "IX_CourseEnrollments_UserId_CourseId",
                table: "CourseEnrollments");

            // Remove o Id Identity
            migrationBuilder.DropColumn(
                name: "Id",
                table: "CourseEnrollments");

            // Recria a coluna Id antiga
            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "CourseEnrollments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Restaura a chave composta original
            migrationBuilder.AddPrimaryKey(
                name: "PK_CourseEnrollments",
                table: "CourseEnrollments",
                columns: new[] { "UserId", "CourseId" });
        }
    }
}