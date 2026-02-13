using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elumatec.Tijdregistratie.Migrations
{
    /// <inheritdoc />
    public partial class SeedMedewerkersMachines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // seed some default machines
            migrationBuilder.InsertData(
                table: "Machines",
                columns: new[] { "MachineNaam" },
                values: new object[,]
                {
                    { "MachineA" },
                    { "MachineB" },
                    { "MachineC" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Machines",
                keyColumn: "MachineNaam",
                keyValues: new object[] { "MachineA", "MachineB", "MachineC" });
        }
    }
}
