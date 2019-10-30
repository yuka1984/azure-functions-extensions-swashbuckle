using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using AzureFunctions.Extensions.Swashbuckle;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TestFunction;

[assembly: WebJobsStartup(typeof(SwashBuckleStartup))]
namespace TestFunction
{
    internal class SwashBuckleStartup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            var executioncontextoptions = builder.Services.BuildServiceProvider()
                .GetService<IOptions<ExecutionContextOptions>>().Value;

            var currentDirectory = executioncontextoptions.AppDirectory;

            //Register the extension
            builder.AddSwashBuckle(Assembly.GetExecutingAssembly(), currentDirectory);
        }
    }
}
