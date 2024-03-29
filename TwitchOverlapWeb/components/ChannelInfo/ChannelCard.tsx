import Link from "next/link";
import {abbreviateNumber} from "js-abbreviation-number";
import ImageFallback from "../ImageFallback";
import {isASCII} from "../../utils/helpers";

export type IndexChannelData = {
  id: string,
  avatar: string,
  displayName: string,
  username: string,
  chatters: number
}

const ChannelCard = ({id, avatar, displayName, username, chatters}: IndexChannelData) => {
  let channelName = displayName;
  if (!isASCII(displayName)) {
    channelName = `${displayName} (${username})`;
  }

  return (
    <div className="channel-card" title={`${channelName}'s viewer overlap`}>
      <Link href={`/${id}`}>
        <a>
          <div className="flex justify-start items-center">
            <div className="flex">
              <ImageFallback src={`https://static-cdn.jtvnw.net/jtv_user_pictures/${avatar}`}
                             fallbackSrc="https://i.imgur.com/V2dxUn8.png"
                             className="rounded-full"
                             alt={id}
                             width="50" height="50" style={{minWidth: "50px"}}
              />
            </div>
            <div className="flex flex-col pl-2 overflow-hidden">
              <div className="truncate">{channelName}</div>
              <div className="truncate text-sm">chatters: {abbreviateNumber(chatters, 1, {padding: false})}</div>
            </div>
          </div>
        </a>
      </Link>
    </div>
  );
};

export default ChannelCard;
