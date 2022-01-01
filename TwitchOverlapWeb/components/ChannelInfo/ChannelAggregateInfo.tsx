import {ChannelInfoPercentage, ChannelOverlapPercentageInfo} from "./ChannelInfo";
import {ChannelData} from "../../pages/[...channel]";
import {DefaultLocale, getDateDiff} from "../../utils/helpers";
import {DateTime} from "luxon";

const ChannelAggregateInfo = ({change, channelTotalOverlap, channelTotalUnique, date}: ChannelData) => {
  const parsedDate = DateTime.fromISO(date);
  return (
    <>
      <div className="stats-card">
        <div className="font-medium mb-1">Last Updated</div>
        <div title={parsedDate.toISO()}>{getDateDiff(parsedDate)}</div>
      </div>
      <div className="stats-card" title="Total unique chatters present in the channel in the time span">
        <div className="font-medium mb-1">Total Unique Chatters</div>
        {change.totalChatterChange !== null && change.totalChatterPercentageChange !== null
          ? <ChannelInfoPercentage curr={channelTotalUnique} prev={change.totalChatterPercentageChange}
                                   description=""
                                   changeDescription={`${change.totalChatterChange >= 0 ? "+" : ""}${change.totalChatterChange.toLocaleString(DefaultLocale)} since last update`}/>
          : <div>{channelTotalUnique.toLocaleString(DefaultLocale)}</div>
        }
      </div>
      <div className="stats-card" title="Percentage of total chatters that was present in another stream">
        <div className="font-medium mb-1">Overlap Percentage</div>
        {change.overlapPercentChange !== null
          ? <ChannelOverlapPercentageInfo change={change.overlapPercentChange}
                                          curr={(channelTotalOverlap / channelTotalUnique * 100).toLocaleString(DefaultLocale, {minimumFractionDigits: 2, maximumFractionDigits: 2})}/>
          : <div>{(channelTotalOverlap / channelTotalUnique * 100).toLocaleString(DefaultLocale, {minimumFractionDigits: 2, maximumFractionDigits: 2})}%</div>
        }
      </div>
      <div className="stats-card" title="Total number of chatters that was present in another stream">
        <div className="font-medium mb-1">Total Shared</div>
        {change.totalOverlapChange !== null && change.overlapPercentChange !== null
          ? <ChannelInfoPercentage curr={channelTotalOverlap} prev={change.totalOverlapPercentageChange}
                                   description=""
                                   changeDescription={`${change.totalOverlapChange >= 0 ? "+" : ""}${change.totalOverlapChange.toLocaleString(DefaultLocale)} since last update`}/>
          : <div>{channelTotalOverlap.toLocaleString(DefaultLocale)}</div>
        }
      </div>
    </>
  );
};

export default ChannelAggregateInfo;
