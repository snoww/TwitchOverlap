import Link from "next/link";

type ChannelTableRowType = {
  shared: number,
  chatters: number,
  channel: string
  data: {
    displayName: string,
    game: string,
    shared: number
  }
}

const ChannelTableRow = ({shared, chatters, channel, data}: ChannelTableRowType) => {
  return (
    <>
      <tr className="border-b border-gray-300">
        <td className="table-channel-col">
          <Link href={`/${channel}`}>{data.displayName}</Link>
        </td>
        <td className="table-stats-col">{((data.shared/shared)*100).toFixed(2).toLocaleString()}%</td>
        <td className="table-stats-col">{data.shared.toLocaleString()}</td>
        <td className="table-stats-col">{((data.shared/chatters)*100).toFixed(2).toLocaleString()}%</td>
        <td className="table-stats-col hover:underline hover:text-pink-500 truncate">
          <a href={`https://www.twitch.tv/directory/game/${data.game}`} target="_blank"
             rel="noopener noreferrer">{data.game}</a>
        </td>
      </tr>
    </>
  );
};

export default ChannelTableRow;
