using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NewsApp.Data;
using NewsApp.Models;
using System;
using System.Globalization;

namespace NewsApp
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // =========================================================
        // SERVICES
        // =========================================================
        public void ConfigureServices(IServiceCollection services)
        {
            // =========================
            // DATABASE
            // =========================
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"))
            );

            // =========================
            // IDENTITY
            // =========================
            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 6;

                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedAccount = false;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

            // =========================
            // COOKIE CONFIG
            // =========================
            services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Account/Login";
                options.AccessDeniedPath = "/Account/AccessDenied";

                options.Cookie.HttpOnly = true;
                options.ExpireTimeSpan = TimeSpan.FromDays(30);
                options.SlidingExpiration = true;
            });

            // =========================
            // LOCALIZATION (EN + BN)
            // =========================
            services.AddLocalization(options =>
            {
                options.ResourcesPath = "Resources";
            });

            services.Configure<RequestLocalizationOptions>(options =>
            {
                var supportedCultures = new[]
                {
                    new CultureInfo("en"),
                    new CultureInfo("bn")
                };

                options.DefaultRequestCulture = new RequestCulture("en");
                options.SupportedCultures = supportedCultures;
                options.SupportedUICultures = supportedCultures;
            });

            // =========================
            // MVC
            // =========================
            services.AddControllersWithViews()
                .AddViewLocalization()
                .AddDataAnnotationsLocalization();

            // =========================
            // RAZOR PAGES
            // =========================
            services.AddRazorPages();

            // =========================
            // SESSION
            // =========================
            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            // =========================
            // CACHE
            // =========================
            services.AddMemoryCache();

            // =========================
            // HTTP CONTEXT
            // =========================
            services.AddHttpContextAccessor();
        }

        // =========================================================
        // PIPELINE
        // =========================================================
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // =========================
            // ERROR HANDLING
            // =========================
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            // =========================
            // HTTPS + STATIC FILES
            // =========================
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            // =========================
            // LOCALIZATION
            // =========================
            var locOptions = app.ApplicationServices
                .GetRequiredService<IOptions<RequestLocalizationOptions>>()
                .Value;

            app.UseRequestLocalization(locOptions);

            // =========================
            // ROUTING
            // =========================
            app.UseRouting();

            // =========================
            // SESSION
            // =========================
            app.UseSession();

            // =========================
            // AUTH
            // =========================
            app.UseAuthentication();
            app.UseAuthorization();

            // =========================
            // SECURITY HEADERS
            // =========================
            app.Use(async (context, next) =>
            {
                context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
                context.Response.Headers["X-Xss-Protection"] = "1; mode=block";
                context.Response.Headers["X-Content-Type-Options"] = "nosniff";

                await next();
            });

            // =========================
            // ENDPOINTS
            // =========================
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");

                endpoints.MapRazorPages();
            });

            // =========================
            // ADMIN SEED
            // =========================
            using (var scope = app.ApplicationServices.CreateScope())
            {
                var services = scope.ServiceProvider;
                SeedAdmin.CreateAdmin(services)
                    .GetAwaiter()
                    .GetResult();
            }
        }
    }
}