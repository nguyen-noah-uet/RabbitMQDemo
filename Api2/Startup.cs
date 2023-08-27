using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Api2.DbContexts;
using Api2.Listener;
using Api2.Repositories;
using Microsoft.EntityFrameworkCore;
using Plain.RabbitMQ;
using RabbitMQ.Client;
using Shared.Messages;
using Shared.Repositories;
using Api2.Services;

namespace Api2
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
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Api2", Version = "v1" });
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
            //        "catalog_exchange",
            //        ExchangeType.Topic));
            //services.AddSingleton<ISubscriber>(s =>
            //    new Subscriber(s.GetService<IConnectionProvider>(),
            //        "order_exchange",
            //        "order_response_queue",
            //        "order_created_routingkey",
            //        ExchangeType.Topic));
            //services.AddHostedService<S1MessageListener>();
            services.AddSingleton<IRabbitMqService, RabbitMqService>();
            services.AddTransient<IMessagePublisherService, MessagePublisherService>();
            services.AddSingleton<IMessageConsumerService, MessageConsumerService>();
            services.AddHostedService<S1MessageListener>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Api2 v1"));
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
