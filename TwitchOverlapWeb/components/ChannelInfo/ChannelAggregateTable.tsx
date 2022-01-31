import {ChannelOverlapData} from "./ChannelDefaultTableRow";
import ChannelAggregateTableRow from "./ChannelAggregateTableRow";
import {AggregateDays} from "../../pages/[...channel]";

const ChannelAggregateTable = ({data, totalUnique, totalShared, type}: {data: ChannelOverlapData[], totalUnique: number, totalShared: number, type: AggregateDays}) => {
  return (
    <>
      <div className="overflow-x-auto">
        <table className="table-fixed mt-4 mx-auto">
          <thead className="text-left font-medium">
          <tr className="border-b-2 border-gray-400">
            <td className="w-2 px-2 md:px-4 py-2" title="Overlap rank">#</td>
            <td className="w-3 px-2 md:px-4 py-2" title="Change compared to last overlap"/>
            <td className="w-1/3 x-2 md:px-4 py-2" title="Channel">Channel</td>
            <td className="px-2 md:px-4 py-2" title="Probability of where a shared chatter is from">Overlap
              Probability
            </td>
            <td className="px-2 md:px-4 py-2" title="Total number of overlap from a channel">Overlap Chatters
            </td>
            <td className="px-2 md:px-4 py-2" title="Percentage of total chatters">% of Total Chatters</td>
          </tr>
          </thead>
          <tbody>
          {data.map((x, i) =>
            <ChannelAggregateTableRow key={x.loginName} shared={totalShared} chatters={totalUnique} index={i} data={x} type={type.toString()}/>
          )}
          </tbody>
        </table>
      </div>
    </>
  );
};

export default ChannelAggregateTable;
