using System;
using EducationalPlataform.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EducationalPlataform.Migrations
{
    [DbContext(typeof(EducationalPlataformContext))]
    [Migration("20260929180000_PaymentLateFee")]
    public partial class PaymentLateFee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "LateFeeApplied",
                table: "Payments",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LateFeeApplied",
                table: "Payments");
        }
    }
}
