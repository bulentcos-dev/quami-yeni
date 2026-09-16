namespace Quami.Web.Components.Shared;

/// <summary>Açılır listedeki tek bir seçenek.</summary>
public sealed record QuamiOption<TValue>(TValue Value, string Text);
