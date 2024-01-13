using Contracts;
using MassTransit;

namespace AuctionService;

public class AuctionCreatedFaultConsumer : IConsumer<Fault<AuctionCreated>>
{
    // ST2-After the SearchService throw an exception and Rabbitmq create a error queue then put it into queues. For detail when the error message come it exchange these queue MassTransit:Fault, search-auction-created_error exchange and forward put into search-auction-created_error queue to persist the message in this queue, 
    //MassTransit:Fault--Contracts:AuctionCreated-- that exchanges -> then kick the auction-auction-created-fault exchage(endpoint of this Consumer) exchanges -> then put the error message into the auction-auction-created-fault Queue for this service can consume
    //now in this AuctionService consume the exception to handle it and resend to RabbitMq still with specific Contracts -> the 2 exchange Contracts:AuctionCreated, and search-auction-created wil be kick exchange
    public async Task Consume(ConsumeContext<Fault<AuctionCreated>> context) // why we set Fault<AuctionCreated> because here we publish and subcribe the event Create a new Auction through the message inside it is Contracts - AuctionCreated -> exchange of the RabbitMq when the message comming
    {
        //context here is contain the fault message was consumed
        Console.WriteLine("--> Consuming faulty creation");

        var exception = context.Message.Exceptions.First();

        if(exception.ExceptionType == "System.ArgumentException") {
            // we can access the message to fix something on that
            context.Message.Message.Model = "FooBar";
            await context.Publish(context.Message.Message);
        }
        else {
            Console.WriteLine("Not an argument exception - update error dashboard somewhere");
        }
    }
}
