echo off

pushd .
cd %~dp0%
nuget pack
popd