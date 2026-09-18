#if (framework == "net10.0")
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
#else
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
#endif
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

#if (framework == "net10.0")
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
#else
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
#endif
ARG BUILD_CONFIGURATION=Release
WORKDIR /build
#if (MultiProject)
COPY ["src/MCA.Api/MCA.Api.csproj", "src/MCA.Api/"]
COPY ["src/MCA.Application/MCA.Application.csproj", "src/MCA.Application/"]
COPY ["src/MCA.Domain/MCA.Domain.csproj", "src/MCA.Domain/"]
COPY ["src/MCA.Infrastructure/MCA.Infrastructure.csproj", "src/MCA.Infrastructure/"]
RUN dotnet restore "src/MCA.Api/MCA.Api.csproj"
COPY . .
WORKDIR "/build/src/MCA.Api"
RUN dotnet build "MCA.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build
#else
COPY ["MCA.csproj", "."]
RUN dotnet restore "MCA.csproj"
COPY . .
WORKDIR "/build"
RUN dotnet build "MCA.csproj" -c $BUILD_CONFIGURATION -o /app/build
#endif

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
#if (MultiProject)
RUN dotnet publish "MCA.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false
#else
RUN dotnet publish "MCA.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false
#endif

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
#if (MultiProject)
ENTRYPOINT ["dotnet", "MCA.Api.dll"]
#else
ENTRYPOINT ["dotnet", "MCA.dll"]
#endif
