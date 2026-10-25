# Build from the repo root: docker build -f TopSupport.Worker/Dockerfile .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY TopSupport.Models/TopSupport.Models.csproj TopSupport.Models/
COPY TopSupport.Services/TopSupport.Services.csproj TopSupport.Services/
COPY TopSupport.Worker/TopSupport.Worker.csproj TopSupport.Worker/
RUN dotnet restore TopSupport.Worker/TopSupport.Worker.csproj
COPY TopSupport.Models/ TopSupport.Models/
COPY TopSupport.Services/ TopSupport.Services/
COPY TopSupport.Worker/ TopSupport.Worker/
RUN dotnet publish TopSupport.Worker/TopSupport.Worker.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 5280
ENTRYPOINT ["dotnet", "TopSupport.Worker.dll"]
