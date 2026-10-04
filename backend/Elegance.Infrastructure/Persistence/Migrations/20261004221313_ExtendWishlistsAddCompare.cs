using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elegance.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExtendWishlistsAddCompare : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "CustomerId",
                table: "wishlists",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<string>(
                name: "GuestTokenHash",
                table: "wishlists",
                type: "varchar(128)",
                maxLength: 128,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<long>(
                name: "VariantId",
                table: "wishlist_items",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "compare_lists",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CustomerId = table.Column<long>(type: "bigint", nullable: true),
                    GuestTokenHash = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_compare_lists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_compare_lists_customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "compare_items",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CompareListId = table.Column<long>(type: "bigint", nullable: false),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_compare_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_compare_items_compare_lists_CompareListId",
                        column: x => x.CompareListId,
                        principalTable: "compare_lists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_compare_items_products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_wishlists_GuestTokenHash",
                table: "wishlists",
                column: "GuestTokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_wishlist_items_VariantId",
                table: "wishlist_items",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_wishlist_items_WishlistId_ProductId_VariantId",
                table: "wishlist_items",
                columns: new[] { "WishlistId", "ProductId", "VariantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_compare_items_CompareListId_ProductId",
                table: "compare_items",
                columns: new[] { "CompareListId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_compare_items_ProductId",
                table: "compare_items",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_compare_lists_CustomerId",
                table: "compare_lists",
                column: "CustomerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_compare_lists_GuestTokenHash",
                table: "compare_lists",
                column: "GuestTokenHash",
                unique: true);

            migrationBuilder.DropIndex(
                name: "IX_wishlist_items_WishlistId_ProductId",
                table: "wishlist_items");

            migrationBuilder.AddForeignKey(
                name: "FK_wishlist_items_product_variants_VariantId",
                table: "wishlist_items",
                column: "VariantId",
                principalTable: "product_variants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_wishlist_items_product_variants_VariantId",
                table: "wishlist_items");

            migrationBuilder.DropTable(
                name: "compare_items");

            migrationBuilder.DropTable(
                name: "compare_lists");

            migrationBuilder.DropIndex(
                name: "IX_wishlists_GuestTokenHash",
                table: "wishlists");

            migrationBuilder.DropIndex(
                name: "IX_wishlist_items_VariantId",
                table: "wishlist_items");

            migrationBuilder.DropIndex(
                name: "IX_wishlist_items_WishlistId_ProductId_VariantId",
                table: "wishlist_items");

            migrationBuilder.DropColumn(
                name: "GuestTokenHash",
                table: "wishlists");

            migrationBuilder.DropColumn(
                name: "VariantId",
                table: "wishlist_items");

            migrationBuilder.AlterColumn<long>(
                name: "CustomerId",
                table: "wishlists",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_wishlist_items_WishlistId_ProductId",
                table: "wishlist_items",
                columns: new[] { "WishlistId", "ProductId" },
                unique: true);
        }
    }
}
