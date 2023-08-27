using Api1.DbContexts;
using Api1.Listeners;
using Api1.Repositories;
using Api1.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Plain.RabbitMQ;
using RabbitMQ.Client;
using Shared.Repositories;

namespace Api1
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

            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Api1", Version = "v1" });
            });
            // Configure Repositories
            services.AddScoped<IBookRepository, BookRepository>();
            // Configure Database
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"));
            });
            // Configure RabbitMQ
            //services.AddSingleton<IConnectionProvider>(new ConnectionProvider(Configuration["RabbitMQ:Url"]));
            //services.AddSingleton<IPublisher>(p =>
            //    new Publisher(p.GetService<IConnectionProvider>(),
            //        "order_exchange",
            //        ExchangeType.Topic));
            //services.AddSingleton<ISubscriber>(s =>
            //    new Subscriber(s.GetService<IConnectionProvider>(),
            //        "catalog_exchange",
            //        "catalog_response_queue",
            //        "catalog_response_routingkey",
            //        ExchangeType.Topic));
            //services.AddHostedService<S2ResponseListener>();
            services.AddSingleton<IRabbitMqService, RabbitMqService>();
            services.AddTransient<IMessagePublisherService, MessagePublisherService>();
            services.AddSingleton<IMessageConsumerService, MessageConsumerService>();
            services.AddHostedService<S2ResponseListener>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Api1 v1"));
            }

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
