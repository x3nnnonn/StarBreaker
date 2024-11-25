#some notes:
# local mode is the easiest to capture just the game traffic
# allow hosts filters the traffic to just the grpc stuff which is what we actually care about
#the script is the most important part, it decides which requests are streamed or read fully.
# This is important because some requests are made at startup and expect to be streamed.
# If we turn off streaming for everything, the game will hang at startup.
# if we turn on streaming for everything, we can't read the data we want.

# The script tries its best to turn on streaming for the types of data that need it for the game to run,
# while still letting us read the other ones

mitmweb.exe --mode local:StarCitizen.exe --allow-hosts 'test1.cloudimperiumgames.com' -s stream-sc.py
#--allow-hosts 'cloudimperiumgames.com'