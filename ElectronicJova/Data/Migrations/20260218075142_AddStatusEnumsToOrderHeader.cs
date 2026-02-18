using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectronicJova.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStatusEnumsToOrderHeader : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OrderStatusValue",
                table: "OrderHeaders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PaymentStatusValue",
                table: "OrderHeaders",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrderStatusValue",
                table: "OrderHeaders");

            migrationBuilder.DropColumn(
                name: "PaymentStatusValue",
                table: "OrderHeaders");
        }
    }
}
