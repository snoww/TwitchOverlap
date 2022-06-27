import NavAtlas from "../../../components/Atlas/NavAtlas";
import { TransformWrapper, TransformComponent } from "react-zoom-pan-pinch";
import Head from "next/head";
import ImageFallback from "../../../components/ImageFallback";
import {AtlasDates} from "../../../utils/helpers";
import AtlasMeta from "../../../components/Atlas/AtlasMeta";
import NavAtlasFooter from "../../../components/Atlas/NavAtlasFooter";

const AtlasImage = () => {
  const latestAtlas = AtlasDates[AtlasDates.length - 1];
  // todo: allow user to change to past atlas
  return (
    <>
      <Head>
        <title>{`Twitch Atlas (Image) - ${latestAtlas.name} - Twitch Viewer Overlap`}</title>
        <AtlasMeta thumbnail={latestAtlas.thumbnail} month={latestAtlas.name}/>
      </Head>
      <NavAtlas version={"image"} enableSwitch/>
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
      <NavAtlasFooter name={latestAtlas.name} index={-1}/>
    </>
  );
};

export default AtlasImage;
