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
        <div title={description}>{curr.toLocaleString()}</div>
        {change >= 0
          ? <div title={changeDescription}
                 className="pl-1 text-xs text-green-500">{`+${(Math.round((change + Number.EPSILON) * 10000) / 100).toLocaleString()}%`}</div>
          : <div title={changeDescription}
                 className="pl-1 text-xs text-red-500">{`${(Math.round((change + Number.EPSILON) * 10000) / 100).toLocaleString()}%`}</div>
        }
      </div>
    </>
  );
};

const ChannelInfoPercentage = ({curr, prev, changeDescription}: ChannelInfoProps) => {
  return (
    <>
      <div className="flex items-center justify-center">
        <div>{curr.toLocaleString()}</div>
        {prev >= 0
          ? <div title={changeDescription}
                 className="pl-1 text-xs text-green-500">{`+${(Math.round((prev + Number.EPSILON) * 10000) / 100).toLocaleString()}%`}</div>
          : <div title={changeDescription}
                 className="pl-1 text-xs text-red-500">{`${(Math.round((prev + Number.EPSILON) * 10000) / 100).toLocaleString()}%`}</div>
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
            className="pl-1 text-xs text-green-500">{`+${(Math.round((change + Number.EPSILON) * 10000) / 100).toLocaleString()}%`}</div>
          : <div
            className="pl-1 text-xs text-red-500">{`${(Math.round((change + Number.EPSILON) * 10000) / 100).toLocaleString()}%`}</div>
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
