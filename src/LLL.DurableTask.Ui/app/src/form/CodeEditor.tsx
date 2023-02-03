import { observer } from "mobx-react-lite";
import React from "react";
import { MuiCodeEditor } from "../components/MuiCodeEditor";
import { Field } from "./useForm";

type PropsDerivedFromField =
  | "required"
  | "label"
  | "value"
  | "onChange"
  | "error"
  | "helperText";

type Props = { field: Field<any> } & Omit<
  React.ComponentProps<typeof MuiCodeEditor>,
  PropsDerivedFromField
>;

export const CodeEditor = observer((props: Props) => {
  return (
    <MuiCodeEditor
      fullWidth
      size="small"
      {...props}
      required={props.field.required}
      label={props.field.label}
      value={props.field.value ?? ""}
      onChange={(value) => (props.field.value = value)}
      error={props.field.hasError}
      helperText={props.field.errorMessage}
    />
  );
});
