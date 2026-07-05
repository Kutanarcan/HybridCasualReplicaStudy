using System.Collections.Generic;
using UnityEngine;

namespace ReplicaProjects.MagicSort
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private List<MagicSortLevel> _LevelList;

        private MagicSortPresentation _presentation;
        private readonly MagicSortOrchestrator _orchestrator = new();

        private void Awake()
        {
            Create();
            Initialize();
        }

        private void Create()
        {
            var presentationPrefab = MagicSortReplicaAssetDatabase.MagicSortPresentationPrefab;

            _presentation = Instantiate(presentationPrefab);
        }

        private void Initialize()
        {
            _orchestrator.Initialize(_LevelList[0]);
            Build(_orchestrator.GetBoard());
            _presentation.AnyBarViewClicked += OnAnyBarViewClicked;
        }

        private void OnAnyBarViewClicked(int barIndex)
        {
            TapResult result = _orchestrator.HandleTap(barIndex);

            _presentation.HandleTapResponse(result);
        }

        public void Build(in SequentialBarArrayInput board)
        {
            var barDataList = new List<BarVisualData>(board.BarCount);

            for (int b = 0; b < board.BarCount; b++)
            {
                var slots = board.Bar(b);

                var data = new BarVisualData();
                data.colorList = new List<int>(board.barHeight);

                for (int i = 0; i < slots.Length; i++)
                {
                    data.colorList.Add(slots[i]);
                }

                barDataList.Add(data);
            }

            _presentation.Initialize(barDataList);
        }
    }
}
