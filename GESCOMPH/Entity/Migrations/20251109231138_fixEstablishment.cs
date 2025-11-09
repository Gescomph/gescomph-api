using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entity.Migrations
{
    /// <inheritdoc />
    public partial class fixEstablishment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Establishments",
                keyColumn: "Id",
                keyValue: 1,
                column: "Active",
                value: false);

            migrationBuilder.UpdateData(
                table: "Establishments",
                keyColumn: "Id",
                keyValue: 2,
                column: "Active",
                value: false);

            migrationBuilder.UpdateData(
                table: "Establishments",
                keyColumn: "Id",
                keyValue: 3,
                column: "Active",
                value: false);

            migrationBuilder.UpdateData(
                table: "Establishments",
                keyColumn: "Id",
                keyValue: 5,
                column: "Active",
                value: false);

            migrationBuilder.UpdateData(
                table: "Establishments",
                keyColumn: "Id",
                keyValue: 6,
                column: "Active",
                value: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Establishments",
                keyColumn: "Id",
                keyValue: 1,
                column: "Active",
                value: true);

            migrationBuilder.UpdateData(
                table: "Establishments",
                keyColumn: "Id",
                keyValue: 2,
                column: "Active",
                value: true);

            migrationBuilder.UpdateData(
                table: "Establishments",
                keyColumn: "Id",
                keyValue: 3,
                column: "Active",
                value: true);

            migrationBuilder.UpdateData(
                table: "Establishments",
                keyColumn: "Id",
                keyValue: 5,
                column: "Active",
                value: true);

            migrationBuilder.UpdateData(
                table: "Establishments",
                keyColumn: "Id",
                keyValue: 6,
                column: "Active",
                value: true);
        }
    }
}
