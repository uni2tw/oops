dotnet publish -c release
copy %cd%\assets\*.* %cd%\bin\release\net5.0\linux-x64\publish\assets\