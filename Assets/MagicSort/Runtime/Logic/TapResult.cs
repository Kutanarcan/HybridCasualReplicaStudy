using ReplicaProjects.MagicSort;

public enum TapKind
{
    Ignored,        // boş bara ilk tık — hiçbir şey olmadı
    SourceSelected, // ilk seçim yapıldı
    Deselected,     // seçili bara tekrar tık
    Retargeted,     // geçersiz hedef → seçim tıklanan bara taşındı
    Consumed        // geçerli hamle — Pour uygulanmalı
}

public readonly struct TapResult
{
    public readonly TapKind Kind;
    public readonly int NewSource;                   // -1 = seçim yok; Orchestrator state'i buna eşitler
    public readonly int SourceBar;                   // Consumed: kaynak; diğerlerinde ilgili bar
    public readonly int TargetBar;                   // sadece Consumed'da anlamlı
    public readonly PlacementOutcome RejectReason;   // sadece Retargeted'da anlamlı
    public readonly AvailablePlacementResult Pour;   // sadece Consumed'da anlamlı
    
    private TapResult(TapKind kind, int newSource, int sourceBar, int targetBar,
                      PlacementOutcome reason, AvailablePlacementResult pour)
    {
        Kind = kind; NewSource = newSource; SourceBar = sourceBar;
        TargetBar = targetBar; RejectReason = reason; Pour = pour;
    }

    public static TapResult Ignored()
        => new(TapKind.Ignored, -1, -1, -1, default, default);
    public static TapResult SourceSelected(int bar)
        => new(TapKind.SourceSelected, bar, bar, -1, default, default);
    public static TapResult Deselected(int bar)
        => new(TapKind.Deselected, -1, bar, -1, default, default);
    public static TapResult Retargeted(int previousSource, int newSource, PlacementOutcome reason)
        => new(TapKind.Retargeted, newSource, previousSource, -1, reason, default);
    public static TapResult Consumed(int sourceBar, int targetBar, in AvailablePlacementResult pour)
        => new(TapKind.Consumed, -1, sourceBar, targetBar, default, pour);
}