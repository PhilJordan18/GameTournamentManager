using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPendingWinnerIdToMatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "SecondPlayerId",
                table: "Matches",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "PendingWinnerId",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Matches_PendingWinnerId",
                table: "Matches",
                column: "PendingWinnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Users_PendingWinnerId",
                table: "Matches",
                column: "PendingWinnerId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Users_PendingWinnerId",
                table: "Matches");

            migrationBuilder.DropIndex(
                name: "IX_Matches_PendingWinnerId",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "PendingWinnerId",
                table: "Matches");

            migrationBuilder.AlterColumn<int>(
                name: "SecondPlayerId",
                table: "Matches",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
