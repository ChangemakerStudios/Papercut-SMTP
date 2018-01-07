FROM microsoft/dotnet:2.0-runtime-nanoserver


COPY . C:\\Papercut
WORKDIR C:\\Papercut

EXPOSE 25 37408
ENTRYPOINT ["dotnet", ".\\Papercut.Service.dll"]