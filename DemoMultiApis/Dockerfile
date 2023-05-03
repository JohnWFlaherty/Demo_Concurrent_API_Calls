#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["DemoMultiApis/DemoMultiApis.csproj", "DemoMultiApis/"]
RUN dotnet restore "DemoMultiApis/DemoMultiApis.csproj"
COPY . .
WORKDIR "/src/DemoMultiApis"
RUN dotnet build "DemoMultiApis.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DemoMultiApis.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DemoMultiApis.dll"]