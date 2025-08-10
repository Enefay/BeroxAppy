using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BeroxAppy.Migrations
{
    /// <inheritdoc />
    public partial class mig_salary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "EmploymentStartDate",
                table: "Employees",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<int>(
                name: "PaymentWeekday",
                table: "Employees",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmploymentStartDate",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "PaymentWeekday",
                table: "Employees");
        }
    }
}
