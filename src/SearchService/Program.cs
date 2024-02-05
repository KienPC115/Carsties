
using System.Net;
using MassTransit;
using Polly;
using Polly.Extensions.Http;
using SearchService;
using SearchService.Data;
using SearchService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddHttpClient<AuctionSvcHttpClient>().AddPolicyHandler(GetPolicy()); // inject the HttpClient into our application
builder.Services.AddMassTransit(x => {
    // - HOW the MassTransit work for consumer - 
    // We publish a message from our auction service that's going to go to the Contracts:AuctionCreated, which will then forward that
    //to exchanges that on to exchanges that are bound to this exchange which in this case is the search-auction-created exchange and then this search-auction-created exchange
    //has a queue also call search-auction-created, then the message will be consume by specific consumer and the message will be deleted at queue 
    // => it works in MassTransit. - default these are fanout exchange

    // We have the consumer, then we need to tell it where to find the consumers that we're creating
    // Any other consumers we create in this same namespace are automatically going to be registered by mass transit - bc MassTransit to scan the assembly containing the AuctionCreatedConsumer class
    x.AddConsumersFromNamespaceContaining<AuctionCreatedConsumer>(); //1. -> which is just going to configure ALL the endpoints based on consumers that we have based on this line.

    // The purpose of add to prefix in the name of consumer -> to Distinguish consumer from each other, because if different service create another AuctionCreatedConsumer bc multiple services need to do something when an auction is created
    x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("search", false));
    // => will see 2 exchanges(Contracts:AuctionCreated, search-auction-created) in RabbitMQ management browser 
    //because The AuctionCreatedConsumer is a MassTransit consumer for the AuctionCreated message type. -> MassTransit processes a message of type AuctionCreated ->  based on the default MassTransit configured convention. <Namespace>:<MessageType>
    //"search-auction-created," leading to the creation of the corresponding Exchange, Queue, and routing key in RabbitMQ. That was config above by SetEndpointNameFormatter()

    // make MassTransit connection to rabbitMq, bc MassTransit can using alot of message broker
    // we can config inside rabbitmq some retry policies for specific endpoints
    x.UsingRabbitMq((context, cfg) => {
        cfg.Host(builder.Configuration["RabbitMq:Host"], "/", host => {
            host.Username(builder.Configuration.GetValue("RabbitMq:Username", "guest"));
            host.Password(builder.Configuration.GetValue("RabbitMq:Password", "guest"));
        });

        //2. if we want to config on a per endpoint basis
        // setup the particular endpoint help the consumer can retry the get message when consumer have a problems
        cfg.ReceiveEndpoint("search-auction-created", e => {
            e.UseMessageRetry(r => r.Interval(5, 5)); // r.Interval(the number of retries, time in between the retries), -> retries 5 times, after each retry fails, it waits 5s to the next retry
        
            // need to specify which consumer we're configuring this for
            e.ConfigureConsumer<AuctionCreatedConsumer>(context); // this is only apply for the auctionCreatedConsumer for this particular configuration
        });

        // config a retry for AuctioUpdatedConsumer
        cfg.ReceiveEndpoint("search-auction-updated", e => {
            e.UseMessageRetry(r => r.Interval(5,5));

            e.ConfigureConsumer<AuctionUpdatedConsumer>(context);
        });

        // config a retry for AuctonDeletedConsumer
        cfg.ReceiveEndpoint("search-auction-deleted", e => {
            e.UseMessageRetry(r => r.Interval(5,5));

            e.ConfigureConsumer<AuctionDeletedConsumer>(context);
        });

        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseAuthorization();

app.MapControllers();

// help the application while it be waiting the auctionService reponse back, it still handle the request and response for client 
// -> help the applicatin won't be cracked
// -> help removed a bit of dependency on that auctionaService
app.Lifetime.ApplicationStarted.Register(async () =>
{
    // read a file so that open the try-catch block
    try
    {
        await DbInitializer.InitDb(app);
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
    }

});

app.Run();

// add a bit of resilience into our HttpRequest inside our search service because it need to send the request to auctionService to get data by HttpClient
//-> to repeat until such time as the data is available and we can get a successful response
// and it will be hanlde the exception while handle the reponse and help the application won't be cracked
// IAsyncPolicy - get form Polly package - Microsoft.Extensions.Http.Polly
static IAsyncPolicy<HttpResponseMessage> GetPolicy()
    => HttpPolicyExtensions
        .HandleTransientHttpError() // config the policy which handle HttpClient requests that fail with conditions indicating a transient failure - những lỗi mà nó đã định nghĩa để handle. ex: refuse connection
        .OrResult(msg => msg.StatusCode == HttpStatusCode.NotFound)
        .WaitAndRetryForeverAsync(_ => TimeSpan.FromMicroseconds(3)); // keep trying until such time as the auction service is backup