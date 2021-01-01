import { TextField as MuiTextField } from "@material-ui/core";
import { TextFieldProps } from "@material-ui/core/TextField";
import { observer } from "mobx-react-lite";
import React from "react";
import { Field } from "./useForm";

export const TextField = observer(
  (props: { field: Field<any> } & TextFieldProps) => {
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
  }
);
