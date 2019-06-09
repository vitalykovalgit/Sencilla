using System;

namespace Sencilla.Component.Security.Entity
{
    [Flags]
    public enum Area
    {
        None = 0,
        WholeApp = 1, 
        CurrentUser = 2,
    }
}
