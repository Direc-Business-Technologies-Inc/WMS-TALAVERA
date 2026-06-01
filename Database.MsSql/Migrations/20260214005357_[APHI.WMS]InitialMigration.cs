using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Database.MsSql.Migrations
{
    /// <inheritdoc />
    public partial class APHIWMSInitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence<int>(
                name: "SN_OUSR",
                startValue: 0L);

            migrationBuilder.CreateTable(
                name: "ODCT",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ODCT", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OMPR",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModuleReference = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Permission = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OMPR", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ONRT",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: true),
                    Icon = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Uri = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Protected = table.Column<bool>(type: "bit", nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ONRT", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OROL",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Archived = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OROL", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ODCN",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Prefix = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CurrentNumber = table.Column<int>(type: "int", nullable: false),
                    NextNumber = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ODCN", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ODCN_ODCT",
                        column: x => x.DocumentTypeId,
                        principalTable: "ODCT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OMDL",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Root = table.Column<bool>(type: "bit", nullable: false),
                    Transactional = table.Column<bool>(type: "bit", nullable: false),
                    NavRouteId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OMDL", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OMDL_ONRT_NavRouteId",
                        column: x => x.NavRouteId,
                        principalTable: "ONRT",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "OUSR",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MiddleName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EmailAddress = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Company = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Archived = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OUSR", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OUSR_OROL_RoleId",
                        column: x => x.RoleId,
                        principalTable: "OROL",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ROL1",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ModulePermission = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ROL1", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ROL1_OROL_RoleId",
                        column: x => x.RoleId,
                        principalTable: "OROL",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MDL1",
                columns: table => new
                {
                    ModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Permission = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MDL1", x => new { x.ModuleId, x.Id });
                    table.ForeignKey(
                        name: "FK_MDL1_OMDL_ModuleId",
                        column: x => x.ModuleId,
                        principalTable: "OMDL",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "USR1",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserName_Value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HashedPassword = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Locked = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USR1", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_USR1_OUSR_UserId",
                        column: x => x.UserId,
                        principalTable: "OUSR",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "USR2",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    Succeeded = table.Column<bool>(type: "bit", nullable: false),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LoginDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USR2", x => x.Id);
                    table.ForeignKey(
                        name: "FK_USR2_OUSR_AccountId",
                        column: x => x.AccountId,
                        principalTable: "OUSR",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "USR3",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ModulePermission = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USR3", x => x.Id);
                    table.ForeignKey(
                        name: "FK_USR3_OUSR_UserId",
                        column: x => x.UserId,
                        principalTable: "OUSR",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ODCN_DocumentTypeId",
                table: "ODCN",
                column: "DocumentTypeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OMDL_NavRouteId",
                table: "OMDL",
                column: "NavRouteId");

            migrationBuilder.CreateIndex(
                name: "IX_OUSR_RoleId",
                table: "OUSR",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_ROL1_RoleId",
                table: "ROL1",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_USR2_AccountId",
                table: "USR2",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_USR3_UserId",
                table: "USR3",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MDL1");

            migrationBuilder.DropTable(
                name: "ODCN");

            migrationBuilder.DropTable(
                name: "OMPR");

            migrationBuilder.DropTable(
                name: "ROL1");

            migrationBuilder.DropTable(
                name: "USR1");

            migrationBuilder.DropTable(
                name: "USR2");

            migrationBuilder.DropTable(
                name: "USR3");

            migrationBuilder.DropTable(
                name: "OMDL");

            migrationBuilder.DropTable(
                name: "ODCT");

            migrationBuilder.DropTable(
                name: "OUSR");

            migrationBuilder.DropTable(
                name: "ONRT");

            migrationBuilder.DropTable(
                name: "OROL");

            migrationBuilder.DropSequence(
                name: "SN_OUSR");
        }
    }
}
