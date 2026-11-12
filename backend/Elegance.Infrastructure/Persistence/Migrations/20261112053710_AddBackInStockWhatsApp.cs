using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elegance.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBackInStockWhatsApp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "back_in_stock_subscriptions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    ProductVariantId = table.Column<long>(type: "bigint", nullable: true),
                    CustomerName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    WhatsAppNumber = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NormalizedWhatsAppNumber = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ConsentGiven = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ConsentTimestamp = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SubscribedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    NotifiedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_back_in_stock_subscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_back_in_stock_subscriptions_product_variants_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "product_variants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_back_in_stock_subscriptions_products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "restock_events",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    VariantId = table.Column<long>(type: "bigint", nullable: true),
                    PreviousStock = table.Column<int>(type: "int", nullable: false),
                    NewStock = table.Column<int>(type: "int", nullable: false),
                    DetectedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_restock_events", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "whatsapp_notifications",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SubscriptionId = table.Column<long>(type: "bigint", nullable: false),
                    RestockEventId = table.Column<long>(type: "bigint", nullable: false),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    WhatsAppNumber = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    WhatsAppMessageId = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MessageTemplateName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    NextAttemptAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    FailureReason = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SentAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DeliveredAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_whatsapp_notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_whatsapp_notifications_back_in_stock_subscriptions_Subscript~",
                        column: x => x.SubscriptionId,
                        principalTable: "back_in_stock_subscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_whatsapp_notifications_products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_whatsapp_notifications_restock_events_RestockEventId",
                        column: x => x.RestockEventId,
                        principalTable: "restock_events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_back_in_stock_subscriptions_NormalizedWhatsAppNumber",
                table: "back_in_stock_subscriptions",
                column: "NormalizedWhatsAppNumber");

            migrationBuilder.CreateIndex(
                name: "IX_back_in_stock_subscriptions_ProductId",
                table: "back_in_stock_subscriptions",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_back_in_stock_subscriptions_ProductId_ProductVariantId_Norma~",
                table: "back_in_stock_subscriptions",
                columns: new[] { "ProductId", "ProductVariantId", "NormalizedWhatsAppNumber", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_back_in_stock_subscriptions_ProductVariantId",
                table: "back_in_stock_subscriptions",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_back_in_stock_subscriptions_Status",
                table: "back_in_stock_subscriptions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_restock_events_DetectedAt",
                table: "restock_events",
                column: "DetectedAt");

            migrationBuilder.CreateIndex(
                name: "IX_restock_events_ProductId",
                table: "restock_events",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_restock_events_VariantId",
                table: "restock_events",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_whatsapp_notifications_NextAttemptAt",
                table: "whatsapp_notifications",
                column: "NextAttemptAt");

            migrationBuilder.CreateIndex(
                name: "IX_whatsapp_notifications_ProductId",
                table: "whatsapp_notifications",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_whatsapp_notifications_RestockEventId",
                table: "whatsapp_notifications",
                column: "RestockEventId");

            migrationBuilder.CreateIndex(
                name: "IX_whatsapp_notifications_Status",
                table: "whatsapp_notifications",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_whatsapp_notifications_SubscriptionId_RestockEventId",
                table: "whatsapp_notifications",
                columns: new[] { "SubscriptionId", "RestockEventId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "whatsapp_notifications");

            migrationBuilder.DropTable(
                name: "back_in_stock_subscriptions");

            migrationBuilder.DropTable(
                name: "restock_events");
        }
    }
}
