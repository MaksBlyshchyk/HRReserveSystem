using System;
using HRReserveSystem.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRReserveSystem.Migrations.Postgres
{
    [DbContext(typeof(PostgresApplicationDbContext))]
    [Migration("20260604120000_ConvertVacancySalaryToNumeric")]
    public partial class ConvertVacancySalaryToNumeric : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add temporary numeric columns
            migrationBuilder.Sql(@"ALTER TABLE ""Vacancies"" ADD COLUMN ""SalaryMin_tmp"" numeric(18,2);");
            migrationBuilder.Sql(@"ALTER TABLE ""Vacancies"" ADD COLUMN ""SalaryMax_tmp"" numeric(18,2);");

            // Populate temporary columns by stripping non-numeric chars and converting commas to dots
            migrationBuilder.Sql(@"
UPDATE ""Vacancies""
SET ""SalaryMin_tmp"" = NULLIF(regexp_replace(replace(""SalaryMin"", ',', '.'), '[^0-9.-]', '', 'g'), '')::numeric
WHERE ""SalaryMin"" IS NOT NULL;
");

            migrationBuilder.Sql(@"
UPDATE ""Vacancies""
SET ""SalaryMax_tmp"" = NULLIF(regexp_replace(replace(""SalaryMax"", ',', '.'), '[^0-9.-]', '', 'g'), '')::numeric
WHERE ""SalaryMax"" IS NOT NULL;
");

            // Replace NULLs in tmp columns with 0 to satisfy NOT NULL if needed
            migrationBuilder.Sql(@"UPDATE ""Vacancies"" SET ""SalaryMin_tmp"" = 0 WHERE ""SalaryMin_tmp"" IS NULL;");
            migrationBuilder.Sql(@"UPDATE ""Vacancies"" SET ""SalaryMax_tmp"" = 0 WHERE ""SalaryMax_tmp"" IS NULL;");

            // Drop old columns and rename tmp columns
            migrationBuilder.Sql(@"ALTER TABLE ""Vacancies"" DROP COLUMN ""SalaryMin"";");
            migrationBuilder.Sql(@"ALTER TABLE ""Vacancies"" DROP COLUMN ""SalaryMax"";");
            migrationBuilder.Sql(@"ALTER TABLE ""Vacancies"" RENAME COLUMN ""SalaryMin_tmp"" TO ""SalaryMin"";");
            migrationBuilder.Sql(@"ALTER TABLE ""Vacancies"" RENAME COLUMN ""SalaryMax_tmp"" TO ""SalaryMax"";");

            // Ensure NOT NULL and check constraint
            migrationBuilder.Sql(@"ALTER TABLE ""Vacancies"" ALTER COLUMN ""SalaryMin"" SET NOT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE ""Vacancies"" ALTER COLUMN ""SalaryMax"" SET NOT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE ""Vacancies"" DROP CONSTRAINT IF EXISTS ""CK_Vacancies_SalaryRange"";");
            migrationBuilder.Sql(@"ALTER TABLE ""Vacancies"" ADD CONSTRAINT ""CK_Vacancies_SalaryRange"" CHECK (""SalaryMax"" >= ""SalaryMin"");");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse: convert numeric back to text columns
            migrationBuilder.Sql(@"ALTER TABLE ""Vacancies"" ADD COLUMN ""SalaryMin_txt"" text;");
            migrationBuilder.Sql(@"ALTER TABLE ""Vacancies"" ADD COLUMN ""SalaryMax_txt"" text;");

            migrationBuilder.Sql(@"UPDATE ""Vacancies"" SET ""SalaryMin_txt"" = COALESCE(""SalaryMin""::text, '');");
            migrationBuilder.Sql(@"UPDATE ""Vacancies"" SET ""SalaryMax_txt"" = COALESCE(""SalaryMax""::text, '');");

            migrationBuilder.Sql(@"ALTER TABLE ""Vacancies"" DROP COLUMN ""SalaryMin"";");
            migrationBuilder.Sql(@"ALTER TABLE ""Vacancies"" DROP COLUMN ""SalaryMax"";");

            migrationBuilder.Sql(@"ALTER TABLE ""Vacancies"" RENAME COLUMN ""SalaryMin_txt"" TO ""SalaryMin"";");
            migrationBuilder.Sql(@"ALTER TABLE ""Vacancies"" RENAME COLUMN ""SalaryMax_txt"" TO ""SalaryMax"";");

            // Recreate prior check constraint (uses CAST if needed)
            migrationBuilder.Sql(@"ALTER TABLE ""Vacancies"" DROP CONSTRAINT IF EXISTS ""CK_Vacancies_SalaryRange"";");
            migrationBuilder.Sql(@"ALTER TABLE ""Vacancies"" ADD CONSTRAINT ""CK_Vacancies_SalaryRange"" CHECK (CAST(""SalaryMax"" AS REAL) >= CAST(""SalaryMin"" AS REAL));");
        }
    }
}
