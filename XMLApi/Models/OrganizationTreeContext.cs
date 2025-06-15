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
                  .HasColumnType("xml"); 
        });
    }
}


public class OrganizationTree
{
    public int Id { get; set; }
    public string? TreeName { get; set; }
    [Column(TypeName = "xml")] 
    public string? TreeData { get; set; } 
}