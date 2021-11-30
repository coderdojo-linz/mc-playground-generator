namespace GeneratorFunction;

using System.Net;
using System.Text.Json;

public class MinecraftUserValidator
{
    private readonly HttpClient httpClient;
    private readonly JsonSerializerOptions jsonOptions;

    public MinecraftUserValidator(IHttpClientFactory factory, JsonSerializerOptions jsonOptions)
    {
        httpClient = factory.CreateClient();
        this.jsonOptions = jsonOptions;
    }

    public async Task<UserData?> GetMinecraftUserData(string name)
    {
        var response = await httpClient.GetAsync($"https://api.mojang.com/users/profiles/minecraft/{name}");
        if (response.StatusCode != HttpStatusCode.OK)
        {
            return null;
        }

        return JsonSerializer.Deserialize<UserData>(await response.Content.ReadAsStringAsync(), jsonOptions);
    }

    public record UserData(
        string Name,
        string Id);
}
