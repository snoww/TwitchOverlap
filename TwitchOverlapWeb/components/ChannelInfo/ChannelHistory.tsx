import ReactEChartsCore from "echarts-for-react/lib/core";
import * as echarts from "echarts/core";
import {LineChart} from "echarts/charts";
import {
  DatasetComponent,
  DataZoomComponent,
  GridComponent,
  LegendComponent,
  TooltipComponent,
} from "echarts/components";
import {CanvasRenderer} from "echarts/renderers";
import useSWR from "swr";
import {AggregateDays} from "../../pages/[...channel]";
import {useTheme} from "next-themes";
import {DateTime} from "luxon";
import {DefaultLocale, fetcher} from "../../utils/helpers";

const stringToRGB = function (str: string) {
  let hash = 0;
  for (let i = 0; i < str.length; i++) {
    hash = str.charCodeAt(i) + ((hash << 5) - hash);
  }
  let colour = "#";
  for (let i = 0; i < 3; i++) {
    const value = (hash >> (i * 8)) & 0xFF;
    const str = ("00" + value.toString(16));
    colour += str.substring(str.length - 2);
  }
  return colour;
};

type ChannelHistory = {
  channel: string,
  type: AggregateDays
}

echarts.use(
  [LineChart,
    GridComponent,
    TooltipComponent,
    LegendComponent,
    DataZoomComponent,
    DatasetComponent,
    CanvasRenderer]);

const ChannelHistory = ({channel, type}: ChannelHistory) => {

  const {theme} = useTheme();

  const {
    data,
    error
  } = useSWR(`https://api.roki.sh/v2/history/${channel}${type === AggregateDays.Default ? "" : `/${type.toString()}`}`,
    fetcher, {
      revalidateIfStale: false,
      revalidateOnFocus: false,
      revalidateOnReconnect: false
    });

  if (error) {
    return <div>Chart Error. :/</div>;
  }

  if (!data) {
    return <ReactEChartsCore echarts={echarts} className={"mt-4"}
                             style={{width: "100%", minHeight: "480px", maxWidth: "100vw"}}
                             showLoading={true}
                             loadingOption={{textColor: "#fff", maskColor: "rgba(255, 255, 255, 0)"}}
                             option={{}} notMerge={true}/>;
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

  // reverse history data, to show the latest data point at the end of the chart
  const chartData = [];
  if (type === AggregateDays.Default) {
    for (let i = data.history.length - 1; i >= 0; i--) {
      const tmp = data.history[i];
      const dt = DateTime.fromISO(tmp.timestamp);
      const dtStr = dt.setLocale(DefaultLocale).toLocaleString({month: "short", day: "numeric"})
        + " "
        + dt.setLocale(DefaultLocale).toLocaleString({hour: "numeric", minute: "numeric"});
      chartData.push({...tmp, timestamp: dtStr});
    }
  } else {
    for (let i = data.history.length - 1; i >= 0; i--) {
      chartData.push(data.history[i]);
    }
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
      formatter: function (params: { name: string, seriesName: string, value: { [x: string]: any }, [x: string]: any }[]) {
        let output;
        if (window.location.pathname.split("/").length > 2) {
          const dt = DateTime.fromJSDate(new Date(`${params[0].name} ${new Date(Date.now()).getUTCFullYear()}`));
          const before = dt.plus({days: -type});
          output = `<div class="mb-2"><b>${before.setLocale(DefaultLocale).toLocaleString({
            month: "short",
            day: "numeric"
          })} - ${dt.setLocale(DefaultLocale).toLocaleString({month: "short", day: "numeric"})}</b></div>`;
        } else {
          output = `<div class="mb-2"><b>${params[0].name}</b></div>`;
        }
        const values: [string, number][] = Object.entries(params[0].value).filter(x => x[0] !== "timestamp");
        for (let i = 0; i < values.length; i++) {
          const param = params.find((x: { seriesName: string; }) => x.seriesName === values[i][0]);
          if (param == null) {
            continue;
          }
          output += `<div class="flex justify-between"><div class="mr-4">${param.marker}${values[i][0]}</div><div class="font-mono">${values[i][1].toLocaleString(DefaultLocale)}</div></div>`;
        }
        return output;
      }
    },
    dataset: {
      dimensions: data.channels,
      source: chartData
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

  if (theme !== "dark") {
    option.textStyle = {
      fontFamily: "Inter",
      color: "#000"
    };
    option.legend = {
      textStyle: {
        color: "#000"
      },
      inactiveColor: "#d3d3d3",
      type: "scroll",
      pageTextStyle: {
        color: "#000"
      },
      pageIconColor: "#2f4554",
      pageIconInactiveColor: "#aaa",
      data: legendData
    };
  }

  return (
    <ReactEChartsCore echarts={echarts} className={"mt-4"} style={{width: "100%", minHeight: "480px", maxWidth: "100vw"}}
                      option={option} notMerge={true}/>
  );
};

export default ChannelHistory;
