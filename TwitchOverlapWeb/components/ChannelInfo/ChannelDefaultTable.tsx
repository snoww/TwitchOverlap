import ChannelDefaultTableRow, {ChannelOverlapData} from "./ChannelDefaultTableRow";
import {ChannelStats} from "../../pages/[...channel]";

const ChannelDefaultTable = ({data, channel}: {data: ChannelOverlapData[], channel: ChannelStats}) => {
  return (
    <>
      <div className="overflow-x-auto">
        <table className="table-fixed mt-4 mx-auto">
          <thead className="text-left font-medium">
          <tr className="border-b-2 border-gray-400">
            <td className="w-2 px-2 md:px-4 py-2" title="Overlap rank">#</td>
            <td className="px-2 md:px-4 py-2" title="Change compared to last overlap"/>
            <td className="w-1/5 px-2 md:px-4 py-2" title="Channel">Channel</td>
            <td className="px-2 md:px-4 py-2" title="Probability of where a shared chatter is from">Overlap
              Probability
            </td>
            <td className="px-2 md:px-4 py-2" title="Total number of overlap from a channel">Overlap Chatters
            </td>
            <td className="px-2 md:px-4 py-2" title="Percentage of total chatters">% of Total Chatters</td>
            <td className="w-1/3 px-2 md:px-4 py-2" title="Current category">Playing</td>
          </tr>
          </thead>
          <tbody>
          {data.map((x, i) =>
            <ChannelDefaultTableRow key={x.loginName} shared={channel.shared} chatters={channel.chatters} data={x} index={i}/>
          )}
          </tbody>
        </table>
      </div>
    </>
  );
};

export default ChannelDefaultTable;
