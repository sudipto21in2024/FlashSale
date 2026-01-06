# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["BoltTickets.sln", "."]
COPY ["BoltTickets.Domain/BoltTickets.Domain.csproj", "BoltTickets.Domain/"]
COPY ["BoltTickets.Application/BoltTickets.Application.csproj", "BoltTickets.Application/"]
COPY ["BoltTickets.Infrastructure/BoltTickets.Infrastructure.csproj", "BoltTickets.Infrastructure/"]
COPY ["BoltTickets.Worker/BoltTickets.Worker.csproj", "BoltTickets.Worker/"]

# Copy all source code
COPY ["BoltTickets.Domain/", "BoltTickets.Domain/"]
COPY ["BoltTickets.Application/", "BoltTickets.Application/"]
COPY ["BoltTickets.Infrastructure/", "BoltTickets.Infrastructure/"]
COPY ["BoltTickets.Worker/", "BoltTickets.Worker/"]

# Restore and publish
RUN dotnet restore "BoltTickets.Worker/BoltTickets.Worker.csproj"
RUN dotnet publish "BoltTickets.Worker/BoltTickets.Worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "BoltTickets.Worker.dll"]