using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PizzeriaMoschini.Migrations
{
    /// <inheritdoc />
    public partial class updatedStaffModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Password",
                table: "Staffs",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Password",
                table: "Staffs");
        }
    }
}
