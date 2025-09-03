# Version and build configuration
$build = "Debug"
$version = "9.0.0"

# Sencilla directories
$dirRoot = "D:\work\"
$dirNugets = Join-Path $dirRoot "Nugets"
$dirSencilla = Join-Path $dirRoot "Sencilla"
$dirNugetsCache = Join-Path $env:USERPROFILE ".nuget\packages\"

# Define packages with their source paths
$packagesInfo = @(
    @{ Name = "Sencilla.Core"; Path = "core" },
    @{ Name = "Sencilla.Web"; Path = "web" },

    @{ Name = "Sencilla.Repository.HttpClient"; Path = "repositories\HttpClient" },
    @{ Name = "Sencilla.Repository.EntityFramework"; Path = "repositories\EntityFramework" },

    @{ Name = "Sencilla.Scheduler"; Path = "scheduler\Core" },
    @{ Name = "Sencilla.Scheduler.EntityFramework"; Path = "scheduler\EntityFramework" },
    @{ Name = "Sencilla.Scheduler.SourceGenerator"; Path = "scheduler\SourceGenerator" },
    
    @{ Name = "Sencilla.Messaging"; Path = "messaging\Core" },
    @{ Name = "Sencilla.Messaging.Mediator"; Path = "messaging\Mediator" },
    @{ Name = "Sencilla.Messaging.Kafka"; Path = "messaging\Kafka" },
    @{ Name = "Sencilla.Messaging.RabbitMQ"; Path = "messaging\RabbitMQ" },
    @{ Name = "Sencilla.Messaging.ServiceBus"; Path = "messaging\ServiceBus" },
    @{ Name = "Sencilla.Messaging.InMemoryQueue"; Path = "messaging\InMemoryQueue" },
    @{ Name = "Sencilla.Messaging.Scheduler"; Path = "messaging\Scheduler" },
    @{ Name = "Sencilla.Messaging.SourceGenerator"; Path = "messaging\SourceGenerator" },

    @{ Name = "Sencilla.Component.I18n"; Path = "components\I18n" },
    @{ Name = "Sencilla.Component.Users"; Path = "components\Users" },
    @{ Name = "Sencilla.Component.Config"; Path = "components\Config" },
    @{ Name = "Sencilla.Component.Files"; Path = "components\Files" },
    @{ Name = "Sencilla.Component.FilesTus"; Path = "components\FilesTus" },    
    @{ Name = "Sencilla.Component.Security"; Path = "components\Security" },
    @{ Name = "Sencilla.Component.Geography"; Path = "components\Geography" },
    
    @{ Name = "Microsoft.EntityFrameworkCore.Extension"; Path = "extensions\EntityFrameworkCore" }
)

# Clean packages using loop
Write-Host "Cleaning old packages..." -ForegroundColor Yellow
foreach ($pkg in $packagesInfo) {
    $packagePath = Join-Path $dirNugetsCache $pkg.Name
    Write-Host "Cleaning $packagePath" -ForegroundColor Gray
    if (Test-Path $packagePath) {
        Remove-Item $packagePath -Recurse -Force -ErrorAction SilentlyContinue
    }
}
Write-Host "";

# Copy packages using loop
Write-Host "Copying new packages..." -ForegroundColor Yellow
foreach ($pkg in $packagesInfo) {
    $sourceFile = Join-Path $dirSencilla "libs\$($pkg.Path)\bin\$build\$($pkg.Name).$version.nupkg"
    
    Write-Host "Copying $($pkg.Name).$version.nupkg" -ForegroundColor Gray -NoNewline
    if (Test-Path $sourceFile) {
        Copy-Item $sourceFile -Destination $dirNugets -Force
        Write-Host " - OK" -ForegroundColor Green
    } else {
        Write-Host " - FAILED" -ForegroundColor Red
    }
}

Write-Host "Update script completed!" -ForegroundColor Green
# Read-Host "Press Enter to exit"