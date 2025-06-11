using System.ComponentModel.DataAnnotations.Schema;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;



public class OrganizationTreeContext : DbContext
{
    public DbSet<OrganizationTree> OrganizationTrees { get; set; }

    public OrganizationTreeContext(DbContextOptions<OrganizationTreeContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OrganizationTree>(entity =>
        {
            entity.Property(e => e.TreeData)
                  .HasColumnType("xml"); // Explicitly set column type
        });
    }
}


public class OrganizationTree
{
    public int Id { get; set; }
    public string? TreeName { get; set; }
    [Column(TypeName = "xml")] // Określa typ kolumny jako xml w SQL Server
    public string? TreeData { get; set; } // Przechowujemy jako string, ale w bazie będzie xml
}