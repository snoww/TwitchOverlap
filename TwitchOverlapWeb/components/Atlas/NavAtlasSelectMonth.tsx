import {Listbox, Transition} from "@headlessui/react";
import {AtlasDates} from "../../utils/helpers";
import React, {Fragment, useState} from "react";

const NavAtlasSelectMonth = ({index}: {index: number}) => {
  const [selectedMonth, setSelectedMonth] = useState(AtlasDates.at(index));

  return (
    <Listbox value={selectedMonth} onChange={setSelectedMonth}>
      <Transition
        as={Fragment}
        leave="transition ease-in duration-100"
        leaveFrom="opacity-100"
        leaveTo="opacity-0"
      >
        <Listbox.Options className={"ml-auto mb-1 py-1 text-right bg-gray-400 dark:bg-gray-600 rounded-lg shadow w-40"}>
          {AtlasDates.map((month) => (
            <Listbox.Option
              key={month.name}
              value={month}
              className={({ active }) =>
                `cursor-default select-none py-1 px-4 ${
                  active ? "bg-pink-500 dark:bg-pink-800" : ""
                }`}
            >
              <a href={`/atlas/${month.path}`}>
                {month.name}
              </a>
            </Listbox.Option>
          ))}
        </Listbox.Options>
      </Transition>
      <Listbox.Button className={"hover:text-pink-500 dark:hover:text-pink-800"}>
        <div className={"inline-flex items-center"} title={"Click to select month"}>
          <svg xmlns="http://www.w3.org/2000/svg" className="h-4 w-4 mr-0.5" fill="none" viewBox="0 0 24 24"
               stroke="currentColor" strokeWidth="2">
            <path strokeLinecap="round" strokeLinejoin="round" d="M8 9l4-4 4 4m0 6l-4 4-4-4"/>
          </svg>Twitch Atlas {selectedMonth?.name}
        </div>
      </Listbox.Button>
    </Listbox>
  );
};

export default NavAtlasSelectMonth;
