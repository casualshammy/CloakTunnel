@RD /S /Q "./output"
dotnet publish -c release -r ubuntu.18.04-x64 --self-contained -o ./output
powershell Compress-Archive output\* slowudppipeserver.zip
@RD /S /Q "./output"
pause