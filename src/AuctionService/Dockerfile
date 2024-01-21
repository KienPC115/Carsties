#STEP1: first thing we need to get a base image to work from -> DotNet SDK inside Docker to help us
# we get from a pulic repo where Microsoft stores. Its Docker images for things like the SDK
# build stage -> set the base image to use for any subsequent instructions that follow and also give this build stage a name.
FROM mcr.microsoft.com/dotnet/sdk:7.0 as build
# next: we need to specify a working directory and we set this for any subsequent
# /src is a directory inside Docker system
WORKDIR /app
# then we need to expose the ports that we're using internally inside Docker
# our containers for each of our different service are going to be running on Port 80
EXPOSE 80

#STEP2:
# copy all .csproj files and restore as distinct layers. Use of the same COPY command
#for every dockerfile in the project to take advantage of docker caching -> speed up our builds of Docker
# COPY   source      dest      -> taking it from our development machine's file system 
#and copying this file into our working directory inside Docker /app which already config
COPY Carsties.sln Carsties.sln
#copy all .csproj file
COPY src/AuctionService/AuctionService.csproj src/AuctionService/AuctionService.csproj
COPY src/SearchService/SearchService.csproj src/SearchService/SearchService.csproj
COPY src/GatewayService/GatewayService.csproj src/GatewayService/GatewayService.csproj
COPY src/Contracts/Contracts.csproj src/Contracts/Contracts.csproj
COPY src/IdentityService/IdentityService.csproj src/IdentityService/IdentityService.csproj

#STEP3:
# Restore the package dependencies
# in the context using DotNet SDK, we have access to the .net Cli inside here
RUN dotnet restore Carsties.sln 

# Copy the app folders over -> then we need to copy the other code over
COPY src/AuctionService src/AuctionService 
# Contracts has depend of the AuctionService
COPY src/Contracts src/Contracts 

# chage the work directory into this particular folder -> we execute the .net publish command
#to publish our auctionservice application and the published files is stored in /app/src/out
WORKDIR /app/src/AuctionService
RUN dotnet publish -c Release -o /app/src/out

#STEP4:
# Now at this stage we want to get a runtime image and we dont need to use th SDK which is quite big
# we can actually use the .net runtime directly once -> we got a published version of our auctionservice
# build runtime image
# dotnet/aspnet:7.0 -> gives us access to much smaller dotnet runtime -> base image
FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /app
# we going to go from our base build image
# from build and put it into the /out of our current working directory 
#-> all published files inside there '.' -> specify the entry point
COPY --from=build /app/src/out .
ENTRYPOINT [ "dotnet", "AuctionService.dll" ]

# => should able to build a Docker container for our auctionService
# -f goto the particular Dockerfile, -t is a tag give it a name of tag, '.' -> to tell it the context of where
#we're trying to execute this command from -> we want to Carsties directory -> then it going to go and find this file, run the code for this
#  docker build -f src/AuctionService/Dockerfile -t testing123 .