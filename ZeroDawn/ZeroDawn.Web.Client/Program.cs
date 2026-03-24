using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ZeroDawn.Shared.Services;
using ZeroDawn.Web.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Add device-specific services used by the ZeroDawn.Shared project
builder.Services.AddSingleton<IFormFactor, FormFactor>();

await builder.Build().RunAsync();
