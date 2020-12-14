import { TextField as MuiTextField, TextFieldProps } from "@material-ui/core";
import React from "react";
import { Field } from "./form-hooks";

export const TextField = (props: { field: Field<any> } & TextFieldProps) => {
  return (
    <MuiTextField
      fullWidth
      variant="outlined"
      size="small"
      {...props}
      required={props.field.required}
      label={props.field.label}
      value={props.field.value ?? ""}
      onChange={(e: any) => (props.field.value = e.target.value)}
      error={props.field.hasError}
      helperText={props.field.errorMessage}
    />
  );
};
