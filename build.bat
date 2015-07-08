@echo off
cls

IF NOT EXIST build\FAKE (tools\nuget\nuget.exe Install FAKE -OutputDirectory "build" -ExcludeVersion)
build\FAKE\tools\Fake.exe build.fsx %*
