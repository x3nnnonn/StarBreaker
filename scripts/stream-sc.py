whitelist = [
    "QueryConfig",
    "WatchConfig",
    "InitiateLogin",
    "PresenceStream",
    "CollectionStream",
    "Listen",
    #todo: add more
]

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
