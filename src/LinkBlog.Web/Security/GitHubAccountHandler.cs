using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.Extensions.Options;

namespace LinkBlog.Web.Security;

public sealed class GitHubAccountHandler : OAuthHandler<GitHubAccountOptions>
{
    public GitHubAccountHandler(IOptionsMonitor<GitHubAccountOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }
}

public sealed class GitHubAccountOptions : OAuthOptions
{
    public GitHubAccountOptions()
    {
        CallbackPath = new PathString("/signin-microsoft");
        AuthorizationEndpoint = GitHubAccountDefaults.AuthorizationEndpoint;
        TokenEndpoint = GitHubAccountDefaults.TokenEndpoint;
        UserInformationEndpoint = GitHubAccountDefaults.UserInformationEndpoint;
        UsePkce = false; // GitHub doesn't support PKCE

        Scope.Add("read:user");
    }
}

public static class GitHubAccountDefaults
{
    public const string AuthenticationScheme = "GitHub";

    public static readonly string DisplayName = "GitHub";

    public static readonly string AuthorizationEndpoint = "https://github.com/login/oauth/authorize";
 
    public static readonly string TokenEndpoint = "https://github.com/login/oauth/access_token";

    public static readonly string UserInformationEndpoint = "https://api.github.com/user";
}

public static class GitHubAccountExtensions
{
    public static AuthenticationBuilder AddGitHubAccount(this AuthenticationBuilder builder)
        => builder.AddOAuth<GitHubAccountOptions, GitHubAccountHandler>(GitHubAccountDefaults.AuthenticationScheme, GitHubAccountDefaults.DisplayName, _ => { });

    public static AuthenticationBuilder AddGitHubAccount(this AuthenticationBuilder builder, Action<GitHubAccountOptions> configureOptions)
        => builder.AddOAuth<GitHubAccountOptions, GitHubAccountHandler>(GitHubAccountDefaults.AuthenticationScheme, GitHubAccountDefaults.DisplayName, configureOptions);

    public static AuthenticationBuilder AddGitHubAccount(this AuthenticationBuilder builder, string authenticationScheme, string displayName, Action<GitHubAccountOptions> configureOptions)
        => builder.AddOAuth<GitHubAccountOptions, GitHubAccountHandler>(authenticationScheme, displayName, configureOptions);
}