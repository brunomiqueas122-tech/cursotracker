using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CursoTracker.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPremium : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UnlockedCount",
                table: "RecommendationRuns",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "RevealMonth",
                table: "Profiles",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RevealedInMonth",
                table: "Profiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<List<string>>(
                name: "Topics",
                table: "Profiles",
                type: "text[]",
                nullable: false,
                defaultValue: new List<string>());

            // O texto do perfil antigo vira o primeiro assunto
            migrationBuilder.Sql("""UPDATE "Profiles" SET "Topics" = ARRAY["LookingFor"];""");

            migrationBuilder.DropColumn(
                name: "LookingFor",
                table: "Profiles");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PremiumUntil",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PremiumCodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CodeHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Hint = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    Days = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RedeemedByEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    RedeemedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PremiumCodes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PremiumCodes_CodeHash",
                table: "PremiumCodes",
                column: "CodeHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PremiumCodes");

            migrationBuilder.DropColumn(
                name: "UnlockedCount",
                table: "RecommendationRuns");

            migrationBuilder.DropColumn(
                name: "RevealMonth",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "RevealedInMonth",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "Topics",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "PremiumUntil",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<string>(
                name: "LookingFor",
                table: "Profiles",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");
        }
    }
}
