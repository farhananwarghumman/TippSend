FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY TippSendApp.csproj ./
RUN dotnet restore TippSendApp.csproj
COPY . ./
RUN dotnet publish TippSendApp.csproj -c Release -o /out --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /out ./
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "TippSendApp.dll"]
