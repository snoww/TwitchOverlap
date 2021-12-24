import {ChannelInfo} from "./ChannelInfo";
import {ChannelStats} from "../../pages/[...channel]";

const ChannelDefaultInfo = (props: {channel: ChannelStats, lastUpdated: string}) => {
  const { channel, lastUpdated } = props;
  return (
    <>
      <div className="stats-card">
        <div className="font-medium mb-1">Last Updated</div>
        <div>{lastUpdated}</div>
      </div>
      <div className="stats-card" title="Total viewers in stream, includes embedded viewers">
        <div className="font-medium mb-1">Viewers</div>
        {channel.history.length == 2
          ? <ChannelInfo curr={channel.viewers} prev={channel.history[1].viewers}
                         description="Total viewers in channel"
                         changeDescription="Change in viewers since last update"/>
          : <div>{channel.viewers.toLocaleString()}</div>
        }
      </div>
      <div className="stats-card" title="Total chatters in stream, excludes embedded viewers">
        <div className="font-medium mb-1">Chatters</div>
        {channel.history.length == 2
          ? <ChannelInfo curr={channel.chatters} prev={channel.history[1].chatters}
                         description="Total chatters in channel"
                         changeDescription="Change in chatters since last update"/>
          : <div>{channel.chatters.toLocaleString()}</div>
        }
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
        {channel.history.length == 2
          ? <ChannelInfo curr={channel.shared} prev={channel.history[1].shared}
                         description="Total shared viewers"
                         changeDescription="Change in shared chatters since last update"/>
          : <div>{channel.shared.toLocaleString()}</div>
        }
      </div>
    </>
  );
};

export default ChannelDefaultInfo;
