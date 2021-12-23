import ReactECharts from "echarts-for-react";
import useSWR from "swr";

const stringToRGB = function (str: string) {
  let hash = 0;
  for (let i = 0; i < str.length; i++) {
    hash = str.charCodeAt(i) + ((hash << 5) - hash);
  }
  let colour = "#";
  for (let i = 0; i < 3; i++) {
    const value = (hash >> (i * 8)) & 0xFF;
    colour += ("00" + value.toString(16)).substr(-2);
  }
  return colour;
};

const fetcher = (url: string) => fetch(url).then(res => res.json());

type ChannelHistory = {
  channel: string
}

const ChannelHistory = ({channel}: ChannelHistory) => {
  const {data, error} = useSWR(`http://192.168.1.104:5000/api/v1/history/${channel}`, fetcher);

  if (error) {
    return <div>Chart Error</div>;
  }

  if (!data) {
    return <ReactECharts className={"mt-4"} style={{width: "100%", minHeight: "480px"}}
                         showLoading={true}
                         loadingOption={{textColor: "#fff", maskColor: "rgba(255, 255, 255, 0)"}}
                         option={{}}/>;
  }

  const lines = [];
  for (let i = 1; i < data.channels.length; i++) {
    const color = stringToRGB(data.channels[i]);
    lines.push({
      type: "line",
      itemStyle: {
        color: color
      },
      lineStyle: {
        color: color
      },
      emphasis: {
        focus: "series"
      },
      animationDuration: 500
    });
  }

  const legendData = data.channels.slice(1).sort();
  const option = {
    textStyle: {
      fontFamily: "Inter",
      color: "#fff"
    },
    legend: {
      textStyle: {
        color: "#fff"
      },
      pageTextStyle: {
        color: "#fff"
      },
      pageIconColor: "#aaa",
      pageIconInactiveColor: "#2f4554",
      inactiveColor: "#5b5b5b",
      type: "scroll",
      data: legendData
    },
    grid: {
      left: "7%",
      right: "5%"
    },
    dataZoom: [{
      type: "slider",
      startValue: data.history.length > 24 ? data.history.length - 24 : 0,
      end: 100,
      rangeMode: ["value", "percent"]
    }],
    tooltip: {
      trigger: "axis",
      formatter: function (params: any[]) {
        let output = "<div class=\"mb-2\"><b>" + params[0].name + " UTC</b></div>";
        const values = Object.entries(params[0].value).filter(x => x[0] !== "timestamp");
        for (let i = 0; i < values.length; i++) {
          const param = params.find(x => x.seriesName === values[i][0]);
          if (param == null) {
            continue;
          }
          // eslint-disable-next-line @typescript-eslint/ban-ts-comment
          // @ts-ignore
          output += "<div class=\"flex justify-between\"><div class=\"mr-4\">" + param.marker + values[i][0] + "</div><div class=\"font-bold\">" + values[i][1].toLocaleString() + "</div></div>";
        }
        return output;
      }
    },
    dataset: {
      dimensions: data.channels,
      source: [...data.history].reverse()
    },
    xAxis: {
      type: "category"
    },
    yAxis: {
      type: "value",
      splitLine: {
        show: false
      },
      axisLine: {
        show: true
      },
      axisTick: {
        show: true
      }
    },
    series: lines,
    media: [{
      query: {
        maxWidth: 600
      },
      option: {
        grid: {
          left: "12%",
          right: "6%"
        },
      }
    },]
  };

  return (
    <ReactECharts className={"mt-4"} style={{width: "100%", minHeight: "480px"}} option={option}/>
  );
};

export default ChannelHistory;
