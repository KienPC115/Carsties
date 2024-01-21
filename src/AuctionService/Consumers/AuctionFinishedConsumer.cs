using AuctionService.Data;
using AuctionService.Entities;
using Contracts;
using MassTransit;

namespace AuctionService;

public class AuctionFinishedConsumer : IConsumer<AuctionFinished>
{
    private readonly AuctionDbContext _dbContext;

    // this class will be consume the message is represent a AuctionFinished event with the Constract is AuctionFinished
    //then Consume() will be handled the auction
    public AuctionFinishedConsumer(AuctionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Consume(ConsumeContext<AuctionFinished> context)
    {
        Console.WriteLine("--> Consuming auction finished");

        // we dont need to track the end datetime of auction
        //that the bidService to track when an auction finished and send this event
        var auction = await _dbContext.Auctions.FindAsync(context.Message.AuctionId);

        if(context.Message.ItemSold) {
            auction.Winner = context.Message.Winner;
            auction.SoldAmount = context.Message.Amount;
        }

        // update the Status of auction -> the end Bid price > reservePrice -> the auctionFinish
        auction.Status = auction.SoldAmount > auction.ReservePrice ? Status.Finished : Status.ReserveNotMet;
    
        await _dbContext.SaveChangesAsync();
    }
}
