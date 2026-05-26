using Microsoft.EntityFrameworkCore;

namespace HRReserveSystem.Data;

public class PostgresApplicationDbContext(DbContextOptions<PostgresApplicationDbContext> options)
    : ApplicationDbContext(options);
