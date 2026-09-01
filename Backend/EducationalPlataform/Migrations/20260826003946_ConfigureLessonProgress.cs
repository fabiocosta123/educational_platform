using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EducationalPlataform.Migrations
{
    /// <inheritdoc />
    public partial class ConfigureLessonProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LessonProgresses_Lessons_LessonId",
                table: "LessonProgresses");

            migrationBuilder.DropForeignKey(
                name: "FK_LessonProgresses_Users_UserId",
                table: "LessonProgresses");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LessonProgresses",
                table: "LessonProgresses");

            migrationBuilder.DropIndex(
                name: "IX_LessonProgresses_UserId",
                table: "LessonProgresses");

            migrationBuilder.RenameTable(
                name: "LessonProgresses",
                newName: "LessonProgress");

            migrationBuilder.RenameIndex(
                name: "IX_LessonProgresses_LessonId",
                table: "LessonProgress",
                newName: "IX_LessonProgress_LessonId");

            migrationBuilder.AlterColumn<int>(
                name: "TotalWatchedSeconds",
                table: "LessonProgress",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<DateTime>(
                name: "StartedAt",
                table: "LessonProgress",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<int>(
                name: "MaxWatchedSecond",
                table: "LessonProgress",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "LastWatchedSecond",
                table: "LessonProgress",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<bool>(
                name: "Completed",
                table: "LessonProgress",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LessonProgress",
                table: "LessonProgress",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_LessonProgress_UserId_LessonId",
                table: "LessonProgress",
                columns: new[] { "UserId", "LessonId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_LessonProgress_Lessons_LessonId",
                table: "LessonProgress",
                column: "LessonId",
                principalTable: "Lessons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LessonProgress_Users_UserId",
                table: "LessonProgress",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LessonProgress_Lessons_LessonId",
                table: "LessonProgress");

            migrationBuilder.DropForeignKey(
                name: "FK_LessonProgress_Users_UserId",
                table: "LessonProgress");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LessonProgress",
                table: "LessonProgress");

            migrationBuilder.DropIndex(
                name: "IX_LessonProgress_UserId_LessonId",
                table: "LessonProgress");

            migrationBuilder.RenameTable(
                name: "LessonProgress",
                newName: "LessonProgresses");

            migrationBuilder.RenameIndex(
                name: "IX_LessonProgress_LessonId",
                table: "LessonProgresses",
                newName: "IX_LessonProgresses_LessonId");

            migrationBuilder.AlterColumn<int>(
                name: "TotalWatchedSeconds",
                table: "LessonProgresses",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<DateTime>(
                name: "StartedAt",
                table: "LessonProgresses",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETDATE()");

            migrationBuilder.AlterColumn<int>(
                name: "MaxWatchedSecond",
                table: "LessonProgresses",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "LastWatchedSecond",
                table: "LessonProgresses",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<bool>(
                name: "Completed",
                table: "LessonProgresses",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: false);

            migrationBuilder.AddPrimaryKey(
                name: "PK_LessonProgresses",
                table: "LessonProgresses",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_LessonProgresses_UserId",
                table: "LessonProgresses",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_LessonProgresses_Lessons_LessonId",
                table: "LessonProgresses",
                column: "LessonId",
                principalTable: "Lessons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LessonProgresses_Users_UserId",
                table: "LessonProgresses",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
