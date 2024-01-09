using MongoDB.Entities;

namespace SearchService.Models;

public class Item : Entity// the same as our auctionDTO what's going to come across once we have our messaging enable
{
    // the Item Class derive form MongoDb Entity -> it provides an ID for our item and effectively in our MongoBD
    // public Guid Id { get; set; } -> it doesn't need
    public int ReservePrice { get; set; }
    public string Seller { get; set; }
    public string Winner { get; set; }
    public int SoldAmount { get; set; }
    public int CurrentHighBid { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime AuctionEnd { get; set; }
    public string Status { get; set; }
    // Item
    public string Make { get; set; }
    public string Model { get; set; }
    public int Year { get; set; }
    public string Color { get; set; }
    public int Mileage { get; set; }
    public string ImageUrl { get; set; }
}
