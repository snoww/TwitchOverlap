import NavAtlas from "../../components/NavAtlas";
import { TransformWrapper, TransformComponent } from "react-zoom-pan-pinch";
import Head from "next/head";
import ImageFallback from "../../components/ImageFallback";
import {AtlasDates} from "../../utils/helpers";

const AtlasImage = () => {
  const latestAtlas = AtlasDates[0];

  return (
    <>
      <Head>
        <title>{`Twitch Atlas (Image) - ${latestAtlas.name} - Twitch Viewer Overlap`}</title>
        <meta property="og:title" content="Twitch Atlas - Twitch Community Map"/>
        <meta property="og:description"
              content="Twitch Atlas. Map of the different communities across Twitch. A network graph showing the overlap in communities of the top channels on Twitch. The site is open source on GitHub."/>
        <meta name="description"
              content="Twitch Atlas. Map of the different communities across Twitch. A network graph showing the overlap in communities of the top channels on Twitch. The site is open source on GitHub."/>
        <meta property="og:image"
              content={`${latestAtlas.thumbnail}?width=1024&height=1024`}/>
      </Head>
      <NavAtlas version={"image"}/>
      <div className="overflow-hidden h-screen w-screen bg-gray-300 dark:bg-gray-800">
        <TransformWrapper limitToBounds={false} minScale={0.1} initialScale={0.2} maxScale={1}>
          <TransformComponent>
            <ImageFallback src={latestAtlas.image} alt={`twitch atlas ${latestAtlas.name}`}
                           fallbackSrc={latestAtlas.imageFallback}
                           className="max-w-none"
            />
          </TransformComponent>
        </TransformWrapper>
      </div>
      <div className="absolute bottom-0 right-0 flex flex-col m-2">
        <div>Twitch Atlas {latestAtlas.name}</div>
        <div className="font-mono text-sm ml-auto">stats.roki.sh/atlas</div>
      </div>
    </>
  );
};

export default AtlasImage;
