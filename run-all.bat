@echo off
cd /d %~dp0
mkdir logs 2>nul
echo Starting BoltTickets Application from %cd%

echo Checking prerequisites...
where docker-compose >nul 2>&1
if %errorlevel% neq 0 (
    echo docker-compose not found! Please install Docker.
    pause
    exit /b 1
)

where dotnet >nul 2>&1
if %errorlevel% neq 0 (
    echo dotnet not found! Please install .NET SDK.
    pause
    exit /b 1
)

where npm >nul 2>&1
if %errorlevel% neq 0 (
    echo npm not found! Please install Node.js.
    pause
    exit /b 1
)

echo Starting Docker services...
docker-compose up -d
if %errorlevel% neq 0 (
    echo Docker Compose failed!
    pause
    exit /b 1
)

echo Waiting for services to be ready...
timeout /t 10 /nobreak > nul

echo Stopping any running .NET processes...
taskkill /f /im dotnet.exe >nul 2>&1

echo Building .NET projects...
dotnet build src
if %errorlevel% neq 0 (
    echo Build failed!
    exit /b 1
)

echo Starting API...
start "API" cmd /c "dotnet run --project src/BoltTickets.API"

echo Starting Worker...
start "Worker" cmd /c "dotnet run --project src/BoltTickets.Worker"

echo Stopping any running Node.js processes...
taskkill /f /im node.exe >nul 2>&1

echo Starting Frontend...
cd src\BoltTickets.Frontend
if not exist node_modules (
    echo Installing npm dependencies...
    npm install
    if %errorlevel% neq 0 (
        echo npm install failed!
        exit /b 1
    )
)
start "Frontend" cmd /c "npm run dev > ..\logs\frontend.log 2>&1"

cd ..
echo Waiting for services to start...
timeout /t 5 /nobreak > nul

echo Opening browsers...
start http://localhost:5162/swagger
start http://localhost:5173

echo All services started!
echo API: http://localhost:5162/swagger
echo Frontend: http://localhost:5173
echo Infrastructure: See docker-compose.yml for ports
pause