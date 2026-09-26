using UnityEngine;

namespace ReplicaProjects.MagicSort
{
    /// <summary>The bolt bar's one-shot sounds, played through its own AudioSource.</summary>
    public sealed class BoltSounds
    {
        private readonly AudioSource _source;
        private readonly AudioClip _up;
        private readonly AudioClip _down;
        private readonly AudioClip _seat;
        private readonly AudioClip _capWin;

        public BoltSounds(AudioSource source, AudioClip up, AudioClip down, AudioClip seat, AudioClip capWin)
        {
            _source = source;
            _up = up;
            _down = down;
            _seat = seat;
            _capWin = capWin;
        }

        public void PlayUp() => _source.PlayOneShot(_up);
        public void PlayDown() => _source.PlayOneShot(_down);
        public void PlaySeat() => _source.PlayOneShot(_seat);
        public void PlayCapWin() => _source.PlayOneShot(_capWin);
    }
}
