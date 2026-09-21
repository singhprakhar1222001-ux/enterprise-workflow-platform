using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkService.Persistance.Migrations
{
    /// <inheritdoc />
    public partial class ProjectUserNameRemoved : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "ProjectUser");

            migrationBuilder.RenameColumn(
                name: "_role",
                table: "ProjectUser",
                newName: "Role");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "ProjectUser",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserId",
                table: "ProjectUser");

            migrationBuilder.RenameColumn(
                name: "Role",
                table: "ProjectUser",
                newName: "_role");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "ProjectUser",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
