using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using TaskManager.Api.Data;
using TaskManager.Api.DTOs;

namespace TaskManager.Tests.Integration;

/// <summary>Hosts the real API in memory with an InMemory database and seeded demo data.</summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Database:Provider", "InMemory");
        builder.UseSetting("Database:Name", $"it-{Guid.NewGuid()}");
        builder.UseSetting("Jwt:Key", "integration-tests-secret-key-at-least-32-chars");
        builder.UseSetting("Swagger:Enabled", "true");
    }

    public async Task<HttpClient> ClientAsAsync(string email)
    {
        var client = CreateClient();
        var res = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, DbSeeder.DemoPassword));
        res.EnsureSuccessStatusCode();
        var auth = await res.Content.ReadFromJsonAsync<AuthResponse>(Json);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);
        return client;
    }
}
