@echo off
if "%1" == "update" (goto update)

if "%1" == "verbose" (goto verbose)
if "%2" == "verbose" (goto verbose)

echo Pulling docker images...
docker pull mongo> nul
docker pull marilyth/mopsbot> nul

echo Cleaning up potential previous setup...
docker stop mopsdb> nul 2>nul
docker stop mopsbot> nul 2>nul
docker rm mopsdb> nul 2>nul
docker rm mopsbot> nul 2>nul
if "%1" == "clean" (rmdir /Q /s %cd%\\database> nul 2>nul)
if "%2" == "clean" (rmdir /Q /s %cd%\\database> nul 2>nul)
mkdir %cd%\\database> nul 2>nul

echo Creating containers...
docker run -it --log-opt max-size=10m --log-opt max-file=5 -v %cd%\\database:/data/db -v %cd%\\mongouser:/mongodata -p 27017:27017 --expose=27017 --name mopsdb -d mongo
docker create -it --log-opt max-size=10m --log-opt max-file=5 -v %cd%\\mopsdata:/publish/mopsdata -p 5000:5000 --expose=5000 --restart unless-stopped --name mopsbot marilyth/mopsbot

echo Securing mongodb...
:loop
timeout /t 1 /nobreak > nul
(docker logs mopsdb |find "Listening on") > nul 2>&1
if errorlevel 1 goto loop

docker exec -it mopsdb mongo /mongodata/createUser.js

docker stop mopsdb> nul
docker rm mopsdb> nul

echo Creating secure mongodb Container
docker create -it --log-opt max-size=10m --log-opt max-file=5 -v %cd%\\database:/data/db -p 27017:27017 --expose=27017 --restart unless-stopped --name mopsdb mongo mongod --auth
docker start mopsdb

echo Done!
exit

:verbose

echo Pulling docker images...
docker pull mongo
docker pull marilyth/mopsbot

echo Cleaning up potential previous setup...
docker stop mopsdb
docker stop mopsbot
docker rm mopsdb
docker rm mopsbot
if "%1" == "clean" (rmdir /Q /s %cd%\\database> nul 2>nul)
if "%2" == "clean" (rmdir /Q /s %cd%\\database> nul 2>nul)
mkdir %cd%\\database

echo Creating containers...
docker run -it --log-opt max-size=10m --log-opt max-file=5 -v %cd%\\database:/data/db -v %cd%\\mongouser:/mongodata -p 27017:27017 --expose=27017 --name mopsdb -d mongo
docker create -it --log-opt max-size=10m --log-opt max-file=5 -v %cd%\\mopsdata:/publish/mopsdata -p 5000:5000 --expose=5000 --restart unless-stopped --name mopsbot marilyth/mopsbot

echo Securing mongodb...
:loop
timeout /t 1 /nobreak > nul
(docker logs mopsdb |find "Listening on")
if errorlevel 1 goto loop

docker exec -it mopsdb mongo /mongodata/createUser.js

docker stop mopsdb
docker rm mopsdb

echo Creating secure mongodb Container
docker create -it --log-opt max-size=10m --log-opt max-file=5 -v %cd%\\database:/data/db -p 27017:27017 --expose=27017 --restart unless-stopped --name mopsdb mongo mongod --auth
docker start mopsdb

echo Done!

:update
echo Pulling latest docker image...
docker pull marilyth/mopsbot
echo Cleaning up previous container...
docker stop mopsbot> nul 2>nul
docker rm mopsbot> nul 2>nul
echo Creating new container...
docker create -it --log-opt max-size=10m --log-opt max-file=5 -v %cd%\\mopsdata:/publish/mopsdata -p 5000:5000 --expose=5000 --restart unless-stopped --name mopsbot marilyth/mopsbot
echo Done! You can now restart Mops with "docker start mopsbot"
exit