import {ChannelInfo} from "./ChannelInfo";
import {ChannelStats} from "../../pages/[...channel]";
import {DefaultLocale, getTimeDiff} from "../../utils/helpers";
import {DateTime} from "luxon";

const ChannelDefaultInfo = ({channel}: {channel: ChannelStats}) => {
  const time = DateTime.fromISO(channel.lastUpdate, {zone: "utc"});
  const lastUpdated = getTimeDiff(time);

  return (
    <>
      <div className="stats-card">
        <div className="font-medium mb-1">Last Updated</div>
        <div title={time.toISO()}>{lastUpdated}</div>
      </div>
      <div className="stats-card" title="Total viewers in stream, includes embedded viewers">
        <div className="font-medium mb-1">Viewers</div>
        {channel.history.length == 2
          ? <ChannelInfo curr={channel.viewers} prev={channel.history[1].viewers}
                         description="Total viewers in channel"
                         changeDescription="Change in viewers since last update"/>
          : <div>{channel.viewers.toLocaleString(DefaultLocale)}</div>
        }
      </div>
      <div className="stats-card" title="Total chatters in stream, excludes embedded viewers">
        <div className="font-medium mb-1">Chatters</div>
        {channel.history.length == 2
          ? <ChannelInfo curr={channel.chatters} prev={channel.history[1].chatters}
                         description="Total chatters in channel"
                         changeDescription="Change in chatters since last update"/>
          : <div>{channel.chatters.toLocaleString(DefaultLocale)}</div>
        }
      </div>
      <div className="stats-card" title="Ratio of chatters to viewers, higher is better">
        <div className="font-medium mb-1">Chatter Ratio</div>
        <div>{(channel.chatters / channel.viewers).toLocaleString(DefaultLocale, {minimumFractionDigits: 2, maximumFractionDigits: 2})}</div>
      </div>
      <div className="stats-card" title="Percentage of total viewers that are watching another stream">
        <div className="font-medium mb-1">Overlap Percentage</div>
        <div>{(channel.shared / channel.viewers * 100).toLocaleString(DefaultLocale, {minimumFractionDigits: 2, maximumFractionDigits: 2})}%</div>
      </div>
      <div className="stats-card" title="Total number of viewers watching another stream">
        <div className="font-medium mb-1">Total Shared</div>
        {channel.history.length == 2
          ? <ChannelInfo curr={channel.shared} prev={channel.history[1].shared}
                         description="Total shared viewers"
                         changeDescription="Change in shared chatters since last update"/>
          : <div>{channel.shared.toLocaleString(DefaultLocale)}</div>
        }
      </div>
    </>
  );
};

export default ChannelDefaultInfo;
