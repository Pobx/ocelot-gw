using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MMLib.SwaggerForOcelot.DependencyInjection;
using Ocelot.DependencyInjection;

namespace ocelot_gw
{
  public class Program
  {
    public static void Main(string[] args)
    {
      CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
      Host.CreateDefaultBuilder(args)
      .ConfigureAppConfiguration((context, config) =>
      {
        var environment = context.HostingEnvironment;
        config.SetBasePath(environment.ContentRootPath)
        .AddOcelotWithSwaggerSupport(options =>
        {
          options.Folder = "Configuration";
        });
      })
      .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>());
  }
}