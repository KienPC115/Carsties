using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // we need to give it an authority which in this case is our IdentityServer
        // tell our resource server who the token was issued by
        options.Authority = builder.Configuration["IdentityServiceUrl"];
        options.RequireHttpsMetadata = false; // set to false bc our IdentityServer is runing on Http
        options.TokenValidationParameters.ValidateAudience = false;
        // setup the NameClaimType is corresponding wilt the username claim of token
        // so if we get by User.Identity.Name -> this will be get the value of username in token
        options.TokenValidationParameters.NameClaimType = "username";
    });

var app = builder.Build();

app.MapReverseProxy();

app.UseAuthentication();
app.UseAuthorization();

app.Run();
