using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Broker.Backoffice.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ChangeOrderFkToOrderId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NonTradeOrders_Orders_Id",
                table: "NonTradeOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_TradeOrders_Orders_Id",
                table: "TradeOrders");

            migrationBuilder.AddColumn<Guid>(
                name: "OrderId",
                table: "TradeOrders",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "OrderId",
                table: "NonTradeOrders",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            // Copy existing Id (which was the FK) into the new OrderId column
            migrationBuilder.Sql("UPDATE TradeOrders SET OrderId = Id");
            migrationBuilder.Sql("UPDATE NonTradeOrders SET OrderId = Id");

            migrationBuilder.CreateIndex(
                name: "IX_TradeOrders_OrderId",
                table: "TradeOrders",
                column: "OrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NonTradeOrders_OrderId",
                table: "NonTradeOrders",
                column: "OrderId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_NonTradeOrders_Orders_OrderId",
                table: "NonTradeOrders",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TradeOrders_Orders_OrderId",
                table: "TradeOrders",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NonTradeOrders_Orders_OrderId",
                table: "NonTradeOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_TradeOrders_Orders_OrderId",
                table: "TradeOrders");

            migrationBuilder.DropIndex(
                name: "IX_TradeOrders_OrderId",
                table: "TradeOrders");

            migrationBuilder.DropIndex(
                name: "IX_NonTradeOrders_OrderId",
                table: "NonTradeOrders");

            migrationBuilder.DropColumn(
                name: "OrderId",
                table: "TradeOrders");

            migrationBuilder.DropColumn(
                name: "OrderId",
                table: "NonTradeOrders");

            migrationBuilder.AddForeignKey(
                name: "FK_NonTradeOrders_Orders_Id",
                table: "NonTradeOrders",
                column: "Id",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TradeOrders_Orders_Id",
                table: "TradeOrders",
                column: "Id",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
