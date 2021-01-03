export function toLocalISO(value: string) {
  if (!value) return "";

  let date = new Date(value);

  const year = String(date.getFullYear()).padStart(4, "0"),
    month = String(date.getMonth() + 1).padStart(2, "0"),
    day = String(date.getDate()).padStart(2, "0"),
    hours = String(date.getHours()).padStart(2, "0"),
    minutes = String(date.getMinutes()).padStart(2, "0"),
    seconds = String(date.getSeconds()).padStart(2, "0"),
    milliseconds = String(date.getMilliseconds()).padStart(3, "0");

  return `${year}-${month}-${day}T${hours}:${minutes}:${seconds}.${milliseconds}`;
}

export function toUtcISO(value: string) {
  if (!value) return "";

  return new Date(value).toISOString();
}

export function formatDateTime(value: string) {
  if (!value) return "";

  let date = new Date(value);

  if (date.getFullYear() <= 1 || date.getFullYear() >= 9999) {
    return "";
  }

  return date.toLocaleString();
}
