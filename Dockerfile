FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["todo-dotnet6.csproj", "."]
RUN dotnet restore "todo-dotnet6.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "todo-dotnet6.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "todo-dotnet6.csproj" -c Release -o /app/publish

FROM base AS final
LABEL org.opencontainers.image.source="https://github.com/thangchung/todo-bicep/todo-container"
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "todo-dotnet6.dll"]
