# Twitch Overlap

This repo contains the site for [stats.roki.sh](https://stats.roki.sh).

## `./TwitchOverlap`

This project contains the source code for the website. It shows data for the overlap of different communities on [Twitch.tv](https://twitch.tv). For each channel, it shows a list of all the channels that share a common chatter. The probability score represented is the percentage of shared viewers compared to the total shared viewers.

The probability score is calculated by `channel_shared/total_shared`.

`channel_shared` is the number of shared viewers from a particular channel.

`total_shared` is the total number of unique shared viewers from all channels.

Motivation from [Subreddit User-Overlap](https://subredditstats.com/subreddit-user-overlaps).

## `./ChannelIntersection`

This project does the actual overlap calculation.

It fetches all channels above 1500 viewers on twitch, and then fetches all the chatters in their channel and stored in a hashset.

It then gets all the combinations of channel pairs, and then compares their chatters to find the number of intersections.

The processed data is stored in a Postgres database, where it stores the overlap data for 14 days.