using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmployeeSelfServiceAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailMessageEmployeeFk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_EmailMessages_EmployeeId",
                table: "EmailMessages",
                column: "EmployeeId");

            migrationBuilder.AddForeignKey(
                name: "FK_EmailMessages_Employees_EmployeeId",
                table: "EmailMessages",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmailMessages_Employees_EmployeeId",
                table: "EmailMessages");

            migrationBuilder.DropIndex(
                name: "IX_EmailMessages_EmployeeId",
                table: "EmailMessages");
        }
    }
}
