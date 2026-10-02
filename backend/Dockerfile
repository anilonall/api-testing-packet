FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY . .
RUN dotnet restore ApiTestingPacket.slnx
RUN dotnet publish src/ApiTestingPacket.API/ApiTestingPacket.API.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
RUN adduser --disabled-password --gecos "" --uid 10001 appuser
COPY --from=build /app .
USER appuser
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "ApiTestingPacket.API.dll"]
