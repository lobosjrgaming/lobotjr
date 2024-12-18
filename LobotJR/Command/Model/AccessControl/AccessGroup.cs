﻿using LobotJR.Data;
using System.Collections.Generic;

namespace LobotJR.Command.Model.AccessControl
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
        /// The name of the access group.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Whether or not this access group automatically includes every user
        /// with the IsAdmin flag set.
        /// </summary>
        public bool IncludeAdmins { get; set; }
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
        /// Collection of all commands restricted to this group.
        /// </summary>
        public virtual List<Restriction> Restrictions { get; set; }
        /// <summary>
        /// Collection of all users enrolled in this group.
        /// </summary>
        public virtual List<Enrollment> Enrollments { get; set; }

        /// <summary>
        /// Creates an empty access group.
        /// </summary>
        public AccessGroup() { }

        /// <summary>
        /// Creates an access group with a name.
        /// </summary>
        /// <param name="name">The name of the group.</param>
        public AccessGroup(int id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}
