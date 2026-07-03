using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtlasBank.NotificationService.Migrations
{
    /// <inheritdoc />
    public partial class RenameRecipientEmailToRecipient : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RecipientEmail",
                table: "Notifications",
                newName: "Recipient");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Recipient",
                table: "Notifications",
                newName: "RecipientEmail");
        }
    }
}
