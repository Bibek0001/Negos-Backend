using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Diyalo.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminControlledContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Faqs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Question = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Answer = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Faqs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HeroSlides",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Badge = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Highlight = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Subtitle = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HeroSlides", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MenuItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Label = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SiteSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tours",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Destination = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Duration = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Difficulty = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tours", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "AdminUsers",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$p3udSULwRe/31khdZJjp4uLWcdeK21zdj/DJMl6XZ0x0RHfZ4snDi");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Faqs");

            migrationBuilder.DropTable(
                name: "HeroSlides");

            migrationBuilder.DropTable(
                name: "MenuItems");

            migrationBuilder.DropTable(
                name: "SiteSettings");

            migrationBuilder.DropTable(
                name: "Tours");

            migrationBuilder.UpdateData(
                table: "AdminUsers",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2uheWG/igi.");
        }
    }
}
