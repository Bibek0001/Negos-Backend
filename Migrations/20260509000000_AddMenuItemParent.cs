using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diyalo.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddMenuItemParent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add nullable ParentId column
            migrationBuilder.AddColumn<int>(
                name: "ParentId",
                table: "MenuItems",
                type: "int",
                nullable: true);

            // -----------------------------------------------------------------------
            // Seed submenu items for every existing tenant.
            // We look up each parent menu item by TenantId + Url, then insert children.
            // Using raw SQL so we don't need to hard-code IDs.
            // -----------------------------------------------------------------------

            // About Us submenus
            migrationBuilder.Sql(@"
                INSERT INTO MenuItems (TenantId, Label, Url, IsVisible, [Order], ParentId)
                SELECT p.TenantId,
                       v.Label,
                       v.Url,
                       1,
                       v.Ord,
                       p.Id
                FROM MenuItems p
                CROSS JOIN (VALUES
                    (N'Welcome to Diyalo',          N'/about',      1),
                    (N'Why Diyalo?',                N'/about#why',  2),
                    (N'Why Volunteering in Nepal?', N'/our-impact', 3),
                    (N'Get Involved',               N'/apply',      4),
                    (N'Our Team',                   N'/about',      5),
                    (N'Free Volunteering',          N'/fees',       6),
                    (N'Nepali Host Families',       N'/about',      7)
                ) AS v(Label, Url, Ord)
                WHERE p.Url = N'/about' AND p.ParentId IS NULL
            ");

            // Placements submenus
            migrationBuilder.Sql(@"
                INSERT INTO MenuItems (TenantId, Label, Url, IsVisible, [Order], ParentId)
                SELECT p.TenantId,
                       v.Label,
                       v.Url,
                       1,
                       v.Ord,
                       p.Id
                FROM MenuItems p
                CROSS JOIN (VALUES
                    (N'Volunteering',             N'/placements/volunteering',   1),
                    (N'Internship',               N'/placements/internship',     2),
                    (N'Nepal Experience Program', N'/placements/nepal-experience',3),
                    (N'Nepali Language School',   N'/placements/language-school', 4),
                    (N'Summer Volunteer Program', N'/placements/summer-program',  5)
                ) AS v(Label, Url, Ord)
                WHERE p.Url = N'/placements/volunteering' AND p.ParentId IS NULL
            ");

            // Our Impact submenus
            migrationBuilder.Sql(@"
                INSERT INTO MenuItems (TenantId, Label, Url, IsVisible, [Order], ParentId)
                SELECT p.TenantId,
                       v.Label,
                       v.Url,
                       1,
                       v.Ord,
                       p.Id
                FROM MenuItems p
                CROSS JOIN (VALUES
                    (N'Our Impact', N'/our-impact', 1),
                    (N'Locations',  N'/locations',  2)
                ) AS v(Label, Url, Ord)
                WHERE p.Url = N'/our-impact' AND p.ParentId IS NULL
            ");

            // Locations submenus
            migrationBuilder.Sql(@"
                INSERT INTO MenuItems (TenantId, Label, Url, IsVisible, [Order], ParentId)
                SELECT p.TenantId,
                       v.Label,
                       v.Url,
                       1,
                       v.Ord,
                       p.Id
                FROM MenuItems p
                CROSS JOIN (VALUES
                    (N'All Locations', N'/locations',               1),
                    (N'Kathmandu',     N'/locations?city=Kathmandu',2),
                    (N'Pokhara',       N'/locations?city=Pokhara',  3),
                    (N'Rural Nepal',   N'/locations?city=Rural',    4)
                ) AS v(Label, Url, Ord)
                WHERE p.Url = N'/locations' AND p.ParentId IS NULL
            ");

            // Fees submenus
            migrationBuilder.Sql(@"
                INSERT INTO MenuItems (TenantId, Label, Url, IsVisible, [Order], ParentId)
                SELECT p.TenantId,
                       v.Label,
                       v.Url,
                       1,
                       v.Ord,
                       p.Id
                FROM MenuItems p
                CROSS JOIN (VALUES
                    (N'Program Fees',        N'/fees',                  1),
                    (N'What''s Included',    N'/whats-included',        2),
                    (N'Payment & Booking',   N'/payment-booking',       3),
                    (N'Charity Tour & Trek', N'/charity-tour-and-trek', 4)
                ) AS v(Label, Url, Ord)
                WHERE p.Url = N'/fees' AND p.ParentId IS NULL
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove all submenu items (those with a ParentId)
            migrationBuilder.Sql("DELETE FROM MenuItems WHERE ParentId IS NOT NULL");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "MenuItems");
        }
    }
}
