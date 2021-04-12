//using IdentityModel.AspNetCore.AccessTokenValidation;
//using IdentityModel.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
//using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Net.Http;
//using System.Threading.Tasks;
using Payment.Helpers;
//using Microsoft.AspNetCore.Authentication.Cookies;
//using Microsoft.AspNetCore.Authentication;
//using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Configuration;
//using Microsoft.OpenApi.Models;
//using Microsoft.AspNetCore.Authentication.JwtBearer;
//using Microsoft.IdentityModel.Tokens;
using System.IO;
using dotenv.net;

namespace Payment
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public IWebHostEnvironment Environment { get; }
        public Startup(IWebHostEnvironment env, IConfiguration configuration)
        {
            var envFilePath = $".env";
            if (File.Exists(envFilePath))
            {
                DotEnv.Config(true, envFilePath);
            }
            else
            {
                DotEnv.AutoConfig();
            }
            Environment = env;
            Configuration = configuration;
        }
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            var adminApiConfiguration = Configuration.GetSection(nameof(AdminApiConfiguration)).Get<AdminApiConfiguration>();
            services.AddSingleton(adminApiConfiguration);

            services.AddControllersWithViews();

            services.RegisterDbContexts();

            services.RegisterServices(Configuration);

            services.AddHttpClient();

            services.AddDistMemoryCache();

            services.AddAuthenticationToken(Configuration);

            services.AddCors();

            services.AddSwaggerGen(adminApiConfiguration);

            //services.AddAuditEventLogging<AdminAuditLogDbContext, AuditLog>(Configuration);

            //services.AddIdSHealthChecks<IdentityServerConfigurationDbContext, IdentityServerPersistedGrantDbContext, AdminIdentityDbContext, AdminLogDbContext, AdminAuditLogDbContext, IdentityServerDataProtectionDbContext>(Configuration, adminApiConfiguration);
            services.AddRedisCheck();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, AdminApiConfiguration adminApiConfiguration)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseStaticFiles();

            app.UseSwagger(adminApiConfiguration);

            app.UseRouting();

            app.UseCors();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
                //endpoints.MapHealthChecks("/health", new HealthCheckOptions
                //{
                //    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                //});
            });
        }
    }
}
