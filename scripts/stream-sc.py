import os

whitelist = [
    "QueryConfig",
    "WatchConfig",
    "InitiateLogin",
    "PresenceStream",
    "CollectionStream",
    "Listen",
    #todo: add more
]

i = 0

def requestheaders(flow):
    print("requesting " + flow.request.url)
    for url in whitelist:
        if url in flow.request.url:
            print("streaming on for " + flow.request.url)
            flow.request.stream = True
            return

    print("streaming off for " + flow.request.url)
    flow.request.stream = False


def responseheaders(flow):
    print("responding " + flow.request.url)
    for url in whitelist:
        if url in flow.request.url:
            print("streaming on for " + flow.request.url)
            flow.response.stream = True
            return

    print("streaming off for " + flow.request.url)
    flow.response.stream = False

def request(flow):
    global i
    i += 1
    endpoint_parts = flow.request.url.split("/")[-2:]
    endpoint = ".".join(endpoint_parts)

    with open(os.path.join("dump", str(i) + '-' + "request" + '-' + endpoint + ".grpc"), "wb") as f:
        if flow.request.content is not None:
            f.write(flow.request.content)
        else:
            f.write(b"")
        
def response(flow):
    global i
    i += 1
    endpoint_parts = flow.request.url.split("/")[-2:]
    endpoint = ".".join(endpoint_parts)

    with open(os.path.join("dump", str(i) + '-' + "response" + '-' + endpoint + ".grpc"), "wb") as f:
        if flow.response.content is not None:
            f.write(flow.response.content)
        else:
            f.write(b"")
