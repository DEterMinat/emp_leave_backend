# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore
COPY ["src/EmployeeLeaveApi/EmployeeLeaveApi.csproj", "src/EmployeeLeaveApi/"]
RUN dotnet restore "src/EmployeeLeaveApi/EmployeeLeaveApi.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/src/EmployeeLeaveApi"
RUN dotnet build "EmployeeLeaveApi.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "EmployeeLeaveApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create uploads directory
RUN mkdir -p /app/wwwroot/uploads

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "EmployeeLeaveApi.dll"]
