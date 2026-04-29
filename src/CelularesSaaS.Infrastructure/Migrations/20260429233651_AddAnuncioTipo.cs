using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CelularesSaaS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAnuncioTipo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Tipo",
                table: "Anuncios",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Tipo",
                table: "Anuncios");
        }
    }
}
