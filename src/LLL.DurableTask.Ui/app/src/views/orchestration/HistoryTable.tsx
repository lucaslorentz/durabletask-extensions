import {
  Box,
  Link,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow
} from "@mui/material";
import React from "react";
import { Link as RouterLink } from "react-router-dom";
import { useWindowSize } from "react-use";
import { useDynamicRefs } from "../../hooks/useDynamicRefs";
import { HistoryEvent } from "../../models/ApiModels";
import { toLocalISO } from "../../utils/date-utils";
import { LineBuilder } from "./LineBuilder";

type Props = {
  historyEvents: HistoryEvent[];
};

export function HistoryTable(props: Props) {
  const { historyEvents: eventsHistory } = props;

  const filteredEvents = eventsHistory.filter(
    (e) =>
      e.eventType !== "OrchestratorStarted" &&
      e.eventType !== "OrchestratorCompleted"
  );

  const [getRefCurrent, getRefCallback] = useDynamicRefs(true);

  useWindowSize();
  const data = prepareData(filteredEvents, getRefCurrent);

  return (
    <TableContainer>
      <Table>
        <TableHead>
          <TableRow>
            <TableCell>Graph</TableCell>
            <TableCell>EventType</TableCell>
            <TableCell>Timestamp</TableCell>
            <TableCell>Target</TableCell>
            <TableCell>Data</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {data.rows.map((row, index) => {
            return (
              <TableRow
                key={index}
                ref={getRefCallback<HTMLTableRowElement>(index)}
              >
                <TableCell
                  padding="none"
                  style={{
                    position: "relative",
                    width: data.svgWidth,
                    minWidth: data.svgWidth,
                    maxWidth: data.svgWidth,
                  }}
                >
                  {index === 0 && (
                    <svg
                      ref={getRefCallback<SVGSVGElement>("svg")}
                      style={{
                        position: "absolute",
                        top: 0,
                        left: 0,
                        width: data.svgWidth,
                        height: data.svgHeight,
                      }}
                    >
                      {data.dots.map((d, i) => (
                        <circle
                          key={i}
                          cx={d.x}
                          cy={d.y}
                          r={5}
                          style={{ fill: d.color }}
                        />
                      ))}
                      {data.lines.map((line, index) => (
                        <path
                          key={index}
                          d={line.toPath()}
                          style={{
                            fill: "none",
                            stroke: line.color,
                            strokeWidth: 2,
                          }}
                        />
                      ))}
                    </svg>
                  )}
                </TableCell>
                <TableCell>
                  <span
                    style={{
                      textDecoration: row.rewound ? "line-through" : undefined,
                    }}
                  >
                    {row.event.instanceId ? (
                      <Link
                        component={RouterLink}
                        to={`/orchestrations/${encodeURIComponent(row.event.instanceId)}`}
                        underline="hover">
                        {row.event.eventType}
                        {row.id && `: ${row.id}`}
                      </Link>
                    ) : (
                      <>
                        {row.event.eventType}
                        {row.id && `: ${row.id}`}
                      </>
                    )}
                  </span>
                  {row.rewound ? (
                    <div style={{ display: "inline-block" }}>
                      &nbsp;(rewound)
                    </div>
                  ) : null}
                </TableCell>
                <TableCell>{toLocalISO(row.event.timestamp)}</TableCell>
                <TableCell>
                  {row.event.name ? (
                    <>
                      {row.event.name}
                      {row.event.version ? ` (${row.event.version})` : null}
                    </>
                  ) : (
                    row.event.fireAt ?? row.event.orchestrationStatus
                  )}
                </TableCell>
                <TableCell
                  padding="none"
                  style={{ wordBreak: "break-all", width: "30%" }}
                >
                  <Box padding={1} style={{ maxHeight: 100, overflow: "auto" }}>
                    {row.event.input ??
                      row.event.result ??
                      row.event.reason ??
                      row.event.data}
                  </Box>
                </TableCell>
              </TableRow>
            );
          })}
        </TableBody>
      </Table>
    </TableContainer>
  );
}

type Data = {
  lines: LineBuilder[];
  dots: DotData[];
  rows: DataRow[];
  svgWidth: number;
  svgHeight: number;
};

