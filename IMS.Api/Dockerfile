FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY IMS.Api.csproj ./
RUN dotnet restore IMS.Api.csproj

COPY . ./
RUN dotnet publish IMS.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 10000

CMD ASPNETCORE_URLS=http://0.0.0.0:${PORT:-10000} dotnet IMS.Api.dll
