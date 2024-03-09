using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace SplenSoft.OperatingRoomSoftware.Net;

public partial class OrsDbContext : DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer("User=ors;Password=cOdTQc-RIUn3v_vCu8wDug;Database=defaultdb;Server=brassy-growler-13696.7tt.aws-us-east-1.cockroachlabs.cloud:26257;");
    }

    public OrsDbContext() { }

    public OrsDbContext(string connString)
    {
        Database.SetConnectionString(connString);
    }

    public virtual DbSet<StoredMetaData> StoredMetaData { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}