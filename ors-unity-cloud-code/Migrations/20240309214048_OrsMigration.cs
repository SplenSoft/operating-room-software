using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ors_unity_cloud_code.Migrations
{
    /// <inheritdoc />
    public partial class OrsMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StoredMetaData",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LastModified = table.Column<long>(type: "bigint", nullable: false),
                    AssetBundleName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SelectableMetaData = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoredMetaData", x => x.ID);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StoredMetaData");
        }
    }
}
