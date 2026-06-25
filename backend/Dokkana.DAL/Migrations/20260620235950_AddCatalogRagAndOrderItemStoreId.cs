using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dokkana.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogRagAndOrderItemStoreId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StoreId",
                table: "OrderItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "FileName",
                table: "CatalogDocuments",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateTable(
                name: "CatalogChunks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CatalogDocumentId = table.Column<int>(type: "int", nullable: false),
                    StoreId = table.Column<int>(type: "int", nullable: false),
                    ChunkIndex = table.Column<int>(type: "int", nullable: false),
                    PageNumber = table.Column<int>(type: "int", nullable: true),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EmbeddingJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmbeddingDimensions = table.Column<int>(type: "int", nullable: true),
                    EmbeddingModel = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogChunks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CatalogChunks_CatalogDocuments_CatalogDocumentId",
                        column: x => x.CatalogDocumentId,
                        principalTable: "CatalogDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CatalogChunks_CatalogDocumentId",
                table: "CatalogChunks",
                column: "CatalogDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogChunks_StoreId",
                table: "CatalogChunks",
                column: "StoreId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CatalogChunks");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "OrderItems");

            migrationBuilder.AlterColumn<string>(
                name: "FileName",
                table: "CatalogDocuments",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(300)",
                oldMaxLength: 300);
        }
    }
}
