namespace LobotJR.Data
{
    /// <summary>
    /// Abstract class that manages Id-based equality for all database data types.
    /// </summary>
    public abstract class TableObject
    {
        /// <summary>
        /// The database id.
        /// </summary>
        public int Id { get; set; }

        public override string ToString()
        {
            return Id.ToString();
        }

        public override bool Equals(object obj)
        {
            if (obj != null && obj.GetType().Equals(GetType()))
            {
                return obj is TableObject other && other.Id == Id;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}
