import {GetStaticPaths, GetStaticProps} from "next";
import {AtlasDates} from "../../utils/helpers";
import Head from "next/head";
import AtlasMeta from "../../components/Atlas/AtlasMeta";
import NavAtlas from "../../components/Atlas/NavAtlas";
import {TransformComponent, TransformWrapper} from "react-zoom-pan-pinch";
import ImageFallback from "../../components/ImageFallback";
import NavAtlasFooter from "../../components/Atlas/NavAtlasFooter";

const atlas = ({path}: {path: {params: {atlas: string}}}) => {
  let selected = AtlasDates.length - 1;
  AtlasDates.forEach((s, i) => {
    if (s.path === path.params.atlas) {
      selected = i;
    }
  });

  const latestAtlas = AtlasDates[selected];

  return (
    <>
      <Head>
        <title>{`Twitch Atlas (Image) - ${latestAtlas.name} - Twitch Viewer Overlap`}</title>
        <AtlasMeta thumbnail={latestAtlas.thumbnail} month={latestAtlas.name}/>
      </Head>
      <NavAtlas version={"image"} enableSwitch={false}/>
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
      <NavAtlasFooter name={latestAtlas.name} index={selected}/>
    </>
  );
};

export default atlas;

export const getStaticPaths: GetStaticPaths = async () => {
  const paths: { params: { atlas: string; }; }[] = [];
  AtlasDates.map(month => {
    paths.push({ params: { atlas: month.path }});
  });
  return { paths, fallback: false };
};

export const getStaticProps: GetStaticProps = async ({params}) => {
  return {
    props: {
      path: {params}
    },
  };
};
