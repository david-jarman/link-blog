using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
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

    protected override async Task<AuthenticationTicket> CreateTicketAsync(ClaimsIdentity identity, AuthenticationProperties properties, OAuthTokenResponse tokens)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, Options.UserInformationEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await Backchannel.SendAsync(request, Context.RequestAborted);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"An error occurred when retrieving GitHub user information ({response.StatusCode}).");
        }

        using (var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Context.RequestAborted)))
        {
            var context = new OAuthCreatingTicketContext(new ClaimsPrincipal(identity), properties, Context, Scheme, Options, Backchannel, tokens, payload.RootElement);
            context.RunClaimActions();
            await Events.CreatingTicket(context);
            return new AuthenticationTicket(context.Principal!, context.Properties, Scheme.Name);
        }
    }
}

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