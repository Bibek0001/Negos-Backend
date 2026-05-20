using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Diyalo.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiTenancy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AdminUsers",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Faqs",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Faqs",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Faqs",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Faqs",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Faqs",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "HeroSlides",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "HeroSlides",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "HeroSlides",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "SiteSettings",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "SiteSettings",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "SiteSettings",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "SiteSettings",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "SiteSettings",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "SiteSettings",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "SiteSettings",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "SiteSettings",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "SiteSettings",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "SiteSettings",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Tours",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Testimonials",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "Key",
                table: "SiteSettings",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "SiteSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Programs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "News",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "MenuItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "HeroSlides",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Faqs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "ContactMessages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Applications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "AdminUsers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "AdminUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Subdomain = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tours_TenantId",
                table: "Tours",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Testimonials_TenantId",
                table: "Testimonials",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SiteSettings_TenantId_Key",
                table: "SiteSettings",
                columns: new[] { "TenantId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Programs_TenantId",
                table: "Programs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_News_TenantId",
                table: "News",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_TenantId",
                table: "MenuItems",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_HeroSlides_TenantId",
                table: "HeroSlides",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Faqs_TenantId",
                table: "Faqs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactMessages_TenantId",
                table: "ContactMessages",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_TenantId",
                table: "Applications",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AdminUsers_TenantId",
                table: "AdminUsers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Subdomain",
                table: "Tenants",
                column: "Subdomain",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_Tours_TenantId",
                table: "Tours");

            migrationBuilder.DropIndex(
                name: "IX_Testimonials_TenantId",
                table: "Testimonials");

            migrationBuilder.DropIndex(
                name: "IX_SiteSettings_TenantId_Key",
                table: "SiteSettings");

            migrationBuilder.DropIndex(
                name: "IX_Programs_TenantId",
                table: "Programs");

            migrationBuilder.DropIndex(
                name: "IX_News_TenantId",
                table: "News");

            migrationBuilder.DropIndex(
                name: "IX_MenuItems_TenantId",
                table: "MenuItems");

            migrationBuilder.DropIndex(
                name: "IX_HeroSlides_TenantId",
                table: "HeroSlides");

            migrationBuilder.DropIndex(
                name: "IX_Faqs_TenantId",
                table: "Faqs");

            migrationBuilder.DropIndex(
                name: "IX_ContactMessages_TenantId",
                table: "ContactMessages");

            migrationBuilder.DropIndex(
                name: "IX_Applications_TenantId",
                table: "Applications");

            migrationBuilder.DropIndex(
                name: "IX_AdminUsers_TenantId",
                table: "AdminUsers");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Tours");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Testimonials");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Programs");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "News");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "HeroSlides");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Faqs");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "ContactMessages");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "AdminUsers");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "AdminUsers");

            migrationBuilder.AlterColumn<string>(
                name: "Key",
                table: "SiteSettings",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.InsertData(
                table: "AdminUsers",
                columns: new[] { "Id", "PasswordHash", "Username" },
                values: new object[] { 1, "$2a$11$p3udSULwRe/31khdZJjp4uLWcdeK21zdj/DJMl6XZ0x0RHfZ4snDi", "admin" });

            migrationBuilder.InsertData(
                table: "Faqs",
                columns: new[] { "Id", "Answer", "IsVisible", "Order", "Question" },
                values: new object[,]
                {
                    { 1, "Fill out our online application form on the Apply Now page. Our team will review your application and get back to you within 3-5 business days.", true, 1, "How do I apply to volunteer?" },
                    { 2, "The minimum age is 18 years old for most programs.", true, 2, "What is the minimum age to volunteer?" },
                    { 3, "We offer flexible durations from 2 weeks to 3 months.", true, 3, "How long can I volunteer?" },
                    { 4, "The fee includes accommodation, meals, airport pickup, orientation, program placement, 24/7 local support, and a certificate of completion.", true, 4, "What is included in the program fee?" },
                    { 5, "No, English is widely spoken in our programs.", true, 5, "Do I need to speak Nepali?" }
                });

            migrationBuilder.InsertData(
                table: "HeroSlides",
                columns: new[] { "Id", "Badge", "Highlight", "ImageUrl", "IsVisible", "Order", "Subtitle", "Title" },
                values: new object[,]
                {
                    { 1, "Volunteer in Nepal", "Change a Life.", null, true, 1, "Diyalo connects passionate volunteers with communities that need them most.", "Light the Way." },
                    { 2, "Make an Impact", "Nepal.", null, true, 2, "Join our construction and community development programs across Nepal.", "Help Rebuild" },
                    { 3, "Teach & Inspire", "Shape the Future.", null, true, 3, "Volunteer as a teacher and give children the gift of education.", "Educate a Child." }
                });

            migrationBuilder.InsertData(
                table: "MenuItems",
                columns: new[] { "Id", "IsVisible", "Label", "Order", "Url" },
                values: new object[,]
                {
                    { 1, true, "Home", 1, "/" },
                    { 2, true, "About Us", 2, "/about" },
                    { 3, true, "Our Impact", 3, "/our-impact" },
                    { 4, true, "Locations", 4, "/locations" },
                    { 5, true, "Fees", 5, "/fees" },
                    { 6, true, "FAQs", 6, "/faqs" },
                    { 7, true, "News", 7, "/news" },
                    { 8, true, "Apply Now", 8, "/apply" },
                    { 9, true, "Contact Us", 9, "/contact" }
                });

            migrationBuilder.InsertData(
                table: "SiteSettings",
                columns: new[] { "Id", "Key", "Value" },
                values: new object[,]
                {
                    { 1, "siteName", "Diyalo" },
                    { 2, "address", "Kathmandu, Nepal" },
                    { 3, "phone", "+977 9800000000" },
                    { 4, "email", "contact@diyalo.org" },
                    { 5, "facebook", "https://facebook.com" },
                    { 6, "instagram", "https://instagram.com" },
                    { 7, "linkedin", "https://linkedin.com" },
                    { 8, "youtube", "https://youtube.com" },
                    { 9, "tiktok", "https://tiktok.com" },
                    { 10, "officeHours", "Sun – Fri: 9am – 5pm" }
                });
        }
    }
}
