# BUILD FIX - CLDV7112w

The original project targeted .NET 10, but the installed Visual Studio in the student's environment does not support .NET 10.

This version targets **.NET 9.0** and pins the SDK to **9.0.317**, which matches the SDK shown in the Visual Studio build output.

## Build

1. Open `ABC.Retail.sln`.
2. Restore NuGet packages.
3. Build > Rebuild Solution.
4. Confirm Error List shows 0 errors.
5. Run the project.

## If Visual Studio says the SDK is missing

Open Developer PowerShell and run:

```powershell
dotnet --version
dotnet --list-sdks
```

The project expects 9.0.317 (or a later 9.0 patch if you change `global.json`).

## Azure deployment

The application remains an ASP.NET Core application and can be deployed to Azure App Service. Configure the Azure Storage connection string under App Service > Environment variables using:

`ConnectionStrings__AzureStorage`