type DotData = {
  x: number;
  y: number;
  color: string;
};

type DataRow = {
  event: HistoryEvent;
  id?: string;
  rewound: boolean;
};

const forkEventTypes = [
  "TaskScheduled",
  "TimerCreated",
  "SubOrchestrationInstanceCreated",
];
const mergeEventTypes = [
  "TaskCompleted",
  "TaskFailed",
  "TimerFired",
  "SubOrchestrationInstanceCompleted",
  "SubOrchestrationInstanceFailed",
];

function prepareData(
  events: any[],
  getRefCurrent: <T>(name: string | number) => T
): Data {
  const svgElement = getRefCurrent<SVGSVGElement>("svg");

  const maxLines = 5;
  const marginLeft = 20;
  const marginRight = 16;
  const marginBottom = 10;
  const linesGap = 20;
  const branchYOffset = 40;

  const linesStack: LineBuilder[] = [];
  const linesById: Record<number, LineBuilder> = {};
  const lines: LineBuilder[] = [];

  const dots: DotData[] = [];
  const rows: DataRow[] = [];
  let lastRowCenterY = 0;

  for (var index = 0; index < events.length; index++) {
    let event = events[index];
    let rewound = false;

    if (event.eventType === "GenericEvent") {
      const rewoundDataJson = /^Rewound: ({.*})$/.exec(event.data)?.[1];
      if (rewoundDataJson) {
        try {
          let rewoundData = JSON.parse(rewoundDataJson);
          event = {
            ...event,
            data: undefined,
            ...rewoundData,
          };
          rewound = true;
        } catch {}
      }
    }

    const rowElement = getRefCurrent<HTMLTableRowElement>(index);
    const rowCenterY =
      rowElement && svgElement
        ? getVerticalCenterRelativeTo(rowElement, svgElement)
        : null;

    const lineId =
      forkEventTypes.includes(event.eventType) ||
      mergeEventTypes.includes(event.eventType)
        ? event.taskScheduledId ?? event.timerId ?? event.eventId
        : -1;

    let line = linesById[lineId];
    if (!line) {
      line = new LineBuilder(getColor(linesStack.map((s) => s.color)));
      linesStack.push(line);
      linesById[lineId] = line;
      lines.push(line);

      if (rowCenterY) {
        if (lineId !== -1) {
          line.moveTo(marginLeft, rowCenterY - branchYOffset);
        } else {
          line.moveTo(marginLeft, rowCenterY);
        }
      }
    }

    const lineIndex = linesStack.indexOf(line);

    if (rowCenterY) {
      for (
        let otherLineIndex = 0;
        otherLineIndex < linesStack.length;
        otherLineIndex++
      ) {
        const otherLine = linesStack[otherLineIndex];
        otherLine.lineTo(
          Math.min(otherLineIndex, maxLines) * linesGap + marginLeft,
          rowCenterY
        );
      }

      dots.push({
        x: line.left,
        y: line.top,
        color: line.color,
      });

      lastRowCenterY = rowCenterY;
    }

    if (mergeEventTypes.includes(event.eventType)) {
      if (rowCenterY) {
        line.lineTo(marginLeft, rowCenterY + branchYOffset);
      }
      linesStack.splice(lineIndex, 1);
      delete linesById[lineId];
    }

    rows.push({
      event: event,
      id: lineId === -1 ? undefined : String(lineId),
      rewound,
    });
  }

  return {
    rows: rows,
    lines: lines,
    dots: dots,
    svgWidth:
      Math.min(lines.length, maxLines) * linesGap + marginLeft + marginRight,
    svgHeight: lastRowCenterY + marginBottom,
  };
}

function getVerticalCenterRelativeTo(
  dotElement: HTMLDivElement,
  svgElement: SVGSVGElement
) {
  const rect = dotElement.getBoundingClientRect();
  const svgRect = svgElement.getBoundingClientRect();
  return rect.top + rect.height / 2 - svgRect.top;
}

function getColor(usedColors: string[]): string {
  for (let color of colors) {
    if (!usedColors.includes(color)) {
      return color;
    }
  }
  return "#000000";
}

export const colors = ["#000000", "#E53935", "#1E88E5", "#4CAF50", "#FF9800"];
