FROM node:22 AS client-build

WORKDIR /client

COPY client/ShareXHost.Client/package*.json ./
RUN npm install

COPY client/ShareXHost.Client/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/ShareXHost/ShareXHost.csproj", "src/ShareXHost/"]
RUN dotnet restore "src/ShareXHost/ShareXHost.csproj"
COPY . .
WORKDIR "/src/src/ShareXHost"
RUN dotnet build "./ShareXHost.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ShareXHost.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=client-build /client/dist ./wwwroot
ENTRYPOINT ["dotnet", "ShareXHost.dll"]
