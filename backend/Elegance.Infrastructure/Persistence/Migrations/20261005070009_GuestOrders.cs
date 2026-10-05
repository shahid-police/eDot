using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elegance.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class GuestOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GuestTokenHash",
                table: "orders",
                type: "varchar(128)",
                maxLength: 128,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_orders_GuestTokenHash",
                table: "orders",
                column: "GuestTokenHash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_orders_GuestTokenHash",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "GuestTokenHash",
                table: "orders");
        }
    }
}
