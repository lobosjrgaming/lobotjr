using LobotJR.Data;

namespace LobotJR.Command
{
    /// <summary>
    /// Represents a user's enrollment in an access group.
    /// </summary>
    public class Enrollment : TableObject
    {
        /// <summary>
        /// The id of the accessgroup the user is enrolled in.
        /// </summary>
        public int GroupId { get; set; }
        /// <summary>
        /// The id of the user enrolled.
        /// </summary>
        public string UserId { get; set; }

        public Enrollment(int groupId, string userId)
        {
            GroupId = groupId;
            UserId = userId;
        }
    }
}
