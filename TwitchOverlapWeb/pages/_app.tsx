import "../styles/globals.css";
import type {AppProps} from "next/app";
import {ThemeProvider} from "next-themes";
import {Router} from "next/router";
import NProgress from "nprogress";
import "../styles/globals.css";

Router.events.on("routeChangeStart", () => NProgress.start());
Router.events.on("routeChangeComplete", () => NProgress.done());
Router.events.on("routeChangeError", () => NProgress.done());

function MyApp({Component, pageProps}: AppProps) {
  return (
    <ThemeProvider attribute={"class"} defaultTheme={"dark"} enableSystem={false}>
      {<Component {...pageProps} />}
    </ThemeProvider>
  );
}

export default MyApp;
