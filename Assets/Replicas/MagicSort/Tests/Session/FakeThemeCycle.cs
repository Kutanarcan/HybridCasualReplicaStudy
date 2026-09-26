namespace ReplicaProjects.MagicSort.Tests
{
    /// <summary>Hand-written fake: two themes, tracks switches and camera resets.</summary>
    public sealed class FakeThemeCycle : IThemeCycle
    {
        public readonly FakeSortTheme[] Themes = { new(), new() };
        public int ActiveIndex;
        public int CameraResets;

        public ISortTheme Active => Themes[ActiveIndex];
        public FakeSortTheme ActiveFake => Themes[ActiveIndex];

        public void ActivateNext()
        {
            Themes[ActiveIndex].Teardown();
            ActiveIndex = (ActiveIndex + 1) % Themes.Length;
        }

        public void ResetCamera() => CameraResets++;
    }
}
