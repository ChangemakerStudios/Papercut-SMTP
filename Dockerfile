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
WORKDIR /app
COPY --from=publish /app/publish .

ENV ASPNETCORE_HTTP_PORTS=80

# HTTP
EXPOSE 80
 
# SMTP
EXPOSE 25 

# optional -- should only be used locally: IPComm
# EXPOSE 37403 

CMD ["dotnet", "Papercut.Service.dll"]
