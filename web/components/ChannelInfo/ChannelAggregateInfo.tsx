import {ChannelInfoPercentage, ChannelOverlapPercentageInfo} from "./ChannelInfo";
import {ChannelData} from "../../pages/[...channel]";
import {getDateDiff} from "../../utils/helpers";
import {DateTime} from "luxon";

const ChannelAggregateInfo = ({change, channelTotalOverlap, channelTotalUnique, date}: ChannelData) => {
  return (
    <>
      <div className="stats-card">
        <div className="font-medium mb-1">Last Updated</div>
        <div>{getDateDiff(DateTime.fromISO(date))}</div>
      </div>
      <div className="stats-card" title="Total unique chatters present in the channel in the time span">
        <div className="font-medium mb-1">Total Unique Chatters</div>
        {change.totalChatterChange !== null && change.totalChatterPercentageChange !== null
          ? <ChannelInfoPercentage curr={channelTotalUnique} prev={change.totalChatterPercentageChange}
                         description=""
                         changeDescription={`${change.totalChatterChange.toLocaleString()} since last update`}/>
          : <div>{channelTotalUnique.toLocaleString()}</div>
        }
      </div>
      <div className="stats-card" title="Percentage of total chatters that was present in another stream">
        <div className="font-medium mb-1">Overlap Percentage</div>
        {change.overlapPercentChange !== null
          ? <ChannelOverlapPercentageInfo change={change.overlapPercentChange} curr={(channelTotalOverlap / channelTotalUnique * 100).toFixed(2).toLocaleString()}/>
          : <div>{(channelTotalOverlap / channelTotalUnique * 100).toFixed(2).toLocaleString()}%</div>
        }
      </div>
      <div className="stats-card" title="Total number of chatters that was present in another stream">
        <div className="font-medium mb-1">Total Shared</div>
        {change.totalOverlapChange !== null && change.overlapPercentChange !== null
          ? <ChannelInfoPercentage curr={channelTotalOverlap} prev={change.totalOverlapPercentageChange}
                         description=""
                         changeDescription={`${change.totalOverlapChange.toLocaleString()} since last update`}/>
          : <div>{channelTotalOverlap.toLocaleString()}</div>
        }
      </div>
    </>
  );
};

export default ChannelAggregateInfo;
