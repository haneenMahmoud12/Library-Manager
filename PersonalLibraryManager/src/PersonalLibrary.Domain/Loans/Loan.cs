using PersonalLibrary.Domain.Common;
using PersonalLibrary.Domain.Libraries;
using PersonalLibrary.Domain.Loans.Enums;

namespace PersonalLibrary.Domain.Loans;

public class Loan : AuditableEntity
{
    public Guid BookCopyId { get; set; }
    public Guid LentByUserId { get; set; }
    public Guid? BorrowerUserId { get; set; }
    public string? BorrowerName { get; set; }
    public string? BorrowerContact { get; set; }
    public DateTimeOffset LoanedAt { get; set; }
    public DateOnly? ExpectedReturnDate { get; set; }
    public DateTimeOffset? ReturnedAt { get; set; }
    public LoanStatus Status { get; set; } = LoanStatus.Active;
    public string? Notes { get; set; }

    public virtual BookCopy BookCopy { get; set; } = null!;
}
