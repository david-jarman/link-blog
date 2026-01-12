Console.WriteLine("Testing HttpClient with proxy environment variables...");
var httpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
Console.WriteLine($"HTTP_PROXY: {httpProxy}");
Console.WriteLine();

// Test 1: Default HttpClient (should fail - doesn't use credentials)
Console.WriteLine("=== Test 1: Default HttpClient ===");
using var client1 = new HttpClient();
try
{
    var response = await client1.GetAsync("http://google.com");
    Console.WriteLine($"Status Code: {response.StatusCode}");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}

Console.WriteLine();

// Test 2: HttpClient with manually configured proxy including credentials
Console.WriteLine("=== Test 2: HttpClient with WebProxy and credentials ===");
if (!string.IsNullOrEmpty(httpProxy))
{
    var proxyUri = new Uri(httpProxy);
    Console.WriteLine($"Proxy scheme: {proxyUri.Scheme}");
    Console.WriteLine($"Proxy host: {proxyUri.Host}");
    Console.WriteLine($"Proxy port: {proxyUri.Port}");
    Console.WriteLine($"UserInfo present: {!string.IsNullOrEmpty(proxyUri.UserInfo)}");
    Console.WriteLine($"UserInfo length: {proxyUri.UserInfo?.Length ?? 0}");

    // Create proxy URL without credentials (just host:port)
    var proxyAddress = $"{proxyUri.Scheme}://{proxyUri.Host}:{proxyUri.Port}";
    Console.WriteLine($"Proxy address: {proxyAddress}");
    var proxy = new System.Net.WebProxy(proxyAddress);

    string[]? userInfo = null;
    if (!string.IsNullOrEmpty(proxyUri.UserInfo))
    {
        userInfo = proxyUri.UserInfo.Split(':', 2);
        Console.WriteLine($"Split parts: {userInfo.Length}");
        if (userInfo.Length == 2)
        {
            Console.WriteLine($"Username: {userInfo[0].Substring(0, Math.Min(30, userInfo[0].Length))}...");
            Console.WriteLine($"Password length: {userInfo[1].Length}");
            Console.WriteLine($"Password prefix: {userInfo[1].Substring(0, Math.Min(10, userInfo[1].Length))}...");

            var credentials = new System.Net.CredentialCache();
            credentials.Add(new Uri(proxyAddress), "Basic", new System.Net.NetworkCredential(userInfo[0], userInfo[1]));
            proxy.Credentials = credentials;
        }
    }

    var handler = new HttpClientHandler
    {
        Proxy = proxy,
        UseProxy = true,
        PreAuthenticate = true
    };
    using var client2 = new HttpClient(handler);

    try
    {
        var response = await client2.GetAsync("http://google.com");
        Console.WriteLine($"Status Code: {response.StatusCode}");
        Console.WriteLine($"Success: {response.IsSuccessStatusCode}");
        var content = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Content length: {content.Length} bytes");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }

    Console.WriteLine();
    Console.WriteLine("=== Test 3: SocketsHttpHandler ===");
    var socketsHandler = new SocketsHttpHandler
    {
        Proxy = proxy,
        UseProxy = true,
        PreAuthenticate = true
    };
    using var client3 = new HttpClient(socketsHandler);

    try
    {
        var response = await client3.GetAsync("http://google.com");
        Console.WriteLine($"Status Code: {response.StatusCode}");
        Console.WriteLine($"Success: {response.IsSuccessStatusCode}");
        var content = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Content length: {content.Length} bytes");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }

    Console.WriteLine();
    Console.WriteLine("=== Test 4: Manual Proxy-Authorization header ===");
    if (userInfo != null && userInfo.Length == 2)
    {
        var manualProxyHandler = new SocketsHttpHandler
        {
            Proxy = new System.Net.WebProxy(proxyAddress),
            UseProxy = true
        };

        // Manually create the Proxy-Authorization header
        var authBytes = System.Text.Encoding.UTF8.GetBytes($"{userInfo[0]}:{userInfo[1]}");
        var authHeader = Convert.ToBase64String(authBytes);
        Console.WriteLine($"Auth header length: {authHeader.Length}");

        using var client4 = new HttpClient(manualProxyHandler);
        client4.DefaultRequestHeaders.Add("Proxy-Authorization", $"Basic {authHeader}");

        try
        {
            var response = await client4.GetAsync("http://google.com");
            Console.WriteLine($"Status Code: {response.StatusCode}");
            Console.WriteLine($"Success: {response.IsSuccessStatusCode}");
            var content = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Content length: {content.Length} bytes");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
