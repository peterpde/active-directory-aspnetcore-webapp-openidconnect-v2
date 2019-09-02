﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.TokenCacheProviders.Sql;
using System.IdentityModel.Tokens.Jwt;
using WebApp_OpenIDConnect_DotNet.Infrastructure;
using WebApp_OpenIDConnect_DotNet.Services.GraphOperations;

namespace WebApp_OpenIDConnect_DotNet
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
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            // This is required to be instantiated before the OpenIdConnectOptions starts getting configured.
            // By default, the claims mapping will map claim names in the old format to accommodate older SAML applications.
            // 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role' instead of 'roles'
            // This flag ensures that the ClaimsIdentity claims collection will be built from the claims in the token
            JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

            // Uncomment the following to initialize the sql server database with tables required to cache tokens.
            // NOTE : This is a one time use method. We advise using it in development environments to create the tables required to enable token caching.
            // For production deployments, preferably, generate the schema from the tables generated in dev environments and use it to create the necessary tables in production.
            // Comment/remove the following line once the database and tables has been created.
            // SqlTokenCacheProviderExtension.CreateTokenCachingTablesInSqlDatabase(new MsalSqlTokenCacheOptions(Configuration.GetConnectionString("TokenCacheDbConnStr")));

            // Token acquisition service based on MSAL.NET
            // and chosen token cache implementation
            services.AddAzureAdV2Authentication(Configuration)
                    .AddMsal(Configuration, new string[] { Constants.ScopeUserRead })
                    .AddSqlAppTokenCache(new MsalSqlTokenCacheOptions(Configuration.GetConnectionString("TokenCacheDbConnStr")))
                    .AddSqlPerUserTokenCache(new MsalSqlTokenCacheOptions(Configuration.GetConnectionString("TokenCacheDbConnStr")));

            // Add Graph
            services.AddGraphService(Configuration);

            services.AddMvc(options =>
            {
                var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
                options.Filters.Add(new AuthorizeFilter(policy));
            })
            .SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
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
            app.UseCookiePolicy();

            app.UseAuthentication();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}