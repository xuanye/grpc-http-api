set -ex

cd $(dirname $0)/../

artifactsFolder="./artifacts"

if [ -d $artifactsFolder ]; then
  rm -R $artifactsFolder
fi

mkdir -p $artifactsFolder

dotnet build ./src/Grape.Grpc.HttpApi/Grape.Grpc.HttpApi.csproj -c Release

dotnet pack ./src/Grape.Grpc.HttpApi/Grape.Grpc.HttpApi.csproj -c Release -o ./$artifactsFolder

dotnet nuget push ./$artifactsFolder/Grape.Grpc.HttpApi.*.nupkg -k $NUGET_KEY -s https://www.nuget.org