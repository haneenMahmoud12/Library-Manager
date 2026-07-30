using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonalLibrary.Domain.Loans;
using PersonalLibrary.Domain.Loans.Enums;
using PersonalLibrary.Infrastructure.Identity;

namespace PersonalLibrary.Infrastructure.Persistence.Configurations;

public sealed class LoanConfiguration : IEntityTypeConfiguration<Loan>
{
    public void Configure(EntityTypeBuilder<Loan> builder)
    {
        builder.ToTable("Loans");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.BorrowerName).HasMaxLength(200);
        builder.Property(x => x.BorrowerContact).HasMaxLength(300);
        builder.Property(x => x.LoanedAt).IsRequired();
        builder.Property(x => x.ExpectedReturnDate).HasColumnType("date");
        builder.Property(x => x.Status)
            .HasDefaultValue(LoanStatus.Active).IsRequired();
        builder.Property(x => x.Notes).HasColumnType("nvarchar(max)");
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.HasIndex(x => x.BookCopyId);
        builder.HasIndex(x => x.LentByUserId);
        builder.HasIndex(x => x.BorrowerUserId);
        builder.HasIndex(x => x.Status);

        builder.HasOne(x => x.BookCopy).WithMany(x => x.Loans)
            .HasForeignKey(x => x.BookCopyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany()
            .HasForeignKey(x => x.LentByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany()
            .HasForeignKey(x => x.BorrowerUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
