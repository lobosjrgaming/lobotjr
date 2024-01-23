using LobotJR.Data;

namespace LobotJR.Command
{
    /// <summary>
    /// Represents a user's enrollment in an access group.
    /// </summary>
    public class Enrollment : TableObject
    {
        /// <summary>
        /// The accessgroup the user is enrolled in.
        /// </summary>
        public virtual AccessGroup Group { get; set; }
        /// <summary>
        /// The id of the user enrolled.
        /// </summary>
        public string UserId { get; set; }

        public Enrollment() { }

        public Enrollment(AccessGroup group, string userId)
        {
            Group = group;
            UserId = userId;
        }
    }
}
