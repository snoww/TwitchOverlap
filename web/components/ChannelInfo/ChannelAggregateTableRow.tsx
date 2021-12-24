import Link from "next/link";
import {ChannelOverlapData, RowChange} from "./ChannelTableRow";

const ChannelAggregateTableRow = ({shared, chatters, data, type}: {shared: number, chatters: number, data: ChannelOverlapData, type: string}) => {
  return (
    <>
      <tr className="border-b border-gray-300">
        <RowChange change={data.change}/>
        <td className="table-channel-col">
          <Link href={`/${data.loginName}/${type}`}>{data.displayName}</Link>
        </td>
        <td className="table-stats-col">{((data.shared/shared)*100).toFixed(2).toLocaleString()}%</td>
        <td className="table-stats-col">{data.shared.toLocaleString()}</td>
        <td className="table-stats-col">{((data.shared/chatters)*100).toFixed(2).toLocaleString()}%</td>
      </tr>
    </>
  );
};

export default ChannelAggregateTableRow;
