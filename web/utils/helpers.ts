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
