#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443
RUN apt-get update --yes && apt-get dist-upgrade --yes && apt-get install --yes libgdiplus
RUN mkdir -p /image-server
VOLUME /image-server

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["ImageServer/ImageServer.csproj", "ImageServer/"]
RUN dotnet restore "ImageServer/ImageServer.csproj"
COPY . .
WORKDIR "/src/ImageServer"
RUN dotnet build "ImageServer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ImageServer.csproj" -c Release -o /app/publish

FROM base AS final

WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ImageServer.dll"]
