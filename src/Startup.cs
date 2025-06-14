namespace Miniblog.Core;

using JavaScriptEngineSwitcher.Extensions.MsDependencyInjection;
using JavaScriptEngineSwitcher.V8;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

using Miniblog.Core.Services;

using System;

using WebMarkupMin.AspNetCoreLatest;
using WebMarkupMin.Core;

using WilderMinds.MetaWeblog;

using IWmmLogger = WebMarkupMin.Core.Loggers.ILogger;
using MetaWeblogService = Services.MetaWeblogService;
using WmmAspNetCoreLogger = WebMarkupMin.AspNetCoreLatest.AspNetCoreLogger;

public class Startup(IConfiguration configuration)
{
    public IConfiguration Configuration { get; } = configuration;

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(
                webBuilder =>
                {
                    _ = webBuilder
                        .UseStartup<Startup>()
                        .ConfigureKestrel(options => options.AddServerHeader = false);
                });

    public static void Main(string[] args) => CreateHostBuilder(args).Build().Run();

    /// <summary>
    /// Configures the specified application.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <param name="env">The Web host environment.</param>
    /// <remarks>This method gets called by the runtime. Use this method to configure the HTTP request pipeline.</remarks>
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            _ = app.UseDeveloperExceptionPage();
        }
        else
        {
            _ = app.UseExceptionHandler("/Shared/Error");
            _ = app.UseHsts();
        }

        _ = app.Use(
            (context, next) =>
            {
                context.Response.Headers.XContentTypeOptions = "nosniff";
                return next();
            });

        _ = app.UseStatusCodePagesWithReExecute("/Shared/Error");
        _ = app.UseWebOptimizer();

        _ = app.UseStaticFilesWithCache();

        if (this.Configuration.GetValue<bool>("forcessl"))
        {
            _ = app.UseHttpsRedirection();
        }

        if (this.Configuration.GetValue<bool>("forceWwwPrefix"))
        {
            _ = app.UseRewriter(new RewriteOptions().AddRedirectToWwwPermanent());
        }

        _ = app.UseMetaWeblog("/metaweblog");
        _ = app.UseAuthentication();

        _ = app.UseWebMarkupMin();

        _ = app.UseRouting();

        _ = app.UseAuthorization();

        _ = app.UseEndpoints(
            endpoints =>
            {
                _ = endpoints.MapControllerRoute("default", "{controller=Blog}/{action=Index}/{id?}");
            });
    }

    /// <summary>
    /// Configures the dependency injection services container.
    /// </summary>
    /// <param name="services">The services.</param>
    /// <remarks>This method gets called by the runtime. Use this method to add services to the container.</remarks>
    public void ConfigureServices(IServiceCollection services)
    {
        _ = services.AddControllersWithViews();
        _ = services.AddRazorPages();

        _ = services.AddSingleton<IUserServices, BlogUserServices>();
        _ = services.AddSingleton<IBlogService, FileBlogService>();
        _ = services.Configure<BlogSettings>(this.Configuration.GetSection("blog"));
        services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        _ = services.AddMetaWeblog<MetaWeblogService>();

        // Progressive Web Apps https://github.com/madskristensen/WebEssentials.AspNetCore.ServiceWorker
        _ = services.AddProgressiveWebApp(
            new WebEssentials.AspNetCore.Pwa.PwaOptions
            {
                OfflineRoute = "/shared/offline/"
            });

        // Output caching
        _ = services.AddOutputCache(options =>
        {
            options.AddBasePolicy(builder => builder.Cache());
            options.AddPolicy("default", policy => policy
                .Expire(TimeSpan.FromSeconds(3600)));
        });

        // Cookie authentication.
        _ = services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(
                options =>
                {
                    options.LoginPath = "/login/";
                    options.LogoutPath = "/logout/";
                });

        // HTML minification (https://github.com/Taritsyn/WebMarkupMin)
        _ = services.AddSingleton<IWmmLogger, WmmAspNetCoreLogger>(); // Used by HTML minifier
        _ = services
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
        _ = services
            .AddJsEngineSwitcher(
                options => options.DefaultEngineName = V8JsEngine.EngineName)
            .AddV8();
        _ = services.AddWebOptimizer(
            pipeline =>
            {
                _ = pipeline.MinifyJsFiles();
                _ = pipeline
                    .CompileScssFiles()
                    .InlineImages(1);
            });
    }
}
