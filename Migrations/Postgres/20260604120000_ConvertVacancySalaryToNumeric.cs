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
            // Migration is a no-op: new databases already have SalaryMin/SalaryMax as numeric(18,2)
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Migration is a no-op: new databases already have SalaryMin/SalaryMax as numeric(18,2)
        }
    }
}
