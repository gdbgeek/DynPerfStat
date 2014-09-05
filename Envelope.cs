using System;

/// <summary>
/// Represents a two-dimensional point.
/// </summary>
public class Coordinate
{
    public double X;
    public double Y;

    public Coordinate()
    {
    }

    public Coordinate(double x, double y)
    {
        this.X = x;
        this.Y = y;
    }
}

/// <summary>
/// Represents a two-dimensional area.
/// </summary>
[Serializable]
public class Envelope
{
    public double XMin;
    public double YMin;
    public double XMax;
    public double YMax;

    public Envelope()
    {
    }

    public Envelope(double xMin, double yMin, double xMax, double yMax)
    {
        this.XMin = xMin;
        this.YMin = yMin;
        this.XMax = xMax;
        this.YMax = yMax;
    }

    public double Width
    {
        get { return this.XMax - this.XMin; }
    }

    public double Height
    {
        get { return this.YMax - this.YMin; }
    }

    public double CentreX
    {
        get { return (this.XMin + this.XMax) / 2; }
    }

    public double CentreY
    {
        get { return (this.YMin + this.YMax) / 2; }
    }

    public static bool operator ==(Envelope env1, Envelope env2)
    {
        if (object.ReferenceEquals(env1, env2))
            return true;
        if (object.ReferenceEquals(env1, null) || object.ReferenceEquals(env2, null))
            return false;
        return env1.XMin == env2.XMin && env1.YMin == env2.YMin && env1.XMax == env2.XMax && env1.YMax == env2.YMax;
    }

    public static bool operator !=(Envelope env1, Envelope env2)
    {
        return !(env1 == env2);
    }

    public override bool Equals(object obj)
    {
        return this == (Envelope)obj;
    }

    public override int GetHashCode()
    {
        return this.XMin.GetHashCode() ^ this.YMin.GetHashCode() ^ this.XMax.GetHashCode() ^ this.YMax.GetHashCode();
    }

    public static bool FuzzyEquals(Envelope env1, Envelope env2, double tolerance)
    {
        if (object.ReferenceEquals(env1, env2))
            return true;
        if (env1 == null | env2 == null)
            return false;
        return Math.Abs(env1.XMin - env2.XMin) <= tolerance && Math.Abs(env1.YMin - env2.YMin) <= tolerance && Math.Abs(env1.XMax - env2.XMax) <= tolerance && Math.Abs(env1.YMax - env2.YMax) <= tolerance;
    }

    public Envelope Clone()
    {
        return new Envelope(this.XMin, this.YMin, this.XMax, this.YMax);
    }

    public static Envelope Intersect(Envelope env1, Envelope env2)
    {
        Envelope result = new Envelope(Math.Max(env1.XMin, env2.XMin), Math.Max(env1.YMin, env2.YMin), Math.Min(env1.XMax, env2.XMax), Math.Min(env1.YMax, env2.YMax));
        if (result.XMax < result.XMin | result.YMax < result.YMin)
            return null;
        return result;
    }

    public static Envelope Union(Envelope env1, Envelope env2)
    {
        Envelope result = new Envelope(Math.Min(env1.XMin, env2.XMin), Math.Min(env1.YMin, env2.YMin), Math.Max(env1.XMax, env2.XMax), Math.Max(env1.YMax, env2.YMax));
        return result;
    }

    public Envelope AdjustForAspectRatio(int imageWidth, int imageHeight)
    {
        Envelope result = this.Clone();
        double aspectRatio = imageWidth / imageHeight;
        if (this.Width / this.Height < aspectRatio)
        {
            // Adjust the width to fix the aspect ratio. 
            double dx = (this.Height * aspectRatio) - this.Width;
            System.Diagnostics.Debug.Assert(dx >= 0);
            result.XMin -= dx / 2;
            result.XMax += dx / 2;
        }
        else
        {
            // Adjust the height to fix the aspect ratio. 
            double dy = (this.Width / aspectRatio) - this.Height;
            System.Diagnostics.Debug.Assert(dy >= 0);
            result.YMin -= dy / 2;
            result.YMax += dy / 2;
        }
        return result;
    }
}