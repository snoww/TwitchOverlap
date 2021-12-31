import Head from "next/head";
import Nav from "../components/Nav";
import "react-sigma-v2/lib/react-sigma-v2.css";
import dynamic from "next/dynamic";

const Atlas2 = () => {
  if (typeof window !== "undefined") {
    // eslint-disable-next-line @typescript-eslint/ban-ts-comment
    // @ts-ignore
    const SigmaContainer = dynamic(() => import("react-sigma-v2").then(mod => mod.SigmaContainer), {ssr: false});
    const Atlas = dynamic(() => import("../components/Atlas/SigmaAtlas"), {ssr: false});

    return (
      <>
        <Head>
          <title>{"Twitch Atlas - Twitch Viewer Overlap"}</title>
          <meta property="og:title" content="Twitch Atlas - Twitch Community Graph"/>
          <meta property="og:description"
                content="Map of the different communities' interconnectedness across Twitch. A network graph showing the overlap in communities of the top channels on Twitch. The site is open source on GitHub."/>
          <meta property="og:image"
                content="https://cdn.discordapp.com/attachments/220571291617329154/854254051524083742/twitch-community-graph-3.png"/>
        </Head>
        <Nav/>
        <SigmaContainer style={{ height: "100vh", width: "100vw", backgroundColor: "rgb(31,41,55)"}}>
          <Atlas/>
        </SigmaContainer>
      </>
    );
  } else {
    return <div>Atlas Error. :/</div>;
  }
};

export default Atlas2;
