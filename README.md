# Twitch Overlap

This repo contains the site for [stats.roki.sh](https://stats.roki.sh).

## `./TwitchOverlap`

This project contains the source for the website. It shows data for the live overlap (every 30 min) of different communities on [Twitch.tv](https://twitch.tv). For each channel, it shows a trend graph of the top 6 overlapping channels. It also shows raw data for all the channels that share a common chatter during the last update. The probability score represented is the percentage of shared viewers compared to the total shared viewers.

Note that throughout this document I use the term "viewers" and "chatters" interchangeably, however, in most cases "chatters" is actually the more accurate term, since that is what Twitch's API gives.

The probability score is calculated by `channel_shared/total_shared`.

`channel_shared` is the number of shared viewers from a particular channel.

`total_shared` is the total number of unique shared viewers from all channels.

Motivation from [Subreddit User-Overlap](https://subredditstats.com/subreddit-user-overlaps).

### Twitch Atlas feature

Force directed graph representation of communities of Twitch, at [stats.roki.sh/atlas](https://stats.roki.sh/atlas). This sub-project was inspired and motivated by [/u/Kgersh's](https://www.reddit.com/user/Kgersh) Twitch Atlas ([Github](https://github.com/KiranGershenfeld/VisualizingTwitchCommunities)). The issue with using Gephi was it was a very manual and tedious process. I wanted to automate the whole thing, such that minimal work needs to go into updating the graph each time. 

The graph shows the overlap of viewers in the top 1000 channels (by unique viewership), and then using the Louvain algorithm for community detection. The site uses the [Apache ECharts](https://github.com/apache/echarts) library for the graph (I've tried other libraries like Sigma.js or D3.js, none of them satisfied the requirements or there was functionality that I wasn't comfortable with using). Since the graph is rendered on your local machine, it might be laggy for browsers without hardware acceleration. However, it does mean that you are able to zoom in and out and pan around to your liking.

## Data Collection
### `./ChannelIntersection`

This project does the half-hourly overlap calculation.

Every 30 minutes, this program fetches all channels above 1500 viewers on Twitch, and then fetches all the chatters in their channel. Then finds all the combinations of channel pairs, and then calculates the intersection of their chatters and counts the total. The data is stored in a Postgres database for a month.

Every hour, this program saves each channel's unique viewers to a file, this is used later on for the Twitch Community Graph.

### `./TwitchGraph`

This project does the monthly overlap calculation.

Each channel that has been fetched by `ChannelIntersection` has it's unique viewers saved in a file. The top 1000 channels by unique viewership is then taken, and we find the overlap between all the channel combinations. There are a total of 499500 (1000 choose 2) possible combinations of channels, which is why I limited to only the top 1k channels, and some channels have over 600k unique viewers. Every combination that has over 1000 viewers overlapping is then saved in the edge list, with the overlap count as it's weight. The channels and overlaps are exported as csv's.

Then there is a python script that does the finishing touches. It first imports the edge list into a weighted-undirected-graph data structure, then the Louvain algorithm is ran on the graph to find and detect communities within. It then imports the nodes of the graph (the channels), and normalizes the size to an appropriate size for the final graph render. It also assigns the channel a specific color for the community that the algorithm decided. The final nodes and edges of the graph are exported as a JSON file, and served on the website. Since the community detection is automatic, there is no legend, unless manually added.