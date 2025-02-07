namespace Miniblog.Core
{
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

    public class Startup
    {
        public Startup(IConfiguration configuration) => this.Configuration = configuration;

        public IConfiguration Configuration { get; }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(
                    webBuilder =>
                    {
                        webBuilder
                            .UseStartup<Startup>()
                            .ConfigureKestrel(options => options.AddServerHeader = false);
                    });

        public static void Main(string[] args) => CreateHostBuilder(args).Build().Run();

        /// <remarks>This method gets called by the runtime. Use this method to configure the HTTP request pipeline.</remarks>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Shared/Error");
                app.UseHsts();
            }

            app.Use(
                (context, next) =>
                {
                    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
                    return next();
                });

            app.UseStatusCodePagesWithReExecute("/Shared/Error");
            app.UseWebOptimizer();

            app.UseStaticFilesWithCache();

            if (this.Configuration.GetValue<bool>("forcessl"))
            {
                app.UseHttpsRedirection();
            }

            if (this.Configuration.GetValue<bool>("forceWwwPrefix"))
            {
                app.UseRewriter(new RewriteOptions().AddRedirectToWwwPermanent());
            }

            app.UseMetaWeblog("/metaweblog");
            app.UseAuthentication();

            app.UseWebMarkupMin();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(
                endpoints =>
                {
                    endpoints.MapControllerRoute("default", "{controller=Blog}/{action=Index}/{id?}");
                });
        }

        /// <remarks>This method gets called by the runtime. Use this method to add services to the container.</remarks>
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
            services.AddRazorPages();

            services.AddSingleton<IUserServices, BlogUserServices>();
            services.AddSingleton<IBlogService, FileBlogService>();
            services.Configure<BlogSettings>(this.Configuration.GetSection("blog"));
            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddMetaWeblog<MetaWeblogService>();

            // Progressive Web Apps https://github.com/madskristensen/WebEssentials.AspNetCore.ServiceWorker
            services.AddProgressiveWebApp(
                new WebEssentials.AspNetCore.Pwa.PwaOptions
                {
                    OfflineRoute = "/shared/offline/"
                });

            // Output caching
            services.AddOutputCache(options =>
            {
                options.AddBasePolicy(builder => builder.Cache());
                options.AddPolicy("default", policy => policy
                    .Expire(TimeSpan.FromSeconds(3600)));
            });


            // Cookie authentication.
            services
                .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(
                    options =>
                    {
                        options.LoginPath = "/login/";
                        options.LogoutPath = "/logout/";
                    });

            // HTML minification (https://github.com/Taritsyn/WebMarkupMin)
            services.AddSingleton<IWmmLogger, WmmAspNetCoreLogger>(); // Used by HTML minifier
            services
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
            services.AddJsEngineSwitcher(options =>
               options.DefaultEngineName = V8JsEngine.EngineName
           ).AddV8();
            services.AddWebOptimizer(
                pipeline =>
                {
                    pipeline.MinifyJsFiles();
                    pipeline.CompileScssFiles()
                            .InlineImages(1);
                });
        }
    }
}
