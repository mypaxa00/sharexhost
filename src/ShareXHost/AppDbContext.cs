using Microsoft.EntityFrameworkCore;

namespace ShareXHost;

public class 
    AppDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<File> Files => Set<File>();
    public DbSet<Link> Links => Set<Link>();
    public DbSet<ApiToken> ApiTokens => Set<ApiToken>();
    
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.Property(e => e.Id).HasColumnName("id").IsRequired();
            entity.Property(e => e.UserName).HasColumnName("user_name").IsRequired();
            entity.Property(e => e.Name).HasColumnName("name").IsRequired();
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash").IsRequired();
            entity.Property(e => e.Role).HasColumnName("role").IsRequired();
            
            entity.HasIndex(e => e.UserName).IsUnique().HasDatabaseName("users_user_name_key");
        });
        
        modelBuilder.Entity<ApiToken>(entity =>
        {
            entity.ToTable("api_tokens");
            entity.Property(e => e.Id).HasColumnName("id").IsRequired();
            entity.Property(e => e.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(e => e.TokenHash).HasColumnName("token_hash").IsRequired();
            entity.Property(e => e.Name).HasColumnName("name").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();

            entity.HasIndex(e => e.TokenHash).IsUnique().HasDatabaseName("api_tokens_token_hash_key");
            
            entity.HasOne(d => d.User).WithMany(e => e.ApiTokens)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_api_tokens_user");
        });
        
        modelBuilder.Entity<File>(entity =>
        {
            entity.ToTable("files");
            entity.Property(e => e.Id).HasColumnName("id").IsRequired();
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.StoragePath).HasColumnName("storage_path").IsRequired();
            entity.Property(e => e.SizeBytes).HasColumnName("size_bytes").IsRequired();
            entity.Property(e => e.ContentType).HasColumnName("content_type").IsRequired();
            entity.Property(e => e.OriginalFileName).HasColumnName("original_file_name").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(e => e.DeleteToken).HasColumnName("delete_token");

            entity.HasIndex(e => e.DeleteToken).IsUnique().HasDatabaseName("files_delete_token_key");;
            entity.HasIndex(e => e.UserId).HasDatabaseName("idx_files_user_id");
            
            entity.HasOne(d => d.User).WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_files_user");
        });
        
        modelBuilder.Entity<Link>(entity =>
        {
            entity.ToTable("links");
            entity.HasKey(e => e.ShortId);
            entity.Property(e => e.ShortId).HasColumnName("short_id").IsRequired();
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Url).HasColumnName("url").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(e => e.DeleteToken).HasColumnName("delete_token");
        
            entity.HasIndex(e => e.DeleteToken).IsUnique();
            
            entity.HasOne(d => d.User).WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_links_user");
        });
    }
}