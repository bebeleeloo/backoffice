using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Broker.Backoffice.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExtendClientsAndCountries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Create Countries table
            migrationBuilder.CreateTable(
                name: "Countries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Iso2 = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    Iso3 = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Countries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Countries_Iso2",
                table: "Countries",
                column: "Iso2",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Countries_Iso3",
                table: "Countries",
                column: "Iso3",
                unique: true,
                filter: "[Iso3] IS NOT NULL");

            // 2. Add new columns to Clients
            migrationBuilder.AddColumn<Guid>(
                name: "ResidenceCountryId",
                table: "Clients",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CitizenshipCountryId",
                table: "Clients",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaritalStatus",
                table: "Clients",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Education",
                table: "Clients",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Ssn",
                table: "Clients",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PassportNumber",
                table: "Clients",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DriverLicenseNumber",
                table: "Clients",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            // 3. Migrate existing string country data to FK references
            // Insert distinct country codes from existing data into Countries table
            migrationBuilder.Sql(@"
                INSERT INTO Countries (Id, Iso2, Name, IsActive)
                SELECT NEWID(), v.Code, v.Code, 1
                FROM (
                    SELECT DISTINCT ResidenceCountry AS Code FROM Clients WHERE ResidenceCountry IS NOT NULL
                    UNION
                    SELECT DISTINCT CitizenshipCountry FROM Clients WHERE CitizenshipCountry IS NOT NULL
                    UNION
                    SELECT DISTINCT Country FROM ClientAddresses WHERE Country IS NOT NULL
                ) v
                WHERE NOT EXISTS (SELECT 1 FROM Countries c WHERE c.Iso2 = v.Code);
            ");

            // Map existing ResidenceCountry strings to FK IDs
            migrationBuilder.Sql(@"
                UPDATE cl SET cl.ResidenceCountryId = c.Id
                FROM Clients cl INNER JOIN Countries c ON cl.ResidenceCountry = c.Iso2
                WHERE cl.ResidenceCountry IS NOT NULL;
            ");

            // Map existing CitizenshipCountry strings to FK IDs
            migrationBuilder.Sql(@"
                UPDATE cl SET cl.CitizenshipCountryId = c.Id
                FROM Clients cl INNER JOIN Countries c ON cl.CitizenshipCountry = c.Iso2
                WHERE cl.CitizenshipCountry IS NOT NULL;
            ");

            // Drop old string country columns from Clients
            migrationBuilder.DropColumn(
                name: "ResidenceCountry",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "CitizenshipCountry",
                table: "Clients");

            // Add FK indexes and constraints for Clients country columns
            migrationBuilder.CreateIndex(
                name: "IX_Clients_ResidenceCountryId",
                table: "Clients",
                column: "ResidenceCountryId");

            migrationBuilder.CreateIndex(
                name: "IX_Clients_CitizenshipCountryId",
                table: "Clients",
                column: "CitizenshipCountryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Clients_Countries_ResidenceCountryId",
                table: "Clients",
                column: "ResidenceCountryId",
                principalTable: "Countries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Clients_Countries_CitizenshipCountryId",
                table: "Clients",
                column: "CitizenshipCountryId",
                principalTable: "Countries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // 4. Replace Country string with CountryId FK in ClientAddresses
            migrationBuilder.AddColumn<Guid>(
                name: "CountryId",
                table: "ClientAddresses",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: Guid.Empty);

            // Map existing Country strings to FK IDs
            migrationBuilder.Sql(@"
                UPDATE ca SET ca.CountryId = c.Id
                FROM ClientAddresses ca INNER JOIN Countries c ON ca.Country = c.Iso2;
            ");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "ClientAddresses");

            migrationBuilder.CreateIndex(
                name: "IX_ClientAddresses_CountryId",
                table: "ClientAddresses",
                column: "CountryId");

            migrationBuilder.AddForeignKey(
                name: "FK_ClientAddresses_Countries_CountryId",
                table: "ClientAddresses",
                column: "CountryId",
                principalTable: "Countries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // 5. Create InvestmentProfiles table
            migrationBuilder.CreateTable(
                name: "InvestmentProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Objective = table.Column<int>(type: "int", nullable: true),
                    RiskTolerance = table.Column<int>(type: "int", nullable: true),
                    LiquidityNeeds = table.Column<int>(type: "int", nullable: true),
                    TimeHorizon = table.Column<int>(type: "int", nullable: true),
                    Knowledge = table.Column<int>(type: "int", nullable: true),
                    Experience = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvestmentProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvestmentProfiles_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InvestmentProfiles_ClientId",
                table: "InvestmentProfiles",
                column: "ClientId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InvestmentProfiles");

            // Restore ClientAddresses.Country string column
            migrationBuilder.DropForeignKey(
                name: "FK_ClientAddresses_Countries_CountryId",
                table: "ClientAddresses");

            migrationBuilder.DropIndex(
                name: "IX_ClientAddresses_CountryId",
                table: "ClientAddresses");

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "ClientAddresses",
                type: "nvarchar(2)",
                maxLength: 2,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(@"
                UPDATE ca SET ca.Country = c.Iso2
                FROM ClientAddresses ca INNER JOIN Countries c ON ca.CountryId = c.Id;
            ");

            migrationBuilder.DropColumn(
                name: "CountryId",
                table: "ClientAddresses");

            // Restore Clients country string columns
            migrationBuilder.DropForeignKey(
                name: "FK_Clients_Countries_ResidenceCountryId",
                table: "Clients");

            migrationBuilder.DropForeignKey(
                name: "FK_Clients_Countries_CitizenshipCountryId",
                table: "Clients");

            migrationBuilder.DropIndex(
                name: "IX_Clients_ResidenceCountryId",
                table: "Clients");

            migrationBuilder.DropIndex(
                name: "IX_Clients_CitizenshipCountryId",
                table: "Clients");

            migrationBuilder.AddColumn<string>(
                name: "ResidenceCountry",
                table: "Clients",
                type: "nvarchar(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CitizenshipCountry",
                table: "Clients",
                type: "nvarchar(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE cl SET cl.ResidenceCountry = c.Iso2
                FROM Clients cl INNER JOIN Countries c ON cl.ResidenceCountryId = c.Id;
                UPDATE cl SET cl.CitizenshipCountry = c.Iso2
                FROM Clients cl INNER JOIN Countries c ON cl.CitizenshipCountryId = c.Id;
            ");

            migrationBuilder.DropColumn(name: "ResidenceCountryId", table: "Clients");
            migrationBuilder.DropColumn(name: "CitizenshipCountryId", table: "Clients");
            migrationBuilder.DropColumn(name: "MaritalStatus", table: "Clients");
            migrationBuilder.DropColumn(name: "Education", table: "Clients");
            migrationBuilder.DropColumn(name: "Ssn", table: "Clients");
            migrationBuilder.DropColumn(name: "PassportNumber", table: "Clients");
            migrationBuilder.DropColumn(name: "DriverLicenseNumber", table: "Clients");

            migrationBuilder.DropTable(
                name: "Countries");
        }
    }
}
