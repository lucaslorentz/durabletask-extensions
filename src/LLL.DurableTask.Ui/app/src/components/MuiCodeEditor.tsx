import Editor from "@monaco-editor/react";
import { TextField as MuiTextField } from "@mui/material";
import { TextFieldProps } from "@mui/material/TextField";
import React from "react";

type OmitTextFieldProps = "multiline" | "onChange";

type Props = React.ComponentProps<typeof Editor> &
  Omit<TextFieldProps, OmitTextFieldProps> & {
    onChange: (newValue: string, e: Event) => void;
  };

export function MuiCodeEditor(props: Props) {
  const { disabled, onChange, ...other } = props;

  return (
    <MuiTextField
      {...other}
      multiline
      InputLabelProps={{ shrink: true }}
      disabled={disabled}
      InputProps={{
        inputComponent: Editor as any,
        inputProps: {
          ...(props as any),
          options: {
            minimap: {
              enabled: false,
            },
            wordBasedSuggestions: false,
            disabled: disabled,
            lineNumbersMinChars: 1,
          },
        },
      }}
    />
  );
}
