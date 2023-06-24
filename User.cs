using System;
using System.Collections.Generic;

namespace TroveSkip
{
    public class User
    {
        public readonly int Id;
        public readonly Dictionary<UserAccess, bool> Accesses;

        public User()
        {
            Accesses = new Dictionary<UserAccess, bool>();
            foreach (var value in Enum.GetValues(typeof(UserAccess)))
            {
                Accesses.Add((UserAccess)value, false);
            }
        }
        // public User(int id, UserAccess access)
        // {
        //     Id = id;
        //     Access = access;
        // }
    }
}