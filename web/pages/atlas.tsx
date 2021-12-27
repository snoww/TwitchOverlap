import Head from "next/head";
import Nav from "../components/Nav";
import ReactECharts from "echarts-for-react";
import {GetStaticProps} from "next";

type Node = {
  id: string,
  name: string,
  size: number,
  color: string
}

type Edge = {
  source: string,
  target: string
}

type AtlasProps = {
  data: {
    nodes: Array<Node>,
    edges: Array<Edge>
  }
}

const Atlas = ({data}: AtlasProps) => {
  const option = {
    tooltip: {},
    series: [
      {
        name: "twitch atlas",
        type: "graph",
        layout: "force",
        roam: true,
        data: data.nodes.map(x => ({
          id: x.id,
          name: x.name,
          symbolSize: x.size,
          itemStyle: {
            color: x.color,
            borderColor: "#a9a9a9",
            borderWidth: 2
          },
          label: {
            color: "#fff",
            fontSize: x.size / 5 >= 12 ? x.size / 5 : 12
          }
        })),
        links: data.edges.map(x => ({
          source: x.source,
          target: x.target,
          lineStyle: {
            color: data.nodes.find(y => y.id === x.source)!.color,
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
          opacity: 0.2
        },
        force: {
          repulsion: 5000,
          // layoutAnimation: false,
          friction: 0.1,
          gravity: 0.05
        },
        silent: true
      }
    ]
  };

  return (
    <>
      <Head>
        <title>{"Twitch Atlas - June 1st > June 15th - Twitch Viewer Overlap"}</title>
        <meta property="og:title" content="Twitch Atlas - Twitch Community Map"/>
        <meta property="og:description"
              content="Map of the different communities across Twitch. A network graph showing the overlap in communities of the top channels on Twitch. Inspired by /u/Kgersh's Twitch Atlas on Reddit. The site is open source on GitHub."/>
        <meta property="og:image"
              content="https://cdn.discordapp.com/attachments/220571291617329154/854254051524083742/twitch-community-graph-3.png"/>
      </Head>
      <Nav/>
      <div className="bg-gray-300 dark:bg-gray-800">
        <ReactECharts style={{height: "100vh"}} option={option}/>
      </div>
    </>
  );
};

export const getStaticProps: GetStaticProps = async (context) => {
  const res = await fetch("https://stats.roki.sh/data/6_2021_graph.json");
  const data = await res.json();

  if (!data) {
    return {
      notFound: true,
    };
  }

  return {
    props: {data}, // will be passed to the page component as props
  };
};

export default Atlas;
