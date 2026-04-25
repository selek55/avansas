using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Avansas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBackgroundJobFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "NotifiedAt",
                table: "StockNotifications",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AbandonedEmailSentAt",
                table: "Carts",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NotifiedAt",
                table: "StockNotifications");

            migrationBuilder.DropColumn(
                name: "AbandonedEmailSentAt",
                table: "Carts");
        }
    }
}
