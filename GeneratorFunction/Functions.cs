namespace GeneratorFunction;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;

public class Functions
{
    private readonly ArmTokenCache tokenCache;
    private readonly NameBuilder nameBuilder;
    private readonly TemplateDeployer deployer;
    private readonly JsonSerializerOptions jsonOptions;
    private readonly PinManager pinManager;
    private readonly MinecraftUserValidator userValidator;

    public Functions(ArmTokenCache tokenCache, NameBuilder nameBuilder, TemplateDeployer deployer, 
        JsonSerializerOptions jsonOptions, PinManager pinManager, MinecraftUserValidator userValidator)
    {
        this.tokenCache = tokenCache;
        this.nameBuilder = nameBuilder;
        this.deployer = deployer;
        this.jsonOptions = jsonOptions;
        this.pinManager = pinManager;
        this.userValidator = userValidator;
    }

    [FunctionName("Generate")]
    public async Task<IActionResult> Generate(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req, ILogger log)
    {
        using var reader = new StreamReader(req.Body);
        var requestBody = await reader.ReadToEndAsync();
        var parameters = JsonSerializer.Deserialize<GenerateParameters>(requestBody, jsonOptions);
        if (parameters == null || parameters.MinecraftUser == null || parameters.Pin == null)
        {
            return new BadRequestObjectResult(new ProblemDetails
            {
                Type = "https://linz.coderdojo.net/api/errors/invalid-parameters",
                Title = "Invalid parameters",
                Detail = "Minecraft user and/or PIN missing"
            });
        }

        var pin = pinManager.ParsePin(parameters.Pin);
        if (pin == null)
        {
            return new BadRequestObjectResult(new ProblemDetails
            {
                Type = "https://linz.coderdojo.net/api/errors/invalid-parameters",
                Title = "Invalid parameters",
                Detail = "Invalid PIN or invalid PIN signature"
            });
        }

        if (DateTime.UtcNow < pin.Value.NotBefore || DateTime.UtcNow > pin.Value.NotBefore + pin.Value.ValidPeriod)
        {
            return new BadRequestObjectResult(new ProblemDetails
            {
                Type = "https://linz.coderdojo.net/api/errors/invalid-parameters",
                Title = "Invalid parameters",
                Detail = "Given PIN does not allow you to generate playgrounds at this time"
            });
        }

        var userData = await userValidator.GetMinecraftUserData(parameters.MinecraftUser);
        if (userData == null)
        {
            return new BadRequestObjectResult(new ProblemDetails
            {
                Type = "https://linz.coderdojo.net/api/errors/invalid-parameters",
                Title = "Invalid parameters",
                Detail = "Invalid Minecraft user"
            });
        }

        var token = await tokenCache.Acquire();

        var name = nameBuilder.GenerateRandomName();

        log.LogInformation($"Deploying playground {name}");
        var response = await deployer.Deploy(name, parameters.MinecraftUser, token);

        return new OkObjectResult(response);
    }

    [FunctionName("DeploymentStatus")]
    public async Task<IActionResult> DeploymentStatus(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req, ILogger log)
    {
        var deploymentName = req.Query["name"];
        var token = await tokenCache.Acquire();

        var status = await deployer.GetStatus(deploymentName, token);
        if (status == null)
        {
            return new NotFoundResult();
        }

        return new OkObjectResult(status);
    }

    internal record GenerateParameters(
        string MinecraftUser,
        string Pin);

    [FunctionName("CreatePin")]
    public async Task<IActionResult> CreatePin(
        [HttpTrigger(AuthorizationLevel.Admin, "post", Route = null)] HttpRequest req, ILogger log)
    {
        using var reader = new StreamReader(req.Body);
        var requestBody = await reader.ReadToEndAsync();
        var parameters = JsonSerializer.Deserialize<CreatePinParameters>(requestBody, jsonOptions);

        parameters.NotBefore ??= DateTime.UtcNow;
        parameters.ValidPeriod ??= TimeSpan.FromHours(3);
        var pin = pinManager.CreatePin(parameters.NotBefore.Value, parameters.ValidPeriod.Value);

        return new OkObjectResult(new { Pin = pin });
    }

    [FunctionName("ParsePin")]
    public async Task<IActionResult> ParsePin(
        [HttpTrigger(AuthorizationLevel.Admin, "post", Route = null)] HttpRequest req, ILogger log)
    {
        using var reader = new StreamReader(req.Body);
        var requestBody = await reader.ReadToEndAsync();
        var parameters = JsonSerializer.Deserialize<ParsePinParameters>(requestBody, jsonOptions);

        if (parameters == null || parameters.Pin == null)
        {
            return new BadRequestObjectResult(new ProblemDetails
            {
                Type = "https://linz.coderdojo.net/api/errors/invalid-parameters",
                Title = "Invalid parameters",
                Detail = "Missing pin"
            });
        }

        try
        {
            var pin = pinManager.ParsePin(parameters.Pin);
            if (pin == null)
            {
                return new BadRequestObjectResult(new ProblemDetails
                {
                    Type = "https://linz.coderdojo.net/api/errors/invalid-parameters",
                    Title = "Invalid parameters",
                    Detail = "Pin's signature is invalid"
                });
            }

            return new OkObjectResult(new { pin.Value.NotBefore, pin.Value.ValidPeriod });
        }
        catch (FormatException ex)
        {
            return new BadRequestObjectResult(new ProblemDetails
            {
                Type = "https://linz.coderdojo.net/api/errors/invalid-parameters",
                Title = "Invalid parameters",
                Detail = ex.Message
            });
        }
    }

    internal record struct CreatePinParameters(
        DateTime? NotBefore,
        TimeSpan? ValidPeriod);

    internal record ParsePinParameters(
        string? Pin);
}
