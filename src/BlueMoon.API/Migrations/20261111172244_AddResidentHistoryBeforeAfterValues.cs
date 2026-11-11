using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlueMoon.API.Migrations
{
    /// <inheritdoc />
    public partial class AddResidentHistoryBeforeAfterValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NewValue",
                table: "ResidenceHistories",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreviousValue",
                table: "ResidenceHistories",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NewValue",
                table: "ResidenceHistories");

            migrationBuilder.DropColumn(
                name: "PreviousValue",
                table: "ResidenceHistories");
        }
    }
}
