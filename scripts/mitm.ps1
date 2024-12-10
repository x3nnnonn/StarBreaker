#some notes:
# local mode is the easiest to capture just the game traffic
# allow hosts filters the traffic to just the grpc stuff which is what we actually care about
#the script is the most important part, it reads a descriptor set file output by our CLI tool, 
# then uses it to decide which requests to stream, displays them nicely in the UI, and writes them to disk

mitmweb.exe --mode local:StarCitizen.exe --allow-hosts 'test1.cloudimperiumgames.com' -s stream-sc.py --set descriptor=sc.bin