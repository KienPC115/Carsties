using MongoDB.Entities;
using SearchService.Models;

namespace SearchService.Services;

public class AuctionSvcHttpClient
{
    // HttpClient is the service can make an Http Request
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;

    public AuctionSvcHttpClient(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _config = config;
    }

    public async Task<List<Item>> GetItemsForSearchDb() {
        var lastUpdated = await DB.Find<Item, string>()
            .Sort(x => x.Descending(x => x.UpdatedAt))
            .Project(x => x.UpdatedAt.ToString())
            .ExecuteFirstAsync();

        // GetFromJsonAsync() -> automatically  deserializes the Json that we get back from AuctionService
        return await _httpClient.GetFromJsonAsync<List<Item>>(_config["AuctionServiceUrl"] 
            + $"/api/auctions?date={lastUpdated}");
    }
}
