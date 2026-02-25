using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Broker.Backoffice.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEntityChangeDisplayNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EntityDisplayName",
                table: "EntityChanges",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RelatedEntityDisplayName",
                table: "EntityChanges",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EntityDisplayName",
                table: "EntityChanges");

            migrationBuilder.DropColumn(
                name: "RelatedEntityDisplayName",
                table: "EntityChanges");
        }
    }
}
