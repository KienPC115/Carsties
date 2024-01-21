using Duende.IdentityServer.Models;

namespace IdentityService;

public static class Config
{
    public static IEnumerable<IdentityResource> IdentityResources =>
        new IdentityResource[]
        {
            new IdentityResources.OpenId(),
            new IdentityResources.Profile(),
        };

    public static IEnumerable<ApiScope> ApiScopes =>
        new ApiScope[]
        {
            new ApiScope("auctionApp", "Auction app full access"),
        };

    public static IEnumerable<Client> Clients =>
        new Client[]
        {
           new Client
           {
            ClientId = "postman",
            ClientName = "Postman",
            AllowedScopes = {"openid", "profile", "auctionApp"},
            RedirectUris = {"https://www.getpostman.com/oauth2/callback"},
            ClientSecrets = new[] {new Secret("NotASecret".Sha256())},
            AllowedGrantTypes = {GrantType.ResourceOwnerPassword}
           },
           new Client
           {
                ClientId = "nextApp",
                ClientName = "nextApp",
                ClientSecrets = {new Secret("secret".Sha256())},
                // that means our client is going to be able to securely talk internally from inside our network
                //to IdentityServer inside our network and be issued with access token without actual browser 
                //This happen with client browser is concerned behind the scenes, and will access accesstoken from IdentityServer
                AllowedGrantTypes = GrantTypes.CodeAndClientCredentials,
                // going to be browser-based version of our client application but we doesnot need
                RequirePkce = false,
                RedirectUris = {"http://localhost:3000/api/auth/callback/id-server"},
                // So that we can enable refresh token functionlity
                AllowOfflineAccess = true,
                AllowedScopes = {"openid", "profile", "auctionApp"},
                AccessTokenLifetime = 3600*24*30 // 30days
           }
        };
}
