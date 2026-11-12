using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elegance.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPromoterPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "promoter_payments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PaymentReference = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PromoterId = table.Column<long>(type: "bigint", nullable: false),
                    TotalPaymentAmount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    PaymentMethod = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TransactionReference = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Remarks = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedByAdminUserId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_promoter_payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_promoter_payments_promoters_PromoterId",
                        column: x => x.PromoterId,
                        principalTable: "promoters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "promoter_payment_allocations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PromoterPaymentId = table.Column<long>(type: "bigint", nullable: false),
                    PromoterCommissionId = table.Column<long>(type: "bigint", nullable: false),
                    AllocatedAmount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_promoter_payment_allocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_promoter_payment_allocations_promoter_commissions_PromoterCo~",
                        column: x => x.PromoterCommissionId,
                        principalTable: "promoter_commissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_promoter_payment_allocations_promoter_payments_PromoterPayme~",
                        column: x => x.PromoterPaymentId,
                        principalTable: "promoter_payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_promoter_payment_allocations_PromoterCommissionId",
                table: "promoter_payment_allocations",
                column: "PromoterCommissionId");

            migrationBuilder.CreateIndex(
                name: "IX_promoter_payment_allocations_PromoterPaymentId",
                table: "promoter_payment_allocations",
                column: "PromoterPaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_promoter_payments_PaymentReference",
                table: "promoter_payments",
                column: "PaymentReference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_promoter_payments_PromoterId",
                table: "promoter_payments",
                column: "PromoterId");

            migrationBuilder.CreateIndex(
                name: "IX_promoter_payments_Status",
                table: "promoter_payments",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "promoter_payment_allocations");

            migrationBuilder.DropTable(
                name: "promoter_payments");
        }
    }
}
