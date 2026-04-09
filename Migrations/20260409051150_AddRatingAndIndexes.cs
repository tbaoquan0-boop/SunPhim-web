using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SunPhim.Migrations
{
    /// <inheritdoc />
    public partial class AddRatingAndIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Rating",
                table: "Movies",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RatingCount",
                table: "Movies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Movies_CreatedAt",
                table: "Movies",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Movies_ImdbScore",
                table: "Movies",
                column: "ImdbScore");

            migrationBuilder.CreateIndex(
                name: "IX_Movies_IsPublished_ImdbScore",
                table: "Movies",
                columns: new[] { "IsPublished", "ImdbScore" });

            migrationBuilder.CreateIndex(
                name: "IX_Movies_IsPublished_Rating",
                table: "Movies",
                columns: new[] { "IsPublished", "Rating" });

            migrationBuilder.CreateIndex(
                name: "IX_Movies_IsPublished_UpdatedAt",
                table: "Movies",
                columns: new[] { "IsPublished", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Movies_Rating",
                table: "Movies",
                column: "Rating");

            migrationBuilder.CreateIndex(
                name: "IX_Movies_Type_IsPublished_UpdatedAt",
                table: "Movies",
                columns: new[] { "Type", "IsPublished", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Movies_UpdatedAt",
                table: "Movies",
                column: "UpdatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Movies_CreatedAt",
                table: "Movies");

            migrationBuilder.DropIndex(
                name: "IX_Movies_ImdbScore",
                table: "Movies");

            migrationBuilder.DropIndex(
                name: "IX_Movies_IsPublished_ImdbScore",
                table: "Movies");

            migrationBuilder.DropIndex(
                name: "IX_Movies_IsPublished_Rating",
                table: "Movies");

            migrationBuilder.DropIndex(
                name: "IX_Movies_IsPublished_UpdatedAt",
                table: "Movies");

            migrationBuilder.DropIndex(
                name: "IX_Movies_Rating",
                table: "Movies");

            migrationBuilder.DropIndex(
                name: "IX_Movies_Type_IsPublished_UpdatedAt",
                table: "Movies");

            migrationBuilder.DropIndex(
                name: "IX_Movies_UpdatedAt",
                table: "Movies");

            migrationBuilder.DropColumn(
                name: "Rating",
                table: "Movies");

            migrationBuilder.DropColumn(
                name: "RatingCount",
                table: "Movies");
        }
    }
}
