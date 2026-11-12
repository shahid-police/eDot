using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elegance.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCheckoutPromoCodeIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CommissionEligibleAmount",
                table: "orders",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CommissionPercentageSnapshot",
                table: "orders",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ProductDiscountTotal",
                table: "orders",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PromoCodeDiscountAmount",
                table: "orders",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PromoCodeDiscountPercentageSnapshot",
                table: "orders",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "PromoCodeId",
                table: "orders",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "PromoterId",
                table: "orders",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OriginalUnitPrice",
                table: "order_items",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<long>(
                name: "AppliedPromoCodeId",
                table: "carts",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "promo_code_usages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PromoCodeId = table.Column<long>(type: "bigint", nullable: false),
                    PromoterId = table.Column<long>(type: "bigint", nullable: false),
                    CustomerId = table.Column<long>(type: "bigint", nullable: true),
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    UsedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_promo_code_usages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_promo_code_usages_customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_promo_code_usages_orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_promo_code_usages_promo_codes_PromoCodeId",
                        column: x => x.PromoCodeId,
                        principalTable: "promo_codes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_promo_code_usages_promoters_PromoterId",
                        column: x => x.PromoterId,
                        principalTable: "promoters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_orders_PromoCodeId",
                table: "orders",
                column: "PromoCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_orders_PromoterId",
                table: "orders",
                column: "PromoterId");

            migrationBuilder.CreateIndex(
                name: "IX_carts_AppliedPromoCodeId",
                table: "carts",
                column: "AppliedPromoCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_promo_code_usages_CustomerId",
                table: "promo_code_usages",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_promo_code_usages_OrderId",
                table: "promo_code_usages",
                column: "OrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_promo_code_usages_PromoCodeId",
                table: "promo_code_usages",
                column: "PromoCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_promo_code_usages_PromoCodeId_CustomerId",
                table: "promo_code_usages",
                columns: new[] { "PromoCodeId", "CustomerId" });

            migrationBuilder.CreateIndex(
                name: "IX_promo_code_usages_PromoterId",
                table: "promo_code_usages",
                column: "PromoterId");

            migrationBuilder.AddForeignKey(
                name: "FK_carts_promo_codes_AppliedPromoCodeId",
                table: "carts",
                column: "AppliedPromoCodeId",
                principalTable: "promo_codes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_orders_promo_codes_PromoCodeId",
                table: "orders",
                column: "PromoCodeId",
                principalTable: "promo_codes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_orders_promoters_PromoterId",
                table: "orders",
                column: "PromoterId",
                principalTable: "promoters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_carts_promo_codes_AppliedPromoCodeId",
                table: "carts");

            migrationBuilder.DropForeignKey(
                name: "FK_orders_promo_codes_PromoCodeId",
                table: "orders");

            migrationBuilder.DropForeignKey(
                name: "FK_orders_promoters_PromoterId",
                table: "orders");

            migrationBuilder.DropTable(
                name: "promo_code_usages");

            migrationBuilder.DropIndex(
                name: "IX_orders_PromoCodeId",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "IX_orders_PromoterId",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "IX_carts_AppliedPromoCodeId",
                table: "carts");

            migrationBuilder.DropColumn(
                name: "CommissionEligibleAmount",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "CommissionPercentageSnapshot",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "ProductDiscountTotal",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "PromoCodeDiscountAmount",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "PromoCodeDiscountPercentageSnapshot",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "PromoCodeId",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "PromoterId",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "OriginalUnitPrice",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "AppliedPromoCodeId",
                table: "carts");
        }
    }
}
