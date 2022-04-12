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

      // services.AddSwaggerForOcelot (Configuration, (options) => {
      //   options.GenerateDocsForAggregates = true;
      //   options.AggregateDocsGeneratorPostProcess = (aggregateRoute, routesDocs, pathItemDoc, documentation) => {
      //     var apiGwPath = Configuration.GetValue<string> ("ApiGwPath");
      //     var aggregateRouteUpstreamPathTemplate = $"{apiGwPath}{aggregateRoute.UpstreamPathTemplate}";

      //     Console.WriteLine ($"aggregateRoute: {aggregateRoute}");
      //     Console.WriteLine ($"routesDocs: {routesDocs}");
      //     Console.WriteLine ($"pathItemDoc: {pathItemDoc}");
      //     Console.WriteLine ($"documentation: {documentation}");

      //     if (!aggregateRoute.UpstreamPathTemplate.Contains ("apigw")) {
      //       aggregateRoute.UpstreamPathTemplate = aggregateRouteUpstreamPathTemplate;
      //     }
      //   };
      // });

      // services.AddOcelot(Configuration).AddAppConfiguration();

      services.AddControllers();
      services.AddOcelot().AddAppConfiguration();
      services.AddSwaggerForOcelot(Configuration);

      // var authenticationProviderKey = "IdentityApiKey";
      var handler = new HttpClientHandler();
      handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

      services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
        {
          Console.WriteLine("=========================> Bearer");
          options.Authority = "https://localhost:5001";
          options.Audience = "client.kma.jwt";
          options.BackchannelHttpHandler = handler;
          options.TokenValidationParameters.ValidTypes = new[] { "at+jwt" };
          options.TokenValidationParameters.ValidateAudience = false;
          // if token does not contain a dot, it is a reference token
          options.ForwardDefaultSelector = Selector.ForwardReferenceToken("introspection");
        })
        .AddOAuth2Introspection("introspection", options =>
        {
          Console.WriteLine("=========================> introspection");
          options.Authority = "https://localhost:5001";
          options.ClientId = "client.kma.token";
          options.ClientSecret = "1e4f9ffe-7949-4993-a92b-f74a9bf6b995";
        });

      services.AddAuthorization(options =>
      {
        options.AddPolicy("ReadOnly", policy => policy.RequireScope(new string[] { "guest", "bio" }));
        options.AddPolicy("FullOperation", policy => policy.RequireScope(new string[] { "pin" }));
      });

      services.AddHttpClient(OAuth2IntrospectionDefaults.BackChannelHttpClientName).ConfigurePrimaryHttpMessageHandler(() => handler);
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
      app.UseHttpsRedirection();
      app.UseRouting();

      app.UseSwaggerForOcelotUI(options =>
      {
        options.PathToSwaggerGenerator = "/swagger/docs";
      });

      app.UseOcelot().Wait();

      // var apiGWPath = Configuration.GetValue<string>("ApiGwPath");
      // app.UseSwaggerForOcelotUI(opt =>
      // {
      //   opt.DownstreamSwaggerEndPointBasePath = $"{apiGWPath}/swagger/docs";
      //   opt.ReConfigureUpstreamSwaggerJson = DefinedUpstreamTransformer;
      // });
      // app.UseOcelot().Wait();

      // app.UseEndpoints (endpoints => {
      //   endpoints.MapControllers ();
      // });

    }

    // private string DefinedUpstreamTransformer(HttpContext context, string openApiJson)
    // {
    //   var swagger = JObject.Parse(openApiJson);
    //   JToken paths = swagger[OpenApiProperties.Paths];

    //   var allpaths = paths.Values<JProperty>().ToList();
    //   var apiGWPath = Configuration.GetValue<string>("ApiGwPath");
    //   foreach (var item in allpaths)
    //   {
    //     Console.WriteLine($"{apiGWPath}" + item.Name, item.Value);
    //     var newProperty = new JProperty($"{apiGWPath}" + item.Name, item.Value);
    //     item.Replace(newProperty);
    //   }

    //   return swagger.ToString(Formatting.Indented);
    // }
  }
}