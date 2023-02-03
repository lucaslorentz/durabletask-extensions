import { ArrowDropDown, Sync } from "@mui/icons-material";
import { Button, ButtonGroup, Menu, MenuItem } from "@mui/material";
import React, { Dispatch, useState } from "react";

const autoRefreshOptions = [
  { value: undefined, label: "Off" },
  { value: 5, label: "5 seconds" },
  { value: 10, label: "10 seconds" },
  { value: 20, label: "20 seconds" },
  { value: 30, label: "30 seconds" },
];

interface Props {
  refreshInterval: number | undefined;
  setRefreshInterval: Dispatch<number | undefined>;
  onClick: () => void;
}

export function AutoRefreshButton(props: Props) {
  const { refreshInterval, setRefreshInterval, onClick } = props;

  const [refreshAnchor, setRefreshAnchor] = useState<HTMLElement | undefined>();

  return (
    <ButtonGroup color="primary" size="small">
      <Button onClick={onClick} title="Refresh">
        <Sync />
      </Button>
      <Button onClick={(e) => setRefreshAnchor(e.currentTarget)}>
        {refreshInterval ? `${refreshInterval} seconds` : "Off"}
        <ArrowDropDown />
      </Button>
      <Menu
        anchorEl={refreshAnchor}
        keepMounted
        anchorOrigin={{
          vertical: "bottom",
          horizontal: "right",
        }}
        transformOrigin={{
          vertical: "top",
          horizontal: "right",
        }}
        open={Boolean(refreshAnchor)}
        onClose={() => setRefreshAnchor(undefined)}
      >
        {autoRefreshOptions.map((option, index) => (
          <MenuItem
            key={index}
            selected={refreshInterval === option.value}
            onClick={() => {
              setRefreshInterval(option.value);
              setRefreshAnchor(undefined);
            }}
          >
            {option.label}
          </MenuItem>
        ))}
      </Menu>
    </ButtonGroup>
  );
}
