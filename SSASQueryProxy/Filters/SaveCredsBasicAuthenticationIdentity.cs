using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Web;

namespace SSASQueryProxy.Filters
{
    public class SaveCredsBasicAuthenticationIdentity : GenericIdentity
    {
        public string Password { get; private set; }

        public SaveCredsBasicAuthenticationIdentity(string name, string password) : base(name)
        {
            Password = password;
        }
    }
}