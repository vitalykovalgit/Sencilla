using System;

namespace Sencilla.Component.Security.Entity
{
    [Flags]
    public enum Role : ulong
    {
        Anonymos = 0,
        Root = 1,

        Everyone = ulong.MaxValue,
    }
}
