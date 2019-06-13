using System.IO;
using Roentgenium.Config;
using Roentgenium.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace Roentgenium
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }
        
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors(opts =>
                opts.AddDefaultPolicy(builder => 
                    builder.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()));

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
            
            services.AddOptions()
                .Configure<AzureConfig>(Configuration.GetSection("Azure"))
                .Configure<StreamConfig>(Configuration.GetSection("Stream"))
                .Configure<FilesystemConfig>(Configuration.GetSection("Filesystem"))
                .Configure<LimitsConfig>(Configuration.GetSection("Limits"));

            services.AddSingleton<IPipelineManager, PipelineManager>();
            services.AddHostedService<PipelineManagerService>();
            services.AddSwaggerGen(c => 
            {
                c.SwaggerDoc(BuiltIns.Version,
                    new Info { Title = BuiltIns.Name, Version = BuiltIns.Version });
                c.IncludeXmlComments(Path.Combine(System.AppContext.BaseDirectory, $"{BuiltIns.Name}.xml"));
            });
        }
        
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseCors();
            app.UseHttpsRedirection();
            app.UseMvc();

            app.UseSwagger(c => c.RouteTemplate = "doc/{documentName}/" + $"{BuiltIns.Name}.json");
            app.UseSwaggerUI(c => 
            {
                c.SwaggerEndpoint($"/doc/{BuiltIns.Version}/{BuiltIns.Name}.json", 
                    $"{BuiltIns.Name} v{BuiltIns.Version}");
                c.DocumentTitle = $"{BuiltIns.Name} v{BuiltIns.Version} Documentation";
                c.DefaultModelRendering(ModelRendering.Model);
                c.DisplayRequestDuration();
            });
        }
    }
}
