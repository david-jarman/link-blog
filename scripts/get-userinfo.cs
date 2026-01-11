var httpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");

if (string.IsNullOrEmpty(httpProxy))
{
    Environment.Exit(1);
}

var uri = new Uri(httpProxy);

var part = args.Length > 0 ? args[0] : "userinfo";

switch (part)
{
    case "user":
        var userInfo = uri.UserInfo;
        var colonIndex = userInfo.IndexOf(':');
        Console.WriteLine(colonIndex >= 0 ? userInfo[..colonIndex] : userInfo);
        break;
    case "password":
        var info = uri.UserInfo;
        var idx = info.IndexOf(':');
        Console.WriteLine(idx >= 0 ? info[(idx + 1)..] : "");
        break;
    case "url":
        Console.WriteLine($"{uri.Scheme}://{uri.Host}:{uri.Port}");
        break;
    default:
        Console.WriteLine(uri.UserInfo);
        break;
}