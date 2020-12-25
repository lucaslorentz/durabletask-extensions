import React from "react";

type Props = {
  error: unknown;
};

export function ErrorAlert(props: Props) {
  const { error } = props;

  if (!error) return null;

  return <div>{String(error)}</div>;
}
