import NavAtlas from "../../components/NavAtlas";
import { TransformWrapper, TransformComponent } from "react-zoom-pan-pinch";
import Head from "next/head";

const AtlasImage = () => {
  return (
    <>
      <Head>
        <title>{"Twitch Atlas (Image) - December 2021 - Twitch Viewer Overlap"}</title>
        <meta property="og:title" content="Twitch Atlas - Twitch Community Map"/>
        <meta property="og:description"
              content="Map of the different communities across Twitch. A network graph showing the overlap in communities of the top channels on Twitch. The site is open source on GitHub."/>
        <meta name="description"
              content="Map of the different communities across Twitch. A network graph showing the overlap in communities of the top channels on Twitch. The site is open source on GitHub."/>
        <meta property="og:image"
              content="https://media.discordapp.net/attachments/220646943498567680/927213689864597564/dec-21-trans.png?width=936&height=936"/>
      </Head>
      <NavAtlas version={"image"}/>
      <div className="overflow-hidden h-screen w-screen">
        <TransformWrapper limitToBounds={false} minScale={0.1} initialScale={0.2} maxScale={1}>
          <TransformComponent>
            <img src="https://cdn.discordapp.com/attachments/220646943498567680/927213689864597564/dec-21-trans.png" alt="twitch atlas december 2021"
                 className="max-w-none"
            />
          </TransformComponent>
        </TransformWrapper>
      </div>
      <div className="absolute bottom-0 right-0 flex flex-col m-2">
        <div>Twitch Atlas December 2021</div>
        <div className="font-mono text-sm ml-auto">stats.roki.sh/atlas</div>
      </div>
    </>
  );
};

export default AtlasImage;
