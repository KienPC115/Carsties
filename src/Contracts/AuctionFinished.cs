namespace Contracts;

// will be using this AuctionFinished event to update certain things inside the auctionService and SearchService
public class AuctionFinished
{
    // specify itemsold as in did the bids meet the reserveprice and has the auction finished -> Item is sold
    // if the auction did not meet the reserveprice and auction finished, then itemsold would be false
    public bool ItemSold { get; set; }
    public string AuctionId { get; set; }
    public string Winner { get; set; }
    public string Seller { get; set; }
    public int? Amount { get; set; } // the end price for the item
}
