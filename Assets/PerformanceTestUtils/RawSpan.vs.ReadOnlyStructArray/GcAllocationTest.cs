using System;
using Unity.Profiling;
using UnityEngine;

namespace ReplicaProjects.MagicSort.Tests
{
    /// <summary>
    /// Her frame binlerce kez "Bar Empty mi?" check'i yapar ve seçilen moda göre
    /// GC allocation üretir ya da üretmez.
    ///
    /// KULLANIM:
    /// 1. Boş bir sahnede bir GameObject'e ekle.
    /// 2. Play'e bas, Profiler'ı aç (Window > Analysis > Profiler).
    /// 3. CPU Usage modülünde Hierarchy görünümüne geç.
    /// 4. Arama kutusuna "MagicSort" yaz — "MagicSort.Workload" satırındaki
    ///    GC Alloc kolonunu izle.
    /// 5. Inspector'dan Mode'u değiştirerek farkı gör:
    ///    - StructDtoWithSpan / StructDtoWithArray / RawSpan  -> GC Alloc: 0 B
    ///    - ClassDto / NewArrayEveryCall                      -> GC Alloc: KB'larca
    ///
    /// NOT: Kesin sonuç için Editor yerine Development Build + Autoconnect Profiler
    /// ile cihazda/standalone'da test etmek en doğrusu; ama struct vs class farkı
    /// Editor'de de net görünür. Script'in kendisi Update içinde string, log,
    /// LINQ vs. kullanmaz — ölçümü kirletmez.
    /// </summary>
    public class GcAllocationTest : MonoBehaviour
    {
        public enum TestMode
        {
            StructDtoWithSpan,   // readonly struct + ReadOnlySpan  -> 0 alloc
            StructDtoWithArray,  // senin yazdığın BarCheckInput    -> 0 alloc
            RawSpan,             // DTO'suz, düz Span parametresi    -> 0 alloc
            ClassDto,            // class DTO, her çağrıda new       -> her frame alloc
            NewArrayEveryCall    // her çağrıda new int[]            -> her frame ÇOK alloc
        }

        [Header("Ayarlar")]
        public TestMode mode = TestMode.StructDtoWithArray;

        [Tooltip("Frame başına kaç kez check çalışsın. Yüksek tut ki fark bariz olsun.")]
        public int iterationsPerFrame = 5000;

        public int barCount = 8;
        public int barHeight = 4;

        // Profiler'da "MagicSort.Workload" olarak görünür; GC Alloc kolonunu bundan oku.
        private static readonly ProfilerMarker s_workloadMarker = new("MagicSort.Workload");

        // State Orchestrator'da yaşar prensibi: dizi BİR KERE allocate edilir.
        private int[] _slots;

        // Sonucu bir yerde kullanmazsak compiler/IL2CPP işlemi eleyebilir,
        // o yüzden volatile bir alana yazıyoruz.
        private int _sink;

        private void Awake()
        {
            _slots = new int[barCount * barHeight];

            // Yarısı dolu yarısı boş bir board hazırla ki iki dal da (dolu/boş) çalışsın.
            for (int bar = 0; bar < barCount; bar++)
            {
                bool empty = (bar % 2 == 0);
                for (int s = 0; s < barHeight; s++)
                    _slots[bar * barHeight + s] = empty ? -1 : bar % 4;
            }
        }

