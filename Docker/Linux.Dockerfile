FROM microsoft/dotnet:2.0-runtime-jessie


COPY . /Papercut
WORKDIR /Papercut

EXPOSE 25 37408
ENTRYPOINT ["dotnet", "./Papercut.Service.dll"]