FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["PayPulse.TrafficApi/PayPulse.TrafficApi.csproj", "PayPulse.TrafficApi/"]
RUN dotnet restore "PayPulse.TrafficApi/PayPulse.TrafficApi.csproj"

COPY . .
WORKDIR /src/PayPulse.TrafficApi
RUN dotnet publish "PayPulse.TrafficApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
ENV ASPNETCORE_HTTP_PORTS=8080
WORKDIR /app
USER $APP_UID
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PayPulse.TrafficApi.dll"]
