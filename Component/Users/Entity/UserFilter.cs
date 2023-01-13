﻿using Sencilla.Core;

namespace Sencilla.Component.Users
{
    public class UserFilter: Filter
    {
        public UserFilter ByEmail(params string[] emails) 
        {
            AddProperty(nameof(User.Email), emails);
            return this;
        }
    }
}
