using PersonalLibrary.Domain.Libraries.Enums;

namespace PersonalLibrary.Domain.Libraries;

public class LibraryMember
{
    public Guid LibraryId { get; set; }
    public Guid UserId { get; set; }
    public LibraryMemberRole Role { get; set; }
    public DateTimeOffset JoinedAt { get; set; }

    public virtual Library Library { get; set; } = null!;
}
