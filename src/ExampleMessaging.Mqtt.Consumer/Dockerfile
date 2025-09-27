# Use the official .NET 8 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Set working directory
WORKDIR /app

# Copy solution file and project files
COPY *.sln .
COPY src/ExampleMessaging.Shared/*.csproj ./src/ExampleMessaging.Shared/
COPY src/ExampleMessaging.Mqtt.Consumer/*.csproj ./src/ExampleMessaging.Mqtt.Consumer/

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY src/ExampleMessaging.Shared/ ./src/ExampleMessaging.Shared/
COPY src/ExampleMessaging.Mqtt.Consumer/ ./src/ExampleMessaging.Mqtt.Consumer/

# Build the application
WORKDIR /app/src/ExampleMessaging.Mqtt.Consumer
RUN dotnet publish -c Release -o /app/publish --no-restore

# Use the official .NET 8 runtime image for running
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime

# Set working directory
WORKDIR /app

# Create a non-root user for security
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Create logs directory and set permissions
RUN mkdir -p /app/logs && chown -R appuser:appuser /app

# Copy the published application
COPY --from=build /app/publish .

# Change ownership of the application files
RUN chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Set environment variables
ENV DOTNET_ENVIRONMENT=Production

# Entry point
ENTRYPOINT ["dotnet", "ExampleMessaging.Mqtt.Consumer.dll"]
