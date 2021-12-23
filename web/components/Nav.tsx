import Link from "next/link";
import Image from "next/image";

const Nav = () => {
  return (
    <nav className="fixed top-0 w-full z-50 top-0 bg-white dark:bg-gray-900 shadow-lg">
      <div className="w-full mx-auto flex flex-wrap items-center justify-between py-2 tracking-tight">
        <div className="flex-1 flex items-center">
          <Link href="/">
            <a className="block sm:hidden md:block">
              <div className="block pl-4" style={{fontSize: 0}}>
                <Image src="/images/roki2-round-10.png" alt="Roki" width={48} height={48} quality={10}/>
              </div>
            </a>
          </Link>
          <div className="pl-4 hidden sm:block">
            <Link href="/">
              <a
                className="text-base no-underline hover:no-underline hover:text-pink-500 dark:hover:text-pink-800 font-extrabold text-xl tracking-tighter">
                Twitch Overlap
              </a>
            </Link>
          </div>
          <div className="pl-8 hidden sm:flex items-center font-medium">
            <Link href="/atlas">
              <a
                className="text-base no-underline hover:no-underline hover:text-pink-500 dark:hover:text-pink-800 text-xl tracking-tighter">Twitch
                Atlas
              </a>
            </Link>
          </div>
        </div>
        <div className="flex-grow flex justify-center">
          <input type="text" id="search-channel"
                 className="dark:bg-gray-700 focus:ring-pink-500 dark:focus:ring-pink-900 focus:border-pink-500 dark:focus:border-pink-900 border-gray-300 dark:border-gray-800 border block rounded-none rounded-l-md z-10 w-40 sm:w-full md:w-1/2"
                 placeholder="channel"/>
          <span
            className="border-gray-300 dark:border-gray-800 dark:bg-gray-700 hover:text-pink-500 inline-flex items-center px-3 border border-l-0 rounded-r-md cursor-pointer">
            <i className="fas fa-search"/>
          </span>
        </div>
        <div className="flex-1 flex justify-end">
          <button
            className="mr-4 text-gray-700 dark:text-gray-200 hover:text-pink-500 dark:hover:text-pink-800 focus:outline-none"
            id="toggle-dark"
            // onClick="toggleDark()"
          >
            <i id="dark-icon" className="fas fa-sun fa-2x"/>
          </button>
          <a className="mr-4 hover:text-pink-500" href="https://github.com/snoww/TwitchOverlap" target="_blank"
             rel="noopener noreferrer"><i className="fab fa-github fa-2x"/></a>
        </div>
      </div>
    </nav>
  );
};

export default Nav;
