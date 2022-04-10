using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

namespace ocelot_gw {
  public class Startup {
    public Startup (IConfiguration configuration) {
      Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices (IServiceCollection services) {

      services.AddControllers ();
      services.AddSwaggerForOcelot (Configuration, (options) => {
        options.GenerateDocsForAggregates = true;
        options.AggregateDocsGeneratorPostProcess = (aggregateRoute, routesDocs, pathItemDoc, documentation) => {
          var apiGwPath = Configuration.GetValue<string> ("ApiGwPath");
          var aggregateRouteUpstreamPathTemplate = $"{apiGwPath}{aggregateRoute.UpstreamPathTemplate}";

          Console.WriteLine ($"aggregateRoute: {aggregateRoute}");
          Console.WriteLine ($"routesDocs: {routesDocs}");
          Console.WriteLine ($"pathItemDoc: {pathItemDoc}");
          Console.WriteLine ($"documentation: {documentation}");

          if (!aggregateRoute.UpstreamPathTemplate.Contains ("apigw")) {
            aggregateRoute.UpstreamPathTemplate = aggregateRouteUpstreamPathTemplate;
          }
        };
      });

      services.AddOcelot (Configuration)
        .AddAppConfiguration ();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure (IApplicationBuilder app, IWebHostEnvironment env) {
      app.UseStaticFiles ();
      app.UseHttpsRedirection ();
      app.UseRouting ();

      var apiGWPath = Configuration.GetValue<string> ("ApiGwPath");
      app.UseSwaggerForOcelotUI (opt => {
        opt.DownstreamSwaggerEndPointBasePath = $"{apiGWPath}/swagger/docs";
        opt.ReConfigureUpstreamSwaggerJson = DefinedUpstreamTransformer;
      });
      app.UseOcelot ().Wait ();

      // app.UseEndpoints (endpoints => {
      //   endpoints.MapControllers ();
      // });

    }

    private string DefinedUpstreamTransformer (HttpContext context, string openApiJson) {
      var swagger = JObject.Parse (openApiJson);
      JToken paths = swagger[OpenApiProperties.Paths];

      var allpaths = paths.Values<JProperty> ().ToList ();
      var apiGWPath = Configuration.GetValue<string> ("ApiGwPath");
      foreach (var item in allpaths) {
        Console.WriteLine ($"{apiGWPath}" + item.Name, item.Value);
        var newProperty = new JProperty ($"{apiGWPath}" + item.Name, item.Value);
        item.Replace (newProperty);
      }

      return swagger.ToString (Formatting.Indented);
    }
  }
}