        private void Update()
        {
            using (s_workloadMarker.Auto())
            {
                int emptyCount = 0;

                switch (mode)
                {
                    case TestMode.StructDtoWithSpan:
                        for (int i = 0; i < iterationsPerFrame; i++)
                        {
                            var input = new BarCheckSpanInput(_slots, i % barCount, barHeight);
                            if (IsBarEmpty(in input)) emptyCount++;
                        }
                        break;

                    case TestMode.StructDtoWithArray:
                        for (int i = 0; i < iterationsPerFrame; i++)
                        {
                            var input = new BarCheckInput(_slots, i % barCount, barHeight);
                            if (IsBarEmpty(in input)) emptyCount++;
                        }
                        break;

                    case TestMode.RawSpan:
                        for (int i = 0; i < iterationsPerFrame; i++)
                        {
                            if (IsBarEmpty(_slots, i % barCount, barHeight)) emptyCount++;
                        }
                        break;

                    case TestMode.ClassDto:
                        for (int i = 0; i < iterationsPerFrame; i++)
                        {
                            // Her çağrıda heap'te bir class instance -> GC Alloc
                            var input = new BarCheckInputClass(_slots, i % barCount, barHeight);
                            if (IsBarEmpty(input)) emptyCount++;
                        }
                        break;

                    case TestMode.NewArrayEveryCall:
                        for (int i = 0; i < iterationsPerFrame; i++)
                        {
                            // "Logic'e sadece ilgili bar'ı kopyalayıp vereyim" tuzağı:
                            // her çağrıda yeni dizi -> en kötü senaryo
                            int barIndex = i % barCount;
                            int[] copy = new int[barHeight];
                            Array.Copy(_slots, barIndex * barHeight, copy, 0, barHeight);
                            if (IsBarEmpty(copy, 0, barHeight)) emptyCount++;
                        }
                        break;
                }

                _sink = emptyCount;
            }
        }

        // ---------------------------------------------------------------
        // LOGIC TARAFI — hepsi static ve pure, Unit test'e birebir taşınabilir
        // ---------------------------------------------------------------

        private static bool IsBarEmpty(ReadOnlySpan<int> slots, int bar, int barHeight)
        {
            ReadOnlySpan<int> barSlots = slots.Slice(bar * barHeight, barHeight);
            for (int i = 0; i < barSlots.Length; i++)
                if (barSlots[i] != -1) return false;
            return true;
        }

        private static bool IsBarEmpty(in BarCheckSpanInput input)
            => IsBarEmpty(input.Slots, input.BarIndex, input.BarHeight);

        private static bool IsBarEmpty(in BarCheckInput input)
            => IsBarEmpty(input.Slots, input.BarIndex, input.BarHeight);

        private static bool IsBarEmpty(BarCheckInputClass input)
            => IsBarEmpty(input.Slots, input.BarIndex, input.BarHeight);

        // ---------------------------------------------------------------
        // DTO VARYANTLARI
        // ---------------------------------------------------------------

        /// <summary>Senin yazdığın versiyon: readonly struct + int[] referansı. 0 alloc.</summary>
        public readonly struct BarCheckInput
        {
            public readonly int BarIndex;
            public readonly int BarHeight;
            public readonly int[] Slots; // referans kopyalanır, dizi kopyalanmaz

            public BarCheckInput(int[] slots, int barIndex, int barHeight)
            {
                Slots = slots; BarIndex = barIndex; BarHeight = barHeight;
            }
        }

        /// <summary>Span taşıyan versiyon: ref struct olmak ZORUNDA. 0 alloc.
        /// Kısıt: field'da saklanamaz, coroutine/async'te tutulamaz.</summary>
        public readonly ref struct BarCheckSpanInput
        {
            public readonly int BarIndex;
            public readonly int BarHeight;
            public readonly ReadOnlySpan<int> Slots;

            public BarCheckSpanInput(ReadOnlySpan<int> slots, int barIndex, int barHeight)
            {
                Slots = slots; BarIndex = barIndex; BarHeight = barHeight;
            }
        }

        /// <summary>Kaçınmak istediğin versiyon: class DTO. Her new -> heap -> GC.</summary>
        public sealed class BarCheckInputClass
        {
            public readonly int BarIndex;
            public readonly int BarHeight;
            public readonly int[] Slots;

            public BarCheckInputClass(int[] slots, int barIndex, int barHeight)
            {
                Slots = slots; BarIndex = barIndex; BarHeight = barHeight;
            }
        }
    }
}
