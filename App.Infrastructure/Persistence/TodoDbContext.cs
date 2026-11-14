using App.Domain.Todos;
using Microsoft.EntityFrameworkCore;

namespace App.Infrastructure.Persistence;

public sealed class TodoDbContext(DbContextOptions<TodoDbContext> options) : DbContext(options)
{
    public DbSet<Todo> Todos => Set<Todo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var todo = modelBuilder.Entity<Todo>();
        todo.ToTable("Todos");
        todo.HasKey(item => item.Id);
        todo.Property(item => item.Title).IsRequired();
        todo.Property(item => item.Description);
        todo.Property(item => item.IsCompleted).IsRequired();
    }
}
