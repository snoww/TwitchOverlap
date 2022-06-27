const AtlasMeta = ({thumbnail, month}: {thumbnail: string, month: string}) => {
  return (
    <>
      <meta property="og:title" content={`Twitch Atlas ${month} - Twitch Community Map`}/>
      <meta property="twitter:title" content={`Twitch Atlas ${month} - Twitch Community Map`}/>
      <meta property="og:description"
            content={`Twitch Atlas ${month}. Map of the different communities across Twitch. A network graph showing the overlap in communities of the top channels on Twitch. The site is open source on GitHub.`}/>
      <meta name="description"
            content={`Twitch Atlas ${month}. Map of the different communities across Twitch. A network graph showing the overlap in communities of the top channels on Twitch. The site is open source on GitHub.`}/>
      <meta name="twitter:description"
            content={`Twitch Atlas ${month}. Map of the different communities across Twitch. A network graph showing the overlap in communities of the top channels on Twitch. The site is open source on GitHub.`}/>
      <meta property="og:image"
            content={`${thumbnail}?width=1024&height=1024`}/>
      <meta property="twitter:image"
            content={`${thumbnail}?width=1024&height=1024`}/>
    </>
  );
};

export default AtlasMeta;
