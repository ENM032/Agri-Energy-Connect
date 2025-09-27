using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication2.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalyticsIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add indexes for analytics queries
            migrationBuilder.CreateIndex(
                name: "IX_Products_ProductDate_Category",
                table: "Products",
                columns: new[] { "ProductDate", "Category" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_Category_ProductDate",
                table: "Products",
                columns: new[] { "Category", "ProductDate" });

            // Add index for user registration analytics
            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_RegistrationDate",
                table: "AspNetUsers",
                column: "RegistrationDate");

            // Add indexes for security logs
            migrationBuilder.CreateIndex(
                name: "IX_SecurityLogs_Timestamp_Action",
                table: "SecurityLogs",
                columns: new[] { "Timestamp", "Action" });

            migrationBuilder.CreateIndex(
                name: "IX_SecurityLogs_UserId_Timestamp",
                table: "SecurityLogs",
                columns: new[] { "UserId", "Timestamp" });

            // Add indexes for failed login attempts
            migrationBuilder.CreateIndex(
                name: "IX_FailedLoginAttempts_AttemptTime",
                table: "FailedLoginAttempts",
                column: "AttemptTime");

            migrationBuilder.CreateIndex(
                name: "IX_FailedLoginAttempts_IpAddress_AttemptTime",
                table: "FailedLoginAttempts",
                columns: new[] { "IpAddress", "AttemptTime" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_ProductDate_Category",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_Category_ProductDate",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_RegistrationDate",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_SecurityLogs_Timestamp_Action",
                table: "SecurityLogs");

            migrationBuilder.DropIndex(
                name: "IX_SecurityLogs_UserId_Timestamp",
                table: "SecurityLogs");

            migrationBuilder.DropIndex(
                name: "IX_FailedLoginAttempts_AttemptTime",
                table: "FailedLoginAttempts");

            migrationBuilder.DropIndex(
                name: "IX_FailedLoginAttempts_IpAddress_AttemptTime",
                table: "FailedLoginAttempts");
        }
    }
}