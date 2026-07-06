using System;
using System.Collections.Generic;

namespace ReplicaProjects.MagicSort
{
    public interface IMagicSortTheme
    {
        /// Tema kendi input yöntemiyle (UI raycast, collider, physics) tıklamayı
        /// yakalar ve bar index olarak raporlar. Core, kaynağını bilmez.
        event Action<int> BarTapped;

        void Build(IReadOnlyList<BarVisualData> bars);
        void Teardown();

        void PlayIntro(Action onComplete);

        void ShowSelected(int bar);
        void ShowDeselected(int bar);

        /// Taşıma animasyonu. Komut skalerdir; tema kendi gerçekliğine çevirir.
        void PlayTransport(in TransportData command, Action onComplete);

        /// animated=false: tema geçişi/yeniden kurulumda tween'siz anlık poz.
        void ShowSolved(int bar, bool animated);
    }
}
