import ImageFallback from "../ImageFallback";
import Link from "next/link";
import {AggregateDays, ChannelStats} from "../../pages/[...channel]";

const ChannelHeader = ({channel, type}: {channel: ChannelStats, type: AggregateDays}) => {
  return (
    <>
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
    </>
  );
};

export default ChannelHeader;
