using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;

namespace LinkBlog.Web.Security;

public sealed class GitHubAccountOptions : OAuthOptions
{
    public GitHubAccountOptions()
    {
        CallbackPath = new PathString("/signin-github");
        AuthorizationEndpoint = GitHubAccountDefaults.AuthorizationEndpoint;
        TokenEndpoint = GitHubAccountDefaults.TokenEndpoint;
        UserInformationEndpoint = GitHubAccountDefaults.UserInformationEndpoint;
        UsePkce = false; // GitHub doesn't support PKCE

        Scope.Add("read:user");

        ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
        ClaimActions.MapJsonKey(ClaimTypes.Name, "name");
        ClaimActions.MapJsonKey(ClaimTypes.Email, "email");
    }
}