import ReactEChartsCore from "echarts-for-react/lib/core";
import * as echarts from "echarts/core";
import {GraphChart} from "echarts/charts";
import {DatasetComponent} from "echarts/components";
import {CanvasRenderer} from "echarts/renderers";
import Head from "next/head";
import useSWR from "swr";
import {fetcher} from "../../utils/helpers";
import NavAtlas from "../../components/NavAtlas";
import Nav from "../../components/Nav";
import {useTheme} from "next-themes";

type Node = {
  id: string,
  name: string,
  value: number,
  size: number,
  color: string,
  category: number,
  x: number,
  y: number
}

type Edge = {
  source: string,
  target: string
}

echarts.use([
  GraphChart,
  DatasetComponent,
  CanvasRenderer
]);

const Atlas = () => {
  const {theme} = useTheme();
  const {data, error} = useSWR("https://d3me8i09xp7mrh.cloudfront.net/dec-21-atlas.json", fetcher, {
    revalidateIfStale: false,
    revalidateOnFocus: false,
    revalidateOnReconnect: false
  });

  if (error) {
    return (
      <>
        <Nav/>
        <div className="text-center mt-24">Atlas Error :/</div>
        <div className="text-center mt-2">Please try again later</div>
      </>
    );
  }

  if (!data) {
    return <ReactEChartsCore echarts={echarts} className={"mt-4"} style={{width: "100%", height: "100vh"}}
                         showLoading={true}
                         loadingOption={{textColor: "#fff", maskColor: "rgba(255, 255, 255, 0)"}}
                         option={{}} notMerge={true}/>;
  }

  const option = {
    series: [
      {
        type: "graph",
        roam: true,
        categories: data.categories,
        data: data.nodes.map((x: Node) => ({
          id: x.id,
          name: x.name,
          symbolSize: x.size,
          value: x.value,
          category: x.category,
          x: x.x,
          y: x.y,
          itemStyle: {
            color: x.color,
            borderColor: "#a1a1aa",
            borderWidth: 2
          },
          label: {
            fontSize: x.size / 2 >= 12 ? x.size / 2 : 12,
          }
        })),
        links: data.edges.map((x: Edge) => ({
          source: x.source,
          target: x.target,
          lineStyle: {
            color: data.nodes.find((y: Node) => y.id === x.source)?.color,
          }
        })),
        labelLayout: {
          hideOverlap: true
        },
        lineStyle: {
          curveness: 0.3,
          opacity: 0.2,
        },
        scaleLimit: {
          min: 1
        },
        zoom: 1.2,
        label: {
          show: true,
          color: "#fafafa",
          fontFamily: "Inter"
        },
        silent: true
      }
    ]
  };

  if (theme !== "dark") {
    option.series[0].label.color = "#000";
  }

  return (
    <>
      <Head>
        <title>{"Twitch Atlas - December 2021 - Twitch Viewer Overlap"}</title>
        <meta property="og:title" content="Twitch Atlas - Twitch Community Map"/>
        <meta property="og:description"
              content="Map of the different communities across Twitch. A network graph showing the overlap in communities of the top channels on Twitch. The site is open source on GitHub."/>
        <meta name="description"
              content="Map of the different communities across Twitch. A network graph showing the overlap in communities of the top channels on Twitch. The site is open source on GitHub."/>
        <meta property="og:image"
              content="https://d3me8i09xp7mrh.cloudfront.net/dec-21-trans.png"/>
      </Head>
      <NavAtlas version={"canvas"}/>
      <div className="bg-gray-300 dark:bg-gray-800">
        <ReactEChartsCore echarts={echarts} style={{height: "100vh"}} option={option} notMerge={true}/>
      </div>
      <div className="absolute bottom-0 right-0 flex flex-col m-2">
        <div>Twitch Atlas December 2021</div>
        <div className="font-mono text-sm ml-auto">stats.roki.sh/atlas</div>
      </div>
    </>
  );
};

export default Atlas;
