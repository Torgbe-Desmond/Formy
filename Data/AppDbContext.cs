using Formify.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Formify.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Folder> Folders => Set<Folder>();
    public DbSet<SchemaTemplate> SchemaTemplates => Set<SchemaTemplate>();
    public DbSet<SchemaEntry> SchemaEntries => Set<SchemaEntry>();
    public DbSet<AppFile> AppFiles => Set<AppFile>();
    public DbSet<AppFileMetadata> AppFileMetadata => Set<AppFileMetadata>();
    public DbSet<FileContent> FileContents => Set<FileContent>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
            e.HasMany(u => u.Projects)
                .WithOne(p => p.Owner)
                .HasForeignKey(p => p.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Project>(e =>
        {
            e.HasMany(p => p.Folders)
                .WithOne(f => f.Project)
                .HasForeignKey(f => f.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Folder>(e =>
        {
            e.HasOne(f => f.Schema)
                .WithOne(s => s.Folder)
                .HasForeignKey<SchemaTemplate>(s => s.FolderId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(f => f.Files)
                .WithOne(a => a.Folder)
                .HasForeignKey(a => a.FolderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<SchemaTemplate>(e =>
        {
            e.HasIndex(s => s.FolderId).IsUnique();
            e.HasMany(s => s.Schemas)
                .WithOne(en => en.SchemaTemplate)
                .HasForeignKey(en => en.SchemaTemplateId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<SchemaEntry>(e =>
        {
            e.HasIndex(en => new { en.SchemaTemplateId, en.Name }).IsUnique();
            e.Property(en => en.SchemaYaml).HasColumnType("text");
            e.Property(en => en.TemplateHtml).HasColumnType("text");
            e.Property(en => en.TemplateCss).HasColumnType("text");
        });

        b.Entity<AppFile>(e =>
        {
            e.HasIndex(a => a.ContentId);
            e.HasMany(a => a.Metadata)
                .WithOne(m => m.File)
                .HasForeignKey(m => m.FileId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<AppFileMetadata>(e =>
        {
            e.HasIndex(m => new { m.FileId, m.Key }).IsUnique();
            e.Property(m => m.Value).HasColumnType("text");
        });

        b.Entity<FileContent>(e =>
        {
            e.Property(f => f.Content).HasColumnType("text");
        });
    }
}
