using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BeroxAppy.Migrations
{
    /// <inheritdoc />
    public partial class mig_finans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CurrentPeriodCommission",
                table: "Employees",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastCommissionResetDate",
                table: "Employees",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSalaryPaymentDate",
                table: "Employees",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaymentDay",
                table: "Employees",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PreferredPaymentMethod",
                table: "Employees",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SalaryPeriod",
                table: "Employees",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "DailyFinancialSummaries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ServiceIncome = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ProductIncome = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OtherIncome = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalIncome = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EmployeePayments = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OperationalExpenses = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OtherExpenses = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalExpenses = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NetProfit = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CashAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreditCardAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DebitCardAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BankTransferAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalReservations = table.Column<int>(type: "int", nullable: false),
                    CompletedReservations = table.Column<int>(type: "int", nullable: false),
                    CancelledReservations = table.Column<int>(type: "int", nullable: false),
                    AverageTicketSize = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsClosed = table.Column<bool>(type: "bit", nullable: false),
                    ClosedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ExtraProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyFinancialSummaries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeCommissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReservationDetailId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EarnedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsPaid = table.Column<bool>(type: "bit", nullable: false),
                    PaidDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeCommissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeCommissions_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeCommissions_ReservationDetails_ReservationDetailId",
                        column: x => x.ReservationDetailId,
                        principalTable: "ReservationDetails",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "EmployeePayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SalaryAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CommissionAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BonusAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaymentMethod = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaymentType = table.Column<int>(type: "int", nullable: false),
                    ExtraProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeePayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeePayments_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeCommissions_EmployeeId",
                table: "EmployeeCommissions",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeCommissions_ReservationDetailId",
                table: "EmployeeCommissions",
                column: "ReservationDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeePayments_EmployeeId",
                table: "EmployeePayments",
                column: "EmployeeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailyFinancialSummaries");

            migrationBuilder.DropTable(
                name: "EmployeeCommissions");

            migrationBuilder.DropTable(
                name: "EmployeePayments");

            migrationBuilder.DropColumn(
                name: "CurrentPeriodCommission",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "LastCommissionResetDate",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "LastSalaryPaymentDate",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "PaymentDay",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "PreferredPaymentMethod",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "SalaryPeriod",
                table: "Employees");
        }
    }
}
