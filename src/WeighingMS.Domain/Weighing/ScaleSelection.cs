namespace WeighingMS.Domain.Weighing;

/// <summary>The scale chosen for a component: always automatic in V1 (the operator never picks a scale).</summary>
public sealed record ScaleSelection(ScaleClass ScaleClass, string ScaleId);
