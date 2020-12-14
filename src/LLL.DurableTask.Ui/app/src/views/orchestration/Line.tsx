export class LineBuilder {
  private commands: string[] = [];
  public left: number = 0;
  public top: number = 0;
  public color: string;

  constructor(color: string) {
    this.color = color;
  }

  lineTo(left: number, top: number) {
    if (this.commands.length === 0) {
      this.commands.push(`M ${left} ${top}`);
      this.left = left;
      this.top = top;
      return;
    }

    var controlPoint1 = {
      left: this.left,
      top: (this.top + top) / 2,
    };

    var midPoint = {
      left: (this.left + left) / 2,
      top: (this.top + top) / 2,
    };

    var controlPoint2 = {
      left: left,
      top: (this.top + top) / 2,
    };

    this.commands.push(
      `Q ${controlPoint1.left} ${controlPoint1.top} ${midPoint.left} ${midPoint.top}`,
      `Q ${controlPoint2.left} ${controlPoint2.top} ${left} ${top}`
    );

    this.left = left;
    this.top = top;
  }

  public toPath(): string {
    return this.commands.join(" ");
  }
}
