using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elegance.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNoShopContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "no_shop_categories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Slug = table.Column<string>(type: "varchar(220)", maxLength: 220, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ImageMediaFileId = table.Column<long>(type: "bigint", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_no_shop_categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_no_shop_categories_media_files_ImageMediaFileId",
                        column: x => x.ImageMediaFileId,
                        principalTable: "media_files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "no_shop_videos",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Title = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Slug = table.Column<string>(type: "varchar(320)", maxLength: 320, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    YouTubeUrl = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    YouTubeVideoId = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CategoryId = table.Column<long>(type: "bigint", nullable: false),
                    VideoType = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CustomThumbnailMediaFileId = table.Column<long>(type: "bigint", nullable: true),
                    IsFeatured = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PublishedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    SeoTitle = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SeoDescription = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedByAdminUserId = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedByAdminUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_no_shop_videos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_no_shop_videos_media_files_CustomThumbnailMediaFileId",
                        column: x => x.CustomThumbnailMediaFileId,
                        principalTable: "media_files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_no_shop_videos_no_shop_categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "no_shop_categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_no_shop_categories_ImageMediaFileId",
                table: "no_shop_categories",
                column: "ImageMediaFileId");

            migrationBuilder.CreateIndex(
                name: "IX_no_shop_categories_IsActive",
                table: "no_shop_categories",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_no_shop_categories_Slug",
                table: "no_shop_categories",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_no_shop_videos_CategoryId",
                table: "no_shop_videos",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_no_shop_videos_CustomThumbnailMediaFileId",
                table: "no_shop_videos",
                column: "CustomThumbnailMediaFileId");

            migrationBuilder.CreateIndex(
                name: "IX_no_shop_videos_DisplayOrder",
                table: "no_shop_videos",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_no_shop_videos_IsFeatured",
                table: "no_shop_videos",
                column: "IsFeatured");

            migrationBuilder.CreateIndex(
                name: "IX_no_shop_videos_PublishedAt",
                table: "no_shop_videos",
                column: "PublishedAt");

            migrationBuilder.CreateIndex(
                name: "IX_no_shop_videos_Slug",
                table: "no_shop_videos",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_no_shop_videos_Status",
                table: "no_shop_videos",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "no_shop_videos");

            migrationBuilder.DropTable(
                name: "no_shop_categories");
        }
    }
}
