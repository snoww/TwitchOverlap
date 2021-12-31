import {useLoadGraph, useRegisterEvents, useSetSettings, useSigma} from "react-sigma-v2";
import {parse} from "graphology-gexf/browser";
import Graph from "graphology";
import {fetcherText, RGBLinearShade} from "../../utils/helpers";
import {useEffect, useState} from "react";
import { Attributes } from "graphology-types";
import useSWR from "swr";

const SigmaAtlas = () => {
  const {
    data,
    error
  } = useSWR("http://localhost:8080/dec-fa-3.gexf",
    fetcherText, {
      revalidateIfStale: false,
      revalidateOnFocus: false,
      revalidateOnReconnect: false
    });

  const sigma = useSigma();
  const loadGraph = useLoadGraph();
  // const registerEvents = useRegisterEvents();
  const setSettings = useSetSettings();

  // const [hoveredNode, setHoveredNode] = useState<string | null>(null);
  // const [hoveredNeighbours, setHoveredNeighbours] = useState<Set<string> | null>(null);

  useEffect(() => {
    setSettings({
      labelDensity: 0.25,
      // labelColor: {color: "#fff"},
      labelRenderedSizeThreshold: 0,
    });

    if (!data) {
      return;
    }
    const graph = parse(Graph, data);
    console.log(graph.getNodeAttributes("xqcow"));

    graph.forEachEdge((_edge, attributes, _source, _target, sourceAttributes) => {
      attributes.size = 0.25;
      attributes.color = RGBLinearShade(-0.5, sourceAttributes.color)
        .replace(")", ",.7)")
        .replace("rgb", "rgba");
    });

    loadGraph(graph);
  }, [data, loadGraph, setSettings]);

  // useEffect(() => {
  //   // Register Sigma events
  //   registerEvents({
  //     enterNode: ({ node }) => {
  //       const graph = sigma.getGraph();
  //       setHoveredNode(node);
  //       setHoveredNeighbours(new Set(graph.neighbors(node)));
  //     },
  //     leaveNode: () => {
  //       setHoveredNode(null);
  //       setHoveredNeighbours(null);
  //     },
  //   });
  // }, [sigma, registerEvents]);
  //
  // useEffect(() => {
  //   setSettings({
  //     nodeReducer: (node: string, data: { [key: string]: unknown }) => {
  //       const newData: Attributes = { ...data };
  //
  //       if (hoveredNeighbours && !hoveredNeighbours.has(node) && hoveredNode !== node) {
  //         newData.label = "";
  //         newData.color = "rgba(31, 41, 55, 0)";
  //       }
  //       return newData;
  //     },
  //     edgeReducer: (edge: string, data: { [key: string]: unknown }) => {
  //       const graph = sigma.getGraph();
  //       const newData = { ...data, hidden: false };
  //       if (hoveredNode && !graph.hasExtremity(edge, hoveredNode)) {
  //         newData.hidden = true;
  //       }
  //       return newData;
  //     },
  //   });
  // }, [sigma, setSettings, hoveredNode, hoveredNeighbours]);

  if (error) {
    return <div>Atlas Error. :/</div>;
  }

  if (!data) {
    return (<div>Loading</div>);
  }

  return (
    <></>
  );
};

export default SigmaAtlas;
