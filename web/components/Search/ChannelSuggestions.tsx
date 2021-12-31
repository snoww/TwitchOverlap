import {useRouter} from "next/router";
import {Dispatch, RefObject, SetStateAction, useEffect, useRef} from "react";

const ChannelSuggestions = ({channelList = [], setFilter, setSearch}: {channelList: string[], setFilter: Dispatch<SetStateAction<string[]>>, setSearch: Dispatch<SetStateAction<string>>}) => {
  const router = useRouter();
  const searchSuggest = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const handleClick = (e: MouseEvent) => {
      // eslint-disable-next-line @typescript-eslint/ban-ts-comment
      // @ts-ignore
      if (searchSuggest && searchSuggest.current && !searchSuggest.current.contains(e.target)) {
        setFilter([]);
      }
    };
    document.addEventListener("mousedown", handleClick);
  }, [searchSuggest, setFilter]);

  return (
    <>
      <div className="absolute top-14 rounded-md w-40 xs:w-60 dark:bg-slate-600 shadow-md" ref={searchSuggest}>
        {channelList.map((channel, index) => {
          if (channel) {
            if (channelList.length === 1) {
              return (
                <div className="px-3 py-2 cursor-pointer dark:hover:bg-slate-500 rounded-md truncate" key={index}
                     onClick={() => {
                       setFilter([]);
                       setSearch(channel);
                       return router.push(channel);
                     }}
                >
                  {channel}
                </div>
              );
            }
            if (index == 0) {
              return (
                <div className="px-3 pt-2 pb-1 cursor-pointer dark:hover:bg-slate-500 rounded-t-md truncate" key={index}
                     onClick={() => {
                       setFilter([]);
                       setSearch(channel);
                       return router.push(channel);
                     }}
                >
                  {channel}
                </div>
              );
            }
            if (index == channelList.length - 1) {
              return (
                <div className="px-3 pt-1 pb-2 cursor-pointer dark:hover:bg-slate-500 rounded-b-md truncate" key={index}
                     onClick={() => {
                       setFilter([]);
                       setSearch(channel);
                       return router.push(channel);
                     }}
                >
                  {channel}
                </div>
              );
            }
            return (
              <div className="px-3 py-1 cursor-pointer dark:hover:bg-slate-500 truncate" key={index}
                   onClick={() => {
                     setFilter([]);
                     setSearch(channel);
                     return router.push(channel);
                   }}
              >
                {channel}
              </div>
            );
          }
          return null;
        })}
      </div>
    </>
  );
};

export default ChannelSuggestions;
