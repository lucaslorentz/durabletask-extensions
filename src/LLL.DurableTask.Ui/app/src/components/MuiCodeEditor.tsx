import Editor from "@monaco-editor/react";
import { TextField as MuiTextField } from "@mui/material";
import { TextFieldProps } from "@mui/material/TextField";
import React from "react";

type OmitTextFieldProps = "multiline" | "onChange";

type EditorProps = React.ComponentProps<typeof Editor>;

type Props = Omit<TextFieldProps, OmitTextFieldProps> & {
  editorProps?: EditorProps;
};

const wrappedEditor = React.forwardRef((props: EditorProps, _ref) => {
  return <Editor {...props} />;
});

export function MuiCodeEditor(props: Props) {
  const { disabled, editorProps, ...other } = props;

  return (
    <MuiTextField
      {...other}
      multiline
      InputLabelProps={{ shrink: true }}
      disabled={disabled}
      InputProps={{
        inputComponent: wrappedEditor as any,
        inputProps: {
          ...(editorProps as any),
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
