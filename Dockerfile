# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["SunPhim.csproj", "./"]
RUN dotnet restore

COPY . .
RUN dotnet publish SunPhim.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# cai dat charset UTF-8 cho tieng Viet
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=0
RUN apt-get update && apt-get install -y --no-install-recommends \
    icu-libs \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

EXPOSE 80
EXPOSE 443

ENTRYPOINT ["dotnet", "SunPhim.dll"]
