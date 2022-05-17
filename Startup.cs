using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using IdentityModel.AspNetCore.AccessTokenValidation;
using IdentityModel.AspNetCore.OAuth2Introspection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using MMLib.Ocelot.Provider.AppConfiguration;
using MMLib.SwaggerForOcelot;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

namespace ocelot_gw
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
      services.AddOcelot(Configuration).AddAppConfiguration();
      services.AddSwaggerForOcelot(Configuration, options => {
        options.GenerateDocsForAggregates = true;
      });
      services.AddControllers();

      var handler = new HttpClientHandler();
      handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

      services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
        {
          Console.WriteLine("=========================> Bearer");
          options.Authority = "https://localhost:5001";
          options.Audience = "ro.client.jwt";
          options.BackchannelHttpHandler = handler;
          options.TokenValidationParameters.ValidTypes = new[] { "at+jwt" };
          options.TokenValidationParameters.ValidateAudience = false;
          options.ForwardDefaultSelector = Selector.ForwardReferenceToken("introspection");
        })
        .AddOAuth2Introspection("introspection", options =>
        {
          Console.WriteLine("=========================> introspection");
          options.Authority = "https://localhost:5001";
          options.ClientId = "ro.client.token";
          options.ClientSecret = "pobx";
        });

      services.AddHttpClient(OAuth2IntrospectionDefaults.BackChannelHttpClientName).ConfigurePrimaryHttpMessageHandler(() => handler);
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
      app.UseRouting();

      app.UseSwaggerForOcelotUI(options =>
      {
        options.PathToSwaggerGenerator = "/swagger/docs";
      });

      app.UseOcelot().Wait();
    }
  }
}