FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY HRReserveSystem.sln ./
COPY HRReserveSystem.csproj ./
COPY HRReserveSystem.Tests/HRReserveSystem.Tests.csproj HRReserveSystem.Tests/
RUN dotnet restore HRReserveSystem.csproj

COPY . ./
RUN dotnet publish HRReserveSystem.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish ./

ENTRYPOINT ["dotnet", "HRReserveSystem.dll"]
