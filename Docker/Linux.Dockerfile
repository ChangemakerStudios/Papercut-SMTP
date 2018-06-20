FROM microsoft/dotnet:2.1-aspnetcore-runtime


COPY . /Papercut
WORKDIR /Papercut

EXPOSE 25 37408
ENTRYPOINT ["dotnet", "./Papercut.Service.dll"]