namespace ReplicaProjects.MagicSort
{
    public enum TapKind
    {
        Ignored,        // boş bara ilk tık — hiçbir şey olmadı
        SourceSelected, // ilk seçim yapıldı
        Deselected,     // seçili bara tekrar tık
        Retargeted,     // geçersiz hedef → seçim tıklanan bara taşındı
        Consumed        // geçerli hamle — Pour uygulanmalı
    }
}
