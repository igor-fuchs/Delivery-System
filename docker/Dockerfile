FROM mcr.microsoft.com/dotnet/sdk:10.0

WORKDIR /workspace

EXPOSE 8080

CMD ["sh", "-c", "dotnet restore src/Presentation/Presentation.csproj && exec dotnet watch run --no-restore --project src/Presentation/Presentation.csproj"]
