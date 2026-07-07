using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Database.MsSql.Migrations
{
    /// <inheritdoc />
    public partial class APHIWMSCreateStoredProcedures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                                        CREATE OR ALTER PROCEDURE APP_SP_GetNextDocumentNumber
                                            @DocumentTypeId UNIQUEIDENTIFIER
                                        AS
                                        BEGIN
                                            SET NOCOUNT ON;

                                            DECLARE @NextNumber INT;

                                            BEGIN TRY
                                                BEGIN TRANSACTION;

                                                UPDATE ODCN WITH (UPDLOCK, HOLDLOCK, ROWLOCK)
                                                SET
                                                    CurrentNumber = [NextNumber],
                                                    [NextNumber] = [NextNumber] + 1,
                                                    @NextNumber = [NextNumber] + 1
                                                WHERE DocumentTypeId = @DocumentTypeId;

                                                COMMIT TRANSACTION;

                                                SELECT
                                                    *
                                                FROM ODCN
                                                WHERE DocumentTypeId = @DocumentTypeId;

                                            END TRY
                                            BEGIN CATCH
                                                    ROLLBACK TRANSACTION;
                                                THROW;
                                            END CATCH
                                        END
                                        ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS APP_SP_GetNextDocumentNumber");
        }
    }
}