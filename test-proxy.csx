using System;
using System.Net.Http;
using System.Threading.Tasks;

Console.WriteLine("Testing HttpClient with proxy environment variables...");
Console.WriteLine($"HTTP_PROXY: {Environment.GetEnvironmentVariable("HTTP_PROXY")}");
Console.WriteLine($"HTTPS_PROXY: {Environment.GetEnvironmentVariable("HTTPS_PROXY")}");
Console.WriteLine();

using var client = new HttpClient();

try
{
    Console.WriteLine("Making request to http://google.com...");
    var response = await client.GetAsync("http://google.com");
    Console.WriteLine($"Status Code: {response.StatusCode}");
    Console.WriteLine($"Success: {response.IsSuccessStatusCode}");
    var content = await response.Content.ReadAsStringAsync();
    Console.WriteLine($"Content length: {content.Length} bytes");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.GetType().Name}");
    Console.WriteLine($"Message: {ex.Message}");
    if (ex.InnerException != null)
    {
        Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
    }
}
