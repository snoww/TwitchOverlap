import Link from "next/link";
import ToggleDark from "../ToggleDark";
import {useRef, useState} from "react";
import Search from "../Search/Search";

const Nav = () => {
  const [showDropdown, setShowShowDropdown] = useState(false);
  const dd = useRef<HTMLDivElement>(null);

  function openDropdown() {
    if (!showDropdown && dd.current !== null) {
      dd.current.focus();
    }
    setShowShowDropdown(!showDropdown);
  }

  function closeDropdown() {
    setShowShowDropdown(false);
  }

  return (
    <nav className="fixed top-0 w-full z-50 top-0 bg-white dark:bg-gray-900 shadow-lg">
      <div className="w-full mx-auto flex flex-nowrap items-center justify-between py-2 tracking-tight">
        <div className="flex-1 flex items-center">
          <Link href="/">
            <a className="block">
              <div className="block pl-4" style={{fontSize: 0}}>
                <img src="/images/roki2-round-10.png" alt="Roki" width={48} height={48} style={{minWidth: "48px"}}/>
              </div>
            </a>
          </Link>
          <div className="pl-4 hidden md:block">
            <Link href="/">
              <a
                className="whitespace-nowrap text-base no-underline hover:no-underline hover:text-pink-500 dark:hover:text-pink-800 font-extrabold text-xl tracking-tighter">
                Twitch Overlap
              </a>
            </Link>
          </div>
          <div className="pl-8 hidden lg:flex xl:hidden items-center font-medium">
            <Link href="/atlas">
              <a
                className="whitespace-nowrap text-base no-underline hover:no-underline text-xl tracking-tighter">
                <div className={"inline-flex text-center items-center"}>
                  <div className={"hover:text-pink-500 dark:hover:text-pink-800"}>Atlas</div>
                  <div className={"text-xs rounded bg-gray-600 p-1 ml-1"}>May</div>
                </div>
              </a>
            </Link>
          </div>
          <div className="pl-8 hidden xl:flex items-center font-medium">
            <Link href="/atlas">
              <a
                className="whitespace-nowrap text-base no-underline hover:no-underline text-xl tracking-tighter">
                <div className={"inline-flex text-center items-center"}>
                  <div className={"hover:text-pink-500 dark:hover:text-pink-800"}>Twitch Atlas</div>
                  <div className={"text-sm rounded bg-gray-600 p-1 ml-1"}>May 2022</div>
                </div>
              </a>
            </Link>
          </div>
        </div>
        <Search/>
        <div className="flex-1 flex justify-end lg:hidden">
          <button id="showDropdown" className="h-8 w-8 mx-2 hover:text-pink-500 dark:hover:text-pink-800" onClick={openDropdown}>
            <i className="fas fa-ellipsis-v"/>
          </button>
        </div>
        <div className="lg:hidden"
          tabIndex={-1} ref={dd} onBlur={(e) => {
          if (!e.currentTarget.contains(e.relatedTarget) && e.relatedTarget?.id !== "showDropdown") {
            closeDropdown();
          }
        }}>
          {showDropdown && <div
            className="lg:hidden absolute top-14 right-2 w-32 bg-slate-200 dark:bg-slate-700 rounded-md shadow-lg p-4">
            <ul className="text-gray-700 dark:text-gray-200">
              <li className="pb-2 mb-2 border-b border-gray-400">
                <Link href="/atlas">
                  <a className="hover:text-pink-500 dark:hover:text-pink-800 flex items-center">
                    <i className="fas fa-lg fa-project-diagram mr-4"/>
                    <div>Atlas</div>
                  </a>
                </Link>
              </li>
              <li className="pb-2 mb-2 border-b border-gray-400">
                <a className="hover:text-pink-500 dark:hover:text-pink-800 flex items-center"
                   href="https://github.com/snoww/TwitchOverlap" target="_blank"
                   rel="noopener noreferrer"><i className="fab fa-github fa-2x mr-2"/>
                  <div>GitHub</div>
                </a>
              </li>
              <li className="mt-2 -mb-1">
                <button
                  className="mr-4 hover:text-pink-500 dark:hover:text-pink-800 focus:outline-none"
                >
                  <ToggleDark>
                    <div className="ml-2">Theme</div>
                  </ToggleDark>
                </button>
              </li>
            </ul>
          </div>}
        </div>
        <div className="hidden flex-1 lg:flex justify-end">
          <button
            className="mr-4 dark:text-gray-200 hover:text-pink-500 dark:hover:text-pink-800 focus:outline-none"
          >
            <ToggleDark/>
          </button>
          <a className="mr-4 hover:text-pink-500 dark:hover:text-pink-800" href="https://github.com/snoww/TwitchOverlap" target="_blank"
             rel="noopener noreferrer"><i className="fab fa-github fa-2x"/></a>
        </div>
      </div>
    </nav>
  );
};

export default Nav;
