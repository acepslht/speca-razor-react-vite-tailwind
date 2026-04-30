
using Speca.Core.Extentions;
using Speca.Core.Services;

var builder = WebApplication.CreateBuilder(args);

var config = builder.Configuration;
var ApplicationConfig = config.GetSection("Application");

builder.Services.AddRazorPages();
builder.Services.AddControllersWithViews();

//if (builder.Environment.IsDevelopment())
//{
//    builder.Services.AddSpecaReverseProxy(ApplicationConfig);
//}

builder.Services.AddScoped<ViteService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapStaticAssets();

#pragma warning disable ASP0014
app.UseEndpoints(endpoints =>
{
    endpoints.MapRazorPages().WithStaticAssets();
    endpoints.MapDefaultControllerRoute();
    //if (builder.Environment.IsDevelopment())
    //{
    //    endpoints.MapSpecaReverseProxy(true);
    //}
});
#pragma warning restore ASP0014


if (app.Environment.IsDevelopment())
{
    app.UseSpa(spa =>
    {
        string port = ApplicationConfig["vite:server:port"] ?? "5173";
        string https = ApplicationConfig["vite:server:https"] ?? "False";
        string schema = "http";

        if (Convert.ToBoolean(https)) schema = "https";

        spa.Options.SourcePath = "../";
        spa.Options.DevServerPort = Convert.ToInt32(port);
        spa.UseViteDevelopmentServer(scriptName: "dev", schema: schema);
    });
}

app.Run();
