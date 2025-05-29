using Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Context;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<Tournament> Tournaments { get; set; }
    public DbSet<TournamentPlayer> TournamentPlayers { get; set; }
    public DbSet<Match> Matches { get; set; }
    public DbSet<Game> Games { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<Status> Status { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
        modelBuilder.Entity<User>().HasIndex(u => u.Username).IsUnique();
        
        modelBuilder.Entity<Match>()
            .HasOne(m => m.PendingWinner)
            .WithMany()
            .HasForeignKey(m => m.PendingWinnerId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Match>().HasOne(m => m.FirstPlayer).WithMany().HasForeignKey(m => m.FirstPlayerId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Match>().HasOne(m => m.SecondPlayer).WithMany().HasForeignKey(m => m.SecondPlayerId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Match>().HasOne(m => m.Winner).WithMany().HasForeignKey(m => m.WinnerId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Role>().HasData(
            new Role {Id = 1, Name = "Admin"},
            new Role {Id = 2, Name = "Player"}
        );
        
        modelBuilder.Entity<Status>().HasData(
            new Status {Id = 1, Name = "En attente"},
            new Status {Id = 2, Name = "En cours"},
            new Status {Id = 3, Name = "Terminé"}
        );
    }
}