dotnet build -c Release
dotnet publish -c Release -r linux-x64 --self-contained true -o publish
cp -r ./publish/* ~/.local/opt/diagnostics
