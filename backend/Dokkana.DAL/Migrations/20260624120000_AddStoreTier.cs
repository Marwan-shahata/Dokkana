using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Dokkana.DAL.Data;

#nullable disable

namespace Dokkana.DAL.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260624120000_AddStoreTier")]
    public partial class AddStoreTier : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Store commercial plan: 0 = Free (default), 1 = Pro.
            migrationBuilder.AddColumn<int>(
                name: "Tier",
                table: "Stores",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Tier",
                table: "Stores");
        }
    }
}
