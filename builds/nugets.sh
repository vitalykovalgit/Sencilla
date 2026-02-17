#!/bin/bash

# Version and build configuration
build="Debug"
version="10.0.0"

# Sencilla directories
dirRoot="/Users/vitalykoval/Work"
dirNugets="$dirRoot/Nugets"
dirSencilla="$dirRoot/Sencilla"
dirNugetsCache="$HOME/.nuget/packages"

# Define packages with their source paths
declare -a packagesInfo=(
    "Sencilla.Core:core"
    "Sencilla.Web:web"
    "Sencilla.Web.MinimalApi:webapi/MinimalApi"

    "Sencilla.Repository.HttpClient:repositories/HttpClient"
    "Sencilla.Repository.EntityFramework:repositories/EntityFramework"

    "Sencilla.Scheduler:scheduler/Core"
    "Sencilla.Scheduler.EntityFramework:scheduler/EntityFramework"
    "Sencilla.Scheduler.SourceGenerator:scheduler/SourceGenerator"
    
    "Sencilla.Messaging:messaging/Core"
    "Sencilla.Messaging.Mediator:messaging/Mediator"
    "Sencilla.Messaging.Kafka:messaging/Kafka"
    "Sencilla.Messaging.RabbitMQ:messaging/RabbitMQ"
    "Sencilla.Messaging.ServiceBus:messaging/ServiceBus"
    "Sencilla.Messaging.InMemoryQueue:messaging/InMemoryQueue"
    "Sencilla.Messaging.Scheduler:messaging/Scheduler"
    "Sencilla.Messaging.SourceGenerator:messaging/SourceGenerator"
    
    "Sencilla.Component.Files:files/Core"
    "Sencilla.Component.Files.LocalDrive:files/LocalDrive"
    "Sencilla.Component.Files.AzureStorage:files/AzureStorage"
    "Sencilla.Component.Files.GoogleStorage:files/GoogleStorage"
    "Sencilla.Component.Files.AmazonS3:files/AmazonS3"
    "Sencilla.Component.Files.Database:files/Database"

    "Sencilla.Component.I18n:components/I18n"
    "Sencilla.Component.Users:components/Users"
    "Sencilla.Component.Config:components/Config"
    "Sencilla.Component.Security:components/Security"
    "Sencilla.Component.Geography:components/Geography"
    
    "Microsoft.EntityFrameworkCore.Extension:extensions/EntityFrameworkCore"
)

printf "\033[1;33mCleaning old packages...\033[0m\n"
for pkg in "${packagesInfo[@]}"; do
    name="${pkg%%:*}"
    packagePath="$dirNugetsCache/$name"
    printf "\033[0;31mCleaning\033[0m $packagePath\033[0m\n"
    rm -rf "$packagePath"
done
echo ""

printf "\033[1;33mCopying new packages...\033[0m\n"
for pkg in "${packagesInfo[@]}"; do
    name="${pkg%%:*}"
    path="${pkg##*:}"
    sourceFile="$dirSencilla/libs/$path/bin/$build/$name.$version.nupkg"
    #printf "\033[0;37mCopying sourceFile: $sourceFile\033[0m\n"
    printf "\033[0;37mCopying $name.$version.nupkg\033[0m"
    if [ -f "$sourceFile" ]; then
        cp -f "$sourceFile" "$dirNugets/"
        printf " - \033[0;32mOK\033[0m\n"
    else
        printf " - \033[0;31mFAILED\033[0m\n"
    fi
done

printf "\033[0;32mUpdate script completed!\033[0m\n"