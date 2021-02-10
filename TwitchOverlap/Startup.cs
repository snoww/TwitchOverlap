using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using TwitchOverlap.Services;
using TwitchOverlapApi.Models;

namespace TwitchOverlap
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
            services.AddHttpClient();
            services.Configure<TwitchDatabaseSettings>(Configuration.GetSection(nameof(TwitchDatabaseSettings)));
            services.AddSingleton<ITwitchDatabaseSettings>(sp => sp.GetRequiredService<IOptions<TwitchDatabaseSettings>>().Value);
            services.AddSingleton<TwitchService>();
            services.AddRouting(options => options.LowercaseUrls = true);
            services.AddControllers();
            
            services.AddControllersWithViews().AddRazorRuntimeCompilation();

            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = "localhost:6379";
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                // endpoints.MapControllerRoute(
                //     name: "default",
                //     pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}