using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PizzeriaMoschini.Migrations
{
    /// <inheritdoc />
    public partial class updatedModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StaffID",
                table: "Reservations",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_StaffID",
                table: "Reservations",
                column: "StaffID");

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_Staffs_StaffID",
                table: "Reservations",
                column: "StaffID",
                principalTable: "Staffs",
                principalColumn: "StaffID",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_Staffs_StaffID",
                table: "Reservations");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_StaffID",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "StaffID",
                table: "Reservations");
        }
    }
}
