import Head from "next/head";
import Nav from "../components/Nav";
import {GetStaticProps} from "next";
import {differenceInDays, differenceInHours, differenceInMinutes, formatISO, parseISO} from "date-fns";
import ChannelTableRow from "../components/ChannelTableRow";
import ChannelHistory from "../components/ChannelHistory";

type ChannelData = {
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
  data: {
    [id: string]: {
      displayName: string,
      game: string,
      shared: number
    }
  }
}


const Channel = ({channel, data}: ChannelData) => {
  let lastUpdated: string;
  const now = new Date();
  const updateDate = parseISO(channel.lastUpdate + "Z");
  if (differenceInMinutes(now, updateDate) <= 60) {
    lastUpdated = `${differenceInMinutes(now, updateDate)} minutes ago`;
  } else if (differenceInHours(now, updateDate) <= 24) {
    lastUpdated = `${differenceInHours(now, updateDate)} hours ago`;
  } else if (differenceInDays(now, updateDate) <= 7) {
    lastUpdated = `${differenceInDays(now, updateDate)} days ago`;
  } else {
    lastUpdated = formatISO(updateDate);
  }

  return (
    <>
      <Head>
        <title>{channel.loginName} - Twitch Overlap</title>
        <meta property="og:title" content={`${channel.displayName} - Twitch Community Overlap`}/>
        <meta property="og:description"
              content={`Chat hopper stats for ${channel.displayName}. Currently sharing ${channel.shared} total viewers. Find out in detail who's viewers are channel hopping to ${channel.displayName}. The site is open source on GitHub.`}/>
        <meta property="og:image" content={`https://static-cdn.jtvnw.net/jtv_user_pictures/${channel.avatar}`}/>
      </Head>
      <Nav/>
      <div className="container w-full md:max-w-5xl xl:max-w-7xl mx-auto mx-2 tracking-tight mt-16 mb-20">
        <div className="flex items-center pt-4 px-4">
          <a href={`https://www.twitch.tv/${channel.loginName}`} target="_blank" rel="noopener noreferrer">
            <img src={`https://static-cdn.jtvnw.net/jtv_user_pictures/${channel.avatar}`} className="rounded-full"
                 alt={`${channel.loginName}-avatar`}
              // onError="if (this.src !== 'https://i.imgur.com/V2dxUn8.png') this.src = 'https://i.imgur.com/V2dxUn8.png'"
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
          <table className="table-fixed mt-4 xl:mx-8">
            <thead className="text-left font-medium">
            <tr className="border-b-2 border-gray-400">
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
            {Object.entries(data).map(([key, value]) =>
              <ChannelTableRow key={key} shared={channel.shared} chatters={channel.chatters} channel={key}
                               data={value}/>
            )}
            </tbody>
          </table>
        </div>
      </div>
    </>
  );
};

export const getStaticProps: GetStaticProps = async ({params}) => {
  const res = await fetch(`http://localhost:5000/api/v1/channel/${params!.channel}`);
  const data = await res.json();

  if (!data) {
    return {
      notFound: true,
    };
  }

  return {
    props: {
      channel: data.channel,
      data: data.data
    }
  };
};

export async function getStaticPaths() {
  const res = await fetch("http://localhost:5000/api/v1/channels");
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
