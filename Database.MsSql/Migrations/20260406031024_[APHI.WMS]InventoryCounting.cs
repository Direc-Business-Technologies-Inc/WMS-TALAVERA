using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Database.MsSql.Migrations
{
    /// <inheritdoc />
    public partial class APHIWMSInventoryCounting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OICD",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WhsCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    WhsName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CountingDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CycleType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Archived = table.Column<bool>(type: "bit", nullable: false),
                    ApprovalStatus = table.Column<int>(type: "int", nullable: false),
                    LsmsDocNum = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SapDocEntry = table.Column<int>(type: "int", nullable: true),
                    SapDocNum = table.Column<int>(type: "int", nullable: true),
                    SapBaseDocEntry = table.Column<int>(type: "int", nullable: true),
                    SapBaseDocNum = table.Column<int>(type: "int", nullable: true),
                    DocumentTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OICD", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ICD1",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InventoryCountingDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ItemName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ActualQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UoMCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    UoMValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UoMName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ICD1", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ICD1_OICD_InventoryCountingDocumentId",
                        column: x => x.InventoryCountingDocumentId,
                        principalTable: "OICD",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ICD2",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SheetNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    InventoryCountingDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CounterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmittedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ICD2", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ICD2_OICD_InventoryCountingDocumentId",
                        column: x => x.InventoryCountingDocumentId,
                        principalTable: "OICD",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ICD3",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SheetNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ItemCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ItemName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UoMCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    UoMValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UoMName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    InventoryCountingSheetVOId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ICD3", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ICD3_ICD2_InventoryCountingSheetVOId",
                        column: x => x.InventoryCountingSheetVOId,
                        principalTable: "ICD2",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ICD1_InventoryCountingDocumentId",
                table: "ICD1",
                column: "InventoryCountingDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_ICD2_InventoryCountingDocumentId",
                table: "ICD2",
                column: "InventoryCountingDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_ICD3_InventoryCountingSheetVOId",
                table: "ICD3",
                column: "InventoryCountingSheetVOId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ICD1");

            migrationBuilder.DropTable(
                name: "ICD3");

            migrationBuilder.DropTable(
                name: "ICD2");

            migrationBuilder.DropTable(
                name: "OICD");
        }
    }
}
