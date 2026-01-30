using PromomaxWeb.Options;
using Umbraco.Community.BlockPreview.Extensions;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Register application services
builder.Services.Configure<PromomaxWeb.Services.Email.SmtpOptions>(builder.Configuration.GetSection("Umbraco:CMS:Global:Smtp"));
builder.Services.AddScoped<PromomaxWeb.Services.Email.IEmailService, PromomaxWeb.Services.Email.RazorEmailService>();
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
