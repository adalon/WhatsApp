using System.Text.Json;
using System.Text.Json.Serialization;
using Devlooped;
using Devlooped.WhatsApp;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Google.Apis.Auth.AspNetCore3;
using Microsoft.Azure.Functions.Worker;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker.Http;

var builder = FunctionsApplication.CreateBuilder(args);
builder.ConfigureFunctionsWebApplication();
builder.AddServiceDefaults();

builder.Services
    .AddAuthentication(options =>
    {
        //options.DefaultChallengeScheme = GoogleOpenIdConnectDefaults.AuthenticationScheme;
        //options.DefaultForbidScheme = GoogleOpenIdConnectDefaults.AuthenticationScheme;
        //options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.Cookie.SameSite = SameSiteMode.None; // Required for cross-site OAuth redirects
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Ensure cookies are secure
    })
    .AddGoogleOpenIdConnect(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
        options.CallbackPath = "/api/googlecallback"; // Matches the Function's callback route
        options.SaveTokens = true; // Save access and refresh tokens
    });

#if DEBUG
builder.Environment.EnvironmentName = "Development";
builder.Configuration.AddUserSecrets<Program>();
#endif

if (builder.Environment.IsDevelopment())
{
    // TODO: doesn't seem to work.
    builder.Logging.AddFilter("Devlooped.WhatsApp.AzureFunctions", LogLevel.Trace);
}

builder.Services.AddSingleton(new JsonSerializerOptions(JsonSerializerDefaults.General)
{
    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    Converters =
    {
        new JsonStringEnumConverter()
    },
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = true
});

builder.Services.AddSingleton(services => builder.Environment.IsDevelopment() ?
    CloudStorageAccount.DevelopmentStorageAccount :
    CloudStorageAccount.TryParse(builder.Configuration["App:Storage"] ?? "", out var storage) ?
    storage :
    throw new InvalidOperationException("Missing required App:Storage connection string."));

builder.Services
    .AddWhatsApp<ProcessHandler>(builder.Configuration)
    // Matches what we use in ConfigureOpenTelemetry
    .UseOpenTelemetry(builder.Environment.ApplicationName)
    .UseLogging()
    .UseStorage()
    .UseConversation();

builder.Build().Run();