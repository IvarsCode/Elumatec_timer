using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elumatec.Tijdregistratie.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "contactpersonen",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Naam = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    TelefoonNummer = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contactpersonen", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "medewerkers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Naam = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_medewerkers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "interventies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Bedrijfsnaam = table.Column<string>(type: "TEXT", nullable: false),
                    ContactpersoonId = table.Column<int>(type: "INTEGER", nullable: false),
                    Machine = table.Column<string>(type: "TEXT", nullable: false),
                    InterneMedewerkerId = table.Column<int>(type: "INTEGER", nullable: false),
                    DatumRecentsteCall = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AantalCalls = table.Column<int>(type: "INTEGER", nullable: false),
                    TotaleLooptijd = table.Column<int>(type: "INTEGER", nullable: false),
                    InterneNotities = table.Column<string>(type: "TEXT", nullable: true),
                    ExterneNotities = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_interventies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_interventies_contactpersonen_ContactpersoonId",
                        column: x => x.ContactpersoonId,
                        principalTable: "contactpersonen",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_interventies_medewerkers_InterneMedewerkerId",
                        column: x => x.InterneMedewerkerId,
                        principalTable: "medewerkers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "documents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    InterventieId = table.Column<int>(type: "INTEGER", nullable: false),
                    FileName = table.Column<string>(type: "TEXT", nullable: false),
                    FilePath = table.Column<string>(type: "TEXT", nullable: true),
                    Content = table.Column<byte[]>(type: "BLOB", nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_documents_interventies_InterventieId",
                        column: x => x.InterventieId,
                        principalTable: "interventies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_documents_InterventieId",
                table: "documents",
                column: "InterventieId");

            migrationBuilder.CreateIndex(
                name: "IX_interventies_ContactpersoonId",
                table: "interventies",
                column: "ContactpersoonId");

            migrationBuilder.CreateIndex(
                name: "IX_interventies_InterneMedewerkerId",
                table: "interventies",
                column: "InterneMedewerkerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "documents");

            migrationBuilder.DropTable(
                name: "interventies");

            migrationBuilder.DropTable(
                name: "contactpersonen");

            migrationBuilder.DropTable(
                name: "medewerkers");
        }
    }
}
