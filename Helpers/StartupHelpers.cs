using System;
using System.Collections.Generic;
using IdentityModel.Client;
using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
//using Microsoft.OpenApi.Models;
using IdentityModel.AspNetCore.AccessTokenValidation;
using System.Reflection;
using Payment.DBContext;
using Payment.Services;
using Microsoft.Extensions.Configuration;
using Nest;
using Payment.Dtos;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Payment.Models;
using Swashbuckle.AspNetCore.Swagger;
using System.Linq;
using Microsoft.OpenApi.Models;
using Payment.Repository;
using Microsoft.Extensions.Options;

namespace Payment.Helpers
{
    public static class StartupHelpers
    {
        public static void AddElasticsearch(this IServiceCollection services)
        {
            var url = DecryptorProvider.Decrypt(Environment.GetEnvironmentVariable("ELASTICSEARCH_URL"));
            var defaultIndex = DecryptorProvider.Decrypt(Environment.GetEnvironmentVariable("ELASTICSEARCH_INDEX"));

            var settings = new ConnectionSettings(new Uri(url))
                .DefaultIndex("orders")
                .DefaultMappingFor<OrderDTO>(i => i
                    .IndexName("orders")
                    .IdProperty(f => f.Id));

            //AddDefaultMappings(settings);

            var client = new ElasticClient(settings);

            services.AddSingleton<IElasticClient>(client);

            CreateIndex<OrderDTO>(client, defaultIndex);
            CreateIndex<TransactionDTO>(client, "transaction");
        }

        private static void CreateIndex<T>(IElasticClient client, string indexName) where T:class
        {
            var createIndexResponse = client.Indices.Create(indexName,
                index => index.Map<T>(x => x.AutoMap())
            );
        }

        public static void UseSwagger(this IApplicationBuilder app, AdminApiConfiguration adminApiConfiguration)
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint($"{adminApiConfiguration.ApiBaseUrl}/swagger/v1/swagger.json", adminApiConfiguration.ApiName);

                //c.OAuthClientId(adminApiConfiguration.OidcSwaggerUIClientId);
                //c.OAuthAppName(adminApiConfiguration.ApiName);
                //c.OAuthUsePkce();
            });
        }

        public static void AddSwaggerGen(this IServiceCollection services, AdminApiConfiguration adminApiConfiguration)
        {
            ////services.AddSwaggerGen(options =>
            ////{
            ////    options.SwaggerDoc(adminApiConfiguration.ApiVersion, new OpenApiInfo { Title = adminApiConfiguration.ApiName, Version = adminApiConfiguration.ApiVersion });

            ////    options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
            ////    {
            ////        Type = SecuritySchemeType.OAuth2,
            ////        Flows = new OpenApiOAuthFlows
            ////        {
            ////            AuthorizationCode = new OpenApiOAuthFlow
            ////            {
            ////                AuthorizationUrl = new Uri($"{adminApiConfiguration.IdentityServerBaseUrl}/connect/authorize"),
            ////                TokenUrl = new Uri($"{adminApiConfiguration.IdentityServerBaseUrl}/connect/token"),
            ////                Scopes = new Dictionary<string, string> {
            ////                    { "email", "email" },
            ////                    { "openid", "openid" },
            ////                    { "profile", "profile" },
            ////                    { "phone", "phone" },
            ////                    { "api2", "api2" }
            ////                }
            ////            }
            ////        }
            ////    });
            ////    options.OperationFilter<AuthorizeCheckOperationFilter>();
            ////});
            // Swagger Tools
            var security = new Dictionary<string, IEnumerable<string>>
                {
                    {"Bearer", new string[] { }},
                };
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "My API",
                    Version = "v1"
                });
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Description = "Please insert JWT with Bearer into field",
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement {
   {
     new OpenApiSecurityScheme
     {
       Reference = new OpenApiReference
       {
         Type = ReferenceType.SecurityScheme,
         Id = "Bearer"
       }
      },
      new string[] { }
    }
  });
            });

        }

        public static void AddDistMemoryCache(this IServiceCollection services)
        {
            services.AddSingleton<IDiscoveryCache>(r =>
            {
                var factory = r.GetRequiredService<IHttpClientFactory>();
                return new DiscoveryCache(Constants.Authority, () => factory.CreateClient());
            });

            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = DecryptorProvider.Decrypt(Environment.GetEnvironmentVariable("REDIS_URL"));
                options.InstanceName = DecryptorProvider.Decrypt(Environment.GetEnvironmentVariable("REDIS_DATABASE"));
            });
        }

        public static void AddAuthenticationToken(this IServiceCollection services, IConfiguration configuration)
        {
            ////services.AddAuthentication("token")
            ////    //JWT tokens
            ////    .AddJwtBearer("token", options =>
            ////    {
            ////        options.Authority = Constants.Authority;
            ////        options.Audience = "api2";
            ////        //options.va
            ////        options.TokenValidationParameters.ValidTypes = new[] { "at+jwt" };
            ////        options.TokenValidationParameters.ValidateIssuer = true;
            ////        options.TokenValidationParameters.ValidateAudience = true;
            ////        options.TokenValidationParameters.RequireExpirationTime = true;
            ////        //if token does not contain a dot, it is a reference tokenauthorize
            ////        options.ForwardDefaultSelector = Selector.ForwardReferenceToken("introspection");
            ////    })
            ////    //reference tokens
            ////    .AddOAuth2Introspection("introspection", options =>
            ////    {
            ////        options.Authority = Constants.Authority;
            ////        options.ClientId = "demo_api_swagger";
            ////        options.ClientSecret = "secretchatapi";
            ////    });
            ////services.AddScopeTransformation();


            var jwtSection = configuration.GetSection("jwt");
            var jwtOptions = new JwtOptionsModel();
            jwtSection.Bind(jwtOptions);
            services.Configure<JwtOptionsModel>(jwtSection);

            // Add bearer authentication
            services.AddAuthentication("token")
                .AddJwtBearer("token", cfg =>
                {
                    //cfg.RequireHttpsMetadata = false;
                    cfg.SaveToken = true;
                    cfg.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateAudience = true,
                        ValidateIssuer = true,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
                        ValidIssuer = jwtOptions.Issuer,
                        ValidAudience = jwtOptions.Issuer,
                        // Validate the token expiry  
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero
                    };
                });
        }

        public static void AddRedisCheck(this IServiceCollection services)
        {
            TimeSpan cacheDuration = new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
            string name = DecryptorProvider.Decrypt(Environment.GetEnvironmentVariable("REDIS_DATABASE"));
            var builder = services.AddHealthChecks()
                .AddRedis(DecryptorProvider.Decrypt(Environment.GetEnvironmentVariable("REDIS_URL")));
        }

        public static void RegisterDbContexts(this IServiceCollection services)
        {
            string identityConnectionString = DecryptorProvider.Decrypt(Environment.GetEnvironmentVariable("CONNECTION_STRING"));
            var migrationsAssembly = typeof(StartupHelpers).GetTypeInfo().Assembly.GetName().Name;
            // Config DB for identity
            services.AddDbContext<AppDbContext>(options => options.UseSqlServer(identityConnectionString, sql => sql.MigrationsAssembly(migrationsAssembly)));
        }
        public static void RegisterServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<PaygateConfig>(configuration.GetSection(nameof(PaygateConfig)));
            services.AddSingleton<IPaygateConfig>(sp => sp.GetRequiredService<IOptions<PaygateConfig>>().Value);

            services.AddSingleton<IPayRepository, PayRepository>();

            services.AddTransient<IPayServices, PayServices>();

            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            services.AddElasticsearch();
        }
    }
}
