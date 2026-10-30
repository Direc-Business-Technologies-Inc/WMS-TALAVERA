using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Database.MsSql.Migrations
{
    /// <inheritdoc />
    public partial class StrAutoFulfillmentProcessing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "STR_AUTO_FULFILLMENT_PROCESS",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NetsuiteOrderId = table.Column<int>(type: "int", nullable: false),
                    State = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    FulfillmentIds = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_STR_AUTO_FULFILLMENT_PROCESS", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "STR_AUTO_ROUTE_ACTIVATION",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RouteKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ActivatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_STR_AUTO_ROUTE_ACTIVATION", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "STR_FULFILLMENT_LEASE",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NetsuiteOrderId = table.Column<int>(type: "int", nullable: false),
                    OwnerToken = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_STR_FULFILLMENT_LEASE", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_STR_AUTO_FULFILLMENT_PROCESS_NetsuiteOrderId",
                table: "STR_AUTO_FULFILLMENT_PROCESS",
                column: "NetsuiteOrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_STR_AUTO_ROUTE_ACTIVATION_RouteKey",
                table: "STR_AUTO_ROUTE_ACTIVATION",
                column: "RouteKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_STR_FULFILLMENT_LEASE_NetsuiteOrderId",
                table: "STR_FULFILLMENT_LEASE",
                column: "NetsuiteOrderId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "STR_AUTO_FULFILLMENT_PROCESS");

            migrationBuilder.DropTable(
                name: "STR_AUTO_ROUTE_ACTIVATION");

            migrationBuilder.DropTable(
                name: "STR_FULFILLMENT_LEASE");
        }
    }
}
