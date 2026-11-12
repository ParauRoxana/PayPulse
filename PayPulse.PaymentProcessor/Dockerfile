FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["PayPulse.PaymentProcessor/PayPulse.PaymentProcessor.csproj", "PayPulse.PaymentProcessor/"]
RUN dotnet restore "PayPulse.PaymentProcessor/PayPulse.PaymentProcessor.csproj"

COPY . .
WORKDIR /src/PayPulse.PaymentProcessor
RUN dotnet publish "PayPulse.PaymentProcessor.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
ENV ASPNETCORE_HTTP_PORTS=8080
WORKDIR /app
USER $APP_UID
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PayPulse.PaymentProcessor.dll"]
