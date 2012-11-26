msbuild TickSpec.VS2008.sln /p:Configuration=Release
msbuild TickSpec.sln /p:Configuration=Release
msbuild TickSpec.Silverlight4.sln /p:Configuration=Release
msbuild TickSpec.Silverlight5.sln /p:Configuration=Release
cd Nuget
..\Nuget.exe pack TickSpec.nuspec
cd ..

