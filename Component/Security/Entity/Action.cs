
namespace Sencilla.Component.Security;

//[Flags]
//public enum Action : ulong
//{
//    Read = 1,
//    Update = 2,
//    Create = 4,
//    Remove = 8,
//    Delete = 16,

//    All = ulong.MaxValue
//}

public enum Action
{
    Read = 1,
    Create = 2,
    Update = 3,
    Delete = 4,

    All = 5,
}
