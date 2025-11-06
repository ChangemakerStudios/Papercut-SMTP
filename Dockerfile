FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
ARG BUILD_VERSION=1.0.0.0
WORKDIR /work
COPY ["src/Papercut.Service/Papercut.Service.csproj", "/work/Papercut.Service/"]
COPY ["src/Papercut.Common/Papercut.Common.csproj", "/work/Papercut.Common/"]
COPY ["src/Papercut.Core/Papercut.Core.csproj", "/work/Papercut.Core/"]
COPY ["src/Papercut.Infrastructure.IPComm/Papercut.Infrastructure.IPComm.csproj", "/work/Papercut.Infrastructure.IPComm/"]
COPY ["src/Papercut.Infrastructure.Smtp/Papercut.Infrastructure.Smtp.csproj", "/work/Papercut.Infrastructure.Smtp/"]
COPY ["src/Papercut.Message/Papercut.Message.csproj", "/work/Papercut.Message/"]
COPY ["src/Papercut.Rules/Papercut.Rules.csproj", "/work/Papercut.Rules/"]
RUN dotnet restore "/work/Papercut.Service/Papercut.Service.csproj"
COPY . .

#RUN sed "s/\(Assembly\(Informational\|File\)Version(\d34[0-9]\+\.[0-9]\+\.[0-9]\+\.\)[0-9]\+/\1$BUILD_VERSION/" src/GlobalAssemblyInfo.cs

WORKDIR "/work/src/Papercut.Service"

RUN dotnet build "Papercut.Service.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Papercut.Service.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final

ARG BUILD_VERSION
ARG BUILD_DATE
ARG VCS_REF

LABEL org.opencontainers.image.title="Papercut SMTP Service" \
      org.opencontainers.image.description="Papercut SMTP Service is a 2-in-1 quick email viewer AND built-in SMTP server" \
      org.opencontainers.image.version=${BUILD_VERSION} \
      org.opencontainers.image.url="https://www.papercut-smtp.com/" \
      org.opencontainers.image.source="https://github.com/ChangemakerStudios/Papercut-SMTP" \
      org.opencontainers.image.created=${BUILD_DATE} \
      org.opencontainers.image.revision=${VCS_REF} \
      org.opencontainers.image.licenses="Apache License, Version 2.0"

WORKDIR /app

COPY --from=publish /app/publish .

# Use Production environment which configures non-privileged ports (2525, 8080)
ENV ASPNETCORE_ENVIRONMENT=Production

# HTTP (non-privileged port)
EXPOSE 8080

# SMTP (non-privileged port - use 587 for STARTTLS)
EXPOSE 2525

# Optional STARTTLS port (requires certificate configuration)
# EXPOSE 587

# Optional TLS/SSL port (requires certificate configuration)
# EXPOSE 465

# optional -- should only be used locally: IPComm
# EXPOSE 37403

CMD ["dotnet", "Papercut.Service.dll"]
