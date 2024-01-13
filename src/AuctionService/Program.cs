using AuctionService;
using AuctionService.Data;
using MassTransit;
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
        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.

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
