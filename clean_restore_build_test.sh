dotnet clean api/api.csproj
dotnet clean api.Tests.Integration/api.Tests.Integration.csproj
dotnet nuget locals all --clear
dotnet restore api/api.csproj
dotnet restore api.Tests.Integration/api.Tests.Integration.csproj
dotnet build api/api.csproj
dotnet build api.Tests.Integration/api.Tests.Integration.csproj
dotnet test api.Tests.Integration/api.Tests.Integration.csproj --logger "console;verbosity=detailed"
