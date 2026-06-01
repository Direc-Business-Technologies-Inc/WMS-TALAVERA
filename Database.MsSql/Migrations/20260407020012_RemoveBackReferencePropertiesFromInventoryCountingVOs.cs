using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Database.MsSql.Migrations
{
    /// <inheritdoc />
    public partial class RemoveBackReferencePropertiesFromInventoryCountingVOs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SheetNo",
                table: "ICD3");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SheetNo",
                table: "ICD3",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }
    }
}
