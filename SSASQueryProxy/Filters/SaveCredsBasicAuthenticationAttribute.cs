using System.Security.Claims;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

namespace SSASQueryProxy.Filters
{
    public class SaveCredsBasicAuthenticationAttribute : BasicAuthenticationAttribute
    {
        protected override async Task<IPrincipal> AuthenticateAsync(string userName, string password, CancellationToken cancellationToken)
        {
            // this authentication filter always successfully validates the user, for any given username and password
            // username and password are stored in custom SaveCredsBasicAuthenticationIdentity instance and are later used in query proxying controller
            //  as credentials for the real SSAS server (in constructed query string)
            var identity = new SaveCredsBasicAuthenticationIdentity(userName, password);
            return new GenericPrincipal(identity, null);
        }
    }
}