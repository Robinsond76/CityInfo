using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Serialization;

namespace CityInfo.API
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc() //Note that for ASP.NET Core 3.0+, it's another service
                .AddMvcOptions(o =>
                {
                    o.OutputFormatters.Add(new XmlDataContractSerializerOutputFormatter()); // This serializes our data to be sent as XML if requested by the Accept Header
                });
                
                //.AddJsonOptions(o =>
                //{
                //    if (o.SerializerSettings.ContractResolver != null)
                //    {
                //        var castedResolver = o.SerializerSettings.ContractResolver
                //            as DefaultContractResolver;
                //        castedResolver.NamingStrategy = null;
                //    }
                //}); This code is for allowing the JSON properties to return capitalized

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
                app.UseExceptionHandler();
            }

            app.UseStatusCodePages(); //Status code page middleware

            app.UseMvc(); ///Note that for ASP.NET Core 3.0+, it's another middleware

           
        }
    }
}
