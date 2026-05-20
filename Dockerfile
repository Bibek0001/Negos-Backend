FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src
COPY . .
RUN dotnet publish Diyalo.Api.csproj -c Release -o /app/publish /p:PublishReact=false --no-self-contained -r linux-musl-x64

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_GCHeapHardLimit=400000000
ENV DOTNET_GCConserveMemory=9
ENTRYPOINT ["dotnet", "Diyalo.Api.dll"]
