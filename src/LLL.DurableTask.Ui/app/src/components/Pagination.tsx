import ChevronLeftIcon from "@mui/icons-material/ChevronLeft";
import ChevronRightIcon from "@mui/icons-material/ChevronRight";
import FirstPageIcon from "@mui/icons-material/FirstPage";
import {
  Box,
  IconButton,
  MenuItem,
  Select,
  Stack,
  Typography,
} from "@mui/material";
import React, { Dispatch } from "react";

interface Props {
  count: number;
  pageSize: number;
  setPageSize: Dispatch<number>;
  continuationTokenStack: string[];
  nextContinuationToken?: string;
  onFirst: () => void;
  onPrevious: () => void;
  onNext: () => void;
}

export function Pagination(props: Props) {
  const {
    count,
    pageSize,
    setPageSize,
    continuationTokenStack,
    nextContinuationToken,
    onFirst,
    onPrevious,
    onNext,
  } = props;

  return (
    <Stack
      marginX={2}
      direction="row"
      spacing={{ xs: 1, sm: 4 }}
      alignItems="center"
      justifyContent={{ xs: "space-between", sm: "center" }}
    >
      <Stack direction="row" alignItems="center">
        <Typography
          variant="body2"
          sx={{
            display: {
              xs: "none",
              sm: "block",
            },
            mr: 1,
          }}
        >
          Rows:
        </Typography>
        <Select
          value={pageSize}
          onChange={(e) => setPageSize(e.target.value as number)}
          SelectDisplayProps={{ style: { fontSize: 13 } }}
          autoWidth
          variant="standard"
          disableUnderline
        >
          <MenuItem value={5}>5</MenuItem>
          <MenuItem value={10}>10</MenuItem>
          <MenuItem value={25}>25</MenuItem>
          <MenuItem value={50}>50</MenuItem>
          <MenuItem value={100}>100</MenuItem>
        </Select>
      </Stack>
      <Typography variant="body2">
        {continuationTokenStack.length * pageSize + 1}–
        {continuationTokenStack.length * pageSize + count}
      </Typography>
      <Box>
        <IconButton
          disabled={continuationTokenStack.length === 0}
          onClick={onFirst}
          size="large"
        >
          <FirstPageIcon />
        </IconButton>
        <IconButton
          disabled={continuationTokenStack.length === 0}
          onClick={onPrevious}
          size="large"
        >
          <ChevronLeftIcon />
        </IconButton>
        <IconButton
          disabled={!nextContinuationToken}
          onClick={onNext}
          size="large"
        >
          <ChevronRightIcon />
        </IconButton>
      </Box>
    </Stack>
  );
}
