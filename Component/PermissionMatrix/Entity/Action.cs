using System;

namespace Sencilla.Component.Security.Entity
{
    [Flags]
    public enum Action : ulong
    {
        None = 0,
        Read = 1,
        Update = 2,
        Create = 4,
        Remove = 8,
        Delete = 16,

        All = ulong.MaxValue
    }
}
