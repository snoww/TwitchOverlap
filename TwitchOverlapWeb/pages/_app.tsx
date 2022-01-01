import "../styles/globals.css";
import type {AppProps} from "next/app";
import {ThemeProvider} from "next-themes";
import {Router} from "next/router";
import NProgress from "nprogress";
import "../styles/globals.css";
import Script from "next/script";
import React from "react";

Router.events.on("routeChangeStart", () => NProgress.start());
Router.events.on("routeChangeComplete", () => NProgress.done());
Router.events.on("routeChangeError", () => NProgress.done());

function MyApp({Component, pageProps}: AppProps) {
  return (
    <>
      {/* Global site tag (gtag.js) - Google Analytics*/}
      <Script
        src="https://www.googletagmanager.com/gtag/js?id=G-53NJ04WTT9"
        strategy="afterInteractive"
      />
      <Script id="google-analytics" strategy="afterInteractive">
        {`
                  window.dataLayer = window.dataLayer || [];
                  function gtag(){window.dataLayer.push(arguments);}
                  gtag('js', new Date());
        
                  gtag('config', 'G-53NJ04WTT9');
                `}
      </Script>
      <ThemeProvider attribute={"class"} defaultTheme={"dark"} enableSystem={false}>
        {<Component {...pageProps} />}
      </ThemeProvider>
    </>
  );
}

export default MyApp;
