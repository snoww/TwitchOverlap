# Twitch Overlap API

This repo contains the API server for [stats.roki.sh](https://stats.roki.sh).

## `./TwitchOverlapApi`

This project is the server for the api, written in ASP.NET Core.

Connects to a mongodb backend server which contains the data for each channel. 

Results are cached for 5 minutes using redis. 

## `./ChannelIntersection`

This project does the actual overlap calculation.

It fetches all channels above 1000 viewers on twitch, and then fetches all the chatters in their channel and stored in a hashset.

It then gets all the combinations of channel pairs, and then compares their chatters to find the number of intersections.

The final data is pushed into the database where it can be fetched by the api.
