import Link from "next/link";

export type ChannelTableRowType = {
  shared: number,
  chatters: number,
  data: ChannelOverlapData,
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
        <i className="fas fa-plus pl-1"/>
      </td>
    );
  }
  if (change === 0) {
    return (
      <td title="no change in position">
        <i className="fas fa-minus pl-1"/>
      </td>
    );
  }
  if (change > 0) {
    return (
      <td className="text-emerald-500 whitespace-nowrap" title="increased position">
        <i className="fas fa-chevron-up pl-1"><span className="font-sans font-medium">{` ${change}`}</span></i>
      </td>
    );
  } else {
    return (
      <td className="text-red-500 whitespace-nowrap" title="increased position">
        <i className="fas fa-chevron-down pl-1"><span className="font-sans font-medium">{` ${Math.abs(change)}`}</span></i>
      </td>
    );
  }
};

const ChannelDefaultTableRow = ({shared, chatters, data}: ChannelTableRowType) => {
  return (
    <>
      <tr className="border-b border-gray-300">
        <RowChange change={data.change}/>
        <td className="table-channel-col">
          <Link href={`/${data.loginName}`}>{data.displayName}</Link>
        </td>
        <td className="table-stats-col">{((data.shared/shared)*100).toLocaleString(undefined, {minimumFractionDigits: 2, maximumFractionDigits: 2})}%</td>
        <td className="table-stats-col">{data.shared.toLocaleString()}</td>
        <td className="table-stats-col">{((data.shared/chatters)*100).toLocaleString(undefined, {minimumFractionDigits: 2, maximumFractionDigits: 2})}%</td>
        <td className="table-stats-col hover:underline hover:text-pink-500 truncate">
          <a href={`https://www.twitch.tv/directory/game/${data.game}`} target="_blank"
             rel="noopener noreferrer">{data.game}</a>
        </td>
      </tr>
    </>
  );
};

export default ChannelDefaultTableRow;
