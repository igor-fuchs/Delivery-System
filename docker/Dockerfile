FROM mcr.microsoft.com/dotnet/sdk:10.0

WORKDIR /workspace

EXPOSE 8080

CMD ["sh", "-c", "dotnet restore && exec dotnet watch run --project src/Presentation/Presentation.csproj"]
