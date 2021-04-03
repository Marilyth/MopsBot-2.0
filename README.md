# MopsBot-2.0
MopsBot converted to Discord.Net 2.0

Mainly a Tracker. Keeps track of Twitch streamers/clips, Youtubers, Twitters, Reddit, JSON, RSS and all sorts of nice things. 

# Setting it up
1. Head over to [Releases](https://github.com/Marilyth/MopsBot-2.0/releases) and download the latest release of MopsSelfHost.zip

2. Unpack it at a location you want to have Mops running in.

3. Make sure [Docker](https://docs.docker.com/engine/install/) is installed on your system, so that calling `docker` in your terminal yields a result.
   Also make sure the port 5000 (mops webhooks) is open and forwarded.

4. Please make sure your database will be secure, by following these [steps](#mongodb-security).

5. On Windows, run `setup.bat` inside the folder the .bat resides in.

   On Linux, run `sudo ./setup.sh` inside the folder the .sh resides in. Make it executable first via `chmod +x ./setup.sh`.

   On Raspbian, not yet supported/tested!
   
6. Done. You can now modify the [Config.json](#config-entries) and [TrackerLimits.json](#trackerlimits-entries) in the ./mopsdata folder and start Mops with <code>docker start mopsbot</code> after you are done.

7. By running `setup.bat update` or `sudo ./setup.sh update` your mopsbot will be updated to the latest version on dockerhub.

# MongoDB security
Your Database is open to the public with a pretty weak account and password!  
To avoid being hacked, do the following:  
1. Open the mongouser/createUser.js and change the entries for <code>user:</code> and <code>pwd:</code> to something secure.
2. Open the mopsdata/Config.json and change the DatabaseURL to <code>mongodb://username:password@172.17.0.1:27017</code>, with your information replacing <code>username</code> and <code>password</code>

# Config entries
Make sure to always enclose both the key and values of the .json in quotation marks!  
- **DiscordToken**: The token of your Discord Bot, required to log in.
- **DatabaseURL**: The URL and accounts/password on which to access the Mongo Database. Doesn't need to be updated if you don't care about security.
- ExceptionLogChannel (optional): The numeric ID of the Discord channel you want exceptions to be logged in.
- CommandLogChannel (optional): The numeric ID of the Discord channel you want executed commands to be logged in.
- **ServerAddress** (optional, but important): The http URL under which your Server can be reached. Replace localhost with your servers IP or name.  
  Non-localhost is required for any webhook Trackers and graphs.  
  For example, the entry looks like <code>http://37.221.195.236</code> for Mops.
- TwitchKey/TwitchSecret (optional): The API Key and Secret to be used in Twitch API calls.
- TwitterKey/TwitterSecret/TwitterToken/TwitterAccessSecret (optional): The API Information to be used in Twitter API calls.
- YoutubeKey (optional): The API Key to be used in Youtube API calls.
- SteamKey (optional): The API Key to be used in Steam API calls.
- OsuKey (optional): The API Key to be used in osu! API calls. Currently not working/outdated.
- BotManager (optional): A colon seperated list of the Discord user IDs of everyone who should be treated as the bot owner.  
  For Gecko and me this looks like <code>110429968252555264:110431936635207680</code>.
- Wordnik (optional): The API Key to be used in Wordnik API calls.
- WolframAlpha (optional): The API Key the be used in Wordnik API calls.

# TrackerLimits entries
Every type of tracker has 3 properties you can assign here.

- TrackersPerServer: How many trackers of that type can be in one server before getting hit with an error on creation.
- PollInterval: How many milliseconds should pass between each check of the tracker.  
  For instant webhook or stream Trackers like Twitch, Twitter and Youtube this shouldn't be changed!
- UpdateInterval: How many milliseconds should pass before a live tracker should be updated.  
  Currently this includes Twitch, YoutubeLive and JSON.

# Gathering API Keys
If you want to use a service that requires API keys, you can obtain them here:

- [Twitch](https://dev.twitch.tv/dashboard/apps/create)
- [Twitter](https://developer.twitter.com/en/account/get-started)
- [Youtube](https://developers.google.com/youtube/v3/getting-started#before-you-start)
- [Osu](https://osu.ppy.sh/home/account/edit#oauth), currently outdated so don't bother.
- [Steam](https://steamcommunity.com/dev/apikey)
