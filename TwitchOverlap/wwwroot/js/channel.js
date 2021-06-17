const myChart = echarts.init(document.getElementById("chart"));
const root = document.querySelector("html");
if (root.classList.contains("dark")) {
    myChart.showLoading({textColor: "#fff",maskColor: "rgba(255, 255, 255, 0)"});
} else {
    myChart.showLoading({maskColor: "rgba(255, 255, 255, 0)"});
}
let legendData;

const changeTheme = () => {
    if (root.classList.contains("dark")) {
        myChart.setOption({
            legend: {
                textStyle: {
                    color: "#fff"
                },
                inactiveColor: "#5b5b5b",
                type: "scroll",
                pageTextStyle: {
                    color: "#fff"
                },
                pageIconColor: "#aaa",
                pageIconInactiveColor: "#2f4554",
                data: legendData
            },
            textStyle: {
                color: "#fff"
            }
        });
    } else {
        myChart.setOption({
            legend: {
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
            },
            textStyle: {
                color: "#000"
            }
        });
    }
};

window.addEventListener("load", () => {
    changeTheme();
});
document.getElementById("toggle-dark").addEventListener("click", () => {
    changeTheme();
});
window.addEventListener("resize", () => {
    myChart.resize();
});

(async function() {
    async function fetchData() {
        try {
            const response = await fetch(`/api/history${window.location.pathname}`);
            return response.json();
        } catch (error) {
            console.error(error);
            return null;
        }
    }

    const data = await fetchData();
    if (data == null || data.channels.length === 0) {
        return;
    }

    myChart.hideLoading();

    const stringToRGB = function(str) {
        let hash = 0;
        for (let i = 0; i < str.length; i++) {
            hash = str.charCodeAt(i) + ((hash << 5) - hash);
        }
        let colour = "#";
        for (let i = 0; i < 3; i++) {
            let value = (hash >> (i * 8)) & 0xFF;
            colour += ("00" + value.toString(16)).substr(-2);
        }
        return colour;
    };

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
    legendData = data.channels.slice(1).sort();
    let option = {
        textStyle: {
            fontFamily: "Inter"
        },
        legend: {
            data: legendData
        },
        grid: {
            left: "7%",
            right: "5%"
        },
        dataZoom: [{
            type: 'slider',
            startValue: data.history.length > 24 ? data.history.length - 24 : 0,
            end: 100,
            rangeMode: ["value", "percent"]
        }],
        tooltip: {
            trigger: "axis",
            formatter: function(params) {
                let output = "<div class=\"mb-2\"><b>" + params[0].name + " UTC</b></div>";
                const values = Object.entries(params[0].value).filter(x => x[0] !== "timestamp");
                for (let i = 0; i < values.length; i++) {
                    let param = params.find(x => x.seriesName === values[i][0]);
                    if (param == null) {
                        continue;
                    }
                    output += "<div class=\"flex justify-between\"><div class=\"mr-4\">" + param.marker + values[i][0] + "</div><div class=\"font-bold\">" + values[i][1].toLocaleString() + "</div></div>";
                }
                return output;
            }
        },
        dataset: {
            dimensions: data.channels,
            source: data.history.reverse()
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
        }, ]
    };

    option && myChart.setOption(option);
})();