using Microsoft.AspNetCore.Authentication;

namespace LinkBlog.Web.Security;

public static class GitHubAccountExtensions
{
    public static AuthenticationBuilder AddGitHubAccount(this AuthenticationBuilder builder)
        => builder.AddOAuth<GitHubAccountOptions, GitHubAccountHandler>(GitHubAccountDefaults.AuthenticationScheme, GitHubAccountDefaults.DisplayName, _ => { });

    public static AuthenticationBuilder AddGitHubAccount(this AuthenticationBuilder builder, Action<GitHubAccountOptions> configureOptions)
        => builder.AddOAuth<GitHubAccountOptions, GitHubAccountHandler>(GitHubAccountDefaults.AuthenticationScheme, GitHubAccountDefaults.DisplayName, configureOptions);

    public static AuthenticationBuilder AddGitHubAccount(this AuthenticationBuilder builder, string authenticationScheme, string displayName, Action<GitHubAccountOptions> configureOptions)
        => builder.AddOAuth<GitHubAccountOptions, GitHubAccountHandler>(authenticationScheme, displayName, configureOptions);
}