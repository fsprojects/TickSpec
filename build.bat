msbuild TickSpec.VS2008.sln /p:Configuration=Release
msbuild TickSpec.sln /p:Configuration=Release
msbuild TickSpec.Silverlight4.sln /p:Configuration=Release
msbuild TickSpec.Silverlight5.sln /p:Configuration=Release
cd Nuget\dotNet
..\..\Nuget.exe pack TickSpec.nuspec
cd ..\..
cd Nuget\Silverlight
..\..\Nuget.exe pack TickSpec.Silverlight.nuspec
cd ..\..

