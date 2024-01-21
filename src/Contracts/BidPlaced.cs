namespace Contracts;

// this class is the Contract when the user bid the price for CarItem
public class BidPlaced
{
    public string Id { get; set; } // Id for the Bid itself
    public string AuctionId { get; set; }
    public string Bidder { get; set; } // bidder Username
    public DateTime BidTime { get; set; }
    public int Amount { get; set; }
    public string BidStatus { get; set; }
}
