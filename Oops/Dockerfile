#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Oops/oops.csproj", "Oops/"]
RUN dotnet restore "Oops/oops.csproj"
COPY . .
WORKDIR "/src/Oops"
RUN dotnet build "oops.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "oops.csproj" -c Release -o /app/publish /p:EnvironmentName=prod

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "oops.dll"]