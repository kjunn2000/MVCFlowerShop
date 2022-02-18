using Microsoft.EntityFrameworkCore.Migrations;

namespace MVCFlowerShop.Migrations.MVCFlowerShopNew
{
    public partial class addRating : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Rating",
                table: "Flower",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Rating",
                table: "Flower");
        }
    }
}
