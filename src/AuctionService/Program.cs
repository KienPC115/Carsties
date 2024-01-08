using AuctionService.Data;
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
