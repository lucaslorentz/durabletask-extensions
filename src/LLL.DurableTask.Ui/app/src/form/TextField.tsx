import { TextField as MuiTextField } from "@mui/material";
import { TextFieldProps } from "@mui/material/TextField";
import { observer } from "mobx-react-lite";
import React from "react";
import { Field } from "./useForm";

export const TextField = observer(
  (props: { field: Field<any> } & TextFieldProps) => {
    const { field, ...other } = props;
    return (
      <MuiTextField
        fullWidth
        size="small"
        {...other}
        required={field.required}
        label={field.label}
        value={field.value ?? ""}
        onChange={(e: any) => (field.value = e.target.value)}
        error={field.hasError}
        helperText={field.errorMessage}
      />
    );
  },
);
