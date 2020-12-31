export function toDatetimeLocal(value: string) {
  if (!value) return "";

  var date = new Date(value);

  var ten = function (i: number) {
      return (i < 10 ? "0" : "") + i;
    },
    YYYY = date.getFullYear(),
    MM = ten(date.getMonth() + 1),
    DD = ten(date.getDate()),
    HH = ten(date.getHours()),
    II = ten(date.getMinutes()),
    SS = ten(date.getSeconds());
  return YYYY + "-" + MM + "-" + DD + "T" + HH + ":" + II + ":" + SS;
}

export function toUtcISO(value: string) {
  if (!value) return "";

  return new Date(value).toISOString();
}
