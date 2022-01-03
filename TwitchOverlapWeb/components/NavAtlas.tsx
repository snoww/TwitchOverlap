import Link from "next/link";
import ToggleDark from "./ToggleDark";

const Nav = ({version}: { version: string }) => {
  return (
    <nav className="fixed top-0 w-full z-50 top-0">
      <div className="w-full mx-auto flex flex-nowrap items-center justify-between py-2 tracking-tight">
        <div className="mx-4 flex-col justify-items-start">
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
          <a className="mr-4 hover:text-pink-500 dark:hover:text-pink-800" href="https://github.com/snoww/TwitchOverlap"
             target="_blank"
             rel="noopener noreferrer"><i className="fab fa-github fa-2x"/></a>
        </div>
      </div>
      <div className="mx-4 -mt-1 whitespace-nowrap text-base tracking-tighter truncate">
          {version === "canvas"
            ? <Link href="/atlas/image">
              <a className="no-underline hover:no-underline hover:text-pink-500 dark:hover:text-pink-800">
                Click here for Image Version (better performance)
              </a>
            </Link>
            : <Link href="/atlas">
              <a className="no-underline hover:no-underline hover:text-pink-500 dark:hover:text-pink-800">
                Click here for Graph Version (better clarity)
              </a>
            </Link>}
      </div>
    </nav>
  );
};

export default Nav;
