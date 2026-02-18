using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectronicJova.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixOrderDetailRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderDetail_OrderHeaders_OrderHeaderId1",
                table: "OrderDetail");

            migrationBuilder.DropIndex(
                name: "IX_OrderDetail_OrderHeaderId1",
                table: "OrderDetail");

            migrationBuilder.DropColumn(
                name: "OrderHeaderId1",
                table: "OrderDetail");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OrderHeaderId1",
                table: "OrderDetail",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderDetail_OrderHeaderId1",
                table: "OrderDetail",
                column: "OrderHeaderId1");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderDetail_OrderHeaders_OrderHeaderId1",
                table: "OrderDetail",
                column: "OrderHeaderId1",
                principalTable: "OrderHeaders",
                principalColumn: "Id");
        }
    }
}
