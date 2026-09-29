using System;
using EducationalPlataform.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EducationalPlataform.Migrations
{
    [DbContext(typeof(EducationalPlataformContext))]
    [Migration("20260929140000_CoursePriceAndInstallments")]
    public partial class CoursePriceAndInstallments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "Courses",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "InstallmentCount",
                table: "Courses",
                type: "integer",
                nullable: false,
                defaultValue: 12);

            migrationBuilder.AddColumn<int>(
                name: "InstallmentNumber",
                table: "Payments",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Price",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "InstallmentCount",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "InstallmentNumber",
                table: "Payments");
        }
    }
}
