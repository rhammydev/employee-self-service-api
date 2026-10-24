using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmployeeSelfServiceAPI.Migrations
{
    /// <inheritdoc />
    public partial class FixApplicationSnapshotForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OpportunityApplications_Departments_DepartmentAtApplyId",
                table: "OpportunityApplications");

            migrationBuilder.DropForeignKey(
                name: "FK_OpportunityApplications_Grades_GradeAtApplyId",
                table: "OpportunityApplications");

            migrationBuilder.DropIndex(
                name: "IX_OpportunityApplications_DepartmentAtApplyId",
                table: "OpportunityApplications");

            migrationBuilder.DropIndex(
                name: "IX_OpportunityApplications_GradeAtApplyId",
                table: "OpportunityApplications");

            migrationBuilder.DropColumn(
                name: "DepartmentAtApplyId",
                table: "OpportunityApplications");

            migrationBuilder.DropColumn(
                name: "GradeAtApplyId",
                table: "OpportunityApplications");

            migrationBuilder.CreateIndex(
                name: "IX_OpportunityApplications_DepartmentIdAtApply",
                table: "OpportunityApplications",
                column: "DepartmentIdAtApply");

            migrationBuilder.CreateIndex(
                name: "IX_OpportunityApplications_GradeIdAtApply",
                table: "OpportunityApplications",
                column: "GradeIdAtApply");

            migrationBuilder.AddForeignKey(
                name: "FK_OpportunityApplications_Departments_DepartmentIdAtApply",
                table: "OpportunityApplications",
                column: "DepartmentIdAtApply",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OpportunityApplications_Grades_GradeIdAtApply",
                table: "OpportunityApplications",
                column: "GradeIdAtApply",
                principalTable: "Grades",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OpportunityApplications_Departments_DepartmentIdAtApply",
                table: "OpportunityApplications");

            migrationBuilder.DropForeignKey(
                name: "FK_OpportunityApplications_Grades_GradeIdAtApply",
                table: "OpportunityApplications");

            migrationBuilder.DropIndex(
                name: "IX_OpportunityApplications_DepartmentIdAtApply",
                table: "OpportunityApplications");

            migrationBuilder.DropIndex(
                name: "IX_OpportunityApplications_GradeIdAtApply",
                table: "OpportunityApplications");

            migrationBuilder.AddColumn<int>(
                name: "DepartmentAtApplyId",
                table: "OpportunityApplications",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GradeAtApplyId",
                table: "OpportunityApplications",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OpportunityApplications_DepartmentAtApplyId",
                table: "OpportunityApplications",
                column: "DepartmentAtApplyId");

            migrationBuilder.CreateIndex(
                name: "IX_OpportunityApplications_GradeAtApplyId",
                table: "OpportunityApplications",
                column: "GradeAtApplyId");

            migrationBuilder.AddForeignKey(
                name: "FK_OpportunityApplications_Departments_DepartmentAtApplyId",
                table: "OpportunityApplications",
                column: "DepartmentAtApplyId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OpportunityApplications_Grades_GradeAtApplyId",
                table: "OpportunityApplications",
                column: "GradeAtApplyId",
                principalTable: "Grades",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
