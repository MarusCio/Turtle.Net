using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Turtle.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCommunityDescriptionAndImg : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PostLikes_AspNetUsers_UserId",
                table: "PostLikes");

            migrationBuilder.DropForeignKey(
                name: "FK_PostLikes_Posts_PostId",
                table: "PostLikes");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Communities",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Communities",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PostLikes_AspNetUsers_UserId",
                table: "PostLikes",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PostLikes_Posts_PostId",
                table: "PostLikes",
                column: "PostId",
                principalTable: "Posts",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PostLikes_AspNetUsers_UserId",
                table: "PostLikes");

            migrationBuilder.DropForeignKey(
                name: "FK_PostLikes_Posts_PostId",
                table: "PostLikes");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Communities");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Communities");

            migrationBuilder.AddForeignKey(
                name: "FK_PostLikes_AspNetUsers_UserId",
                table: "PostLikes",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PostLikes_Posts_PostId",
                table: "PostLikes",
                column: "PostId",
                principalTable: "Posts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
