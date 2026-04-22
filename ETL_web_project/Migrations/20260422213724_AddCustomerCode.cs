using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ETL_web_project.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomerCode",
                schema: "stg",
                table: "SalesRaw",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerCode",
                schema: "silver",
                table: "SalesClean",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomerCode",
                schema: "stg",
                table: "SalesRaw");

            migrationBuilder.DropColumn(
                name: "CustomerCode",
                schema: "silver",
                table: "SalesClean");
        }
    }
}
