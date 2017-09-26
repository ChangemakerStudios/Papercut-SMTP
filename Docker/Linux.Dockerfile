FROM microsoft/dotnet:1.1.4-runtime


COPY . /Papercut
WORKDIR /Papercut

EXPOSE 25 37408
ENTRYPOINT ["dotnet", "./Papercut.Service.dll"]