FROM microsoft/dotnet:2.1-aspnetcore-runtime-nanoserver-1803


COPY . C:\\Papercut
WORKDIR C:\\Papercut

EXPOSE 25 37408
ENTRYPOINT ["dotnet", ".\\Papercut.Service.dll"]