@echo off
echo Pulling base Docker images for BoltTickets project...

docker pull mcr.microsoft.com/dotnet/sdk:9.0
if %errorlevel% neq 0 (
    echo Failed to pull .NET SDK 9.0
    goto :error
)

docker pull mcr.microsoft.com/dotnet/aspnet:9.0
if %errorlevel% neq 0 (
    echo Failed to pull ASP.NET 9.0
    goto :error
)

docker pull mcr.microsoft.com/dotnet/runtime:9.0
if %errorlevel% neq 0 (
    echo Failed to pull .NET Runtime 9.0
    goto :error
)

docker pull node:18-alpine
if %errorlevel% neq 0 (
    echo Failed to pull Node.js 18 Alpine
    goto :error
)

docker pull nginx:alpine
if %errorlevel% neq 0 (
    echo Failed to pull Nginx Alpine
    goto :error
)

echo All base images pulled and cached locally successfully.
echo You can now run the deployment script without downloading images.
pause
exit /b 0

:error
echo An error occurred while pulling images.
pause
exit /b 1