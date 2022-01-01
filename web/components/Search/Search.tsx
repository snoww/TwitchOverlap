import {useEffect, useState} from "react";
import {useRouter} from "next/router";
import useSWR from "swr";
import {fetcher} from "../../utils/helpers";
import ChannelSuggestions from "./ChannelSuggestions";

const Search = () => {
  const [search, setSearch] = useState("");
  const [allChannels, setAllChannels] = useState<Array<string>>([]);
  const [filteredChannels, setFilteredChannels] = useState<Array<string>>([]);
  const router = useRouter();

  const {data} = useSWR("https://api.roki.sh/v2/channels/2000",
    fetcher, {
      revalidateIfStale: false,
      revalidateOnFocus: false,
      revalidateOnReconnect: false
    });

  useEffect(() => {
    setAllChannels(data);
  }, [data]);

  const updateInput = async (input: string) => {
    setSearch(input);
    if (input === "") {
      setFilteredChannels([]);
      return;
    }
    let i = 0;
    setFilteredChannels(allChannels.filter((x) => {
      if (i < 10 && x.includes(input.toLowerCase())) {
        i++;
        return true;
      }
      return false;
    }));
  };

  return (
    <>
      <div className="flex-grow flex justify-center">
        <input type="text"
               className="dark:bg-gray-700 focus:ring-pink-500 dark:focus:ring-pink-900 focus:border-pink-500 dark:focus:border-pink-900 border-gray-300 dark:border-gray-800 border block rounded-none rounded-l-md z-10 ml-2 xs:ml-0 w-40 xs:w-full md:w-1/2"
               placeholder="channel"
               value={search}
               onChange={(e) => updateInput(e.target.value)}
               onKeyUp={(e) => {
                 if (e.key === "Enter" && search !== "") {
                   setSearch("");
                   return router.push(search);
                 }
               }}
               spellCheck={false}
        />
        <span
          className="border-gray-300 dark:border-gray-800 dark:bg-gray-700 hover:text-pink-500 inline-flex items-center px-3 border border-l-0 rounded-r-md cursor-pointer"
          onClick={() => {
            setSearch("");
            return router.push(search);
          }}
        >
          <i className="fas fa-search"/>
        </span>
        <ChannelSuggestions channelList={filteredChannels} setFilter={setFilteredChannels} setSearch={setSearch}/>
      </div>
    </>
  );
};

export default Search;
