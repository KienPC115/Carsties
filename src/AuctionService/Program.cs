using AuctionService;
using AuctionService.Data;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddDbContext<AuctionDbContext>(opt =>
{
    opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// AppDomain.CurrentDomain.GetAssemblies() provides the location of effectively the assembly
//that application is running in. It's going to take a look for any classes that
//derive from the profile class and register the mapping
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

// - Vấn đề lớn nhất của microservice là consistance data giữa các Database của mỗi service, vì trong quá trình send/publish message hoặc
//subcribe the message đó ở rabbitmq thì sẽ có nhiều vẫn đề xảy ra ví dụ như mongoDb (DB của thằng subcribes), rabbitmq bị crack/down thì
//gây ra inconsistance dữ liệu. ==> Chi tiết ở source 41. What could go wrong? in Section4: RabbitMq
// - Ta có 2 options để giải quyết vấn đề này:
// + Outbox message: if the service bus is down, then our message can sit in an outbox and they'll be retried at a future point
//when the service is backup, they'll be removed from outbox and publish into the message bus.
// + Retry: if at first we don't succeed, we try, try agian
builder.Services.AddMassTransit(x => {
    // !IMPORTANT
    // - Problem_1: This problem causing by the rabbitmq - service bus is down so that our message cannot be delivered -> slove it with this solution is using Outbox(op1 above) to store the message.
    x.AddEntityFrameworkOutbox<AuctionDbContext>(o => {
        // in that outbox every 10s and attempt to deliver the messages that are contained in there
        // We'll see that the query to our outbox is being executed to see if there's any messages that have not been delivered
        o.QueryDelay = TimeSpan.FromSeconds(10);
        // set update store the info message inot postgres db
        o.UsePostgres();
        o.UseBusOutbox();
    });

    x.AddConsumersFromNamespaceContaining<AuctionCreatedFaultConsumer>();
    x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("auction", false));

    x.UsingRabbitMq((context, cfg) => {
        // When AuctionService containerized into Docker, it can not connect to Rabbitmq bc the Docker run with enviroment network different development machine
        // if we don't specify a configuration for Rabbitmq, then it is going to use localhost by default
        // we need override that so that Docker can use a different location for Rabbitmq so that AuctionService can connect to Rabbitmq when it's running inside a container
        // we need to tell RabbitMq about the host of where this is running on
        cfg.Host(builder.Configuration["RabbitMq:Host"], "/", host => {
            host.Username(builder.Configuration.GetValue("RabbitMq:Username", "guest"));
            host.Password(builder.Configuration.GetValue("RabbitMq:Password", "guest"));
        });

        cfg.ConfigureEndpoints(context);
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        // we need to give it an authority which in this case is our IdentityServer
        // tell our resource server who the token was issued by
        options.Authority = builder.Configuration["IdentityServiceUrl"];
        options.RequireHttpsMetadata = false; // set to false bc our IdentityServer is runing on Http
        options.TokenValidationParameters.ValidateAudience = false;
        // setup the NameClaimType is corresponding wilt the username claim of token
        // so if we get by User.Identity.Name -> this will be get the value of username in token
        options.TokenValidationParameters.NameClaimType = "username";
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// to seed data
try
{
    DbInitializer.InitDb(app);
}
catch (Exception ex)
{
    Console.WriteLine(ex);
}

app.Run();

// To create a Postgres image
// - set up the docker-compose.yml
// - run cli: docker compose up -d -> will create Postgres image in Docker container
