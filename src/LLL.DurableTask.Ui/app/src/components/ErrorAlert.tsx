import React from "react";
import Alert from '@mui/material/Alert';

type Props = {
  error: unknown;
};

export function ErrorAlert(props: Props) {
  const { error } = props;

  if (!error) return null;

  return <Alert severity="error">{String(error)}</Alert>;
}
