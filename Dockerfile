FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish Diyalo.Api.csproj -c Release -o /app/publish /p:PublishReact=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_GCHeapHardLimit=450000000
ENV DOTNET_GCConserveMemory=9
ENV DOTNET_TieredCompilation=0
ENV DOTNET_ReadyToRun=0
ENTRYPOINT ["dotnet", "Diyalo.Api.dll"]
