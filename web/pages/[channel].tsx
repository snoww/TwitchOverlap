import Head from "next/head";
import Nav from "../components/Nav";
import {GetStaticProps} from "next";
import Link from "next/link";
import ChannelTableRow, {ChannelOverlapData} from "../components/ChannelTableRow";
import {DateTime} from "luxon";
import {getTimeDiff} from "../utils/helpers";
import ImageFallback from "../components/ImageFallback";
import ChannelHistory from "../components/ChannelHistory";

enum AggregateDays {
  Default = 0,
  OneDay = 1,
  ThreeDays = 3,
  SevenDays = 7
}

type ChannelData = {
  type: AggregateDays,
  channel: {
    id: number,
    loginName: string
    displayName: string,
    avatar: string,
    game: string,
    viewers: number,
    chatters: number,
    shared: number,
    lastUpdate: string
  },
  data: ChannelOverlapData[]
}

const Channel = ({type, channel, data}: ChannelData) => {
  const lastUpdated = getTimeDiff(DateTime.fromISO(channel.lastUpdate, {zone: "utc"}));
  return (
    <>
      <Head>
        <title>{channel.displayName} - Twitch Overlap</title>
        <meta property="og:title" content={`${channel.displayName} - Twitch Community Overlap`}/>
        <meta property="og:description"
              content={`Chat hopper stats for ${channel.displayName}. Currently sharing ${channel.shared} total viewers. Find out in detail which channels ${channel.displayName}'s viewers are watching, or who's viewers are watching ${channel.displayName}. The site is open source on GitHub.`}/>
        <meta property="og:image" content={`https://static-cdn.jtvnw.net/jtv_user_pictures/${channel.avatar}`}/>
      </Head>
      <Nav/>
      <div className="container w-full md:max-w-5xl xl:max-w-7xl mx-auto tracking-tight mt-16 mb-20">
        <div className="block md:flex justify-between">
          <div className="flex items-center pt-4 px-4">
            <a href={`https://www.twitch.tv/${channel.loginName}`} target="_blank" rel="noopener noreferrer"
               className="flex">
              <ImageFallback
                src={`https://static-cdn.jtvnw.net/jtv_user_pictures/${channel.avatar.replace("70x70", "300x300")}`}
                fallbackSrc="https://i.imgur.com/V2dxUn8.png"
                className="rounded-full"
                alt={`${channel.loginName}-avatar`}
                width="70" height="70" layout="fixed"
                priority={true}
              />
            </a>
            <div className="pl-3 flex flex-col">
              <a className="text-2xl hover:underline hover:text-pink-500"
                 href={`https://www.twitch.tv/${channel.loginName}`} target="_blank"
                 rel="noopener noreferrer">{channel.displayName}</a>
              <a className="hover:underline hover:text-pink-500"
                 href={`https://www.twitch.tv/directory/game/${channel.game}`} target="_blank"
                 rel="noopener noreferrer">{channel.game}</a>
            </div>
          </div>
          <div className="mt-4 ml-4 mr-4 md:mt-0 md:ml-0 flex items-end">
            <Link href={`/${channel.loginName}`}>
              <a
                className={`rounded border border-gray-300 dark:border-gray-800 flex flex-col py-2 shadow-md px-2 ${type == AggregateDays.Default ? "bg-pink-500 hover:bg-pink-600 dark:bg-pink-800 dark:hover:bg-pink-700" : "dark:bg-gray-700 hover:text-pink-500"}`}
                title="30 min stats">
                30 min
              </a>
            </Link>
            <Link href={`/${channel.loginName}`}>
              <a
                className={`rounded border border-gray-300 dark:border-gray-800 flex flex-col py-2 shadow-md px-2 ${type == AggregateDays.OneDay ? "bg-pink-500 hover:bg-pink-600 dark:bg-pink-800 dark:hover:bg-pink-700" : "dark:bg-gray-700 hover:text-pink-500"}`}
                title="30 min stats">
                1 day
              </a>
            </Link>
            <Link href={`/${channel.loginName}`}>
              <a
                className={`rounded border border-gray-300 dark:border-gray-800 flex flex-col py-2 shadow-md px-2 ${type == AggregateDays.ThreeDays ? "bg-pink-500 hover:bg-pink-600 dark:bg-pink-800 dark:hover:bg-pink-700" : "dark:bg-gray-700 hover:text-pink-500"}`}
                title="30 min stats">
                3 days
              </a>
            </Link>
            <Link href={`/${channel.loginName}`}>
              <a
                className={`rounded border border-gray-300 dark:border-gray-800 flex flex-col py-2 shadow-md px-2 ${type == AggregateDays.SevenDays ? "bg-pink-500 hover:bg-pink-600 dark:bg-pink-800 dark:hover:bg-pink-700" : "dark:bg-gray-700 hover:text-pink-500"}`}
                title="30 min stats">
                7 days
              </a>
            </Link>
          </div>
        </div>
        <div className="pt-4 grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-6 gap-4 px-4 text-center">
          <div className="stats-card">
            <div className="font-medium mb-1">Last Updated</div>
            <div>{lastUpdated}</div>
          </div>
          <div className="stats-card" title="Total viewers in stream, includes embedded viewers">
            <div className="font-medium mb-1">Viewers</div>
            <div>{channel.viewers.toLocaleString()}</div>
          </div>
          <div className="stats-card" title="Total chatters in stream, excludes embedded viewers">
            <div className="font-medium mb-1">Chatters</div>
            <div>{channel.chatters.toLocaleString()}</div>
          </div>
          <div className="stats-card" title="Ratio of chatters to viewers, higher is better">
            <div className="font-medium mb-1">Chatter Ratio</div>
            <div>{(channel.chatters / channel.viewers).toFixed(2).toLocaleString()}</div>
          </div>
          <div className="stats-card" title="Percentage of total viewers that are watching another stream">
            <div className="font-medium mb-1">Overlap Percentage</div>
            <div>{(channel.shared / channel.viewers * 100).toFixed(2).toLocaleString()}%</div>
          </div>
          <div className="stats-card" title="Total number of viewers watching another stream">
            <div className="font-medium mb-1">Total Shared</div>
            <div>{channel.shared.toLocaleString()}</div>
          </div>
        </div>
        <ChannelHistory channel={channel.loginName}/>
        <div className="overflow-x-auto">
          <table className="table-fixed mt-4 mx-auto">
            <thead className="text-left font-medium">
            <tr className="border-b-2 border-gray-400">
              <td className="px-2 md:px-4 py-2" title="Change compared to last overlap">Δ</td>
              <td className="w-1/6 px-2 md:px-4 py-2" title="Channel">Channel</td>
              <td className="w-1/6 px-2 md:px-4 py-2" title="Probability of where a shared chatter is from">Overlap
                Probability
              </td>
              <td className="w-1/6 px-2 md:px-4 py-2" title="Total number of overlap from a channel">Overlap Chatters
              </td>
              <td className="w-1/6 px-2 md:px-4 py-2" title="Percentage of total chatters">% of Total Chatters</td>
              <td className="w-1/3 px-2 md:px-4 py-2" title="Current category">Playing</td>
            </tr>
            </thead>
            <tbody>
            {data.map(x =>
              <ChannelTableRow key={x.loginName} shared={channel.shared} chatters={channel.chatters} data={x}/>
            )}
            </tbody>
          </table>
        </div>
      </div>
    </>
  );
};

export const getStaticProps: GetStaticProps = async ({params}) => {
  const res = await fetch(`http://192.168.1.104:5000/api/v1/channel/${params!.channel}`);
  const data = await res.json();

  if (!data) {
    return {
      notFound: true,
    };
  }

  return {
    props: {
      type: data.type,
      channel: data.channel,
      data: data.data
    }
  };
};

export async function getStaticPaths() {
  const res = await fetch("http://192.168.1.104:5000/api/v1/channels");
  const channels = await res.json();

  // Get the paths we want to pre-render based on posts
  const paths = channels.map((x: string) => ({
    params: {channel: x},
  }));

  // We'll pre-render only these paths at build time.
  // { fallback: blocking } will server-render pages
  // on-demand if the path doesn't exist.
  return {paths, fallback: "blocking"};
}

export default Channel;
