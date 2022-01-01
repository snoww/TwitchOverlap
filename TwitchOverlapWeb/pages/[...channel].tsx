import Head from "next/head";
import Nav from "../components/Nav";
import {ChannelOverlapData} from "../components/ChannelInfo/ChannelDefaultTableRow";
import ChannelHistory from "../components/ChannelInfo/ChannelHistory";
import {useRouter} from "next/router";
import ChannelDefaultInfo from "../components/ChannelInfo/ChannelDefaultInfo";
import ChannelAggregateInfo from "../components/ChannelInfo/ChannelAggregateInfo";
import ChannelDefaultTable from "../components/ChannelInfo/ChannelDefaultTable";
import ChannelAggregateTable from "../components/ChannelInfo/ChannelAggregateTable";
import {ParsedUrlQuery} from "querystring";
import ChannelHeader from "../components/ChannelInfo/ChannelHeader";
import Sadge from "../components/Images/Sadge";
import {DefaultLocale} from "../utils/helpers";

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
  if (notFound && (type === AggregateDays.Default || channel === null) || notFound && !channel) {
    return (
      <>
        <Head>
          <title>No Data - Twitch Viewer Overlap</title>
          <meta property="og:title" content="Twitch Community Overlap"/>
          <meta property="og:description"
                content="The site shows stats about the overlap of viewers and chatters from different channels on Twitch. You can find out who your favorite streamer shares viewers with, or how many people are currently chat hopping. The site is open source on GitHub."/>
          <meta property="og:image" content="/images/roki2-round-10.png"/>
        </Head>
        <Nav/>
        <div className="container w-full md:max-w-5xl xl:max-w-7xl mx-auto tracking-tight mt-16 mb-20">
          <div className="pt-4 px-4">
            <Sadge/>
            <div className="pt-2">No data recorded for <span className="font-bold">{asPath.split("/")[1]}</span></div>
            <div>Only channels above {(1000).toLocaleString(DefaultLocale)} concurrent viewers and 500 chatters are recorded at the
              moment, requirements may be lowered in the future.
            </div>
          </div>
        </div>
      </>
    );
  }
  if (notFound) {
    return (
      <>
        <Head>
          <title>No Data - Twitch Viewer Overlap</title>
          <meta property="og:title" content={`${channel.displayName} - Twitch Community Overlap`}/>
          <meta property="og:description"
                content="The site shows stats about the overlap of viewers and chatters from different channels on Twitch. You can find out who your favorite streamer shares viewers with, or how many people are currently chat hopping. The site is open source on GitHub."/>
          <meta property="og:image" content="/images/roki2-round-10.png"/>
        </Head>
        <Nav/>
        <div className="container w-full md:max-w-5xl xl:max-w-7xl mx-auto tracking-tight mt-16 mb-20">
          <ChannelHeader channel={channel} type={type}/>
          <div className="pt-4 px-4">
            <Sadge/>
            <div className="pt-2">No {type.toString()} day aggregate data recorded for {channel.displayName}.</div>
            <div>Not enough data collected for <span className="font-bold">{channel.displayName}</span>. Viewer
              data needs to be collected for more than 1 day in order to calculate aggregate data.
            </div>
          </div>
        </div>
      </>
    );
  }

  return (
    <>
      <Head>
        <title>{`${channel.displayName}${type === AggregateDays.Default ? "" : ` - ${type.toString()} Day Aggregate`} - Twitch Viewer Overlap`}</title>
        <meta property="og:title" content={`${channel.displayName} - Twitch Community Overlap`}/>
        <meta property="og:description"
              content={`Viewer and chatter overlap stats for ${channel.displayName}. Currently sharing ${channel.shared.toLocaleString(DefaultLocale)} total viewers. Find out in detail which channels ${channel.displayName}'s viewers are watching, or who's viewers are watching ${channel.displayName}. The site is open source on GitHub.`}/>
        <meta property="og:image" content={`https://static-cdn.jtvnw.net/jtv_user_pictures/${channel.avatar}`}/>
        <meta name="description"
              content={`Viewer and chatter overlap stats for ${channel.displayName}. Showing ${channel.displayName}'s audience overlap. Currently sharing ${channel.shared.toLocaleString(DefaultLocale)} total viewers. Find out in detail which channels ${channel.displayName}'s viewers are watching, or who's viewers are watching ${channel.displayName}. The site is open source on GitHub.`}/>
      </Head>
      <Nav/>
      <div className="container w-full md:max-w-5xl xl:max-w-7xl mx-auto tracking-tight mt-16 mb-20">
        <ChannelHeader channel={channel} type={type}/>
        <div
          className={`pt-4 grid grid-cols-2 gap-4 px-4 text-center ${type === AggregateDays.Default ? "sm:grid-cols-3 lg:grid-cols-6" : "md:grid-cols-4"}`}>
          {type === AggregateDays.Default
            ? <ChannelDefaultInfo channel={channel}/>
            : <ChannelAggregateInfo change={change} channelTotalOverlap={channelTotalOverlap}
                                    channelTotalUnique={channelTotalUnique}
                                    channel={channel} notFound={notFound} type={AggregateDays.Default} data={data}
                                    date={date}/>
          }
        </div>
        <ChannelHistory channel={channel.loginName} type={type}/>
        {type === AggregateDays.Default
          ? <ChannelDefaultTable data={data} channel={channel}/>
          : <ChannelAggregateTable data={data} totalUnique={channelTotalUnique} totalShared={channelTotalOverlap}
                                   type={type}/>
        }
      </div>
    </>
  );
};

interface Params extends ParsedUrlQuery {
  channel: string[],
}

export const getServerSideProps = async (context: { params: Params; }) => {
  const params = context.params as Params;
  const channel = params.channel;
  const request = "https://api.roki.sh/v2/channel/";
  if (channel === null || channel === undefined) {
    return {
      redirect: {
        destination: "/"
      }
    };
  }

  let res;
  let type: AggregateDays;
  if (channel.length == 1) {
    res = await fetch(request + channel[0]);
    type = AggregateDays.Default;
  } else if (channel.length == 2 && (channel[1] === "1" || channel[1] === "3" || channel[1] === "7")) {
    res = await fetch(`${request + channel[0]}/${channel[1]}`);
    if (channel[1] === "1") {
      type = AggregateDays.OneDay;
    } else if (channel[1] === "3") {
      type = AggregateDays.ThreeDays;
    } else {
      type = AggregateDays.SevenDays;
    }
  } else {
    return {
      redirect: {
        destination: "/" + channel[0]
      }
    };
  }

  if (!res.ok) {
    if (type !== AggregateDays.Default) {
      res = await fetch(request + channel[0]);
      if (res.ok) {
        const data = await res.json();
        return {
          props: {
            notFound: true,
            type: type,
            channel: data.channel
          }
        };
      }
    }

    return {
      props: {
        notFound: true,
        type: type
      }
    };
  }

  const data = await res.json();

  if (!data) {
    return {
      props: {
        notFound: true,
        type: type
      }
    };
  }

  return {
    props: {
      notFound: false,
      ...data
    }
  };
};

export default Channel;
