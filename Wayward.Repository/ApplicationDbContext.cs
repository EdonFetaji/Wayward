using Wayward.Domain;
using Wayward.Domain.DomainModels;
using Wayward.Domain.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Wayward.Domain.DomainModels;
namespace Wayward.Repository;

public class ApplicationDbContext : IdentityDbContext<WaywardUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    public virtual DbSet<Flight> Flights { get; set; }
    public virtual DbSet<WishList> WishLists { get; set; }
    public virtual DbSet<Booking> Bookings { get; set; }
    public virtual DbSet<BookedFlight> BookedFlights { get; set; }
    public virtual DbSet<Seat> Seats{ get; set; }
    public virtual DbSet<EmailMessage> EmailMessages { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 1–1: BookedFlight ↔ Seat (Seat is dependent; FK on Seat.BookedFlightId)
        modelBuilder.Entity<BookedFlight>()
            .HasOne(bf => bf.Seat)
            .WithOne(s => s.BookedFlight)
            .HasForeignKey<Seat>(s => s.BookedFlightId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

    }
}
