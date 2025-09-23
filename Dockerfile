# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy csproj and restore
COPY Wayward.Web/Wayward.Web.csproj Wayward.Web/
RUN dotnet restore Wayward.Web/Wayward.Web.csproj

# Copy source and build
COPY . .
RUN dotnet publish Wayward.Web/Wayward.Web.csproj -c Release -o /publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Wayward.Web.dll"]
