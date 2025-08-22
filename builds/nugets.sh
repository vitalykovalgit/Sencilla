#!/bin/bash

# Version and build configuration
build="Debug"
version="9.0.0"

# Sencilla directories
dirRoot="/Users/vitalykoval/Work"
dirNugets="$dirRoot/Nugets"
dirSencilla="$dirRoot/Sencilla"
dirNugetsCache="$HOME/.nuget/packages"

# Define packages with their source paths
declare -a packagesInfo=(
    "Sencilla.Core:core"
    "Sencilla.Web:web"
    "Sencilla.Repository.HttpClient:repositories/HttpClient"
    "Sencilla.Repository.EntityFramework:repositories/EntityFramework"
    "Sencilla.Messaging:messaging/Core"
    "Sencilla.Messaging.MediatR:messaging/MediatR"
    "Sencilla.Messaging.Kafka:messaging/Kafka"
    "Sencilla.Messaging.RabbitMQ:messaging/RabbitMQ"
    "Sencilla.Messaging.ServiceBus:messaging/ServiceBus"
    "Sencilla.Messaging.InMemoryQueue:messaging/InMemoryQueue"
    "Sencilla.Messaging.Scheduler:messaging/Scheduler"
    "Sencilla.Component.I18n:components/I18n"
    "Sencilla.Component.Users:components/Users"
    "Sencilla.Component.Config:components/Config"
    "Sencilla.Component.Files:components/Files"
    "Sencilla.Component.FilesTus:components/FilesTus"
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
    printf "\033[0;37mCopying $name.$version.nupkg\033[0m"
    if [ -f "$sourceFile" ]; then
        cp -f "$sourceFile" "$dirNugets/"
        printf " - \033[0;32mOK\033[0m\n"
    else
        printf " - \033[0;31mFAILED\033[0m\n"
    fi
done

printf "\033[0;32mUpdate script completed!\033[0m\n"