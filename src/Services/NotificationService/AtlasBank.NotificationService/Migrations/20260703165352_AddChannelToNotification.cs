using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtlasBank.NotificationService.Migrations
{
    /// <inheritdoc />
    public partial class AddChannelToNotification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Type",
                table: "Notifications",
                newName: "Channel");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Channel",
                table: "Notifications",
                newName: "Type");
        }
    }
}
