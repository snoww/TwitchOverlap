import Link from "next/link";
import {ChannelOverlapData, RowChange} from "./ChannelDefaultTableRow";
import {DefaultLocale, isASCII} from "../../utils/helpers";

const ChannelAggregateTableRow = ({shared, chatters, index, data, type}: {shared: number, chatters: number, index: number, data: ChannelOverlapData, type: string}) => {
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
          <Link href={`/${data.loginName}/${type}`}>{channelName}</Link>
        </td>
        <td className="table-stats-col">{((data.shared/shared)*100).toLocaleString(DefaultLocale, {minimumFractionDigits: 2, maximumFractionDigits: 2})}%</td>
        <td className="table-stats-col">{data.shared.toLocaleString(DefaultLocale)}</td>
        <td className="table-stats-col">{((data.shared/chatters)*100).toLocaleString(DefaultLocale, {minimumFractionDigits: 2, maximumFractionDigits: 2})}%</td>
      </tr>
    </>
  );
};

export default ChannelAggregateTableRow;
