import Head from "next/head";
import Nav from "../components/Nav";
import Link from "next/link";
import Image from "next/image";
import {ChannelOverlapData} from "../components/ChannelInfo/ChannelTableRow";
import {DateTime} from "luxon";
import {getTimeDiff} from "../utils/helpers";
import ImageFallback from "../components/ImageFallback";
import ChannelHistory from "../components/ChannelInfo/ChannelHistory";
import {useRouter} from "next/router";
import ChannelDefaultInfo from "../components/ChannelInfo/ChannelDefaultInfo";
import ChannelAggregateInfo from "../components/ChannelInfo/ChannelAggregateInfo";
import ChannelDefaultTable from "../components/ChannelInfo/ChannelDefaultTable";
import ChannelAggregateTable from "../components/ChannelInfo/ChannelAggregateTable";
import {ParsedUrlQuery} from "querystring";

export enum AggregateDays {
  Default = 0,
  OneDay = 1,
  ThreeDays = 3,
  SevenDays = 7
}

type ChannelPrev = {
  timestamp: string,
  viewers: number,
  chatters: number,
  shared: number
}

export type ChannelStats = {
  id: number,
  loginName: string
  displayName: string,
  avatar: string,
  game: string,
  viewers: number,
  chatters: number,
  shared: number,
  history: ChannelPrev[],
  lastUpdate: string
}

type ChannelAggregateChange = {
  totalChatterChange: number,
  totalChatterPercentageChange: number,
  overlapPercentChange: number,
  totalOverlapChange: number,
  totalOverlapPercentageChange: number,
}

export type ChannelData = {
  notFound: boolean
  type: AggregateDays,
  channel: ChannelStats,
  change: ChannelAggregateChange,
  channelTotalUnique: number,
  channelTotalOverlap: number,
  date: string,
  data: ChannelOverlapData[]
}

const Channel = ({change, channel, channelTotalOverlap, channelTotalUnique, data, notFound, type, date}: ChannelData) => {
  const {asPath} = useRouter();
  if (notFound) {
    return (
      <>
        <Head>
          <title>No Data - Twitch Overlap</title>
          <meta property="og:title" content="@Model - Twitch Community Overlap"/>
          <meta property="og:description"
                content="The site shows stats about the overlap of chatters from different channels on Twitch. You can find out who your favorite streamer shares viewers with, or how many people are currently chat hopping. The site is open source on GitHub."/>
          <meta property="og:image" content="/images/roki2-round-10.png"/>
        </Head>
        <Nav/>
        <div className="container w-full md:max-w-5xl xl:max-w-7xl mx-auto tracking-tight mt-16 mb-20">
          <div className="pt-4">
            <Image className="flex" src="https://cdn.frankerfacez.com/emoticon/425196/4" alt="Sadge" width="56"
                   height="43"/>
            <div className="pt-2">No data recorded for <span className="font-bold">{asPath.split("/")[1]}</span></div>
            <div>Only channels above {(1000).toLocaleString()} concurrent viewers and 500 chatters are recorded at the
              moment, requirements
              may be lowered in the future.
            </div>
          </div>
        </div>
      </>
    );
  }

  const lastUpdated = getTimeDiff(DateTime.fromISO(channel.lastUpdate, {zone: "utc"}));
  return (
    <>
      <Head>
        <title>{`${channel.displayName}${type === AggregateDays.Default ? "" : ` - ${type.toString()} Day Aggregate`} - Twitch Overlap`}</title>
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
            <Link href={`/${channel.loginName}/1`}>
              <a
                className={`rounded border border-gray-300 dark:border-gray-800 flex flex-col py-2 shadow-md px-2 ${type == AggregateDays.OneDay ? "bg-pink-500 hover:bg-pink-600 dark:bg-pink-800 dark:hover:bg-pink-700" : "dark:bg-gray-700 hover:text-pink-500"}`}
                title="1 day stats">
                1 day
              </a>
            </Link>
            <Link href={`/${channel.loginName}/3`}>
              <a
                className={`rounded border border-gray-300 dark:border-gray-800 flex flex-col py-2 shadow-md px-2 ${type == AggregateDays.ThreeDays ? "bg-pink-500 hover:bg-pink-600 dark:bg-pink-800 dark:hover:bg-pink-700" : "dark:bg-gray-700 hover:text-pink-500"}`}
                title="3 day stats">
                3 days
              </a>
            </Link>
            <Link href={`/${channel.loginName}/7`}>
              <a
                className={`rounded border border-gray-300 dark:border-gray-800 flex flex-col py-2 shadow-md px-2 ${type == AggregateDays.SevenDays ? "bg-pink-500 hover:bg-pink-600 dark:bg-pink-800 dark:hover:bg-pink-700" : "dark:bg-gray-700 hover:text-pink-500"}`}
                title="7 day stats">
                7 days
              </a>
            </Link>
          </div>
        </div>
        <div
          className={`pt-4 grid grid-cols-2 gap-4 px-4 text-center ${type === AggregateDays.Default ? "sm:grid-cols-3 lg:grid-cols-6" : "md:grid-cols-4"}`}>
          {type === AggregateDays.Default
            ? <ChannelDefaultInfo channel={channel} lastUpdated={lastUpdated}/>
            : <ChannelAggregateInfo change={change} channelTotalOverlap={channelTotalOverlap}
                                    channelTotalUnique={channelTotalUnique}
                                    channel={channel} notFound={notFound} type={AggregateDays.Default} data={data}
                                    date={date}/>
          }
        </div>
        <ChannelHistory channel={channel.loginName} type={type}/>
        {type === AggregateDays.Default
          ? <ChannelDefaultTable data={data} channel={channel}/>
          : <ChannelAggregateTable data={data} totalUnique={channelTotalUnique} totalShared={channelTotalOverlap} type={type}/>
        }
      </div>
    </>
  );
};

interface Params extends ParsedUrlQuery {
  channel: string[],
}

export const getStaticProps = async (context: { params: Params; }) => {
  const params = context.params as Params;
  const channel = params.channel;
  const request = "http://192.168.1.104:5000/api/v1/channel/";
  if (channel === null || channel === undefined) {
    return {
      redirect: {
        destination: "/"
      }
    };
  }

  let res;
  if (channel.length == 1) {
    res = await fetch(request + channel[0]);
  } else if (channel.length == 2 && (channel[1] === "1" || channel[1] === "3" || channel[1] === "7")) {
    res = await fetch(`${request + channel[0]}/${channel[1]}`);
  } else {
    return {
      redirect: {
        destination: "/" + channel[0]
      }
    };
  }

  if (!res.ok) {
    return {
      props: {
        notFound: true
      }
    };
  }

  const data = await res.json();

  if (!data) {
    return {
      props: {
        notFound: true,
      }
    };
  }

  return {
    props: {
      notFound: false,
      ...data
    },
    revalidate: 60
  };
};

export async function getStaticPaths() {
  const res = await fetch("http://192.168.1.104:5000/api/v1/channels/500");
  const channels = await res.json();

  const paths = channels.map((x: string) => ({
    params: {channel: [x]},
  }));

  return {paths, fallback: "blocking"};
}

export default Channel;
