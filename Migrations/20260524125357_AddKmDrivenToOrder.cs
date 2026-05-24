using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TippSendApp.Migrations
{
    /// <inheritdoc />
    public partial class AddKmDrivenToOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "KmDriven",
                table: "Orders",
                type: "decimal(8,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "KmDriven",
                table: "Orders");
        }
    }
}
