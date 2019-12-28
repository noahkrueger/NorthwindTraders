using System.Collections.Generic;
using System.Security.Claims;
using IdentityModel;
using IdentityServer4.Models;
using IdentityServer4.Test;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Northwind.Application.Common.Interfaces;
using Northwind.Common;
using Northwind.Infrastructure.Files;
using Northwind.Infrastructure.Identity;

namespace Northwind.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
        {
            services.AddScoped<IUserManager, UserManagerService>();
            services.AddTransient<INotificationService, NotificationService>();
            services.AddTransient<IDateTime, MachineDateTime>();
            services.AddTransient<ICsvFileBuilder, CsvFileBuilder>();

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("NorthwindDatabase")));

            services.AddDefaultIdentity<ApplicationUser>()
                .AddEntityFrameworkStores<ApplicationDbContext>();

            if (environment.IsEnvironment("Test"))
            {
                services.AddIdentityServer()
                    .AddApiAuthorization<ApplicationUser, ApplicationDbContext>(options =>
                    {
                        options.Clients.Add(new Client
                        {
                            ClientId = "Northwind.IntegrationTests",
                            AllowedGrantTypes = { GrantType.ResourceOwnerPassword },
                            ClientSecrets = { new Secret("secret".Sha256()) },
                            AllowedScopes = { "Northwind.WebUIAPI", "openid", "profile" }
                        });
                    }).AddTestUsers(new List<TestUser>
                    {
                        new TestUser
                        {
                            SubjectId = "f26da293-02fb-4c90-be75-e4aa51e0bb17",
                            Username = "jason@northwind",
                            Password = "Northwind1!",
                            Claims = new List<Claim>
                            {
                                new Claim(JwtClaimTypes.Email, "jason@northwind")
                            }
                        }
                    });
            }
            else
            {
                services.AddIdentityServer()
                    .AddApiAuthorization<ApplicationUser, ApplicationDbContext>(options =>
                    {
                        options.Clients.Add(
                            // new Client
                            //     {
                            //         AllowOfflineAccess = true,
                            //         AllowedScopes = {"openid", "profile", "testapi"},
                            //         RedirectUris = {"https://www.getpostman.com/oauth2/callback"},
                            //         Enabled = true,
                            //         ClientId = "832afa32-cabe-40a0-8909-2241cd85e47d.Local.apps",
                            //         ClientSecrets = {new Secret{Value = "NotASecret".Sha512()}},
                            //         ClientName = "PostMan Login",
                            //         PostLogoutRedirectUris = {"http://localhost:5002/signout-callback-oidc"},
                            //         ClientUri = null,
                            //         AllowedGrantTypes = {GrantTypes.Code},
                            //         AllowAccessTokensViaBrowser = true,
                            //         LogoUri = null
                            //     });
                            new Client
                            {
                                ClientId = "postman-api",
                                ClientName = "Postman Test Client",
                                AllowedGrantTypes = GrantTypes.Code,
                                AllowAccessTokensViaBrowser = true,
                                RequireConsent = false,
                                RedirectUris = {"https://www.getpostman.com/oauth2/callback"},
                                PostLogoutRedirectUris =  {"https://www.getpostman.com"},
                                AllowedCorsOrigins = {"https://www.getpostman.com"},
                                EnableLocalLogin = true,
                                AllowedScopes = 
                                {
                                    IdentityServer4.IdentityServerConstants.StandardScopes.OpenId,
                                    IdentityServer4.IdentityServerConstants.StandardScopes.Profile,
                                    IdentityServer4.IdentityServerConstants.StandardScopes.Email,
                                    "postman_api"
                                },
                                ClientSecrets = new List<Secret>() { new Secret("SomeValue".Sha512())}
                            });
                    });
            }

            services.AddAuthentication()
                .AddIdentityServerJwt();

            return services;
        }
    }
}
