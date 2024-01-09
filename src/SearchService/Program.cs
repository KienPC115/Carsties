
using System.Net;
using Polly;
using Polly.Extensions.Http;
using SearchService.Data;
using SearchService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddHttpClient<AuctionSvcHttpClient>().AddPolicyHandler(GetPolicy()); // inject the HttpClient into our application

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseAuthorization();

app.MapControllers();

// help the application while it be waiting the auctionService reponse back, it still handle the request and response for client 
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
// and it will be hanlde the exception while handle the reponse
// IAsyncPolicy - get form Polly package - Microsoft.Extensions.Http.Polly
static IAsyncPolicy<HttpResponseMessage> GetPolicy()
    => HttpPolicyExtensions
        .HandleTransientHttpError() // config the policy which handle HttpClient requests that fail with conditions indicating a transient failure - những lỗi mà nó đã định nghĩa để handle. ex: refuse connection
        .OrResult(msg => msg.StatusCode == HttpStatusCode.NotFound)
        .WaitAndRetryForeverAsync(_ => TimeSpan.FromMicroseconds(3)); // keep trying until such time as the auction service is backup