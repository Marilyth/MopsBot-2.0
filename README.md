# MopsBot-2.0
MopsBot converted to Discord.Net 2.0

Mainly a Tracker. Keeps track of Twitch streamers/clips, Youtubers, Twitters, Reddit, JSON, RSS and all sorts of nice things. 

1. Head over to https://github.com/Marilyth/MopsBot-2.0/releases and download the latest release of MopsSelfHost.zip

2. Unpack it at a location you want to have Mops running in.

3. Make sure docker is installed on your system, so that calling "docker" in your terminal yields a result.
   Also make sure the port 5000 (mops webhooks) is open and forwarded.

4. On Windows, run "setup.bat" inside the folder the .bat resides in.
   On Linux, run "./setup.sh" inside the folder the .sh resides in.
   
5. Done. You can not modify the Config.json and TrackerLimits.json in the ./mopsdata folder and start Mops with
   Start Mops with "docker start mopsbot" when you are done.
   
   
   
If you want to use a service that requires API keys, you can obtain them here:

Twitch:
https://dev.twitch.tv/dashboard/apps/create

Twitter:
https://developer.twitter.com/en/account/get-started

Youtube:
https://developers.google.com/youtube/v3/getting-started#before-you-start

Osu (currently not working):
https://osu.ppy.sh/home/account/edit#oauth

Steam:
https://steamcommunity.com/dev/apikey
