using JavaScriptEngineSwitcher.Extensions.MsDependencyInjection;
using JavaScriptEngineSwitcher.V8;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Miniblog.Core;
using Miniblog.Core.Services;

using System.IO.Compression;

using WebMarkupMin.AspNetCoreLatest;
using WebMarkupMin.Core;

using WilderMinds.MetaWeblog;

using IWmmLogger = WebMarkupMin.Core.Loggers.ILogger;
using MetaWeblogService = Miniblog.Core.Services.MetaWeblogService;
using WmmAspNetCoreLogger = WebMarkupMin.AspNetCoreLatest.AspNetCoreLogger;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

builder.Services.AddSingleton<IUserServices, BlogUserServices>();
builder.Services.AddSingleton<IBlogService, FileBlogService>();
builder.Services.Configure<BlogSettings>(builder.Configuration.GetSection("blog"));
builder.Services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddMetaWeblog<MetaWeblogService>();

// Progressive Web Apps https://github.com/madskristensen/WebEssentials.AspNetCore.ServiceWorker
builder.Services.AddProgressiveWebApp(
    new WebEssentials.AspNetCore.Pwa.PwaOptions
    {
        OfflineRoute = "/shared/offline/"
    });

// Output caching
builder.Services.AddOutputCache(
    options =>
    {
        options.AddBasePolicy(builder => builder.Cache());
        options
            .AddPolicy(
                "default",
                policy => policy.Expire(TimeSpan.FromSeconds(3600)));
    });

// Cookie authentication.
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(
        options =>
        {
            options.LoginPath = "/login/";
            options.LogoutPath = "/logout/";
        });

// HTML minification (https://github.com/Taritsyn/WebMarkupMin)
builder.Services.AddSingleton<IWmmLogger, WmmAspNetCoreLogger>(); // Used by HTML minifier
builder.Services
    .AddWebMarkupMin(
        options =>
        {
            options.AllowMinificationInDevelopmentEnvironment = true;
            options.DisablePoweredByHttpHeaders = true;
        })
    .AddHtmlMinification(
        options =>
        {
            options.MinificationSettings.RemoveOptionalEndTags = false;
            options.MinificationSettings.WhitespaceMinificationMode = WhitespaceMinificationMode.Safe;
        });

// Bundling, minification and Sass transpiration (https://github.com/ligershark/WebOptimizer)
builder.Services
    .AddJsEngineSwitcher(
        options => options.DefaultEngineName = V8JsEngine.EngineName)
    .AddV8();
builder.Services.AddWebOptimizer(
    pipeline =>
    {
        _ = pipeline.MinifyJsFiles();
        _ = pipeline
            .CompileScssFiles()
            .InlineImages(1);
    });

// Compress HTTP response
builder.Services.AddResponseCompression(
    options =>
    {
        options.Providers.Add<BrotliCompressionProvider>();
        options.Providers.Add<GzipCompressionProvider>();
        options.EnableForHttps = true;
        options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(["application/json"]);
    });
builder.Services.Configure<BrotliCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);
builder.Services.Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    _ = app.UseDeveloperExceptionPage();
}
else
{
    _ = app.UseExceptionHandler("/Shared/Error");
    _ = app.UseHsts();
}

app.UseResponseCompression();

app.Use(
    (context, next) =>
    {
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        return next();
    });

app.UseStatusCodePagesWithReExecute("/Shared/Error");
app.UseWebOptimizer();

app.UseStaticFilesWithCache();

if (app.Configuration.GetValue<bool>("forcessl"))
{
    _ = app.UseHttpsRedirection();
}

if (app.Configuration.GetValue<bool>("forceWwwPrefix"))
{
    _ = app.UseRewriter(new RewriteOptions().AddRedirectToWwwPermanent());
}

app.UseMetaWeblog("/metaweblog");
app.UseAuthentication();

app.UseWebMarkupMin();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute("default", "{controller=Blog}/{action=Index}/{id?}");

app.Run();
