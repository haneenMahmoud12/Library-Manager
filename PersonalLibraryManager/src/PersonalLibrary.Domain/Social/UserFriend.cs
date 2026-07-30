using PersonalLibrary.Domain.Common;
using PersonalLibrary.Domain.Social.Enums;

namespace PersonalLibrary.Domain.Social;

public class UserFriend : AuditableEntity
{
    public Guid RequesterUserId { get; set; }
    public Guid AddresseeUserId { get; set; }
    public FriendshipStatus Status { get; set; } = FriendshipStatus.Pending;
    public DateTimeOffset RequestedAt { get; set; }
    public DateTimeOffset? RespondedAt { get; set; }
}
