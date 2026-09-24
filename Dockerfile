# Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props ComplaintManagement.sln ./
COPY src/ src/
RUN dotnet restore src/ComplaintManagement.Api/ComplaintManagement.Api.csproj
RUN dotnet publish src/ComplaintManagement.Api/ComplaintManagement.Api.csproj -c Release -o /app --no-restore

# Runtime: non-root, no SDK
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "ComplaintManagement.Api.dll"]
