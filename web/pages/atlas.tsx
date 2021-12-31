import Head from "next/head";
import ReactECharts from "echarts-for-react";
import useSWR from "swr";
import {fetcher} from "../utils/helpers";
import NavAtlas from "../components/NavAtlas";

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

const Atlas = () => {
  const {data, error} = useSWR("http://192.168.1.220:8080/dec-fa-4.json", fetcher, {
    revalidateIfStale: false,
    revalidateOnFocus: false,
    revalidateOnReconnect: false
  });

  if (error) {
    return <div>Chart Error. :/</div>;
  }

  if (!data) {
    return <ReactECharts className={"mt-4"} style={{width: "100%", height: "100vh"}}
                         showLoading={true}
                         loadingOption={{textColor: "#fff", maskColor: "rgba(255, 255, 255, 0)"}}
                         option={{}} notMerge={true}/>;
  }

  const option = {
    tooltip: {},
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
            borderColor: "#a9a9a9",
            borderWidth: 2
          },
          label: {
            color: "#fff",
            fontSize: x.size / 2 >= 12 ? x.size / 2 : 12,
            fontFamily: "Inter"
          }
        })),
        links: data.edges.map((x: Edge) => ({
          source: x.source,
          target: x.target,
          lineStyle: {
            color: data.nodes.find((y: Node) => y.id === x.source)?.color,
          }
        })),
        label: {
          show: true,
          position: "inside"
        },
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
        silent: true
      }
    ]
  };

  return (
    <>
      <Head>
        <title>{"Twitch Atlas - December 2021 - Twitch Viewer Overlap"}</title>
        <meta property="og:title" content="Twitch Atlas - Twitch Community Map"/>
        <meta property="og:description"
              content="Map of the different communities across Twitch. A network graph showing the overlap in communities of the top channels on Twitch. Inspired by /u/Kgersh's Twitch Atlas on Reddit. The site is open source on GitHub."/>
        <meta property="og:image"
              content="https://cdn.discordapp.com/attachments/220571291617329154/854254051524083742/twitch-community-graph-3.png"/>
      </Head>
      <NavAtlas/>
      <div className="bg-gray-300 dark:bg-gray-800">
        <ReactECharts style={{height: "100vh"}} option={option} notMerge={true}/>
      </div>
      <div className="absolute bottom-0 right-0 flex flex-col m-2">
        <div>Twitch Atlas December 2021</div>
        <div className="font-mono text-sm ml-auto">stats.roki.sh/atlas</div>
      </div>
    </>
  );
};

export default Atlas;
