# Minimal Dockerfile - uses pre-published output, only needs ASP.NET Runtime (no SDK).
# Build with: docker build -t paperless-forms:latest -f Dockerfile.publish /tmp/published_app
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY . .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "PaperlessForms.Web.dll"]
