using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddTimeTableProfileFK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_TimeTables_UserId",
                table: "TimeTables",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_TimeTables_Profiles_UserId",
                table: "TimeTables",
                column: "UserId",
                principalTable: "Profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TimeTables_Profiles_UserId",
                table: "TimeTables");

            migrationBuilder.DropIndex(
                name: "IX_TimeTables_UserId",
                table: "TimeTables");
        }
    }
}
