using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

[assembly: FunctionsStartup(typeof(GeneratorFunction.Startup))]

namespace GeneratorFunction;

public class Startup : FunctionsStartup
{
    public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
    {
    }

    public override void Configure(IFunctionsHostBuilder builder)
    {
        builder.Services.AddSingleton<ArmTokenCache>();
        builder.Services.AddSingleton<NameBuilder>();
        builder.Services.AddHttpClient();
        builder.Services.AddSingleton<TemplateDeployer>();
        builder.Services.AddSingleton(new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        });
        builder.Services.AddSingleton<PinManager>();
        builder.Services.AddSingleton<MinecraftUserValidator>();
    }
}
