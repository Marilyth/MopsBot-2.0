#!/bin/sh

if [ "$1" = "update" ]; then
	echo "Pulling docker images..."
	docker pull marilyth/mopsbot
	echo "echo Cleaning up previous container..."
	docker stop mopsbot
	docker rm mopsbot
	echo "Creating new container..."
	docker create -it --log-opt max-size=10m --log-opt max-file=5 -v $(pwd)/mopsdata:/publish/mopsdata -p 5000:5000 --expose=5000 --restart unless-stopped --name mopsbot marilyth/mopsbot
	echo "Done! You can now restart Mops with 'docker start mopsbot'"
	exit
fi

if [ "$1" = "verbose" ] || [ "$2" = "verbose" ]; then
    	echo "Pulling docker images..."
	docker pull mongo
	docker pull marilyth/mopsbot

	echo "Cleaning up potential previous setup..."
	docker stop mopsdb
	docker stop mopsbot
	if [ "$1" = "clean" ] || [ "$2" = "clean" ]; then
    		rm -r ./database
	fi
	mkdir ./database
	docker rm mopsdb
	docker rm mopsbot

	echo "Creating containers..."
	docker run -it --log-opt max-size=10m --log-opt max-file=5 -v $(pwd)/database:/data/db -v $(pwd)/mongouser:/mongodata -p 27017:27017 --expose=27017 --name mopsdb -d mongo
	docker create -it --log-opt max-size=10m --log-opt max-file=5 -v $(pwd)/mopsdata:/publish/mopsdata -p 5000:5000 --expose=5000 --restart unless-stopped --name mopsbot marilyth/mopsbot

	echo "Waiting for MongoDB to start..."
	while :
	do
  		log=$(docker logs mopsdb)
  		if echo "$log" | grep -qe '.*Listening on.*' ; then
    			break
  		fi
  		sleep 1
	done

	echo "Securing MongoDB..."
	docker exec -it mopsdb mongo /mongodata/createUser.js

	docker stop mopsdb
	docker rm mopsdb

	echo "Creating secure MongoDB Container..."
	docker create -it --log-opt max-size=10m --log-opt max-file=5 -v $(pwd)/database:/data/db -p 27017:27017 --expose=27017 --restart unless-stopped --name mopsdb mongo mongod --auth
	docker start mopsdb

	echo "Done!"

else

	echo "Pulling docker images..."
	docker pull mongo >/dev/null
	docker pull marilyth/mopsbot >/dev/null

	echo "Cleaning up potential previous setup..."
	docker stop mopsdb >/dev/null 2>&1
	docker stop mopsbot >/dev/null 2>&1
	if [ "$1" = "clean" ] || [ "$2" = "clean" ]; then
    		rm -r ./database >/dev/null 2>&1
	fi
	mkdir ./database >/dev/null 2>&1
	docker rm mopsdb >/dev/null 2>&1
	docker rm mopsbot >/dev/null 2>&1

	echo "Creating containers..."
	docker run -it --log-opt max-size=10m --log-opt max-file=5 -v $(pwd)/database:/data/db -v $(pwd)/mongouser:/mongodata -p 27017:27017 --expose=27017 --name mopsdb -d mongo
	docker create -it --log-opt max-size=10m --log-opt max-file=5 -v $(pwd)/mopsdata:/publish/mopsdata -p 5000:5000 --expose=5000 --restart unless-stopped --name mopsbot marilyth/mopsbot

	echo "Waiting for MongoDB to start..."
	while :
	do
  		log=$(docker logs mopsdb)
  		if echo "$log" | grep -qe '.*Listening on.*' ; then
    			break
  		fi
  		sleep 1
	done

	echo "Securing MongoDB..."
	docker exec -it mopsdb mongo /mongodata/createUser.js >/dev/null

	docker stop mopsdb >/dev/null
	docker rm mopsdb >/dev/null

	echo "Creating secure MongoDB Container..."
	docker create -it --log-opt max-size=10m --log-opt max-file=5 -v $(pwd)/database:/data/db -p 27017:27017 --expose=27017 --restart unless-stopped --name mopsdb mongo mongod --auth
	docker start mopsdb

	echo "Done!"

fi
