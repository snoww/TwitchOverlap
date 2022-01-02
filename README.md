# Twitch Overlap

This repo contains the site for [stats.roki.sh](https://stats.roki.sh).

## `./TwitchOverlapWeb`

This project contains the source for the website. It shows data for the live overlap (every 30 min) of different communities on [Twitch.tv](https://twitch.tv). For each channel, it shows a trend graph of the top 6 overlapping channels. It also shows raw data, along with a probability score for all the channels that share a common chatter during the last update. The probability score represents the probability for any given chatter to be present in that channel as well.

It also shows aggregate viewer overlap stats for the past 1 day, 3 days, and 7 days (most accurate). This data is a more general representation of how different communities share their audiences, as it takes into account where viewers go while their main streamer is offline. It also shows the total chatters and shared chatters that the streamer had during that time period.

Note that throughout this document the terms "viewers" and "chatters" interchangeably, however, in most cases "chatters" is actually the more accurate term, since that is what Twitch's API gives.

The probability score is calculated by `channel_shared/total_shared`.

`channel_shared` is the number of shared viewers from a particular channel.

`total_shared` is the total number of unique shared viewers from all channels.

Motivation from [Subreddit User-Overlap](https://subredditstats.com/subreddit-user-overlaps).

### Twitch Atlas (Updated to December 2021)

![december 2021 twitch atlas](https://cdn.discordapp.com/attachments/220646943498567680/927218161428877393/dec-21.png)

Force directed graph representation of communities of Twitch, at [stats.roki.sh/atlas](https://stats.roki.sh/atlas). This sub-project was inspired and motivated by /u/Kgersh's Twitch Atlas ([Github](https://github.com/KiranGershenfeld/VisualizingTwitchCommunities)).

The graph shows the overlap of viewers in the top 1500 channels (by total unique viewership), and then using the Louvain algorithm for community detection. The site uses the [Apache ECharts](https://github.com/apache/echarts) library at the moment for the graph rendering (I've tried to get it working with Sigma.js for the smooth WebGL rendering, but couldn't get it to work how I wanted to). Since the graph is rendered on your local machine, it will be laggy due to the sheer amount of nodes and edges in the graph it needs to render. However, it does mean that you are able to zoom in and out and pan around to your liking.

I added an image version of the graph at [stats.roki.sh/atlas/image](https://stats.roki.sh/atlas/image). This version is for people who prefer to have better performance while viewing.

I plan on updating the atlas every month (hopefully), with the option to select any past atlases generated.

### `./TwitchOverlapApi`
The backend is written with ASP.NET Core. It serves the data that is needed for the frontend website. Data is queried from a Postgres database that stores all the overlap data. Requests results are also cached using redis to speed up performance.

## Data Collection
### `.Utilities/ChannelIntersection`

This project does the half-hourly overlap calculation.

Every 30 minutes, this program fetches all channels above 1000 viewers on Twitch, and then fetches all the chatters in their channel. Then finds all the combinations of channel pairs, and then calculates the intersection of their chatters and counts the total. The data is then stored in a Postgres database.

Every hour, this program saves each channel's unique viewers to a file, this is used later on for the Twitch Community Graph.

### `.Utilities/TwitchGraph`

This project does the monthly overlap calculation. Then it uses the top 1500 channels for the month to build the graph.

### `.Utilities/GexfParser`

This project converts gephi files into json representation that is used for the atlas page.

## Disclaimer

Not affiliated with Twitch.tv. This project is a hobby of mine. 
