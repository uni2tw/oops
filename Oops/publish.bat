dotnet publish -c release
if not exist "%cd%\bin\release\net5.0\linux-x64\publish\assets" md "%cd%\bin\release\net5.0\linux-x64\publish\assets"
copy %cd%\assets\*.* %cd%\bin\release\net5.0\linux-x64\publish\assets\