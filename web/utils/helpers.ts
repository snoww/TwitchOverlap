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
    lastUpdated = start.toISO();
  }

  return lastUpdated;
}

export function getDateDiff(start: DateTime): string {
  let lastUpdated: string;

  const now = DateTime.utc();
  const diff = Interval.fromDateTimes(start, now);
  if (diff.length("days") >= 1) {
    lastUpdated = "Today";
  } else if (diff.length("days") == 2) {
    lastUpdated = "Yesterday";
  } else {
    lastUpdated = `${Math.floor(diff.length("day"))} days ago`;
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
export const fetcherText = (url: string) => fetch(url).then(res => res.text());


export const DefaultLocale = "en-US";
