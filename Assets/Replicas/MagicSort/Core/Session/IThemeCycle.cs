namespace ReplicaProjects.MagicSort
{
    /// <summary>The set of visual themes the player can cycle through. Implemented by the Unity shell.</summary>
    public interface IThemeCycle
    {
        ISortTheme Active { get; }

        /// <summary>Tears down the active theme and activates the next one.</summary>
        void ActivateNext();

        /// <summary>Restores the camera pose a theme expects before it builds (themes fit the camera relatively).</summary>
        void ResetCamera();
    }
}
