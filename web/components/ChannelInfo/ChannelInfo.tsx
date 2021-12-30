import {DefaultLocale} from "../../utils/helpers";

type ChannelInfoProps = {
  curr: number,
  prev: number,
  description: string,
  changeDescription: string
}

const ChannelInfo = ({curr, prev, description, changeDescription}: ChannelInfoProps) => {
  const change = (curr - prev) / prev;
  return (
    <>
      <div className="flex items-center justify-center">
        <div title={description}>{curr.toLocaleString(DefaultLocale)}</div>
        {change >= 0
          ? <div title={changeDescription}
                 className="pl-1 text-xs text-green-500">{`+${(change * 100).toLocaleString(DefaultLocale, {minimumFractionDigits: 2, maximumFractionDigits: 2})}%`}</div>
          : <div title={changeDescription}
                 className="pl-1 text-xs text-red-500">{`${(change * 100).toLocaleString(DefaultLocale, {minimumFractionDigits: 2, maximumFractionDigits: 2})}%`}</div>
        }
      </div>
    </>
  );
};

const ChannelInfoPercentage = ({curr, prev, changeDescription}: ChannelInfoProps) => {
  return (
    <>
      <div className="flex items-center justify-center">
        <div>{curr.toLocaleString(DefaultLocale)}</div>
        {prev >= 0
          ? <div title={changeDescription}
                 className="pl-1 text-xs text-green-500">{`+${(prev * 100).toLocaleString(DefaultLocale, {minimumFractionDigits: 2, maximumFractionDigits: 2})}%`}</div>
          : <div title={changeDescription}
                 className="pl-1 text-xs text-red-500">{`${(prev * 100).toLocaleString(DefaultLocale, {minimumFractionDigits: 2, maximumFractionDigits: 2})}%`}</div>
        }
      </div>
    </>
  );
};

const ChannelOverlapPercentageInfo = ({change, curr}: { curr: string, change: number }) => {
  return (
    <>
      <div className="flex items-center justify-center">
        <div>{curr}%</div>
        {change >= 0
          ? <div
            className="pl-1 text-xs text-green-500">{`+${(change * 100).toLocaleString(DefaultLocale, {minimumFractionDigits: 2, maximumFractionDigits: 2})}%`}</div>
          : <div
            className="pl-1 text-xs text-red-500">{`${(change * 100).toLocaleString(DefaultLocale, {minimumFractionDigits: 2, maximumFractionDigits: 2})}%`}</div>
        }
      </div>
    </>
  );
};

export {
  ChannelInfo,
  ChannelInfoPercentage,
  ChannelOverlapPercentageInfo
};
