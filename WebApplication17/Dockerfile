FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["WebApplication17.csproj", "WebApplication17/"]
RUN dotnet restore "WebApplication17/WebApplication17.csproj"
COPY . WebApplication17/
WORKDIR "/src/WebApplication17"
RUN dotnet build "WebApplication17.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "WebApplication17.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM docker:dind
USER root

RUN apk update && apk add --no-cache \
    icu-libs \
    krb5-libs \
    libgcc \
    libintl \
    libssl3 \
    libstdc++ \
    zlib \
    bash \
    wget \
    docker-cli \
    procps \
    curl

RUN wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh -O dotnet-install.sh && \
    chmod +x ./dotnet-install.sh && \
    ./dotnet-install.sh --channel 8.0 --install-dir /usr/share/dotnet && \
    ln -s /usr/share/dotnet/dotnet /usr/bin/dotnet && \
    rm dotnet-install.sh
    
WORKDIR /app
COPY --from=publish /app/publish .
COPY entrypoint.sh .
COPY ./Executor .

EXPOSE 80

ENTRYPOINT ["/app/entrypoint.sh"]