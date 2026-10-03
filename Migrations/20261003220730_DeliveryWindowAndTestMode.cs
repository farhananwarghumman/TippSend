using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TippSendApp.Migrations
{
    /// <inheritdoc />
    public partial class DeliveryWindowAndTestMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AgreedDeliveryWindow",
                table: "Orders",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PaymentIsTest",
                table: "Orders",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AgreedDeliveryWindow",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PaymentIsTest",
                table: "Orders");
        }
    }
}
