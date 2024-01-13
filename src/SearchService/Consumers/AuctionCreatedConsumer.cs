using AutoMapper;
using Contracts;
using MassTransit;
using MongoDB.Entities;
using SearchService.Models;

namespace SearchService;

public class AuctionCreatedConsumer : IConsumer<AuctionCreated>
{
    private readonly IMapper _mapper;

    public AuctionCreatedConsumer(IMapper mapper)
    {
        _mapper = mapper;
    }

    // define what we want to do when we consume this particular message when it arrives from service bus
    public async Task Consume(ConsumeContext<AuctionCreated> context)
    {
        // !IMPORTANT
        // - Problem_2: when we get the message in our consumer and we attempt to save it to the database, that might fail and we have an exception
        //because that could possibly be a transient type of error where the database may comeback online
        //that we want to retry several times before giving up and genuienly throwing exception
        // -> to slove this we retry the consumer access that endpoint severtal times to get the message -> setup in the program class
        Console.WriteLine("--> Consuming auction created: " + context.Message.Id); // Message is represent a Contracts - message obj from AuctionService publish into service bus, now SearchSv get it

        var item = _mapper.Map<Item>(context.Message);

        // !IMPORTANT
        // - Problem_3: We're not dealing with the faults in MassTransit, we just simple use a retry,but these queues(MassTransit:Fault, MassTransit:Fault--Contracts:AuctionCreated--, search-auction-created_error) were created
        //when we have an exception from our consumer that we didn't deal with, then that exception gets put into this queue.
        // => we can also consume fault messages and do something with them. If there's an exception, we can do something about that we know could possibly be generated, then we can take an action on that exception.
        // ex: ST1-Throw a exception when AuctionSv created a new auction(AuctionCreated in Contracts) and send message into rabbitmq (rabbitmq create a exchange of Contracts:AuctionCreated and search-auction-created rp for exchange, queue) 
        //then SearchSv get the message but SearchSv throw a exception(like a publish/send message for rabittmq) and rabbitmq create a exchange and queues and put ex into the queue. Now in AuctionService can consume that error to take an specific action to slove problem.
        if(item.Model == "Foo") throw new ArgumentException("Cannot sell cars with name of Foo");

        await item.SaveAsync(); // save this item into DB
    }
}
