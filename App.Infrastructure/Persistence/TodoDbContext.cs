using App.Domain.Todos;
using App.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace App.Infrastructure.Persistence;

public sealed class TodoDbContext(DbContextOptions<TodoDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Todo> Todos => Set<Todo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var todo = modelBuilder.Entity<Todo>();
        todo.ToTable("Todos");
        todo.HasKey(item => item.Id);
        todo.Property(item => item.Title).IsRequired();
        todo.Property(item => item.Description);
        todo.Property(item => item.IsCompleted).IsRequired();
        todo.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(item => item.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
