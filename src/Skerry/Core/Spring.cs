namespace Skerry.Core;

/// <summary>Damped spring for the island's shape animation (hand-written, driven from CompositionTarget.Rendering).</summary>
public struct Spring
{
    public double Value, Velocity, Target;

    public Spring(double value) { Value = Target = value; Velocity = 0; }

    public readonly bool IsSettled(double eps = 0.05) =>
        Math.Abs(Target - Value) < eps && Math.Abs(Velocity) < eps * 20;

    public void Snap() { Value = Target; Velocity = 0; }

    /// <summary>Advances by <paramref name="dt"/> seconds (sub-stepped for stability). Returns true while still moving.</summary>
    public bool Step(double dt, double stiffness, double damping)
    {
        const double h = 1.0 / 240;
        dt = Math.Min(dt, 0.1);
        while (dt > 1e-9)
        {
            var step = Math.Min(h, dt);
            var accel = stiffness * (Target - Value) - damping * Velocity;
            Velocity += accel * step;      // semi-implicit Euler
            Value += Velocity * step;
            dt -= step;
        }
        if (IsSettled()) { Snap(); return false; }
        return true;
    }
}
