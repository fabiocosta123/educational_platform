FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY EducationalPlataform.csproj ./
RUN dotnet restore EducationalPlataform.csproj

COPY . ./
RUN dotnet publish EducationalPlataform.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

RUN mkdir -p /app/wwwroot/uploads/lessons

COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

ENTRYPOINT ["dotnet", "EducationalPlataform.dll"]
