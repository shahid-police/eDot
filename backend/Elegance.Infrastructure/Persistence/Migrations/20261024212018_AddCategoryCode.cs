using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elegance.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "categories",
                type: "varchar(10)",
                maxLength: 10,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_categories_Code",
                table: "categories",
                column: "Code",
                unique: true);

            // Backfills SKU codes for the categories this app ships with (see
            // DevelopmentSeeder.cs / CatalogDemoSeeder.cs for their slugs). Matched by slug, so
            // this is a no-op wherever a given slug doesn't exist - safe on any environment.
            migrationBuilder.Sql("""
                UPDATE categories SET Code = CASE Slug
                    WHEN 'bridal-jewellery' THEN 'BRID'
                    WHEN 'traditional-jewellery' THEN 'TRAD'
                    WHEN 'party-wear-jewellery' THEN 'PARTY'
                    WHEN 'bangles-bracelets' THEN 'BB'
                    WHEN 'earrings-studs' THEN 'STUD'
                    WHEN 'earrings-hoops' THEN 'HOOP'
                    WHEN 'earrings-jhumkas' THEN 'JHUM'
                    WHEN 'earrings-chandbalis' THEN 'CHAN'
                    WHEN 'necklaces-sets' THEN 'NSET'
                    WHEN 'necklaces-chokers' THEN 'CHOK'
                    WHEN 'necklaces-pendant' THEN 'PNECK'
                    WHEN 'rings-statement' THEN 'SRING'
                    WHEN 'rings-adjustable' THEN 'ARING'
                    WHEN 'bangles-traditional' THEN 'TBANG'
                    WHEN 'bangles-kada' THEN 'KADA'
                    WHEN 'bracelets' THEN 'BRAC'
                    WHEN 'rings' THEN 'RING'
                    WHEN 'necklaces' THEN 'NECK'
                    WHEN 'earrings' THEN 'EARR'
                    ELSE Code
                END
                WHERE Slug IN (
                    'bridal-jewellery', 'traditional-jewellery', 'party-wear-jewellery', 'bangles-bracelets',
                    'earrings-studs', 'earrings-hoops', 'earrings-jhumkas', 'earrings-chandbalis',
                    'necklaces-sets', 'necklaces-chokers', 'necklaces-pendant',
                    'rings-statement', 'rings-adjustable',
                    'bangles-traditional', 'bangles-kada', 'bracelets',
                    'rings', 'necklaces', 'earrings'
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_categories_Code",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "categories");
        }
    }
}
