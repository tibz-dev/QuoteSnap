using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuoteSnap.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionEmailToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalSubscriptionEmailToken",
                table: "Subscriptions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExternalSubscriptionEmailToken",
                table: "Subscriptions");
        }
    }
}
