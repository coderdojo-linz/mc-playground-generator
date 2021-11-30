using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mime;
using System.Text;
using System.Text.Json;

namespace GeneratorFunction;

public class TemplateDeployer
{
    private readonly HttpClient httpClient;
    private readonly IConfiguration configuration;
    private readonly JsonSerializerOptions jsonOptions;

    public TemplateDeployer(IHttpClientFactory factory, IConfiguration configuration, JsonSerializerOptions jsonOptions)
    {
        httpClient = factory.CreateClient();
        this.configuration = configuration;
        this.jsonOptions = jsonOptions;
    }

    public async Task<DeployReponse> Deploy(string playgroundName, string minecraftUser, string token)
    {
        // Build URL and body
        var url = $"https://management.azure.com/subscriptions/{configuration["SubscriptionId"]}/resourcegroups/{configuration["ResourceGroup"]}/providers/Microsoft.Resources/deployments/{playgroundName}?api-version=2021-04-01";
        var body = new CreateDeployment(
            new(
                new("https://cddataexchange.blob.core.windows.net/data-exchange/mc_playground/azuredeploy.json"),
                new(
                    new(playgroundName), 
                    new(minecraftUser),
                    new(configuration["PullPrincipal"]),
                    new(configuration["PullPrincipalSecret"]))));
        var bodyJson = JsonSerializer.Serialize(body, jsonOptions);

        // Build ARM request
        var httpRequestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Put,
            RequestUri = new Uri(url),
            Headers = {
                    { HttpRequestHeader.Authorization.ToString(), $"Bearer {token}" },
                    { HttpRequestHeader.ContentType.ToString(), MediaTypeNames.Application.Json },
                    { HttpRequestHeader.Accept.ToString(), MediaTypeNames.Application.Json },
                },
            Content = new StringContent(bodyJson, Encoding.UTF8, MediaTypeNames.Application.Json)
        };

        // Send ARM request
        var result = await httpClient.SendAsync(httpRequestMessage);
        result.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<DeployReponse>(await result.Content.ReadAsStringAsync(), jsonOptions)!;
    }

    public async Task<DeployReponse?> GetStatus(string deploymentName, string token)
    {
        var url = $"https://management.azure.com/subscriptions/{configuration["SubscriptionId"]}/resourcegroups/{configuration["ResourceGroup"]}/providers/Microsoft.Resources/deployments/{deploymentName}?api-version=2021-04-01";
        var httpRequestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri(url),
            Headers = {
                    { HttpRequestHeader.Authorization.ToString(), $"Bearer {token}" },
                    { HttpRequestHeader.Accept.ToString(), MediaTypeNames.Application.Json },
                }
        };

        var result = await httpClient.SendAsync(httpRequestMessage);
        if (result.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        result.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<DeployReponse>(await result.Content.ReadAsStringAsync(), jsonOptions)!;
    }

    internal record CreateDeployment(
        DeploymentProperties Properties);

    internal record DeploymentProperties(
        TemplateLink TemplateLink,
        Parameters Parameters,
        string Mode = "Incremental");

    internal record Parameters(
        ValueHolder ContainerGroupName,
        ValueHolder McUserName,
        ValueHolder AcrPullPrincipal,
        ValueHolder AcrPullPrincipalSecret);

    internal record ValueHolder(string value);

    internal record TemplateLink(
        string Uri);

    public record DeployReponse(
        string Id,
        string Name,
        DeployProperties Properties);

    public record DeployProperties(
        string ProvisioningState);
}
