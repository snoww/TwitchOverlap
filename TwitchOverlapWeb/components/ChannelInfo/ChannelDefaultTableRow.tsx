import Link from "next/link";
import {DefaultLocale, isASCII} from "../../utils/helpers";

export type ChannelTableRowType = {
  shared: number,
  chatters: number,
  data: ChannelOverlapData,
  index: number
}

export type ChannelOverlapData = {
  change: number,
  loginName: string,
  displayName: string,
  game: string,
  shared: number
}

export const RowChange = (prop: {change: number}) => {
  const change = prop.change;
  if (change === null) {
    return (
      <td className="text-blue-500" title="new overlap">
        <i className="fas fa-plus pl-2"/>
      </td>
    );
  }
  if (change === 0) {
    return (
      <td title="no change in position">
        {/*<i className="fas fa-minus pl-2"/>*/}
      </td>
    );
  }
  if (change > 0) {
    return (
      <td className="text-emerald-500 whitespace-nowrap" title="increased position">
        <i className="fas fa-chevron-up pl-2"><span className="font-sans font-normal">{` ${change}`}</span></i>
      </td>
    );
  } else {
    return (
      <td className="text-red-500 whitespace-nowrap" title="decreased position">
        <i className="fas fa-chevron-down pl-2"><span className="font-sans font-normal">{` ${Math.abs(change)}`}</span></i>
      </td>
    );
  }
};

const ChannelDefaultTableRow = ({shared, chatters, data, index}: ChannelTableRowType) => {
  let channelName = data.displayName;
  if (!isASCII(channelName)) {
    channelName = `${data.displayName} (${data.loginName})`;
  }
  return (
    <>
      <tr className="border-b border-gray-300">
        <td className={"text-center"}>{index + 1}</td>
        <RowChange change={data.change}/>
        <td className="table-channel-col">
          <Link href={`/${data.loginName}`}>{channelName}</Link>
        </td>
        <td className="table-stats-col">{((data.shared/shared)*100).toLocaleString(DefaultLocale, {minimumFractionDigits: 2, maximumFractionDigits: 2})}%</td>
        <td className="table-stats-col">{data.shared.toLocaleString(DefaultLocale)}</td>
        <td className="table-stats-col">{((data.shared/chatters)*100).toLocaleString(DefaultLocale, {minimumFractionDigits: 2, maximumFractionDigits: 2})}%</td>
        <td className="table-stats-col hover:underline hover:text-pink-500 truncate">
          <a href={`https://www.twitch.tv/directory/game/${data.game}`} target="_blank"
             rel="noopener noreferrer">{data.game}</a>
        </td>
      </tr>
    </>
  );
};

export default ChannelDefaultTableRow;
