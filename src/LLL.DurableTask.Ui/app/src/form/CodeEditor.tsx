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
  const { field, editorProps, ...other } = props;
  return (
    <MuiCodeEditor
      fullWidth
      size="small"
      {...other}
      required={field.required}
      label={field.label}
      value={field.value ?? ""}
      editorProps={{
        ...editorProps,
        onChange: (value) => (field.value = value),
      }}
      error={field.hasError}
      helperText={field.errorMessage}
    />
  );
});
