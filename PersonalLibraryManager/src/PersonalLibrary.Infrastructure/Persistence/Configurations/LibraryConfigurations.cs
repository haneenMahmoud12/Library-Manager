using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonalLibrary.Domain.Libraries;
using PersonalLibrary.Domain.Libraries.Enums;
using PersonalLibrary.Infrastructure.Identity;

namespace PersonalLibrary.Infrastructure.Persistence.Configurations;

public sealed class LibraryConfiguration : IEntityTypeConfiguration<Library>
{
    public void Configure(EntityTypeBuilder<Library> builder)
    {
        builder.ToTable("Libraries");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.Visibility)
            .HasDefaultValue(LibraryVisibility.Private).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.HasIndex(x => x.CreatedByUserId);

        builder.HasOne<ApplicationUser>().WithMany()
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class LibraryMemberConfiguration : IEntityTypeConfiguration<LibraryMember>
{
    public void Configure(EntityTypeBuilder<LibraryMember> builder)
    {
        builder.ToTable("LibraryMembers");
        builder.HasKey(x => new { x.LibraryId, x.UserId });
        builder.Property(x => x.Role).IsRequired();
        builder.Property(x => x.JoinedAt).IsRequired();
        builder.HasIndex(x => x.UserId);

        builder.HasOne(x => x.Library).WithMany(x => x.Members)
            .HasForeignKey(x => x.LibraryId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<ApplicationUser>().WithMany()
            .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class LibraryLocationConfiguration
    : IEntityTypeConfiguration<LibraryLocation>
{
    public void Configure(EntityTypeBuilder<LibraryLocation> builder)
    {
        builder.ToTable("LibraryLocations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.HasIndex(x => x.LibraryId);
        builder.HasIndex(x => x.ParentLocationId);
        builder.HasIndex(x => new { x.LibraryId, x.Name });

        builder.HasOne(x => x.Library).WithMany(x => x.Locations)
            .HasForeignKey(x => x.LibraryId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.ParentLocation).WithMany(x => x.ChildLocations)
            .HasForeignKey(x => x.ParentLocationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class BookCopyConfiguration : IEntityTypeConfiguration<BookCopy>
{
    public void Configure(EntityTypeBuilder<BookCopy> builder)
    {
        builder.ToTable("BookCopies");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Format).IsRequired();
        builder.Property(x => x.ReadingStatus).IsRequired();
        builder.Property(x => x.AcquisitionDate).HasColumnType("date");
        builder.Property(x => x.PurchasePrice).HasPrecision(12, 2);
        builder.Property(x => x.CurrencyCode).HasMaxLength(3);
        builder.Property(x => x.IsSigned).HasDefaultValue(false).IsRequired();
        builder.Property(x => x.IsFavourite).HasDefaultValue(false).IsRequired();
        builder.Property(x => x.PersonalNotes).HasColumnType("nvarchar(max)");
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.HasIndex(x => x.LibraryId);
        builder.HasIndex(x => x.BookEditionId);
        builder.HasIndex(x => x.LocationId);
        builder.HasIndex(x => x.AddedByUserId);
        builder.HasIndex(x => new { x.LibraryId, x.BookEditionId });

        builder.HasOne(x => x.Library).WithMany(x => x.BookCopies)
            .HasForeignKey(x => x.LibraryId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.BookEdition).WithMany()
            .HasForeignKey(x => x.BookEditionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Location).WithMany(x => x.BookCopies)
            .HasForeignKey(x => x.LocationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany()
            .HasForeignKey(x => x.AddedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
