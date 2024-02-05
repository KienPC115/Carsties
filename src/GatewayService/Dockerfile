FROM mcr.microsoft.com/dotnet/sdk:7.0 as build
WORKDIR /app
EXPOSE 80

# copy all .csproj files and restore as distinct layers. Use of the same COPY command
# for every dockerfile in the project to take advantage of docker caching -> speed up our builds of Docker
COPY Carsties.sln Carsties.sln 
COPY src/AuctionService/AuctionService.csproj src/AuctionService/AuctionService.csproj
COPY src/SearchService/SearchService.csproj src/SearchService/SearchService.csproj
COPY src/GatewayService/GatewayService.csproj src/GatewayService/GatewayService.csproj
COPY src/Contracts/Contracts.csproj src/Contracts/Contracts.csproj
COPY src/IdentityService/IdentityService.csproj src/IdentityService/IdentityService.csproj

# Restore the package dependencies
RUN dotnet restore Carsties.sln 
# over here to up is the same for every service -> take advantage of Docker caching

# Copy the app folders over -> then we need to copy the other code over
COPY src/GatewayService src/GatewayService 
WORKDIR /app/src/GatewayService
RUN dotnet publish -c Release -o /app/src/out

# Build runtime image
# dotnet/aspnet:7.0 -> gives us access to much smaller dotnet runtime -> base image
FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /app
COPY --from=build /app/src/out .
ENTRYPOINT [ "dotnet", "GatewayService.dll" ]