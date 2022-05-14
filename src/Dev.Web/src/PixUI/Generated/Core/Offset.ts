import * as PixUI from '@/PixUI'
import * as System from '@/System'

export class Offset implements System.IEquatable<Offset> {
    public static readonly Zero: Offset = new Offset(0, 0);

    public readonly Dx: number;
    public readonly Dy: number;

    public constructor(dx: number, dy: number) {
        this.Dx = dx;
        this.Dy = dy;
    }

    public static Lerp(a: Nullable<Offset>, b: Nullable<Offset>, t: number): Nullable<Offset> {
        if (b == null) {
            if (a == null)
                return null;
            return new Offset(<number><unknown>(a.Dx * (1.0 - t)), <number><unknown>(a.Dy * (1.0 - t)));
        }

        if (a == null)
            return new Offset(<number><unknown>(b.Dx * t), <number><unknown>(b.Dy * t));

        return new Offset(PixUI.FloatUtils.Lerp(a.Dx, b.Dx, t), PixUI.FloatUtils.Lerp(a.Dy, b.Dy, t));
    }

    public Equals(other: Offset): boolean {
        return this.Dx == other.Dx && this.Dy == other.Dy;
    }

    public static op_Equality(left: Offset, right: Offset): boolean {
        return left.Equals((right).Clone());
    }

    public static op_Inequality(left: Offset, right: Offset): boolean {
        return !left.Equals((right).Clone());
    }

    public Clone(): Offset {
        return new Offset(this.Dx, this.Dy);
    }

    public toString(): string {
        return `{{Dx=${this.Dx}, Dy=${this.Dy}}}`;
    }
}
