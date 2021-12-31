import Link from "next/link";
import Image from "next/image";
import ToggleDark from "./ToggleDark";
import {useRef, useState} from "react";

const Nav = () => {
  return (
    <nav className="fixed top-0 w-full z-50 top-0">
      <div className="w-full mx-auto flex flex-nowrap items-center justify-between py-2 tracking-tight">
        <div className="mx-4 p-1 rounded-md bg-gray-700">
          <Link href="/">
            <a
              className="whitespace-nowrap text-base no-underline hover:no-underline hover:text-pink-500 dark:hover:text-pink-800 font-extrabold text-xl tracking-tighter">
              Twitch Overlap
            </a>
          </Link>
        </div>
        <div className="flex-1 flex justify-end">
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
