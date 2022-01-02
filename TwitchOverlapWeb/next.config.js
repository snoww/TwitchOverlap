// eslint-disable-next-line @typescript-eslint/no-var-requires
const withTM = require("next-transpile-modules")(["echarts", "zrender"]);

module.exports = withTM({
  reactStrictMode: true,
  images: {
    domains: ["static-cdn.jtvnw.net", "i.imgur.com", "cdn.frankerfacez.com"],
  },
});
