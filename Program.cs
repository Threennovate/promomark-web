using PromomarkWeb.Options;
using PromomarkWeb.Configuration;
using Umbraco.Community.BlockPreview.Extensions;
using Microsoft.Extensions.Options;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Register application services
builder.Services.Configure<PromomarkWeb.Services.Email.SmtpOptions>(builder.Configuration.GetSection("Umbraco:CMS:Global:Smtp"));
builder.Services.AddScoped<PromomarkWeb.Services.Email.IEmailService, PromomarkWeb.Services.Email.RazorEmailService>();
builder.Services.Configure<GoogleRecaptchaOptions>(builder.Configuration.GetSection("GoogleRecaptcha"));
builder.Services.AddHttpClient();

builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddComposers()
    .AddBlockPreview(options =>
   {
       options.BlockGrid = new()
       {
           Enabled = true
       };
       options.BlockList = new()
       {
           Enabled = false
       };
   })
    .Build();

builder.Services.AddTransient<IConfigureOptions<StaticFileOptions>, ConfigureStaticFileOptions>();

WebApplication app = builder.Build();

await app.BootUmbracoAsync();


app.UseUmbraco()
    .WithMiddleware(u =>
    {
        u.UseBackOffice();
        u.UseWebsite();
    })
    .WithEndpoints(u =>
    {
        u.UseBackOfficeEndpoints();
        u.UseWebsiteEndpoints();
    });

await app.RunAsync();
