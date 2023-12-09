using LobotJR.Data;

namespace LobotJR.Command
{
    /// <summary>
    /// Represents a group that provides certain users access to restricted
    /// commands. Any command that is restricted to an access group can only be
    /// accessed by users that are enrolled in the group(s) that the command is
    /// restricted to.
    /// </summary>
    public class AccessGroup : TableObject
    {
        /// <summary>
        /// The name of the role.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Whether or not this access group automatically includes every user
        /// with the IsAdmin flag set.
        /// </summary>
        public bool IncludeAdmin { get; set; }
        /// <summary>
        /// Whether or not this access group automatically includes every user
        /// with the IsMod flag set.
        /// </summary>
        public bool IncludeMods { get; set; }
        /// <summary>
        /// Whether or not this access group automatically includes every user
        /// with the IsVip flag set.
        /// </summary>
        public bool IncludeVips { get; set; }
        /// <summary>
        /// Whether or not this access group automatically includes every user
        /// with the IsSub flag set.
        /// </summary>
        public bool IncludeSubs { get; set; }
        /// <summary>
        /// A comma-delimited list of user ids.
        /// </summary>

        /// <summary>
        /// Creates an empty user role.
        /// </summary>
        public AccessGroup()
        {
        }

        /// <summary>
        /// Creates a user role with a name.
        /// </summary>
        /// <param name="name">The name of the role.</param>
        public AccessGroup(string name)
        {
            Name = name;
        }
    }
}
