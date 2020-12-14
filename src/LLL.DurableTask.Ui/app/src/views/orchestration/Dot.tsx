import React from "react";
import { CSSProperties } from "@material-ui/core/styles/withStyles";

type Props = {
  style?: CSSProperties;
};

export const Dot = React.forwardRef(
  (props: Props, ref: React.Ref<HTMLDivElement>) => {
    const { style } = props;

    return (
      <div
        ref={ref}
        style={{
          backgroundColor: "black",
          width: 10,
          height: 10,
          borderRadius: "50%",
          ...style,
        }}
      ></div>
    );
  }
);
