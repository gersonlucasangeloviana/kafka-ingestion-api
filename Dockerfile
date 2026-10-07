FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY global.json Directory.Build.props KafkaIngestion.slnx ./
COPY src/KafkaIngestion.Api/KafkaIngestion.Api.csproj src/KafkaIngestion.Api/packages.lock.json src/KafkaIngestion.Api/
RUN dotnet restore src/KafkaIngestion.Api --locked-mode
COPY src/KafkaIngestion.Api/ src/KafkaIngestion.Api/
RUN dotnet publish src/KafkaIngestion.Api -c Release --no-restore -o /app /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
COPY --from=build /app .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "KafkaIngestion.Api.dll"]
