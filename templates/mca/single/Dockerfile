FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
#if (MultiProject)
COPY ["MCA.Api/MCA.Api.csproj", "MCA.Api/"]
COPY ["MCA.Application/MCA.Application.csproj", "MCA.Application/"]
COPY ["MCA.Domain/MCA.Domain.csproj", "MCA.Domain/"]
COPY ["MCA.Infrastructure/MCA.Infrastructure.csproj", "MCA.Infrastructure/"]
RUN dotnet restore "MCA.Api/MCA.Api.csproj"
COPY . .
WORKDIR "/src/MCA.Api"
RUN dotnet build "MCA.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build
#else
COPY ["MCA.csproj", "."]
RUN dotnet restore "MCA.csproj"
COPY . .
WORKDIR "/src"
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
