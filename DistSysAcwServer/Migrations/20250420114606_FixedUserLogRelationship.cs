using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DistSysAcwServer.Migrations
{
    /// <inheritdoc />
    public partial class FixedUserLogRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Logs_Users_UserApiKey",
                table: "Logs");

            migrationBuilder.AlterColumn<string>(
                name: "UserApiKey",
                table: "Logs",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Logs_Users_UserApiKey",
                table: "Logs",
                column: "UserApiKey",
                principalTable: "Users",
                principalColumn: "ApiKey",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Logs_Users_UserApiKey",
                table: "Logs");

            migrationBuilder.AlterColumn<string>(
                name: "UserApiKey",
                table: "Logs",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddForeignKey(
                name: "FK_Logs_Users_UserApiKey",
                table: "Logs",
                column: "UserApiKey",
                principalTable: "Users",
                principalColumn: "ApiKey");
        }
    }
}
