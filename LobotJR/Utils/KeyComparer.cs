using System.Collections.Generic;

namespace LobotJR.Utils
{
    public class KeyComparer<T1, T2> : IEqualityComparer<KeyValuePair<T1, T2>>
    {
        public bool Equals(KeyValuePair<T1, T2> x, KeyValuePair<T1, T2> y)
        {
            return x.Key.Equals(y.Key);
        }

        public int GetHashCode(KeyValuePair<T1, T2> obj)
        {
            return obj.Key.GetHashCode();
        }
    }
}
