FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["xmlParser.csproj", "./"]
RUN dotnet restore "xmlParser.csproj"

COPY . .
RUN dotnet publish "xmlParser.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "xmlParser.dll"]
