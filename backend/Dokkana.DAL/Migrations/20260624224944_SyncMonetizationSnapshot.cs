using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dokkana.DAL.Migrations
{
    /// <summary>
    /// No-op migration. The two earlier monetization migrations (AddStoreTier and
    /// AddMonetizationFeatures) were hand-written, so the model snapshot had drifted.
    /// This migration carries the regenerated, in-sync snapshot (see the .Designer.cs)
    /// so future `dotnet ef migrations add` diffs correctly. It intentionally makes no
    /// schema changes — every table/column already exists from the previous migrations.
    /// </summary>
    public partial class SyncMonetizationSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty — snapshot baseline sync only.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty — snapshot baseline sync only.
        }
    }
}
