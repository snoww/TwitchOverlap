import {DateTime, Interval} from "luxon";

export function getTimeDiff(start: DateTime): string {
  let lastUpdated: string;

  const now = DateTime.utc();
  const diff = Interval.fromDateTimes(start, now);
  if (diff.length("minutes") <= 60) {
    const rounded = Math.floor(diff.length("minutes"));
    if (rounded === 1) {
      lastUpdated = `${rounded} minute ago`;
    } else {
      lastUpdated = `${rounded} minutes ago`;
    }
  } else if (diff.length("hours") <= 24) {
    const rounded = Math.floor(diff.length("hours"));
    if (rounded === 1) {
      lastUpdated = `${rounded} hour ago`;
    } else {
      lastUpdated = `${rounded} hours ago`;
    }
  } else if (diff.length("days") <= 7) {
    const rounded = Math.floor(diff.length("days"));
    if (rounded === 1) {
      lastUpdated = `${rounded} day ago`;
    } else {
      lastUpdated = `${rounded} days ago`;
    }
  } else {
    lastUpdated = start.toLocaleString({year: "numeric", month: "short", day: "numeric", hour: "numeric", minute: "numeric"});
  }

  return lastUpdated;
}

export function getDateDiff(start: DateTime): string {
  let lastUpdated: string;

  const now = DateTime.utc();
  const diff = Interval.fromDateTimes(start, now);
  const len = Math.floor(diff.length("day"));
  if (len <= 1) {
    lastUpdated = "Today";
  } else if (len === 2) {
    lastUpdated = "Yesterday";
  } else if (len === 7) {
    lastUpdated = "1 week ago";
  } else {
    lastUpdated = `${len} days ago`;
  }

  return lastUpdated;
}

// shade rgb
// from https://stackoverflow.com/a/13542669/11934162
export const RGBLinearShade = (percentage: number, color: string) => {
  const i = parseInt,
    r = Math.round,
    [a, b, c, d] = color.split(","),
    lz = percentage < 0,
    t = lz ? 0 : 255 * percentage,
    P = lz ? 1 + percentage : 1 - percentage;
  return "rgb" + (d ? "a(" : "(") + r(i(a[3] == "a" ? a.slice(5) : a.slice(4)) * P + t) + "," + r(i(b) * P + t) + "," + r(i(c) * P + t) + (d ? "," + d : ")");
};

export const fetcher = (url: string) => fetch(url).then(res => res.json());

export const DefaultLocale = "en-US";

export const API = "https://api.roki.sh/v2";

export const CDN = "https://d7d8uit9j3127.cloudfront.net/";

export const AtlasDates = [
  {
    name: "December 2021",
    shortName: "Dec. 2021",
    path: "dec21",
    json: `${CDN}2021-12-atlas.json`,
    image: "https://cdn.discordapp.com/attachments/220646943498567680/927213689864597564/dec-21-trans.png",
    imageFallback: `${CDN}2021-12-atlas.png`,
    thumbnail: "https://media.discordapp.net/attachments/220646943498567680/927218161428877393/dec-21.png"
  },
  {
    name: "January 2022",
    shortName: "Jan. 2022",
    path: "jan22",
    json: `${CDN}2022-01-atlas.json`,
    image: "https://cdn.discordapp.com/attachments/220646943498567680/937858525361766460/2022-01-atlas.png",
    imageFallback: `${CDN}2022-01-atlas.png`,
    thumbnail: "https://media.discordapp.net/attachments/220646943498567680/937858525361766460/2022-01-atlas.png"
  },
  {
    name: "February 2022",
    shortName: "Feb. 2021",
    path: "feb22",
    json: `${CDN}2022-02-atlas.json`,
    image: "https://cdn.discordapp.com/attachments/220646943498567680/990852271338303488/2022-02-atlas-trans.png",
    imageFallback: `${CDN}2022-02-atlas-trans.png`,
    thumbnail: "https://cdn.discordapp.com/attachments/220646943498567680/990852272378486814/2022-02-atlas.png"
  },
  {
    name: "March 2022",
    shortName: "Mar. 2021",
    path: "mar22",
    json: `${CDN}2022-03-atlas.json`,
    image: "https://cdn.discordapp.com/attachments/220646943498567680/990852330868064256/2022-03-atlas-trans.png",
    imageFallback: `${CDN}2022-03-atlas-trans.png`,
    thumbnail: "https://cdn.discordapp.com/attachments/220646943498567680/990852329731412048/2022-03-atlas.png"
  },
  {
    name: "April 2022",
    shortName: "Apr. 2021",
    path: "apr22",
    json: `${CDN}2022-04-atlas.json`,
    image: "https://cdn.discordapp.com/attachments/220646943498567680/990853612605440021/2022-04-atlas-trans.png",
    imageFallback: `${CDN}2022-04-atlas-trans.png`,
    thumbnail: "https://cdn.discordapp.com/attachments/220646943498567680/990852306490757140/2022-04-atlas.png"
  },
  {
    name: "May 2022",
    shortName: "May 2021",
    path: "may22",
    json: `${CDN}2022-05-atlas.json`,
    image: "https://cdn.discordapp.com/attachments/220646943498567680/990852345787187210/2022-05-atlas-trans.png",
    imageFallback: `${CDN}2022-05-atlas-trans.png`,
    thumbnail: "https://cdn.discordapp.com/attachments/220646943498567680/990852344851865640/2022-05-atlas.png"
  },
];
